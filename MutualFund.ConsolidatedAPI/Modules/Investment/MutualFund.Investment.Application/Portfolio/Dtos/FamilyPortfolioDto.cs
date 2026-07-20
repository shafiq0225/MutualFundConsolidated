namespace MutualFund.Investment.Application.Portfolio.Dtos
{
    /// <summary>
    /// Combined portfolio for the entire family group.
    /// Head of Family sees all members' investments.
    /// Admin sees all investors combined.
    /// </summary>
    public class FamilyPortfolioDto
    {
        // ── Report ─────────────────────────────────────────────────
        public DateTime ReportDate { get; set; }

        // ── Family Grand Total ─────────────────────────────────────
        public decimal TotalFamilyInvested { get; set; }
        public decimal TotalFamilyCurrentValue { get; set; }
        public decimal TotalFamilyProfitLoss { get; set; }
        public decimal FamilyReturnPercent { get; set; }
        public bool IsFamilyProfit { get; set; }

        // ── Per Investor breakdown ─────────────────────────────────
        public IEnumerable<PortfolioReportDto> InvestorPortfolios { get; set; }
            = new List<PortfolioReportDto>();
    }
}