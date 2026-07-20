namespace MutualFund.Investment.Application.Orders.Dtos
{
    /// <summary>
    /// Used to move an order through the workflow:
    ///   Requested → Assigned → Submitted → Verified → Active
    ///   (any of Requested/Assigned → Cancelled)
    ///
    /// NOTE: PurchaseNAV/FolioNumber/UnitsAllotted are now collected
    /// upfront at order creation (see CreateOrderDto), not here.
    /// Verified → Active is system-cascaded automatically within the
    /// same call — there is no separate "Active" request from the UI.
    /// </summary>
    public class UpdateOrderStatusDto
    {
        public string NewStatus { get; set; } = string.Empty;
        // "Assigned" | "Submitted" | "Verified" | "Cancelled"
        // ("Active" is not accepted directly — see remarks above)

        // ── Required when NewStatus = "Assigned" ───────────────────
        public DateTime? AssignedDate { get; set; }
        public string? AssignedStaffName { get; set; }

        // ── Required when NewStatus = "Submitted" ──────────────────
        public DateTime? SubmittedDate { get; set; }
        public string? SubmittedByUserId { get; set; }
        // Reference (cheque no. / txn ID) — updates whichever field
        // matches the order's existing PaymentMode
        public string? Reference { get; set; }

        // ── Required when NewStatus = "Verified" ───────────────────
        public DateTime? VerifiedDate { get; set; }
        public string? VerifiedByUserId { get; set; }

        // ── Optional ───────────────────────────────────────────────
        public string? Notes { get; set; }
    }
}
