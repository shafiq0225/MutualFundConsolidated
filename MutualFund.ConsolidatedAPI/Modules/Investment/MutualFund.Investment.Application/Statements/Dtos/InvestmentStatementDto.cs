namespace MutualFund.Investment.Application.Statements.Dtos
{
    /// <summary>
    /// Returned to caller — full statement details
    /// </summary>
    public class InvestmentStatementDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        // ── Order Info (for display) ───────────────────────────────
        public string OrderNumber { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;

        // ── Investor ───────────────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Statement ──────────────────────────────────────────────
        public DateTime StatementDate { get; set; }

        // ── File ───────────────────────────────────────────────────
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string FileSizeText { get; set; } = string.Empty;
        // e.g. "1.2 MB"

        // ── Upload Audit ───────────────────────────────────────────
        public string UploadedByUserId { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string? Notes { get; set; }
    }
}