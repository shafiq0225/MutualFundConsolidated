using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Enums;
using MutualFund.Investment.Domain.Interfaces;
using MutualFund.Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MutualFund.Investment.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly InvestmentDbContext _context;

        public OrderRepository(InvestmentDbContext context)
        {
            _context = context;
        }

        // ── Create ────────────────────────────────────────────────
        public async Task<InvestmentOrder> AddAsync(InvestmentOrder order)
        {
            await _context.InvestmentOrders.AddAsync(order);
            return order;
        }

        // ── Read ──────────────────────────────────────────────────
        public async Task<InvestmentOrder?> GetByIdAsync(int id)
        {
            return await _context.InvestmentOrders
                .AsNoTracking()
                .Include(o => o.Holding)
                .Include(o => o.Statement)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<InvestmentOrder?> GetByOrderNumberAsync(
            string orderNumber)
        {
            return await _context.InvestmentOrders
                .AsNoTracking()
                .Include(o => o.Holding)
                .Include(o => o.Statement)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<IEnumerable<InvestmentOrder>> GetAllAsync()
        {
            return await _context.InvestmentOrders
                .AsNoTracking()
                .Include(o => o.Holding)
                .Include(o => o.Statement)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InvestmentOrder>> GetByInvestorAsync(
            string investorUserId)
        {
            return await _context.InvestmentOrders
                .AsNoTracking()
                .Include(o => o.Holding)
                .Include(o => o.Statement)
                .Where(o => o.InvestorUserId == investorUserId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InvestmentOrder>> GetByStatusAsync(
            OrderStatus status)
        {
            return await _context.InvestmentOrders
                .AsNoTracking()
                .Include(o => o.Holding)
                .Include(o => o.Statement)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        // ── Update ────────────────────────────────────────────────
        public Task UpdateAsync(InvestmentOrder order)
        {
            order.UpdatedAt = DateTime.UtcNow;
            _context.InvestmentOrders.Update(order);
            return Task.CompletedTask;
        }

        // ── Helpers ───────────────────────────────────────────────
        public async Task<string> GenerateOrderNumberAsync()
        {
            // Format: ORD-2026-0001
            var year = DateTime.UtcNow.Year;

            var lastOrder = await _context.InvestmentOrders
                .Where(o => o.OrderNumber.StartsWith($"ORD-{year}-"))
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastOrder != null)
            {
                var parts = lastOrder.OrderNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int last))
                    nextNumber = last + 1;
            }

            return $"ORD-{year}-{nextNumber:D4}";
            // D4 = zero-padded 4 digits: 0001, 0002 ... 9999
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.InvestmentOrders
                .AnyAsync(o => o.Id == id);
        }
    }
}