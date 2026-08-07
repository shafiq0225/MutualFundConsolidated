using Microsoft.AspNetCore.Authorization;

namespace MutualFund.Auth.Infrastructure.Authorization
{
    /// <summary>
    /// Custom Authorization Requirement that specifies the permission code required to access a resource.
    /// Implements IAuthorizationRequirement (Marker Interface).
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                throw new ArgumentNullException(nameof(permission));

            Permission = permission;
        }
    }
}
