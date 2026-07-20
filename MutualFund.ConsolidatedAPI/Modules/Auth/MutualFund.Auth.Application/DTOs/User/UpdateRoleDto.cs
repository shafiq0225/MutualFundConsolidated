using MutualFund.Auth.Domain.Enums;

namespace MutualFund.Auth.Application.DTOs.User
{
    public class UpdateRoleDto
    {
        public UserRole NewRole { get; set; }
    }
}