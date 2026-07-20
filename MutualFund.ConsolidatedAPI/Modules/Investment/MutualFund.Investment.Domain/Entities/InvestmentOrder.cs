using MutualFund.Investment.Domain.Enums;

namespace MutualFund.Investment.Domain.Entities
{
    /// <summary>
    /// Represents one investment instruction from
    /// Head of Family to Admin.
    /// Created when Admin records the investment details.
    /// </summary>
    public class InvestmentOrder
    {
        public int Id { get; set; }

        // ── Order Identity ────────────────────────────────────────
        public string OrderNumber { get; set; } = string.Empty;
        // e.g. ORD-2026-0001 — auto generated

        // ── Who is investing ──────────────────────────────────────
        public string InvestorUserId { get; set; } = string.Empty;
        // FK → Users table in Auth DB (Head or Family Member)

        public string InvestorName { get; set; } = string.Empty;
        // Stored for quick display — no join needed

        // ── Which scheme ──────────────────────────────────────────
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;

        // ── Amount ────────────────────────────────────────────────
        public decimal InvestedAmount { get; set; }

        // ── Payment Details ───────────────────────────────────────
        public PaymentMode PaymentMode { get; set; }
        public string? ChequeNumber { get; set; } // if Cheque
        public DateTime? ChequeDate { get; set; } // if Cheque
        public string? BankName { get; set; } // if Cheque
        public string? TransactionRef { get; set; } // if NEFT/RTGS

        // ── Order Date ────────────────────────────────────────────
        public DateTime OrderDate { get; set; }

        // ── Status ────────────────────────────────────────────────
        public OrderStatus Status { get; set; } = OrderStatus.Requested;

        // ── Assignment Details ────────────────────────────────────
        public DateTime? AssignedDate { get; set; }
        public string? AssignedStaffName { get; set; }
        // Field staff assigned to visit — free text per new design

        // ── Submission Details ────────────────────────────────────
        public DateTime? SubmittedDate { get; set; }
        // When employee visited MF office

        public string? SubmittedByUserId { get; set; }
        // Which employee submitted

        // ── Verification Details (admin double-checks submission) ──
        public DateTime? VerifiedDate { get; set; }
        public string? VerifiedByUserId { get; set; }

        // ── Valuation — collected upfront at order creation now,
        // per the new design (previously deferred to confirmation) ──
        public decimal? PurchaseNAV { get; set; }
        // NAV at which units were allotted

        public decimal? UnitsAllotted { get; set; }
        // Units = InvestedAmount ÷ PurchaseNAV

        public string? FolioNumber { get; set; }
        // Assigned by MF company — unique per investor per fund

        // ── Activation — system-cascaded immediately after Verified,
        // not a separate manual step (see OrderStatus.cs) ───────────
        public DateTime? ActivatedDate { get; set; }

        // ── Admin Notes ───────────────────────────────────────────
        public string? Notes { get; set; }

        // ── Audit ─────────────────────────────────────────────────
        public string CreatedByUserId { get; set; } = string.Empty;
        // Admin who created this order

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation ────────────────────────────────────────────
        public Holding? Holding { get; set; }
        public InvestmentStatement? Statement { get; set; }
    }
}