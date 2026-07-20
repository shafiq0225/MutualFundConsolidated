using MutualFund.Auth.Domain.Enums;

namespace MutualFund.Auth.Domain.Entities
{
    /// <summary>
    /// Links a User (FamilyMember type) to a FamilyGroup.
    /// Only Admin can add or remove members.
    /// </summary>
    public class FamilyMember
    {
        public int Id { get; set; }
        public int FamilyGroupId { get; set; }
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// This member's relationship to the Head of Family.
        /// Never Self — that value is reserved for the Head's own
        /// synthesized entry (see FamilyRelationshipType).
        /// </summary>
        public FamilyRelationshipType RelationshipType { get; set; }

        /// <summary>
        /// Optional free-text label to distinguish multiple members of
        /// the same RelationshipType — e.g. two sons: "Son1"/"Son2", or
        /// their actual names. Purely cosmetic, not used for logic.
        /// </summary>
        public string? DisplayLabel { get; set; }

        public string AddedByAdminId { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ───────────────────────────────────────────────
        public FamilyGroup FamilyGroup { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}