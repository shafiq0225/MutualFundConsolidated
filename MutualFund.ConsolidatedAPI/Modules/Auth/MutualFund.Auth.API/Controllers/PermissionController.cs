using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MutualFund.Auth.Application.DTOs.Permission;
using MutualFund.Auth.Application.UseCases.Commands;
using MutualFund.Auth.Application.UseCases.Queries;
using System.Security.Claims;

namespace MutualFund.Auth.API.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize(Policy = "AdminOnly")]
    public class PermissionController : ControllerBase
    {
        private readonly GetPermissionsQuery _query;
        private readonly AssignPermissionCommand _assignCommand;
        private readonly RevokePermissionCommand _revokeCommand;
        private readonly GetUsersQuery _usersQuery;

        public PermissionController(
            GetPermissionsQuery query,
            AssignPermissionCommand assignCommand,
            RevokePermissionCommand revokeCommand,
            GetUsersQuery usersQuery)
        {
            _query = query;
            _assignCommand = assignCommand;
            _revokeCommand = revokeCommand;
            _usersQuery = usersQuery;
        }

        /// <summary>
        /// Get all available permissions in the system.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _query.GetAllAsync();
            return Ok(permissions);
        }

        /// <summary>
        /// Get all permissions assigned to a specific user.
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPermissions(string userId)
        {
            var user = await _usersQuery.GetByIdAsync(userId);
            var result = await _query.GetUserPermissionsAsync(
                userId, user.FullName, user.Email);
            return Ok(result);
        }

        /// <summary>
        /// Assign a permission to a user. Admin only.
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> Assign(
            [FromBody] AssignPermissionDto dto)
        {
            var adminId = GetCurrentUserId();
            await _assignCommand.ExecuteAsync(
                dto.UserId, dto.PermissionCode, adminId);

            return Ok(new
            {
                message = $"Permission '{dto.PermissionCode}' " +
                                 $"assigned to user '{dto.UserId}' successfully.",
                userId = dto.UserId,
                permissionCode = dto.PermissionCode
            });
        }

        /// <summary>
        /// Revoke a permission from a user. Admin only.
        /// </summary>
        [HttpDelete("revoke")]
        public async Task<IActionResult> Revoke(
            [FromBody] AssignPermissionDto dto)
        {
            var adminId = GetCurrentUserId();
            await _revokeCommand.ExecuteAsync(
                dto.UserId, dto.PermissionCode, adminId);

            return Ok(new
            {
                message = $"Permission '{dto.PermissionCode}' " +
                                 $"revoked from user '{dto.UserId}' successfully.",
                userId = dto.UserId,
                permissionCode = dto.PermissionCode
            });
        }

        private string GetCurrentUserId() =>
            User.FindFirstValue("sub") ?? string.Empty;

    }
}