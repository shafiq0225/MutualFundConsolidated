using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Interfaces;
using MutualFund.Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MutualFund.Investment.Infrastructure.Repositories
{
    public class HoldingRepository : IHoldingRepository
    {
        private readonly InvestmentDbContext _context;

        public HoldingRepository(InvestmentDbContext context)
        {
            _context = context;
        }

        // ── Create ────────────────────────────────────────────────
        public async Task<Holding> AddAsync(Holding holding)
        {
            await _context.Holdings.AddAsync(holding);
            return holding;
        }

        // ── Read ──────────────────────────────────────────────────
        public async Task<Holding?> GetByIdAsync(int id)
        {
            return await _context.Holdings
                .AsNoTracking()
                .Include(h => h.Order)
                .FirstOrDefaultAsync(h => h.Id == id);
        }

        public async Task<Holding?> GetByOrderIdAsync(int orderId)
        {
            return await _context.Holdings
                .AsNoTracking()
                .Include(h => h.Order)
                .FirstOrDefaultAsync(h => h.OrderId == orderId);
        }

        public async Task<IEnumerable<Holding>> GetAllActiveAsync()
        {
            return await _context.Holdings
                .AsNoTracking()
                .Include(h => h.Order)
                .Where(h => h.IsActive)
                .OrderBy(h => h.InvestorName)
                .ThenBy(h => h.SchemeName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Holding>> GetByInvestorAsync(
            string investorUserId)
        {
            return await _context.Holdings
                .AsNoTracking()
                .Include(h => h.Order)
                .Where(h => h.InvestorUserId == investorUserId
                         && h.IsActive)
                .OrderBy(h => h.SchemeName)
                .ToListAsync();
        }

        // ── Update ────────────────────────────────────────────────
        public Task UpdateAsync(Holding holding)
        {
            _context.Holdings.Update(holding);
            return Task.CompletedTask;
        }

        // ── Helpers ───────────────────────────────────────────────
        public async Task<bool> ExistsForOrderAsync(int orderId)
        {
            return await _context.Holdings
                .AnyAsync(h => h.OrderId == orderId);
        }

        public async Task<IEnumerable<Holding>> GetAllByInvestorActiveAsync(string investorUserId)
        {
            return await _context.Holdings
                .AsNoTracking()
                .Include(h => h.Order)
                .Where(h => h.InvestorUserId == investorUserId
                         && h.IsActive)
                .OrderBy(h => h.SchemeName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Holding>> GetAllActiveGroupedAsync()
        {
            return await _context.Holdings
                .AsNoTracking()
                .Include(h => h.Order)
                .Where(h => h.IsActive)
                .OrderBy(h => h.InvestorName)
                .ThenBy(h => h.SchemeName)
                .ToListAsync();
        }
    }
}