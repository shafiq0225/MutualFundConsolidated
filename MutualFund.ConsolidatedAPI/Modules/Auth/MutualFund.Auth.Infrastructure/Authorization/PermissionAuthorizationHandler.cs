using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Infrastructure.Authorization
{
    /// <summary>
    /// Custom Authorization Handler that evaluates PermissionRequirement rules.
    /// Inherits AuthorizationHandler<PermissionRequirement>.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // 1. Ensure user is authenticated
            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Authorization failed: User is unauthenticated.");
                return Task.CompletedTask;
            }

            // 2. Admin Superuser Bypass Rule: Admin passes all permission checks
            if (context.User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin"))
            {
                _logger.LogInformation("Authorization succeeded via Admin superuser role.");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // 3. Permission Claim Evaluation
            var permissionClaims = context.User.FindAll("permissions")
                .Select(c => c.Value)
                .ToList();

            if (permissionClaims.Any(p => p.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Authorization succeeded for permission '{Permission}'.", requirement.Permission);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Authorization failed: User lacks required permission '{Permission}'.", requirement.Permission);
            }

            return Task.CompletedTask;
        }
    }
}
