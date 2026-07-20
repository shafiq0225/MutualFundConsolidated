namespace MutualFund.Investment.Domain.Entities
{
    /// <summary>
    /// One row per Holding per trading day.
    /// Created by the daily background job at 9 AM.
    /// Stores the P&L calculation for that day.
    ///
    /// Report columns this feeds:
    ///   Date of Purchase  → Holding.PurchaseDate
    ///   Year              → Holding.PurchaseDate.Year
    ///   Invested Amount   → Holding.InvestedAmount
    ///   Purchase NAV      → Holding.PurchaseNAV
    ///   Number of Units   → Holding.Units
    ///   Current NAV       → this.CurrentNAV
    ///   Total Amount      → this.CurrentValue
    ///   Profit / Loss     → this.ProfitLoss
    ///   Percentage        → this.ProfitLossPercent
    /// </summary>
    public class PortfolioSnapshot
    {
        public int Id { get; set; }

        // ── Linked Holding ────────────────────────────────────────
        public int HoldingId { get; set; }
        // FK → Holdings

        // ── Investor (denormalised for fast queries) ───────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Scheme (denormalised) ─────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;

        // ── Date of snapshot ──────────────────────────────────────
        public DateTime SnapshotDate { get; set; }
        // The trading day this snapshot was calculated

        // ── NAV on snapshot date ──────────────────────────────────
        public decimal CurrentNAV { get; set; }
        // Fetched from DetailedSchemes table (App 2)

        // ── Calculated Values ─────────────────────────────────────
        public decimal InvestedAmount { get; set; }
        // Copy from Holding — for easy report display

        public decimal CurrentValue { get; set; }
        // Units × CurrentNAV

        public decimal ProfitLoss { get; set; }
        // CurrentValue - InvestedAmount
        // Positive = profit, Negative = loss

        public decimal ProfitLossPercent { get; set; }
        // (ProfitLoss ÷ InvestedAmount) × 100

        // ── Audit ─────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ────────────────────────────────────────────
        public Holding Holding { get; set; } = null!;
    }
}