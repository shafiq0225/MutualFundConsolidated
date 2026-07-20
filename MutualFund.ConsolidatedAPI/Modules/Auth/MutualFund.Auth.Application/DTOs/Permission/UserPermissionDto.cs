namespace MutualFund.Auth.Application.DTOs.Permission
{
    public class UserPermissionDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = new();
    }
}