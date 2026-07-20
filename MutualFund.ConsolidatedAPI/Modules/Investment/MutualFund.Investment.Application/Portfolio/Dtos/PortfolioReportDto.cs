namespace MutualFund.Investment.Application.Portfolio.Dtos
{
    /// <summary>
    /// Complete portfolio report for one investor.
    /// Equivalent to the Excel report the staff used to prepare manually.
    ///
    /// Report columns:
    ///   Date of Purchase | Year | Invested Amount | Purchase NAV
    ///   Units | Current NAV | Total Amount | Profit/Loss | %
    /// </summary>
    public class PortfolioReportDto
    {
        // ── Investor ───────────────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Report Date ────────────────────────────────────────────
        public DateTime ReportDate { get; set; }

        // ── Summary ────────────────────────────────────────────────
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal OverallReturnPercent { get; set; }
        public bool IsOverallProfit { get; set; }
        public int TotalHoldings { get; set; }

        // ── Holdings detail rows ───────────────────────────────────
        public IEnumerable<PortfolioRowDto> Holdings { get; set; }
            = new List<PortfolioRowDto>();
    }

    /// <summary>
    /// One row in the portfolio report table.
    /// Matches the Excel columns exactly.
    /// </summary>
    public class PortfolioRowDto
    {
        // ── Col 1: Scheme ──────────────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;

        // ── Col 2: Date of Purchase ────────────────────────────────
        public DateTime PurchaseDate { get; set; }
        public string PurchaseDateText { get; set; } = string.Empty;
        // formatted: "15 Jan 2026"

        // ── Col 3: Year ────────────────────────────────────────────
        public int Year { get; set; }

        // ── Col 4: Invested Amount ─────────────────────────────────
        public decimal InvestedAmount { get; set; }

        // ── Col 5: Purchase NAV ────────────────────────────────────
        public decimal PurchaseNAV { get; set; }

        // ── Col 6: Number of Units ─────────────────────────────────
        public decimal Units { get; set; }

        // ── Col 7: Current NAV ─────────────────────────────────────
        public decimal CurrentNAV { get; set; }

        // ── Col 8: Total Amount (Current Value) ────────────────────
        public decimal TotalAmount { get; set; }
        // Units × CurrentNAV

        // ── Col 9: Profit / Loss ───────────────────────────────────
        public decimal ProfitLoss { get; set; }
        // TotalAmount - InvestedAmount
        // Positive = Profit, Negative = Loss

        // ── Col 10: Percentage ─────────────────────────────────────
        public decimal Percentage { get; set; }
        // (ProfitLoss / InvestedAmount) × 100

        public bool IsProfit { get; set; }
        // true = green, false = red in UI

        // ── Snapshot Date ──────────────────────────────────────────
        public DateTime? SnapshotDate { get; set; }
        // Date when CurrentNAV was fetched
    }
}