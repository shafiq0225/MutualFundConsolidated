using MutualFund.Investment.Domain.Enums;

namespace MutualFund.Investment.Application.Orders
{
    /// <summary>
    /// Full order details — returned by all order queries
    /// </summary>
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        // ── Investor ───────────────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;

        // ── Scheme ─────────────────────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;

        // ── Amount ─────────────────────────────────────────────────
        public decimal InvestedAmount { get; set; }

        // ── Payment ────────────────────────────────────────────────
        public string PaymentMode { get; set; } = string.Empty;
        public string? ChequeNumber { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string? TransactionRef { get; set; }

        // ── Dates ──────────────────────────────────────────────────
        public DateTime OrderDate { get; set; }
        public DateTime? AssignedDate { get; set; }
        public string? AssignedStaffName { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? VerifiedDate { get; set; }
        public DateTime? ActivatedDate { get; set; }

        // ── Status ─────────────────────────────────────────────────
        public string Status { get; set; } = string.Empty;
        public int StatusCode { get; set; }

        // ── Valuation ───────────────────────────────────────────────
        public decimal? PurchaseNAV { get; set; }
        public decimal? UnitsAllotted { get; set; }
        public string? FolioNumber { get; set; }

        // ── Notes ──────────────────────────────────────────────────
        public string? Notes { get; set; }

        // ── Audit ──────────────────────────────────────────────────
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ── Related ────────────────────────────────────────────────
        public bool HasHolding { get; set; }
        // True once order reaches Active — Holding exists

        public bool HasStatement { get; set; }
        // True when Admin has uploaded PDF statement
    }

    /// <summary>
    /// Summary version — used in list views
    /// </summary>
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime OrderDate { get; set; }
        public bool HasStatement { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}