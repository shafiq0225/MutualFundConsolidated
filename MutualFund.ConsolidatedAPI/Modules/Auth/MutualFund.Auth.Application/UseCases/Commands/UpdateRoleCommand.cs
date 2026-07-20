using MutualFund.Auth.Application.DTOs.User;
using MutualFund.Auth.Domain.Enums;
using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class UpdateRoleCommand
    {
        private readonly IUserService _userService;
        private readonly ILogger<UpdateRoleCommand> _logger;

        public UpdateRoleCommand(
            IUserService userService,
            ILogger<UpdateRoleCommand> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<UserDto> ExecuteAsync(
            string userId, UserRole newRole, string adminId)
        {
            var user = await _userService.UpdateRoleAsync(
                userId, newRole, adminId);

            _logger.LogInformation(
                "User role updated — UserId={UserId} NewRole={Role} By={AdminId}",
                userId, newRole, adminId);

            return MapToDto(user);
        }

        private static UserDto MapToDto(
            Domain.Entities.ApplicationUser u) => new()
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                PanNumber = u.PanNumber,
                Role = u.Role,
                UserType = u.UserType,
                ApprovalStatus = u.ApprovalStatus,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                ApprovedAt = u.ApprovedAt,
                LastLoginAt = u.LastLoginAt,
                RejectionReason = u.RejectionReason
            };
    }
}