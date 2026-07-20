using Microsoft.AspNetCore.Mvc;
using MutualFundNav.Application.UseCases.Commands;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NavController : ControllerBase
    {
        private readonly DownloadAndStoreNavCommand _command;
        private readonly UpsertNavCommand _upsertCommand;
        private readonly IDateHelper _dateHelper;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly ILogger<NavController> _logger;

        public NavController(
            DownloadAndStoreNavCommand command,
            UpsertNavCommand upsertCommand,
            IDateHelper dateHelper,
            IUnitOfWork uow,
            IConfiguration config,
            ILogger<NavController> logger)
        {
            _command = command;
            _upsertCommand = upsertCommand;
            _dateHelper = dateHelper;
            _uow = uow;
            _config = config;
            _logger = logger;
        }

        private string NavTopic =>
            _config["Kafka:Topics:NavFileProcessed"] ?? "nav-file-processed";

        /// <summary>
        /// Manually trigger a NAV download for the last trading date.
        /// Also persists a JobExecutionLog entry for audit trail.
        /// </summary>
        [HttpPost("trigger")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TriggerDownload([FromQuery] bool replace = false, CancellationToken ct = default)
        {
            var startedAt = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var targetDate = await _dateHelper.GetTargetNavDateAsync();
            var latest = await _uow.NavFiles.GetLatestDateAsync();
            if (latest.HasValue && targetDate <= latest.Value && !replace)
            {
                targetDate = latest.Value.AddDays(1);
            }

            _logger.LogInformation("Manual trigger for NAV date {Date}",
                targetDate.ToString("yyyy-MM-dd"));

            if (replace)
            {
                var upsertResult = await _upsertCommand.ExecuteAsync(targetDate, NavTopic, "NavController.ManualUpsert", ct: ct);

                stopwatch.Stop();
                await PersistJobLogAsync(
                    "NavController.ManualUpsert",
                    startedAt, stopwatch.Elapsed,
                    upsertResult.IsSuccess,
                    upsertResult.IsSuccess ? null : upsertResult.ErrorMessage,
                    $"Manual upsert for {targetDate:yyyy-MM-dd}");

                return upsertResult.IsSuccess
                    ? Ok(new
                    {
                        date = targetDate.ToString("yyyy-MM-dd"),
                        wasReplaced = upsertResult.Data.WasReplaced,
                        recordCount = upsertResult.Data.RecordCount,
                        checksum = upsertResult.Data.Checksum,
                        kafkaPublished = upsertResult.Data.KafkaPublished,
                        kafkaError = upsertResult.Data.KafkaErrorMessage
                    })
                    : BadRequest(new { error = upsertResult.ErrorMessage });
            }

            var result = await _command.ExecuteAsync(
                targetDate,
                NavTopic,
                ct: ct,
                triggerSource: "NavController.ManualTrigger",
                allowReprocess: true);

            stopwatch.Stop();
            await PersistJobLogAsync(
                "NavController.ManualTrigger",
                startedAt, stopwatch.Elapsed,
                result.IsSuccess,
                result.IsSuccess ? null : result.ErrorMessage,
                $"Manual trigger for {targetDate:yyyy-MM-dd}");

            return result.IsSuccess
                ? Ok(new
                {
                    date = targetDate.ToString("yyyy-MM-dd"),
                    wasStored = result.Data,
                    message = result.Data
                                    ? "NAV downloaded and stored successfully"
                                    : "NAV data already exists for this date"
                })
                : BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>
        /// Trigger a NAV download for a specific date (yyyy-MM-dd).
        /// </summary>
        [HttpPost("trigger/{date:datetime}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TriggerDownloadForDate(DateTime date, [FromQuery] bool replace = false, CancellationToken ct = default)
        {
            var startedAt = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _logger.LogInformation("Manual trigger for specific date {Date}",
                date.ToString("yyyy-MM-dd"));

            if (replace)
            {
                var upsertResult = await _upsertCommand.ExecuteAsync(date, NavTopic, "NavController.ManualUpsert", ct: ct);

                stopwatch.Stop();
                await PersistJobLogAsync(
                    "NavController.ManualUpsert",
                    startedAt, stopwatch.Elapsed,
                    upsertResult.IsSuccess,
                    upsertResult.IsSuccess ? null : upsertResult.ErrorMessage,
                    $"Manual upsert for specific date {date:yyyy-MM-dd}");

                return upsertResult.IsSuccess
                    ? Ok(new { date = date.ToString("yyyy-MM-dd"), wasReplaced = upsertResult.Data.WasReplaced })
                    : BadRequest(new { error = upsertResult.ErrorMessage });
            }

            var result = await _command.ExecuteAsync(
                date,
                NavTopic,
                ct: ct,
                triggerSource: "NavController.ManualTriggerForDate",
                allowReprocess: true);

            stopwatch.Stop();
            await PersistJobLogAsync(
                "NavController.ManualTrigger",
                startedAt, stopwatch.Elapsed,
                result.IsSuccess,
                result.IsSuccess ? null : result.ErrorMessage,
                $"Manual trigger for specific date {date:yyyy-MM-dd}");

            return result.IsSuccess
                ? Ok(new { date = date.ToString("yyyy-MM-dd"), wasStored = result.Data })
                : BadRequest(new { error = result.ErrorMessage });
        }

        /// <summary>Returns the last trading date that would be downloaded.</summary>
        [HttpGet("target-date")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTargetDate()
        {
            var date = await _dateHelper.GetTargetNavDateAsync();
            var latest = await _uow.NavFiles.GetLatestDateAsync();
            if (latest.HasValue && date <= latest.Value)
            {
                date = latest.Value.AddDays(1);
            }
            return Ok(new { targetDate = date.ToString("yyyy-MM-dd") });
        }

        /// <summary>Returns the latest NAV date stored in the database.</summary>
        [HttpGet("latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatest()
        {
            var latest = await _uow.NavFiles.GetLatestDateAsync();
            return latest.HasValue
                ? Ok(new { latestNavDate = latest.Value.ToString("yyyy-MM-dd") })
                : NotFound(new { message = "No NAV data stored yet" });
        }

        /// <summary>Returns all stored NAV dates, most recent first.</summary>
        [HttpGet("dates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllDates()
        {
            var dates = await _uow.NavFiles.GetAllDatesAsync();
            return Ok(dates.Select(d => d.ToString("yyyy-MM-dd")));
        }

        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _uow.NavFiles.GetAllSummariesAsync();
            return Ok(history.Select(f => new
            {
                id = f.Id,
                navDate = f.NavDate.ToString("yyyy-MM-dd"),
                fileSizeBytes = f.FileSizeBytes,
                recordCount = f.RecordCount,
                checksum = f.Checksum,
                downloadedAt = f.DownloadedAt.ToString("o"),
                isHoliday = f.IsHoliday
            }));
        }

        /// <summary>
        /// Retrieve NAV file content by the DownloadedAt date (UTC date part match).
        /// Example: GET /api/nav/content?downloadedAt=2026-06-24
        /// </summary>
        [HttpGet("content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContent([FromQuery] DateTime? downloadedAt = null)
        {
            var queryDate = (downloadedAt ?? DateTime.UtcNow).Date;
            var nav = await _uow.NavFiles.GetByDownloadedAtAsync(queryDate);
            if (nav is null) return NotFound(new { message = "No NAV file found for the specified DownloadedAt date" });

            return Ok(new
            {
                navDate = nav.NavDate.ToString("yyyy-MM-dd"),
                downloadedAt = nav.DownloadedAt.ToString("o"),
                fileContent = nav.FileContent,
                recordCount = nav.RecordCount,
                sizeBytes = nav.FileSizeBytes,
                checksum = nav.Checksum
            });
        }

        // ── Private helpers ────────────────────────────────────────────────
        private async Task PersistJobLogAsync(
            string jobName,
            DateTime startedAt,
            TimeSpan elapsed,
            bool success,
            string? error,
            string? details = null)
        {
            try
            {
                var log = new JobExecutionLog
                {
                    JobName = jobName,
                    StartedAt = startedAt,
                    CompletedAt = DateTime.UtcNow,
                    IsSuccess = success,
                    ErrorMessage = error,
                    ElapsedSeconds = elapsed.TotalSeconds,
                    Details = details
                };

                await _uow.JobLogs.AddAsync(log);
                await _uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                // Log persistence failure must never break the API response
                _logger.LogError(ex, "Failed to persist JobExecutionLog for {Job}", jobName);
            }
        }
    }
}