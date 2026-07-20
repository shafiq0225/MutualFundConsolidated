namespace MutualFund.Investment.Application.Portfolio.Dtos
{
    /// <summary>
    /// One active investment holding with latest P&L
    /// </summary>
    public class HoldingDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        // ── Investor ───────────────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Scheme ─────────────────────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;

        // ── Purchase Details ───────────────────────────────────────
        public DateTime PurchaseDate { get; set; }
        public int PurchaseYear { get; set; }
        public decimal PurchaseNAV { get; set; }
        public decimal InvestedAmount { get; set; }
        public decimal Units { get; set; }

        // ── Current Values (from latest snapshot) ──────────────────
        public decimal CurrentNAV { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercent { get; set; }
        public bool IsProfit { get; set; }

        // ── Snapshot Date ──────────────────────────────────────────
        public DateTime? LastUpdatedDate { get; set; }

        public bool IsActive { get; set; }
    }
}