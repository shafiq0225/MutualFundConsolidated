using MutualFund.Auth.Application.DTOs.Family;
using MutualFund.Auth.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Auth.Application.UseCases.Commands
{
    public class CreateFamilyGroupCommand
    {
        private readonly IFamilyService _familyService;
        private readonly ILogger<CreateFamilyGroupCommand> _logger;

        public CreateFamilyGroupCommand(
            IFamilyService familyService,
            ILogger<CreateFamilyGroupCommand> logger)
        {
            _familyService = familyService;
            _logger = logger;
        }

        public async Task<FamilyGroupDto> ExecuteAsync(
            CreateFamilyGroupDto dto, string adminId)
        {
            var group = await _familyService.CreateFamilyGroupAsync(
                dto.GroupName, dto.HeadUserId, adminId);

            _logger.LogInformation(
                "Family group created — GroupId={Id} " +
                "Head={HeadUserId} By={AdminId}",
                group.Id, dto.HeadUserId, adminId);

            return MapToDto(group);
        }

        internal static FamilyGroupDto MapToDto(
            Domain.Entities.FamilyGroup g)
        {
            var memberDtos = g.Members.Select(m => new FamilyMemberDto
            {
                UserId = m.UserId,
                FullName = $"{m.User?.FirstName} {m.User?.LastName}",
                Email = m.User?.Email ?? string.Empty,
                PanNumber = m.User?.PanNumber ?? string.Empty,
                RelationshipType = m.RelationshipType.ToString(),
                DisplayLabel = m.DisplayLabel,
                AddedAt = m.AddedAt
            }).ToList();

            // Synthesize the Head's own entry as "Self" — the Head is
            // never a FamilyMember row, so this is the only place its
            // relationship label gets attached, giving consumers one
            // complete list instead of having to special-case the Head.
            var headDto = new FamilyMemberDto
            {
                UserId = g.HeadUserId,
                FullName = $"{g.HeadUser?.FirstName} {g.HeadUser?.LastName}",
                Email = g.HeadUser?.Email ?? string.Empty,
                PanNumber = g.HeadUser?.PanNumber ?? string.Empty,
                RelationshipType =
                    Domain.Enums.FamilyRelationshipType.Self.ToString(),
                DisplayLabel = null,
                AddedAt = g.CreatedAt
            };

            return new FamilyGroupDto
            {
                Id = g.Id,
                GroupName = g.GroupName,
                HeadUserId = g.HeadUserId,
                HeadUserName = $"{g.HeadUser?.FirstName} {g.HeadUser?.LastName}",
                HeadUserEmail = g.HeadUser?.Email ?? string.Empty,
                HeadPanNumber = g.HeadUser?.PanNumber ?? string.Empty,
                CreatedAt = g.CreatedAt,
                IsActive = g.IsActive,
                Members = memberDtos,
                AllMembers = new List<FamilyMemberDto> { headDto }
                    .Concat(memberDtos)
                    .ToList()
            };
        }
    }
}