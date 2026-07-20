namespace MutualFund.Auth.Application.DTOs.Family
{
    public class AddFamilyMemberDto
    {
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// e.g. "Spouse", "Mother", "Son", "Daughter", "Other".
        /// Must match a MutualFund.Auth.Domain.Enums.FamilyRelationshipType
        /// value — never "Self" (reserved for the Head).
        /// </summary>
        public string RelationshipType { get; set; } = string.Empty;

        /// <summary>
        /// Optional — distinguishes multiple members of the same
        /// RelationshipType, e.g. "Son1"/"Son2", or an actual name.
        /// </summary>
        public string? DisplayLabel { get; set; }
    }
}