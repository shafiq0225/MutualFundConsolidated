namespace MutualFund.Auth.Domain.Entities
{
    /// <summary>
    /// Master list of all available permissions in the system.
    /// Seeded on startup from PermissionType static class.
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }

        /// <summary>Unique code e.g. "scheme.read"</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Human-readable name e.g. "Read Schemes"</summary>
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ───────────────────────────────────────────────
        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
    }
}