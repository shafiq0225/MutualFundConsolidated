namespace MutualFund.Investment.Domain.Enums
{
    /// <summary>
    /// Investment order workflow, per the new Passbook design:
    /// Requested → Assigned → Submitted → Verified → Active (→ Cancelled)
    ///
    /// Verified → Active is a system-cascaded transition, not a separate
    /// manual admin action — the UI never exposes a distinct "Activate"
    /// step; "Verify & complete" does both in one call. Both states are
    /// kept distinct here (with their own timestamps) purely for audit
    /// history, not as separate user-facing stages.
    /// </summary>
    public enum OrderStatus
    {
        Requested = 1,   // Admin logged the instruction — awaiting field visit
        Assigned = 2,   // Field staff assigned — visit pending
        Submitted = 3,   // Form & cheque/txn submitted — pending verification
        Verified = 4,   // Admin verified submitted details
        Active = 5,   // System-cascaded from Verified — Holding created, daily P&L tracked
        Cancelled = 6    // Cancelled before submission
    }
}