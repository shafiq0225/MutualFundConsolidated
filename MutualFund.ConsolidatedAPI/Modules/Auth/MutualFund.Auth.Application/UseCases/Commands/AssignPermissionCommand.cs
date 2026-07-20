using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class AssignPermissionCommand
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<AssignPermissionCommand> _logger;

        public AssignPermissionCommand(
            IPermissionService permissionService,
            ILogger<AssignPermissionCommand> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task ExecuteAsync(
            string userId, string permissionCode, string adminId)
        {
            await _permissionService.AssignPermissionAsync(
                userId, permissionCode, adminId);

            _logger.LogInformation(
                "Permission assigned — UserId={UserId} " +
                "Permission={Code} By={AdminId}",
                userId, permissionCode, adminId);
        }
    }
}