using MutualFund.Investment.Domain.Entities;

namespace MutualFund.Investment.Domain.Interfaces
{
    public interface IStatementRepository
    {
        // ── Create ────────────────────────────────────────────────
        Task<InvestmentStatement> AddAsync(InvestmentStatement statement);

        // ── Read ──────────────────────────────────────────────────
        Task<InvestmentStatement?> GetByIdAsync(int id);
        Task<InvestmentStatement?> GetByOrderIdAsync(int orderId);
        Task<IEnumerable<InvestmentStatement>> GetByInvestorAsync(
            string investorUserId);
        Task<IEnumerable<InvestmentStatement>> GetAllAsync();

        // ── Helpers ───────────────────────────────────────────────
        Task<bool> ExistsForOrderAsync(int orderId);
    }
}