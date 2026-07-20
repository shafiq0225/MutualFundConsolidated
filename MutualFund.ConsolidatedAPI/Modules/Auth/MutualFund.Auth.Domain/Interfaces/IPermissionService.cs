using MutualFund.Auth.Domain.Entities;

namespace MutualFund.Auth.Domain.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);

        /// <summary>
        /// Admin assigns a permission to a user.
        /// </summary>
        Task AssignPermissionAsync(
            string userId,
            string permissionCode,
            string adminId);

        /// <summary>
        /// Admin revokes a previously assigned permission.
        /// </summary>
        Task RevokePermissionAsync(
            string userId,
            string permissionCode,
            string adminId);
    }
}