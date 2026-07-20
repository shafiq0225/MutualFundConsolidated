namespace MutualFund.Auth.Application.DTOs.Family
{
    public class FamilyGroupDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string HeadUserId { get; set; } = string.Empty;
        public string HeadUserName { get; set; } = string.Empty;
        public string HeadUserEmail { get; set; } = string.Empty;
        public string HeadPanNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Real dependents only — unchanged shape, for
        /// backward compatibility with existing consumers.</summary>
        public List<FamilyMemberDto> Members { get; set; } = new();

        /// <summary>
        /// Head (RelationshipType="Self") + all real dependents, in one
        /// list — convenient for rendering a complete family tree
        /// without special-casing the Head separately.
        /// </summary>
        public List<FamilyMemberDto> AllMembers { get; set; } = new();

        public int MemberCount => Members.Count;
    }
}