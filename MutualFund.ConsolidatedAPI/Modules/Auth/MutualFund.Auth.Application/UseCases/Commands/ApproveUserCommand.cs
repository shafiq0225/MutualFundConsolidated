using MutualFund.Auth.Application.DTOs.User;
using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class ApproveUserCommand
    {
        private readonly IUserService _userService;
        private readonly ILogger<ApproveUserCommand> _logger;

        public ApproveUserCommand(
            IUserService userService,
            ILogger<ApproveUserCommand> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<UserDto> ExecuteAsync(
            string userId, string adminId)
        {
            var user = await _userService.ApproveUserAsync(
                userId, adminId);

            _logger.LogInformation(
                "User approved — UserId={UserId} By={AdminId}",
                userId, adminId);

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