using MutualFund.Auth.Application.DTOs.Family;
using MutualFund.Auth.Domain.Enums;
using MutualFund.Auth.Domain.Exceptions;
using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class AddFamilyMemberCommand
    {
        private readonly IFamilyService _familyService;
        private readonly ILogger<AddFamilyMemberCommand> _logger;

        public AddFamilyMemberCommand(
            IFamilyService familyService,
            ILogger<AddFamilyMemberCommand> logger)
        {
            _familyService = familyService;
            _logger = logger;
        }

        public async Task<FamilyGroupDto> ExecuteAsync(
            int groupId, AddFamilyMemberDto dto, string adminId)
        {
            if (!Enum.TryParse<FamilyRelationshipType>(
                    dto.RelationshipType, ignoreCase: true, out var relationshipType))
            {
                var validValues = string.Join(", ",
                    Enum.GetNames<FamilyRelationshipType>()
                        .Where(n => n != nameof(FamilyRelationshipType.Self)));

                throw new AuthException(
                    $"Invalid RelationshipType '{dto.RelationshipType}'. " +
                    $"Valid values: {validValues}.",
                    "INVALID_RELATIONSHIP_TYPE", 400);
            }

            var group = await _familyService.AddMemberAsync(
                groupId, dto.UserId, relationshipType,
                dto.DisplayLabel, adminId);

            _logger.LogInformation(
                "Family member added — GroupId={GroupId} " +
                "UserId={UserId} Relationship={Relationship} By={AdminId}",
                groupId, dto.UserId, relationshipType, adminId);

            return CreateFamilyGroupCommand.MapToDto(group);
        }
    }
}