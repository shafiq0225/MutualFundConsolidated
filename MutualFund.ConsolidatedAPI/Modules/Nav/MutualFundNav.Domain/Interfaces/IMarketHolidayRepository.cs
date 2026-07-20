using MutualFundNav.Domain.Entities;

namespace MutualFundNav.Domain.Interfaces
{
    public interface IMarketHolidayRepository
    {
        Task<bool> IsHolidayAsync(DateTime date);
        Task AddAsync(MarketHoliday holiday);
        Task<IEnumerable<MarketHoliday>> GetHolidaysForYearAsync(int year);

        /// <summary>
        /// Returns the <see cref="MarketHoliday"/> record for the given date,
        /// or null if it is a trading day.
        /// Used by GET /api/holidays/today to return holiday description and source.
        /// </summary>
        Task<MarketHoliday?> GetByDateAsync(DateTime date);
    }
}