using MutualFundNav.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFundNav.Infrastructure.Helpers
{
    /// <summary>
    /// Resolves the target NAV date (last trading day before today).
    /// Stateless and fully async — no static state, injected via DI.
    /// </summary>
    public class DateHelper : IDateHelper
    {
        private readonly INseHolidayFetcher _holidayFetcher;
        private readonly ILogger<DateHelper> _logger;

        public DateHelper(INseHolidayFetcher holidayFetcher, ILogger<DateHelper> logger)
        {
            _holidayFetcher = holidayFetcher;
            _logger         = logger;
        }

        public async Task<DateTime> GetTargetNavDateAsync()
        {
            var tz = GetIstTimeZone();
            var todayIst = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz).Date;
            var candidate = todayIst.AddDays(-1);

            while (!await IsTradingDayAsync(candidate))
            {
                _logger.LogDebug("{Date} is not a trading day — stepping back", candidate.ToString("yyyy-MM-dd"));
                candidate = candidate.AddDays(-1);
            }

            return candidate;
        }

        private static TimeZoneInfo GetIstTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
        }

        public async Task<bool> IsTradingDayAsync(DateTime date)
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return false;

            try
            {
                var holidays = await _holidayFetcher.FetchAllHolidaysAsync();
                return !holidays.Contains(date.Date);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Holiday check failed for {Date} — assuming trading day", date);
                return true;
            }
        }
    }
}
