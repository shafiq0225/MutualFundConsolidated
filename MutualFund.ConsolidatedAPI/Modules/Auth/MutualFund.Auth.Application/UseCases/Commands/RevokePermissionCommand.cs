using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class RevokePermissionCommand
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<RevokePermissionCommand> _logger;

        public RevokePermissionCommand(
            IPermissionService permissionService,
            ILogger<RevokePermissionCommand> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task ExecuteAsync(
            string userId, string permissionCode, string adminId)
        {
            await _permissionService.RevokePermissionAsync(
                userId, permissionCode, adminId);

            _logger.LogInformation(
                "Permission revoked — UserId={UserId} " +
                "Permission={Code} By={AdminId}",
                userId, permissionCode, adminId);
        }
    }
}