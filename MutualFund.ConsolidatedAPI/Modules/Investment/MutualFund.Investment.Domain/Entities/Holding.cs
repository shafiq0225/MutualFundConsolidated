namespace MutualFund.Investment.Domain.Entities
{
    /// <summary>
    /// Represents an active investment holding.
    /// Created automatically when an InvestmentOrder is Verified
    /// (which cascades immediately to Active).
    /// Used for daily P&L calculation.
    /// </summary>
    public class Holding
    {
        public int Id { get; set; }

        // ── Linked Order ──────────────────────────────────────────
        public int OrderId { get; set; }
        // FK → InvestmentOrders

        // ── Investor ──────────────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Scheme ────────────────────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;

        // ── Purchase Details ──────────────────────────────────────
        public DateTime PurchaseDate { get; set; }
        public decimal PurchaseNAV { get; set; }
        // NAV on the day of purchase

        public decimal InvestedAmount { get; set; }
        // Original amount invested

        public decimal Units { get; set; }
        // Units = InvestedAmount ÷ PurchaseNAV

        // ── Status ────────────────────────────────────────────────
        public bool IsActive { get; set; } = true;
        // False when redeemed (Phase 2)

        // ── Audit ─────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ────────────────────────────────────────────
        public InvestmentOrder Order { get; set; } = null!;
        public ICollection<PortfolioSnapshot> Snapshots { get; set; }
            = new List<PortfolioSnapshot>();
    }
}