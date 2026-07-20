namespace MutualFund.Auth.Application.DTOs.Family
{
    public class FamilyMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;

        /// <summary>"Self" (Head), "Spouse", "Mother", "Son", etc.</summary>
        public string RelationshipType { get; set; } = string.Empty;

        /// <summary>Optional distinguishing label, e.g. "Son1".</summary>
        public string? DisplayLabel { get; set; }

        public DateTime AddedAt { get; set; }
    }
}