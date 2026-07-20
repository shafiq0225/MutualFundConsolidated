using MutualFund.Investment.Application.Portfolio.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MutualFund.Investment.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class PortfolioController : BaseController
    {
        private readonly GetPortfolioQuery _getPortfolio;
        private readonly GetFamilyPortfolioQuery _getFamilyPortfolio;
        private readonly GetAllHoldingsQuery _getAllHoldings;
        private readonly ILogger<PortfolioController> _logger;

        public PortfolioController(
            GetPortfolioQuery getPortfolio,
            GetFamilyPortfolioQuery getFamilyPortfolio,
            GetAllHoldingsQuery getAllHoldings,
            ILogger<PortfolioController> logger)
        {
            _getPortfolio = getPortfolio;
            _getFamilyPortfolio = getFamilyPortfolio;
            _getAllHoldings = getAllHoldings;
            _logger = logger;
        }

        // ── GET /api/portfolio/me ──────────────────────────────────
        /// <summary>
        /// Get my own portfolio report.
        /// Available to all roles.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Policy = "AnyRole")]
        public async Task<IActionResult> GetMyPortfolio(
            [FromQuery] DateTime? asOfDate = null)
        {
            var result = await _getPortfolio.ExecuteAsync(
                CurrentUserId,
                CurrentUserName,
                asOfDate);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/portfolio/investor/{userId} ───────────────────
        /// <summary>
        /// Get portfolio report for a specific investor.
        /// Admin/Employee only.
        /// </summary>
        [HttpGet("investor/{userId}")]
        [Authorize(Policy = "CanViewAllPortfolio")]
        public async Task<IActionResult> GetByInvestor(
            string userId,
            [FromQuery] string? investorName = null,
            [FromQuery] DateTime? asOfDate = null)
        {
            var result = await _getPortfolio.ExecuteAsync(
                userId,
                investorName ?? userId,
                asOfDate);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/portfolio/family ──────────────────────────────
        /// <summary>
        /// Get combined portfolio for all family members.
        /// Phase 1: All roles can see all investments.
        /// </summary>
        [HttpGet("family")]
        [Authorize(Policy = "CanViewInvestorPage")]
        public async Task<IActionResult> GetFamilyPortfolio(
            [FromQuery] DateTime? asOfDate = null)
        {
            var result = await _getFamilyPortfolio.ExecuteAsync(asOfDate);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/portfolio/holdings ────────────────────────────
        /// <summary>
        /// Get all active holdings with latest P&L values.
        /// Admin/Employee only — full list view.
        /// </summary>
        [HttpGet("holdings")]
        [Authorize(Policy = "CanViewAllPortfolio")]
        public async Task<IActionResult> GetAllHoldings()
        {
            var result = await _getAllHoldings.ExecuteAsync();

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}