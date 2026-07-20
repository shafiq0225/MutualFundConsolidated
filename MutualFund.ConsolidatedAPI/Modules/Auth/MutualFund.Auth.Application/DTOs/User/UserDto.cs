using MutualFund.Auth.Domain.Enums;

namespace MutualFund.Auth.Application.DTOs.User
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public UserType UserType { get; set; }
        public string UserTypeName => UserType.ToString();
        public ApprovalStatus ApprovalStatus { get; set; }
        public string StatusName => ApprovalStatus.ToString();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}