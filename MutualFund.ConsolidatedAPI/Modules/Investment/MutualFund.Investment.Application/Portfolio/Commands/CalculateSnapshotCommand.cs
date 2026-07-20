using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Portfolio.Commands
{
    public class CalculateSnapshotCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INavRateService _navRateService;
        private readonly ILogger<CalculateSnapshotCommand> _logger;

        public CalculateSnapshotCommand(
            IUnitOfWork unitOfWork,
            INavRateService navRateService,
            ILogger<CalculateSnapshotCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _navRateService = navRateService;
            _logger = logger;
        }

        /// <summary>
        /// Runs daily at 9 AM (after App 1 downloads NAV at 8:30 AM).
        /// For each active Holding:
        ///   1. Fetch latest NAV from DetailedSchemes (App 2 data)
        ///   2. Calculate: CurrentValue, P&L, %
        ///   3. Save PortfolioSnapshot
        /// </summary>
        public async Task<Result<SnapshotSummaryDto>> ExecuteAsync(
            DateTime? snapshotDate = null)
        {
            var date = snapshotDate?.Date ?? DateTime.Today;

            _logger.LogInformation(
                "====== Portfolio Snapshot Job Started — {Date} ======",
                date.ToString("yyyy-MM-dd"));

            try
            {
                // ── Clear existing snapshots for this date to enable recalculation ──
                _logger.LogInformation("Clearing existing snapshots on {Date} to enable recalculation...", date.ToString("yyyy-MM-dd"));
                await _unitOfWork.Portfolio.DeleteSnapshotsForDateAsync(date);
                await _unitOfWork.CompleteAsync();

                // ── Get all active holdings ────────────────────────
                var holdings = await _unitOfWork.Holdings
                    .GetAllActiveAsync();

                var holdingList = holdings.ToList();

                if (!holdingList.Any())
                {
                    _logger.LogInformation(
                        "No active holdings found. Snapshot skipped.");

                    return Result<SnapshotSummaryDto>.Success(
                        new SnapshotSummaryDto { SnapshotDate = date });
                }

                _logger.LogInformation(
                    "Processing {Count} active holdings...",
                    holdingList.Count);

                // ── Get latest NAV via interface ───────────────────
                var schemeCodes = holdingList
                    .Select(h => h.SchemeCode)
                    .Distinct()
                    .ToList();

                // INavRateService is injected — implemented in Infrastructure
                var latestNavMap = await _navRateService
                    .GetLatestNavAsync(schemeCodes);

                // ── Calculate snapshots ────────────────────────────
                var snapshots = new List<PortfolioSnapshot>();
                var skipped = 0;
                var noNavCount = 0;

                foreach (var holding in holdingList)
                {

                    // Get current NAV for this scheme
                    if (!latestNavMap.TryGetValue(
                            holding.SchemeCode, out var currentNAV)
                        || currentNAV <= 0)
                    {
                        _logger.LogWarning(
                            "No NAV found for scheme {Code} — {Name}",
                            holding.SchemeCode, holding.SchemeName);
                        noNavCount++;
                        continue;
                    }

                    // ── P&L Calculation ────────────────────────────
                    var currentValue = Math.Round(
                        holding.Units * currentNAV, 2);

                    var profitLoss = Math.Round(
                        currentValue - holding.InvestedAmount, 2);

                    var profitLossPercent = holding.InvestedAmount > 0
                        ? Math.Round(
                            (profitLoss / holding.InvestedAmount) * 100, 4)
                        : 0;

                    snapshots.Add(new PortfolioSnapshot
                    {
                        HoldingId = holding.Id,
                        InvestorUserId = holding.InvestorUserId,
                        InvestorName = holding.InvestorName,
                        SchemeCode = holding.SchemeCode,
                        SchemeName = holding.SchemeName,
                        FundName = holding.FundName,
                        SnapshotDate = date,
                        CurrentNAV = currentNAV,
                        InvestedAmount = holding.InvestedAmount,
                        CurrentValue = currentValue,
                        ProfitLoss = profitLoss,
                        ProfitLossPercent = profitLossPercent,
                        CreatedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation(
                        "  {Investor} | {Scheme} | " +
                        "NAV={NAV} | Value={Value} | " +
                        "P&L={PL} ({Pct}%)",
                        holding.InvestorName,
                        holding.SchemeName,
                        currentNAV,
                        currentValue,
                        profitLoss,
                        profitLossPercent);
                }

                // ── Save all snapshots in one batch ────────────────
                if (snapshots.Any())
                {
                    await _unitOfWork.Portfolio
                        .AddSnapshotRangeAsync(snapshots);
                    await _unitOfWork.CompleteAsync();
                }

                var summary = new SnapshotSummaryDto
                {
                    SnapshotDate = date,
                    TotalHoldings = holdingList.Count,
                    Calculated = snapshots.Count,
                    Skipped = skipped,
                    NoNavFound = noNavCount,
                    TotalInvested = snapshots.Sum(s => s.InvestedAmount),
                    TotalValue = snapshots.Sum(s => s.CurrentValue),
                    TotalProfitLoss = snapshots.Sum(s => s.ProfitLoss)
                };

                _logger.LogInformation(
                    "====== Snapshot Complete — " +
                    "Calculated={Calc} Skipped={Skip} NoNAV={NoNav} " +
                    "TotalValue={Value} TotalP&L={PL} ======",
                    summary.Calculated,
                    summary.Skipped,
                    summary.NoNavFound,
                    summary.TotalValue,
                    summary.TotalProfitLoss);

                return Result<SnapshotSummaryDto>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error calculating portfolio snapshots for {Date}", date);
                return Result<SnapshotSummaryDto>
                    .Failure($"Snapshot failed: {ex.Message}");
            }
        }
    }

    // ── Snapshot summary DTO ───────────────────────────────────────
    public class SnapshotSummaryDto
    {
        public DateTime SnapshotDate { get; set; }
        public int TotalHoldings { get; set; }
        public int Calculated { get; set; }
        public int Skipped { get; set; }
        public int NoNavFound { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
    }
}