using Microsoft.EntityFrameworkCore;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Infrastructure.Data;

namespace MutualFundNav.Infrastructure.Repositories
{
    public class MarketHolidayRepository : IMarketHolidayRepository
    {
        private readonly ApplicationDbContext _context;

        public MarketHolidayRepository(ApplicationDbContext context) => _context = context;

        public async Task<bool> IsHolidayAsync(DateTime date) =>
            await _context.MarketHolidays
                .AnyAsync(h => h.HolidayDate.Date == date.Date);

        public async Task AddAsync(MarketHoliday holiday) =>
            await _context.MarketHolidays.AddAsync(holiday);

        public async Task<IEnumerable<MarketHoliday>> GetHolidaysForYearAsync(int year) =>
            await _context.MarketHolidays
                .Where(h => h.HolidayDate.Year == year)
                .OrderBy(h => h.HolidayDate)
                .ToListAsync();

        /// <summary>
        /// Returns the <see cref="MarketHoliday"/> entry for the given date,
        /// or null when it is a trading day.
        /// </summary>
        public async Task<MarketHoliday?> GetByDateAsync(DateTime date) =>
            await _context.MarketHolidays
                .FirstOrDefaultAsync(h => h.HolidayDate.Date == date.Date);
    }
}