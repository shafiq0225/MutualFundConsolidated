using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;
using MutualFund.Auth.Domain.Exceptions;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Data;

namespace MutualFund.Auth.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<ApplicationUser> RegisterAsync(
            string firstName,
            string lastName,
            string email,
            string password,
            string panNumber)
        {
            // Validate PAN format: 5 letters + 4 digits + 1 letter
            if (!IsValidPan(panNumber))
                throw new InvalidPanFormatException(panNumber);

            var normalizedPan = panNumber.Trim().ToUpper();
            var normalizedEmail = email.Trim().ToLower();

            // ── Handle rejected user re-registration ─────────────────────────────
            // If a rejected user tries to re-register with the same PAN or email,
            // remove the old rejected record so they can start fresh.
            var rejectedByPan = await _context.Users
                .FirstOrDefaultAsync(u => u.PanNumber == normalizedPan
                                       && u.ApprovalStatus == ApprovalStatus.Rejected);
            if (rejectedByPan != null)
            {
                // Remove related data first to avoid FK violations
                var panTokens = _context.RefreshTokens.Where(r => r.UserId == rejectedByPan.Id);
                _context.RefreshTokens.RemoveRange(panTokens);
                await _userManager.DeleteAsync(rejectedByPan);
            }

            var rejectedByEmail = await _userManager.FindByEmailAsync(normalizedEmail);
            if (rejectedByEmail != null && rejectedByEmail.ApprovalStatus == ApprovalStatus.Rejected)
            {
                var emailTokens = _context.RefreshTokens.Where(r => r.UserId == rejectedByEmail.Id);
                _context.RefreshTokens.RemoveRange(emailTokens);
                await _userManager.DeleteAsync(rejectedByEmail);
                rejectedByEmail = null; // cleared — re-check below won't block
            }

            // Check duplicate email (only if NOT a cleared rejected user)
            if (rejectedByEmail != null || await _userManager.FindByEmailAsync(normalizedEmail) != null)
                throw new EmailAlreadyExistsException(normalizedEmail);

            // Check duplicate PAN (only if NOT a cleared rejected user)
            var panExists = await _context.Users
                .AnyAsync(u => u.PanNumber == normalizedPan);
            if (panExists)
                throw new PanAlreadyExistsException(panNumber);

            // ── UserId = PAN ──────────────────────────────────────────
            // Per design decision: PAN becomes the literal Identity Id,
            // not just a separate field. Since the DB is fresh (no
            // legacy users to migrate), and ApplicationUser.Id is a
            // plain client-assignable string (IdentityUser<string>, no
            // DB-generated value), setting it explicitly here is safe.
            // This means the JWT "sub" claim (which is user.Id) and
            // every FK referencing UserId now naturally carry PAN.
            var user = new ApplicationUser
            {
                Id = normalizedPan,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = normalizedEmail,
                UserName = normalizedEmail,
                PanNumber = normalizedPan,
                Role = UserRole.User,
                UserType = UserType.None,
                ApprovalStatus = ApprovalStatus.Pending,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new IdentityOperationException(
                    result.Errors.Select(e => e.Description));

            return user;
        }

        public async Task<(string AccessToken, string RefreshToken)> LoginAsync(
            string email,
            string password,
            string? ipAddress = null)
        {
            var user = await _userManager.FindByEmailAsync(email)
                ?? throw new InvalidCredentialsException();

            // Check approval status before password validation
            // — avoids leaking whether email exists
            switch (user.ApprovalStatus)
            {
                case ApprovalStatus.Pending:
                    throw new AccountPendingApprovalException();
                case ApprovalStatus.Rejected:
                    throw new AccountRejectedException(user.RejectionReason);
            }

            if (!user.IsActive)
                throw new AccountDisabledException();

            var validPassword = await _userManager
                .CheckPasswordAsync(user, password);
            if (!validPassword)
                throw new InvalidCredentialsException();

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return await GenerateTokensAsync(user, ipAddress);
        }

        public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(
            string refreshToken,
            string? ipAddress = null)
        {
            var stored = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken)
                ?? throw new InvalidRefreshTokenException();

            if (!stored.IsActive)
                throw new InvalidRefreshTokenException();

            // Rotate — revoke old, create new
            stored.RevokedAt = DateTime.UtcNow;
            stored.RevokedByIp = ipAddress;

            var (accessToken, newRefreshToken) =
                await GenerateTokensAsync(stored.User, ipAddress);

            stored.ReplacedByToken = newRefreshToken;
            await _context.SaveChangesAsync();

            return (accessToken, newRefreshToken);
        }

        public async Task LogoutAsync(string userId, string refreshToken)
        {
            var stored = await _context.RefreshTokens
                .FirstOrDefaultAsync(r =>
                    r.Token == refreshToken &&
                    r.UserId == userId);

            if (stored == null || !stored.IsActive) return;

            stored.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UserNotFoundException(userId);

            var result = await _userManager.ChangePasswordAsync(
                user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                if (errors.Any(e => e.Contains("Incorrect")))
                    throw new IncorrectCurrentPasswordException();

                throw new IdentityOperationException(errors);
            }
        }

        // ── Private Helpers ──────────────────────────────────────────

        private async Task<(string AccessToken, string RefreshToken)>
            GenerateTokensAsync(
                ApplicationUser user,
                string? ipAddress = null)
        {
            // Get active permissions for this user
            var permissions = await _context.UserPermissions
                .Where(up => up.UserId == user.Id
                          && up.RevokedAt == null)
                .Include(up => up.Permission)
                .Select(up => up.Permission.Code)
                .ToListAsync();

            // Admin always gets all permissions
            if (user.Role == UserRole.Admin)
                permissions = Domain.Enums.PermissionType
                    .GetAll().ToList();

            var accessToken = _tokenService
                .GenerateAccessToken(user, permissions);
            var refreshToken = _tokenService
                .GenerateRefreshToken(user.Id, ipAddress);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return (accessToken, refreshToken.Token);
        }

        private static bool IsValidPan(string pan)
        {
            if (string.IsNullOrWhiteSpace(pan)) return false;
            return Regex.IsMatch(pan.Trim().ToUpper(),
                @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$");
        }
    }
}