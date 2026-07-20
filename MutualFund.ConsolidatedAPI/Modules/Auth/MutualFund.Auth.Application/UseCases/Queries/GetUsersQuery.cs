using MutualFund.Auth.Application.DTOs.User;
using MutualFund.Auth.Domain.Interfaces;

namespace MutualFund.Auth.Application.UseCases.Queries
{
    public class GetUsersQuery
    {
        private readonly IUserService _userService;

        public GetUsersQuery(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            var users = await _userService.GetAllUsersAsync();
            return users.Select(MapToDto);
        }

        public async Task<UserDto> GetByIdAsync(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return MapToDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetPendingAsync()
        {
            var users = await _userService.GetPendingUsersAsync();
            return users.Select(MapToDto);
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