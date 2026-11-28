using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MutualFund.Investment.Application.Analytics.Queries;
using MutualFundNav.API.Services;
using System.Security.Claims;

namespace MutualFund.Investment.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "CanViewInvestorPage")]
    public class AnalyticsController : BaseController
    {
        private readonly PortfolioAnalyticsQuery _analyticsQuery;
        private readonly NavSyncCoordinator? _navSyncCoordinator;

        public AnalyticsController(
            PortfolioAnalyticsQuery analyticsQuery,
            NavSyncCoordinator? navSyncCoordinator = null)
        {
            _analyticsQuery = analyticsQuery;
            _navSyncCoordinator = navSyncCoordinator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalytics([FromQuery] string? investorUserId = null)
        {
            if (_navSyncCoordinator != null)
            {
                await _navSyncCoordinator.WaitForSyncAsync(
                    TimeSpan.FromSeconds(30), HttpContext.RequestAborted);
            }

            if (!TryResolveInvestorScope(investorUserId, out var resolvedId, out var altPan))
                return Forbid();

            var result = await _analyticsQuery.ExecuteAsync(resolvedId, altPan);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpGet("month-orders")]
        public async Task<IActionResult> GetMonthOrders(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] string? schemeCode = null,
            [FromQuery] string? investorUserId = null)
        {
            if (year < 2000 || month < 1 || month > 12)
                return BadRequest(new { error = "Valid year and month (1-12) are required." });

            if (!TryResolveInvestorScope(investorUserId, out var resolvedId, out var altPan))
                return Forbid();

            var result = await _analyticsQuery.GetMonthOrdersAsync(year, month, schemeCode, resolvedId, altPan);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        private bool TryResolveInvestorScope(
            string? requestedId,
            out string? resolvedId,
            out string altPan)
        {
            var currentPan = User.FindFirstValue("panNumber") ?? string.Empty;
            resolvedId = requestedId;

            // Only Admin and Employee (with CanViewInvestorPage) can view arbitrary investors;
            // Investor ("User") accounts are always scoped to their own UserId / PAN.
            if (!IsAdmin && !IsEmployee)
            {
                if (!string.IsNullOrWhiteSpace(requestedId)
                    && !string.Equals(requestedId, "all", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(requestedId, "family", StringComparison.OrdinalIgnoreCase)
                    && requestedId != CurrentUserId
                    && (string.IsNullOrEmpty(currentPan) || requestedId != currentPan))
                {
                    altPan = string.Empty;
                    return false;
                }

                resolvedId = CurrentUserId;
            }

            altPan = (resolvedId == CurrentUserId) ? currentPan : string.Empty;
            return true;
        }
    }
}
