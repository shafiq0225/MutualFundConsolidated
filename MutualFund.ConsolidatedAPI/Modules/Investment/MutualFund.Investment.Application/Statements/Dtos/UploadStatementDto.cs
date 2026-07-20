namespace MutualFund.Investment.Application.Statements.Dtos
{
    /// <summary>
    /// Input from Admin when uploading a PDF statement.
    /// MF company sends PDF via email 3-5 days after investment.
    /// Admin opens the order → uploads the PDF.
    /// </summary>
    public class UploadStatementDto
    {
        public int OrderId { get; set; }
        // Which investment order this statement belongs to

        public DateTime StatementDate { get; set; }
        // Date printed on the statement from MF company

        public string? Notes { get; set; }
        // Optional note from Admin

        // ── File (received from API controller) ────────────────────
        public Stream FileStream { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public long FileSizeBytes { get; set; }
    }
}