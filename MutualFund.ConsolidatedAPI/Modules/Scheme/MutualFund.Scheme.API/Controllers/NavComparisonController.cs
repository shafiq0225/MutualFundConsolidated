using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MutualFund.Scheme.Application.UseCases.Queries;
using MutualFund.Scheme.Domain.Exceptions;

namespace MutualFund.Scheme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AllRoles")]
    public class NavComparisonController : ControllerBase
    {
        private readonly GetNavComparisonQuery _query;
        private readonly GetSchemeDetailsQuery _detailsQuery;
        private readonly IMemoryCache _cache;

        private const string DAILY_CACHE_KEY = "navcomparison_daily";
        private static string SchemeDetailKey(string code) => $"scheme_detail_{code}";

        public NavComparisonController(
            GetNavComparisonQuery query,
            GetSchemeDetailsQuery detailsQuery,
            IMemoryCache cache)
        {
            _query = query;
            _detailsQuery = detailsQuery;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetComparison(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate >= endDate)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "dateRange", new[] { "startDate must be earlier than endDate." } }
                });

            var result = await _query.ExecuteAsync(startDate.Date, endDate.Date);
            return Ok(result);
        }

        [HttpGet("daily")]
        public async Task<IActionResult> GetDailyComparison()
        {
            if (!_cache.TryGetValue(DAILY_CACHE_KEY, out object? cachedResult))
            {
                cachedResult = await _query.ExecuteDailyAsync();

                _cache.Set(DAILY_CACHE_KEY, cachedResult, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Priority = CacheItemPriority.High
                });
            }

            return Ok(cachedResult);
        }

        [HttpGet("{schemeCode}/details")]
        public async Task<IActionResult> GetSchemeDetails(string schemeCode)
        {
            if (string.IsNullOrWhiteSpace(schemeCode))
                return BadRequest(new { error = "Scheme code is required." });

            var cacheKey = SchemeDetailKey(schemeCode);

            if (!_cache.TryGetValue(cacheKey, out object? cachedDetail))
            {
                cachedDetail = await _detailsQuery.ExecuteAsync(schemeCode);

                _cache.Set(cacheKey, cachedDetail, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Priority = CacheItemPriority.Normal
                });
            }

            return Ok(cachedDetail);
        }

        /// <summary>
        /// Force-clear the daily NAV comparison cache. Admin only — this
        /// isn't a read, it's a manual trigger for expensive DB reloads,
        /// so it's deliberately not open to Employees/End Users the way
        /// the reads above are.
        /// </summary>
        [HttpPost("daily/refresh")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult RefreshDailyCache()
        {
            _cache.Remove(DAILY_CACHE_KEY);
            return Ok(new { message = "Daily NAV cache cleared. Next request will reload from DB." });
        }
    }
}