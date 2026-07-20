using MutualFund.Scheme.Domain.Entities;

namespace MutualFund.Scheme.Domain.Interfaces
{
    public interface IMarketHolidayRepository
    {
        Task<bool> ExistsByDateAsync(DateTime date);
        Task AddAsync(MarketHoliday holiday);
        Task<MarketHoliday?> GetByDateAsync(DateTime date);
    }
}