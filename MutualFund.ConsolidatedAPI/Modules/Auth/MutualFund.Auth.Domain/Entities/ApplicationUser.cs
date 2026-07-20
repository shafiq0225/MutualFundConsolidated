using Microsoft.AspNetCore.Identity;
using MutualFund.Auth.Domain.Enums;

namespace MutualFund.Auth.Domain.Entities
{
    /// <summary>
    /// Extended Identity user — covers Admin, Employee and User roles.
    /// PAN number is stored for all user types for KYC purposes.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // ── Personal Info ────────────────────────────────────────────
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// PAN (Permanent Account Number) — required for all user types.
        /// Format: 5 letters + 4 digits + 1 letter (e.g. ABCDE1234F)
        /// </summary>
        public string PanNumber { get; set; } = string.Empty;

        // ── Role & Type ──────────────────────────────────────────────
        public UserRole Role { get; set; } = UserRole.User;

        /// <summary>
        /// Applicable only when Role = User.
        /// Distinguishes Head of Family from Family Member.
        /// </summary>
        public UserType UserType { get; set; } = UserType.None;

        // ── Approval ─────────────────────────────────────────────────
        /// <summary>
        /// All new registrations start as Pending.
        /// Admin must approve before user can log in.
        /// </summary>
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByUserId { get; set; }
        public string? RejectionReason { get; set; }

        // ── Status ───────────────────────────────────────────────────
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // ── Navigation ───────────────────────────────────────────────
        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();

        public ICollection<UserPermission> UserPermissions { get; set; }
            = new List<UserPermission>();
    }
}