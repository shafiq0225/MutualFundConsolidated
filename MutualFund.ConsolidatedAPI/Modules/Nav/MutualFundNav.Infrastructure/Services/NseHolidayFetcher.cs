using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.Infrastructure.Services
{
    public class NseHolidayFetcher : INseHolidayFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NseHolidayFetcher> _logger;
        private const string CacheKey = "NSE_HOLIDAYS_ALL";

        public NseHolidayFetcher(
            HttpClient httpClient,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<NseHolidayFetcher> logger)
        {
            _httpClient    = httpClient;
            _cache         = cache;
            _configuration = configuration;
            _logger        = logger;
        }

        public async Task<List<DateTime>> FetchHolidaysForYearAsync(int year)
        {
            var all = await FetchAllHolidaysAsync();
            return all.Where(h => h.Year == year).ToList();
        }

        public async Task<HashSet<DateTime>> FetchAllHolidaysAsync()
        {
            if (_cache.TryGetValue(CacheKey, out HashSet<DateTime>? cached) && cached is not null)
            {
                _logger.LogDebug("Returning {Count} holidays from cache", cached.Count);
                return cached;
            }

            try
            {
                var apiUrl = _configuration["AppSettings:NseHolidayApiUrl"]
                             ?? "https://www.nseindia.com/api/holiday-master?type=trading";

                _logger.LogInformation("Fetching holidays from NSE API");
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var json     = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var holidays = new HashSet<DateTime>();

                if (doc.RootElement.TryGetProperty("MF", out var mfElement))
                {
                    foreach (var item in mfElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("tradingDate", out var dateProp))
                        {
                            var dateStr = dateProp.GetString();
                            if (!string.IsNullOrWhiteSpace(dateStr) &&
                                DateTime.TryParseExact(dateStr, "dd-MMM-yyyy",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                            {
                                holidays.Add(dt.Date);
                            }
                        }
                    }
                }

                _cache.Set(CacheKey, holidays, TimeSpan.FromHours(24));
                _logger.LogInformation("Fetched {Count} market holidays from NSE", holidays.Count);
                return holidays;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch NSE holidays — returning empty set");
                return new HashSet<DateTime>();
            }
        }

        public async Task RefreshHolidaysAsync()
        {
            _cache.Remove(CacheKey);
            await FetchAllHolidaysAsync();
        }
    }
}
