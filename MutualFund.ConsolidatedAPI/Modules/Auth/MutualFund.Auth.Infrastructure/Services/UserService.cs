using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;
using MutualFund.Auth.Domain.Exceptions;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Data;

namespace MutualFund.Auth.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync() =>
            await _context.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

        public async Task<ApplicationUser> GetUserByIdAsync(string userId) =>
            await _context.Users.FindAsync(userId)
                ?? throw new UserNotFoundException(userId);

        public async Task<IEnumerable<ApplicationUser>> GetPendingUsersAsync() =>
            await _context.Users
                .Where(u => u.ApprovalStatus == ApprovalStatus.Pending)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

        public async Task<ApplicationUser> ApproveUserAsync(
            string userId, string adminId)
        {
            var user = await GetUserByIdAsync(userId);

            user.ApprovalStatus = ApprovalStatus.Approved;
            user.IsActive = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedByUserId = adminId;
            user.RejectionReason = null;

            await _userManager.UpdateAsync(user);
            return user;
        }

        public async Task<ApplicationUser> RejectUserAsync(
            string userId, string adminId, string? reason = null)
        {
            var user = await GetUserByIdAsync(userId);

            user.ApprovalStatus = ApprovalStatus.Rejected;
            user.IsActive = false;
            user.RejectionReason = reason;
            user.ApprovedByUserId = adminId;

            await _userManager.UpdateAsync(user);
            return user;
        }

        public async Task<ApplicationUser> UpdateRoleAsync(
            string userId, UserRole newRole, string adminId)
        {
            var user = await GetUserByIdAsync(userId);

            // Cannot change Admin's role
            if (user.Role == UserRole.Admin)
                throw new UnauthorizedActionException(
                    "change the role of an Admin account");

            user.Role = newRole;

            // Reset UserType if changing away from User role
            if (newRole != UserRole.User)
                user.UserType = UserType.None;

            await _userManager.UpdateAsync(user);
            return user;
        }

        public async Task DeactivateUserAsync(string userId, string adminId)
        {
            var user = await GetUserByIdAsync(userId);

            if (user.Role == UserRole.Admin)
                throw new UnauthorizedActionException(
                    "deactivate an Admin account");

            user.IsActive = false;

            // Revoke all active refresh tokens
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId
                         && r.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
                token.RevokedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();
        }
    }
}