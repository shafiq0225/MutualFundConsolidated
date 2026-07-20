namespace MutualFund.Auth.Domain.Entities
{
    /// <summary>
    /// Represents a permission granted to a specific user by an Admin.
    /// Permissions can be revoked at any time.
    /// </summary>
    public class UserPermission
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public string GrantedByUserId { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByUserId { get; set; }

        // ── Computed ─────────────────────────────────────────────────
        public bool IsActive => RevokedAt == null;

        // ── Navigation ───────────────────────────────────────────────
        public ApplicationUser User { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
    }
}