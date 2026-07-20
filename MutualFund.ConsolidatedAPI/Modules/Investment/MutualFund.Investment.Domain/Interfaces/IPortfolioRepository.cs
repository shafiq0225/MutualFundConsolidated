using MutualFund.Investment.Domain.Entities;

namespace MutualFund.Investment.Domain.Interfaces
{
    public interface IPortfolioRepository
    {
        // ── Create ────────────────────────────────────────────────
        Task AddSnapshotAsync(PortfolioSnapshot snapshot);
        Task AddSnapshotRangeAsync(IEnumerable<PortfolioSnapshot> snapshots);

        // ── Read ──────────────────────────────────────────────────
        Task<PortfolioSnapshot?> GetLatestByHoldingAsync(int holdingId);

        Task<IEnumerable<PortfolioSnapshot>> GetByInvestorAsync(
            string investorUserId, DateTime? date = null);
        // Returns latest snapshot per holding for an investor

        Task<IEnumerable<PortfolioSnapshot>> GetAllLatestAsync();
        // Returns latest snapshot for every active holding
        // Used by Admin to see all portfolios

        Task<bool> SnapshotExistsAsync(int holdingId, DateTime date);
        // Prevents duplicate snapshots for same day

        Task DeleteSnapshotsForDateAsync(DateTime date);
        // Deletes all snapshots for a specific date (enables recalculation)
    }
}