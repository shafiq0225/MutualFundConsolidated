using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MutualFund.Auth.Application.DTOs.User;
using MutualFund.Auth.Application.UseCases.Commands;
using MutualFund.Auth.Application.UseCases.Queries;
using MutualFund.Auth.Domain.Enums;
using System.Security.Claims;

namespace MutualFund.Auth.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly GetUsersQuery _query;
        private readonly ApproveUserCommand _approveCommand;
        private readonly RejectUserCommand _rejectCommand;
        private readonly UpdateRoleCommand _updateRoleCommand;

        public UserController(
            GetUsersQuery query,
            ApproveUserCommand approveCommand,
            RejectUserCommand rejectCommand,
            UpdateRoleCommand updateRoleCommand)
        {
            _query = query;
            _approveCommand = approveCommand;
            _rejectCommand = rejectCommand;
            _updateRoleCommand = updateRoleCommand;
        }

        /// <summary>
        /// Get all users. Admin, or Employee granted user.manage.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _query.GetAllAsync();
            return Ok(users);
        }

        /// <summary>
        /// Get user by ID. Admin, or Employee granted user.manage.
        /// </summary>
        [HttpGet("{userId}")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<IActionResult> GetById(string userId)
        {
            var user = await _query.GetByIdAsync(userId);
            return Ok(user);
        }

        /// <summary>
        /// Get all pending approval requests. Admin, or Employee granted
        /// user.manage.
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<IActionResult> GetPending()
        {
            var users = await _query.GetPendingAsync();
            return Ok(users);
        }

        /// <summary>
        /// Approve a pending user registration. Admin, or Employee
        /// granted user.manage.
        /// </summary>
        [HttpPut("{userId}/approve")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<IActionResult> Approve(string userId)
        {
            var adminId = GetCurrentUserId();
            var result = await _approveCommand.ExecuteAsync(userId, adminId);
            return Ok(result);
        }

        /// <summary>
        /// Reject a pending user registration. Admin, or Employee
        /// granted user.manage.
        /// </summary>
        [HttpPut("{userId}/reject")]
        [Authorize(Policy = "CanManageUsers")]
        public async Task<IActionResult> Reject(
            string userId, [FromBody] RejectUserDto dto)
        {
            var adminId = GetCurrentUserId();
            var result = await _rejectCommand.ExecuteAsync(
                userId, adminId, dto.Reason);
            return Ok(result);
        }

        /// <summary>
        /// Update a user's role. Admin only — deliberately NOT delegable
        /// to Employees, since granting this would let an Employee
        /// escalate themselves (or anyone) to Admin.
        /// </summary>
        [HttpPut("{userId}/role")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateRole(
            string userId, [FromBody] UpdateRoleDto dto)
        {
            var adminId = GetCurrentUserId();
            var result = await _updateRoleCommand.ExecuteAsync(
                userId, dto.NewRole, adminId);
            return Ok(result);
        }

        /// <summary>
        /// Get current authenticated user's profile.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _query.GetByIdAsync(userId);
            return Ok(user);
        }

        private string GetCurrentUserId() =>
            User.FindFirstValue("sub") ?? string.Empty;

    }
}