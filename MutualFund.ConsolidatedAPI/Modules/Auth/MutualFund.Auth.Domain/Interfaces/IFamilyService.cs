using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;

namespace MutualFund.Auth.Domain.Interfaces
{
    public interface IFamilyService
    {
        Task<IEnumerable<FamilyGroup>> GetAllFamilyGroupsAsync();
        Task<FamilyGroup> GetFamilyGroupAsync(int groupId);

        /// <summary>
        /// Admin creates a family group and assigns a Head of Family.
        /// </summary>
        Task<FamilyGroup> CreateFamilyGroupAsync(
            string groupName,
            string headUserId,
            string adminId);

        /// <summary>
        /// Admin adds a User (FamilyMember) to a group, with their
        /// relationship to the Head (e.g. Spouse, Son, Daughter).
        /// RelationshipType.Self is rejected — reserved for the Head.
        /// </summary>
        Task<FamilyGroup> AddMemberAsync(
            int groupId,
            string userId,
            FamilyRelationshipType relationshipType,
            string? displayLabel,
            string adminId);

        /// <summary>
        /// Admin removes a member from a group.
        /// </summary>
        Task RemoveMemberAsync(
            int groupId,
            string userId,
            string adminId);
    }
}