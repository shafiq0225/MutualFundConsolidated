using MutualFund.Investment.Application.Family.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MutualFund.Investment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "CanViewInvestorPage")]
    public class FamilyPortfolioController : BaseController
    {
        private readonly FamilyPortfolioQuery _query;
        private readonly ILogger<FamilyPortfolioController> _logger;

        public FamilyPortfolioController(
            FamilyPortfolioQuery query,
            ILogger<FamilyPortfolioController> logger)
        {
            _query = query;
            _logger = logger;
        }

        /// <summary>
        /// Screen 1 — Family Overview
        /// GET /api/family/portfolio
        /// Total family value + all members summary
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFamilyOverview()
        {
            var result = await _query.GetFamilyOverviewAsync();

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        /// <summary>
        /// Screen 2 — Member Holdings
        /// GET /api/family/portfolio/{userId}
        /// One member's holdings with quick period returns
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetMemberHoldings(string userId)
        {
            // Non-admin can only see own holdings
            if (!CanViewAllPortfolioData && userId != CurrentUserId)
                return Forbid();

            var result = await _query.GetMemberHoldingsAsync(userId);

            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        /// <summary>
        /// Screen 3 — Scheme Detail
        /// GET /api/family/portfolio/{userId}/scheme/{schemeCode}
        /// Full scheme detail: units, avg NAV, daily/weekly/period returns
        /// </summary>
        [HttpGet("{userId}/scheme/{schemeCode}")]
        public async Task<IActionResult> GetSchemeDetail(
            string userId,
            string schemeCode)
        {
            if (!CanViewAllPortfolioData && userId != CurrentUserId)
                return Forbid();

            var result = await _query
                .GetSchemeDetailAsync(userId, schemeCode);

            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}