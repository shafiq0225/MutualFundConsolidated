namespace MutualFund.Auth.Domain.Entities
{
    /// <summary>
    /// A family group has one Head of Family and multiple members.
    /// Only Admin can create groups and add members.
    /// </summary>
    public class FamilyGroup
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;

        /// <summary>UserId of the Head of Family (UserType = HeadOfFamily)</summary>
        public string HeadUserId { get; set; } = string.Empty;

        public string CreatedByAdminId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // ── Navigation ───────────────────────────────────────────────
        public ApplicationUser HeadUser { get; set; } = null!;
        public ICollection<FamilyMember> Members { get; set; }
            = new List<FamilyMember>();
    }
}