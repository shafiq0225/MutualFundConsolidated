using MutualFund.Investment.Application.Portfolio.Commands;
using Microsoft.Extensions.Logging;
using Quartz;

namespace MutualFund.Investment.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Runs daily at 9:00 AM (after App 1 downloads NAV at 8:30 AM).
    /// For every active Holding:
    ///   — Fetches latest NAV from DetailedSchemes (App 2 data)
    ///   — Calculates CurrentValue, P&L, Percentage
    ///   — Saves PortfolioSnapshot record
    /// </summary>
    [DisallowConcurrentExecution]
    // ← Prevents two instances running at the same time
    public class PortfolioSnapshotJob : IJob
    {
        private readonly CalculateSnapshotCommand _command;
        private readonly ILogger<PortfolioSnapshotJob> _logger;

        public PortfolioSnapshotJob(
            CalculateSnapshotCommand command,
            ILogger<PortfolioSnapshotJob> logger)
        {
            _command = command;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(
                "============================================");
            _logger.LogInformation(
                "Portfolio Snapshot Job triggered at {Time}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.LogInformation(
                "============================================");

            try
            {
                var tz = GetIstTimeZone();
                var todayIst = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz).Date;
                var targetDate = todayIst.AddDays(-1);

                var result = await _command.ExecuteAsync(targetDate);

                if (result.IsSuccess && result.Data != null)
                {
                    var summary = result.Data;

                    _logger.LogInformation(
                        "Snapshot Summary — Date: {Date} | " +
                        "Holdings: {Total} | " +
                        "Calculated: {Calc} | " +
                        "Skipped: {Skip} | " +
                        "No NAV: {NoNav}",
                        summary.SnapshotDate.ToString("yyyy-MM-dd"),
                        summary.TotalHoldings,
                        summary.Calculated,
                        summary.Skipped,
                        summary.NoNavFound);

                    _logger.LogInformation(
                        "Portfolio Totals — " +
                        "Invested: ₹{Invested:N2} | " +
                        "Current Value: ₹{Value:N2} | " +
                        "P&L: ₹{PL:N2}",
                        summary.TotalInvested,
                        summary.TotalValue,
                        summary.TotalProfitLoss);
                }
                else
                {
                    _logger.LogError(
                        "Snapshot job failed: {Error}",
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled error in PortfolioSnapshotJob");

                // Rethrow so Quartz marks it as failed
                throw new JobExecutionException(ex);
            }

            _logger.LogInformation(
                "============================================");
            _logger.LogInformation("Portfolio Snapshot Job Completed");
            _logger.LogInformation(
                "============================================");
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
    }
}