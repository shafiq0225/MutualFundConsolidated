namespace MutualFund.Auth.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }

        // ── Computed ─────────────────────────────────────────────────
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsExpired && !IsRevoked;

        // ── Navigation ───────────────────────────────────────────────
        public ApplicationUser User { get; set; } = null!;
    }
}