using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Enums;

namespace MutualFund.Investment.Domain.Interfaces
{
    public interface IOrderRepository
    {
        // ── Create ────────────────────────────────────────────────
        Task<InvestmentOrder> AddAsync(InvestmentOrder order);

        // ── Read ──────────────────────────────────────────────────
        Task<InvestmentOrder?> GetByIdAsync(int id);
        Task<InvestmentOrder?> GetByOrderNumberAsync(string orderNumber);
        Task<IEnumerable<InvestmentOrder>> GetAllAsync();
        Task<IEnumerable<InvestmentOrder>> GetByInvestorAsync(string investorUserId);
        Task<IEnumerable<InvestmentOrder>> GetByStatusAsync(OrderStatus status);

        // ── Update ────────────────────────────────────────────────
        Task UpdateAsync(InvestmentOrder order);

        // ── Helpers ───────────────────────────────────────────────
        Task<string> GenerateOrderNumberAsync();
        // Generates next order number: ORD-2026-0001

        Task<bool> ExistsAsync(int id);
    }
}