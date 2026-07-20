using MutualFund.Auth.Application.DTOs.User;
using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class RejectUserCommand
    {
        private readonly IUserService _userService;
        private readonly ILogger<RejectUserCommand> _logger;

        public RejectUserCommand(
            IUserService userService,
            ILogger<RejectUserCommand> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<UserDto> ExecuteAsync(
            string userId, string adminId, string? reason)
        {
            var user = await _userService.RejectUserAsync(
                userId, adminId, reason);

            _logger.LogInformation(
                "User rejected — UserId={UserId} By={AdminId} Reason={Reason}",
                userId, adminId, reason);

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