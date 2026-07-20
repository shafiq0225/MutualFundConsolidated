using MutualFund.Investment.Application.Portfolio.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace MutualFund.Investment.API.Controllers
{
    /// <summary>
    /// Allows manual triggering of background jobs.
    /// Used for testing and on-demand recalculation.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly CalculateSnapshotCommand _snapshotCommand;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            CalculateSnapshotCommand snapshotCommand,
            ISchedulerFactory schedulerFactory,
            ILogger<JobsController> logger)
        {
            _snapshotCommand = snapshotCommand;
            _schedulerFactory = schedulerFactory;
            _logger = logger;
        }

        /// <summary>
        /// Manually trigger portfolio snapshot calculation.
        /// Useful for testing or recalculating after NAV update.
        /// </summary>
        [HttpPost("snapshot")]
        [Authorize(Policy = "CanRunSnapshot")]
        public async Task<IActionResult> TriggerSnapshot(
            [FromQuery] DateTime? date = null)
        {
            var targetDate = date?.Date ?? DateTime.Today;

            _logger.LogInformation(
                "Manual snapshot trigger for date: {Date}",
                targetDate.ToString("yyyy-MM-dd"));

            var result = await _snapshotCommand.ExecuteAsync(targetDate);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new
            {
                message = "Snapshot calculated successfully",
                snapshotDate = result.Data!.SnapshotDate
                    .ToString("yyyy-MM-dd"),
                totalHoldings = result.Data.TotalHoldings,
                calculated = result.Data.Calculated,
                skipped = result.Data.Skipped,
                noNavFound = result.Data.NoNavFound,
                totalInvested = result.Data.TotalInvested,
                totalValue = result.Data.TotalValue,
                totalProfitLoss = result.Data.TotalProfitLoss
            });
        }

        /// <summary>
        /// Health check — reports the ACTUAL scheduler/trigger state from
        /// Quartz, rather than a hardcoded description. Previously this
        /// endpoint always claimed "9:00 AM IST" regardless of what
        /// SnapshotScheduleTime was configured to (or whether the job was
        /// even registered), which masked the underlying scheduling bug.
        /// </summary>
        [HttpGet("status")]
        [Authorize(Policy = "CanViewInvestorPage")]
        public async Task<IActionResult> Status()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var triggerKey = new TriggerKey(
                "PortfolioSnapshotTrigger", "Investment");

            var trigger = await scheduler.GetTrigger(triggerKey);

            if (trigger is null)
            {
                return Ok(new
                {
                    status = "NotScheduled",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    message = "PortfolioSnapshotJob has no registered trigger. " +
                              "The job will not run automatically."
                });
            }

            var nextFireTimeUtc = trigger.GetNextFireTimeUtc();
            var nextFireTimeLocal = nextFireTimeUtc?.ToLocalTime();
            var cronDescription = trigger is ICronTrigger cronTrigger
                ? cronTrigger.CronExpressionString
                : null;

            return Ok(new
            {
                status = scheduler.IsStarted ? "Running" : "SchedulerNotStarted",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                cronExpression = cronDescription,
                nextFireTimeLocal = nextFireTimeLocal?
                    .ToString("yyyy-MM-dd HH:mm:ss zzz"),
                message = nextFireTimeLocal is not null
                    ? $"Portfolio snapshot job next fires at {nextFireTimeLocal:yyyy-MM-dd HH:mm} (local)."
                    : "Trigger is registered but has no upcoming fire time " +
                      "(may be paused or completed)."
            });
        }
    }
}