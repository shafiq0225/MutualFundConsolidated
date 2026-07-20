using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;

namespace MutualFund.Auth.Domain.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser> GetUserByIdAsync(string userId);
        Task<IEnumerable<ApplicationUser>> GetPendingUsersAsync();

        /// <summary>
        /// Admin approves a pending registration.
        /// Sets IsActive = true, ApprovalStatus = Approved.
        /// </summary>
        Task<ApplicationUser> ApproveUserAsync(
            string userId, string adminId);

        /// <summary>
        /// Admin rejects a pending registration with a reason.
        /// </summary>
        Task<ApplicationUser> RejectUserAsync(
            string userId, string adminId, string? reason = null);

        /// <summary>
        /// Admin changes a user's role.
        /// </summary>
        Task<ApplicationUser> UpdateRoleAsync(
            string userId, UserRole newRole, string adminId);

        /// <summary>
        /// Admin deactivates a user — they can no longer log in.
        /// </summary>
        Task DeactivateUserAsync(string userId, string adminId);
    }
}