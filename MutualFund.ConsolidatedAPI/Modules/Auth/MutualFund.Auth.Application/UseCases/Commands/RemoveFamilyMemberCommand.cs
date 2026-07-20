using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class RemoveFamilyMemberCommand
    {
        private readonly IFamilyService _familyService;
        private readonly ILogger<RemoveFamilyMemberCommand> _logger;

        public RemoveFamilyMemberCommand(
            IFamilyService familyService,
            ILogger<RemoveFamilyMemberCommand> logger)
        {
            _familyService = familyService;
            _logger = logger;
        }

        public async Task ExecuteAsync(
            int groupId, string userId, string adminId)
        {
            await _familyService.RemoveMemberAsync(
                groupId, userId, adminId);

            _logger.LogInformation(
                "Family member removed — GroupId={GroupId} " +
                "UserId={UserId} By={AdminId}",
                groupId, userId, adminId);
        }
    }
}