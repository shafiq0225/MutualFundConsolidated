using MutualFund.Auth.Application.DTOs.Permission;
using MutualFund.Auth.Domain.Interfaces;

namespace MutualFund.Auth.Application.UseCases.Queries
{
    public class GetPermissionsQuery
    {
        private readonly IPermissionService _permissionService;

        public GetPermissionsQuery(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task<IEnumerable<PermissionDto>> GetAllAsync()
        {
            var perms = await _permissionService.GetAllPermissionsAsync();
            return perms.Select(MapToDto);
        }

        public async Task<UserPermissionDto> GetUserPermissionsAsync(
            string userId, string userFullName, string userEmail)
        {
            var perms = await _permissionService
                .GetUserPermissionsAsync(userId);

            return new UserPermissionDto
            {
                UserId = userId,
                UserFullName = userFullName,
                UserEmail = userEmail,
                Permissions = perms.Select(MapToDto).ToList()
            };
        }

        private static PermissionDto MapToDto(
            Domain.Entities.Permission p) => new()
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description
            };
    }
}