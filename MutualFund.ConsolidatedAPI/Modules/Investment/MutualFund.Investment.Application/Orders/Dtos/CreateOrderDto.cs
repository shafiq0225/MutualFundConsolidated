namespace MutualFund.Investment.Application.Orders.Dtos
{
    /// <summary>
    /// Input from Admin when creating a new investment order.
    /// Head of Family calls Admin → Admin fills this form.
    /// </summary>
    public class CreateOrderDto
    {
        // ── Who is investing ───────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        // Selected from family member dropdown

        public string InvestorName { get; set; } = string.Empty;

        // ── Which scheme ───────────────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;

        // ── Amount ─────────────────────────────────────────────────
        public decimal InvestedAmount { get; set; }

        // ── Payment Details ────────────────────────────────────────
        public string PaymentMode { get; set; } = string.Empty;
        // "Cheque" | "NEFT" | "RTGS" | "IMPS" | "Online"

        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionRef { get; set; }

        // ── Order Date ─────────────────────────────────────────────
        public DateTime OrderDate { get; set; } = DateTime.Today;

        // ── Valuation — collected upfront per new design ───────────
        public decimal PurchaseNAV { get; set; }
        public string FolioNumber { get; set; } = string.Empty;
        // UnitsAllotted is computed server-side (InvestedAmount ÷ PurchaseNAV)

        // ── Notes ──────────────────────────────────────────────────
        public string? Notes { get; set; }
    }
}