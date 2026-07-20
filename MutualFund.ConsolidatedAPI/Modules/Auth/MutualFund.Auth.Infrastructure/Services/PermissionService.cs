using Microsoft.EntityFrameworkCore;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Exceptions;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Data;

namespace MutualFund.Auth.Infrastructure.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync() =>
            await _context.Permissions
                .OrderBy(p => p.Code)
                .ToListAsync();

        public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(
            string userId) =>
            await _context.UserPermissions
                .Where(up => up.UserId == userId
                          && up.RevokedAt == null)
                .Include(up => up.Permission)
                .Select(up => up.Permission)
                .OrderBy(p => p.Code)
                .ToListAsync();

        public async Task AssignPermissionAsync(
            string userId,
            string permissionCode,
            string adminId)
        {
            // Verify user exists
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new UserNotFoundException(userId);

            // Verify permission code exists
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode)
                ?? throw new PermissionNotFoundException(permissionCode);

            // Check not already assigned
            var alreadyAssigned = await _context.UserPermissions
                .AnyAsync(up => up.UserId == userId
                             && up.PermissionId == permission.Id
                             && up.RevokedAt == null);

            if (alreadyAssigned)
                throw new PermissionAlreadyAssignedException(
                    permissionCode, userId);

            _context.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permission.Id,
                GrantedByUserId = adminId,
                GrantedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RevokePermissionAsync(
            string userId,
            string permissionCode,
            string adminId)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Code == permissionCode)
                ?? throw new PermissionNotFoundException(permissionCode);

            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up =>
                    up.UserId == userId &&
                    up.PermissionId == permission.Id &&
                    up.RevokedAt == null)
                ?? throw new PermissionNotAssignedException(
                    permissionCode, userId);

            userPermission.RevokedAt = DateTime.UtcNow;
            userPermission.RevokedByUserId = adminId;

            await _context.SaveChangesAsync();
        }
    }
}