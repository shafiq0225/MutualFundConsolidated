namespace MutualFund.Investment.Domain.Entities
{
    /// <summary>
    /// PDF statement sent by the Mutual Fund company
    /// 3-5 days after investment.
    /// Admin uploads it → stored in Azure Blob Storage.
    /// Head + Family Members can view it in the app.
    /// </summary>
    public class InvestmentStatement
    {
        public int Id { get; set; }

        // ── Linked Order ──────────────────────────────────────────
        public int OrderId { get; set; }
        // FK → InvestmentOrders

        // ── Investor ──────────────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Statement Details ─────────────────────────────────────
        public DateTime StatementDate { get; set; }
        // Date on the statement from MF company

        // ── File Storage ──────────────────────────────────────────
        public string FilePath { get; set; } = string.Empty;
        // Azure Blob Storage URL
        // e.g. https://amfinavstorage.blob.core.windows.net/
        //      investment-statements/ORD-2026-0001.pdf

        public string FileName { get; set; } = string.Empty;
        // Original file name when Admin uploaded

        public long FileSizeBytes { get; set; }
        // File size for display

        // ── Upload Audit ──────────────────────────────────────────
        public string UploadedByUserId { get; set; } = string.Empty;
        // Admin who uploaded

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
        // Optional note from Admin

        // ── Navigation ────────────────────────────────────────────
        public InvestmentOrder Order { get; set; } = null!;
    }
}