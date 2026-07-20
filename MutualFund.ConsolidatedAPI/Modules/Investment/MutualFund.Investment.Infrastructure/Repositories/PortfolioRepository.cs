using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Interfaces;
using MutualFund.Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MutualFund.Investment.Infrastructure.Repositories
{
    public class PortfolioRepository : IPortfolioRepository
    {
        private readonly InvestmentDbContext _context;

        public PortfolioRepository(InvestmentDbContext context)
        {
            _context = context;
        }

        // ── Create ────────────────────────────────────────────────
        public async Task AddSnapshotAsync(PortfolioSnapshot snapshot)
        {
            await _context.PortfolioSnapshots.AddAsync(snapshot);
        }

        public async Task AddSnapshotRangeAsync(
            IEnumerable<PortfolioSnapshot> snapshots)
        {
            await _context.PortfolioSnapshots.AddRangeAsync(snapshots);
        }

        // ── Read ──────────────────────────────────────────────────
        public async Task<PortfolioSnapshot?> GetLatestByHoldingAsync(
            int holdingId)
        {
            return await _context.PortfolioSnapshots
                .AsNoTracking()
                .Where(s => s.HoldingId == holdingId)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PortfolioSnapshot>> GetByInvestorAsync(
            string investorUserId,
            DateTime? date = null)
        {
            var targetDate = date?.Date ?? DateTime.UtcNow.Date;

            // Get all active holdings for this investor
            var holdingIds = await _context.Holdings
                .AsNoTracking()
                .Where(h => h.InvestorUserId == investorUserId && h.IsActive)
                .Select(h => h.Id)
                .ToListAsync();

            var snapshots = new List<PortfolioSnapshot>();

            if (holdingIds.Count > 0)
            {
                // Get the latest snapshot date for each holding in 1 query
                var maxDates = await _context.PortfolioSnapshots
                    .AsNoTracking()
                    .Where(s => holdingIds.Contains(s.HoldingId) && s.SnapshotDate <= targetDate)
                    .GroupBy(s => s.HoldingId)
                    .Select(g => new { HoldingId = g.Key, MaxDate = g.Max(x => x.SnapshotDate) })
                    .ToListAsync();

                if (maxDates.Count > 0)
                {
                    var maxDatesList = maxDates.Select(m => m.MaxDate).Distinct().ToList();

                    // Query candidate snapshots in 1 query
                    var candidateSnapshots = await _context.PortfolioSnapshots
                        .AsNoTracking()
                        .Where(s => holdingIds.Contains(s.HoldingId) && maxDatesList.Contains(s.SnapshotDate))
                        .ToListAsync();

                    // Filter exactly matching HoldingId + MaxDate in memory
                    snapshots = candidateSnapshots
                        .Where(s => maxDates.Any(m => m.HoldingId == s.HoldingId && m.MaxDate == s.SnapshotDate))
                        .ToList();
                }
            }

            return snapshots.OrderBy(s => s.SchemeName);
        }

        public async Task<IEnumerable<PortfolioSnapshot>> GetAllLatestAsync()
        {
            // Get all active holding IDs
            var holdingIds = await _context.Holdings
                .AsNoTracking()
                .Where(h => h.IsActive)
                .Select(h => h.Id)
                .ToListAsync();

            var snapshots = new List<PortfolioSnapshot>();

            if (holdingIds.Count > 0)
            {
                // Get the latest snapshot date for each holding in 1 query
                var maxDates = await _context.PortfolioSnapshots
                    .AsNoTracking()
                    .Where(s => holdingIds.Contains(s.HoldingId))
                    .GroupBy(s => s.HoldingId)
                    .Select(g => new { HoldingId = g.Key, MaxDate = g.Max(x => x.SnapshotDate) })
                    .ToListAsync();

                if (maxDates.Count > 0)
                {
                    var maxDatesList = maxDates.Select(m => m.MaxDate).Distinct().ToList();

                    // Query candidate snapshots in 1 query
                    var candidateSnapshots = await _context.PortfolioSnapshots
                        .AsNoTracking()
                        .Where(s => holdingIds.Contains(s.HoldingId) && maxDatesList.Contains(s.SnapshotDate))
                        .ToListAsync();

                    // Filter exactly matching HoldingId + MaxDate in memory
                    snapshots = candidateSnapshots
                        .Where(s => maxDates.Any(m => m.HoldingId == s.HoldingId && m.MaxDate == s.SnapshotDate))
                        .ToList();
                }
            }

            return snapshots
                .OrderBy(s => s.InvestorName)
                .ThenBy(s => s.SchemeName);
        }

        // ── Helpers ───────────────────────────────────────────────
        public async Task<bool> SnapshotExistsAsync(
            int holdingId, DateTime date)
        {
            return await _context.PortfolioSnapshots
                .AnyAsync(s => s.HoldingId == holdingId
                            && s.SnapshotDate == date.Date);
        }

        public async Task DeleteSnapshotsForDateAsync(DateTime date)
        {
            var targetDate = date.Date;
            var existing = await _context.PortfolioSnapshots
                .Where(s => s.SnapshotDate == targetDate)
                .ToListAsync();

            if (existing.Count > 0)
            {
                _context.PortfolioSnapshots.RemoveRange(existing);
            }
        }
    }
}