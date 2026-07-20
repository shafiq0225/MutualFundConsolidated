using MutualFundNav.Application.UseCases.Commands;
using MutualFundNav.Domain.Contracts;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.API.Workers
{
    public class NavDownloadWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<NavDownloadWorker> _logger;

        public NavDownloadWorker(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<NavDownloadWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NavDownloadWorker started");

            // ── Feature 1: Missed-run detection on startup ─────────────────
            await CheckAndRunMissedJobAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var next = ComputeNextRun();
                    var delay = next - DateTime.Now;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

                    _logger.LogInformation(
                        "Next NAV download scheduled at {NextRun} (in {Delay:hh\\:mm\\:ss})",
                        next, delay);

                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                        await RunJobAsync("NavDownloadWorker.Scheduled", stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "NavDownloadWorker loop fault — restarting in 60s");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }

            _logger.LogInformation("NavDownloadWorker stopped");
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// On startup, check if today's scheduled run was missed.
        /// A run is considered missed if:
        ///   - Today's schedule time has already passed
        ///   - AND no NAV data exists for today's target date
        ///   - AND no successful job ran today
        /// </summary>
        private async Task CheckAndRunMissedJobAsync(CancellationToken ct)
        {
            try
            {
                var tz = GetIstTimeZone();
                var nowIst = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
                var scheduleTime = TimeSpan.Parse(_config["AppSettings:ScheduleTime"] ?? "08:30:00");
                var scheduledToday = nowIst.Date.Add(scheduleTime);

                // If scheduled time hasn't passed yet — nothing missed
                if (nowIst < scheduledToday)
                {
                    _logger.LogInformation(
                        "Startup check: scheduled time {Time} not yet reached — no missed run",
                        scheduledToday.ToString("HH:mm"));
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var dateHelper = scope.ServiceProvider.GetRequiredService<IDateHelper>();

                // Check if a job already ran successfully today
                var latestJob = await uow.JobLogs.GetLatestAsync();
                bool jobRanToday = latestJob is not null
                    && TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(latestJob.StartedAt, DateTimeKind.Utc), tz).Date == nowIst.Date
                    && latestJob.IsSuccess;

                if (jobRanToday)
                {
                    _logger.LogInformation(
                        "Startup check: job already ran today at {Time} — no missed run",
                        latestJob!.StartedAt.ToString("HH:mm:ss"));
                    return;
                }

                // Check if NAV data exists for the target date
                var targetDate = await dateHelper.GetTargetNavDateAsync();
                bool dataExists = await uow.NavFiles.ExistsByDateAsync(targetDate);

                if (dataExists)
                {
                    _logger.LogInformation(
                        "Startup check: NAV data for {Date} already exists — no action needed",
                        targetDate.ToString("yyyy-MM-dd"));
                    return;
                }

                // Missed run confirmed — execute now
                _logger.LogWarning(
                    "Startup check: MISSED RUN detected! Scheduled at {Scheduled}, " +
                    "target date {Date} has no data. Running now...",
                    scheduledToday.ToString("HH:mm"), targetDate.ToString("yyyy-MM-dd"));

                await RunJobAsync("NavDownloadWorker.MissedRun", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Startup missed-run check failed — will run on next schedule");
            }
        }

        private async Task RunJobAsync(string jobName, CancellationToken ct)
        {
            var startedAt = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool success = false;
            string? error = null;

            _logger.LogInformation(
                "========== {JobName} Started at {Time} ==========",
                jobName, DateTime.Now);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                var dateHelper = sp.GetRequiredService<IDateHelper>();
                var command = sp.GetRequiredService<DownloadAndStoreNavCommand>();
                var uow = sp.GetRequiredService<IUnitOfWork>();
                var kafkaPublisher = sp.GetRequiredService<IKafkaPublisher<MarketHolidayEvent>>();

                // Publish any market-holiday event (and persist Kafka publish log)
                await PublishHolidayIfTodayIsHolidayAsync(dateHelper, kafkaPublisher, uow, jobName);

                var targetDate = await dateHelper.GetTargetNavDateAsync();
                _logger.LogInformation("Target NAV date: {Date}", targetDate.ToString("yyyy-MM-dd"));

                var result = await command.ExecuteAsync(
                    targetDate,
                    kafkaTopic: _config["Kafka:Topics:NavFileProcessed"] ?? "nav-file-processed",
                    ct: ct,
                    triggerSource: jobName);

                if (result.IsSuccess)
                {
                    success = true;
                    _logger.LogInformation(result.Data
                        ? "NAV downloaded and stored for {Date}"
                        : "NAV data already exists for {Date}",
                        targetDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    error = result.ErrorMessage;
                    _logger.LogError("Command failed: {Error}", error);
                }

                stopwatch.Stop();
                await PersistJobLogAsync(uow, jobName, startedAt, stopwatch.Elapsed, success, error,
                    $"Target: {targetDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                error = ex.Message;
                _logger.LogCritical(ex, "Unexpected error in {JobName}", jobName);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    await PersistJobLogAsync(uow, jobName, startedAt, stopwatch.Elapsed,
                        false, ex.Message, null);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to persist job error log");
                }
            }
            finally
            {
                _logger.LogInformation(
                    "========== {JobName} Completed — Success: {Success}, Elapsed: {Elapsed:F2}s ==========",
                    jobName, success, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private async Task PublishHolidayIfTodayIsHolidayAsync(
            IDateHelper dateHelper,
            IKafkaPublisher<MarketHolidayEvent> kafkaPublisher,
            IUnitOfWork uow,
            string triggerSource)
        {
            var today = DateTime.Today;
            if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return;

            bool isTradingDay = await dateHelper.IsTradingDayAsync(today);
            if (isTradingDay) return;

            _logger.LogInformation(
                "Today ({Date}) is a market holiday — publishing MarketHolidayEvent",
                today.ToString("yyyy-MM-dd"));

            try
            {
                var topic = _config["Kafka:Topics:MarketHoliday"] ?? "market-holidays";
                var publishResult = await kafkaPublisher.PublishAsync(
                    topic: topic,
                    key: today.ToString("yyyy-MM-dd"),
                    message: new MarketHolidayEvent
                    {
                        HolidayDate = today,
                        PublishedAt = DateTime.UtcNow
                    });

                // Persist Kafka publish audit record
                try
                {
                    await uow.KafkaPublishLogs.AddAsync(new Domain.Entities.KafkaPublishLog
                    {
                        Topic = topic,
                        EventType = "MarketHoliday",
                        MessageKey = today.ToString("yyyy-MM-dd"),
                        MessageSizeBytes = publishResult.MessageSizeBytes,
                        IsSuccess = publishResult.IsSuccess,
                        ErrorMessage = publishResult.ErrorMessage,
                        PublishedAt = DateTime.UtcNow,
                        ElapsedMs = publishResult.ElapsedMs,
                        TriggerSource = triggerSource,
                        NavDate = null,
                        Partition = publishResult.Partition,
                        Offset = publishResult.Offset
                    });
                    await uow.CompleteAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to persist KafkaPublishLog for MarketHoliday {Date}", today.ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish MarketHolidayEvent for {Date}",
                    today.ToString("yyyy-MM-dd"));
            }
        }

        private static async Task PersistJobLogAsync(
            IUnitOfWork uow,
            string jobName,
            DateTime startedAt,
            TimeSpan elapsed,
            bool success,
            string? error,
            string? details)
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
            await uow.JobLogs.AddAsync(log);
            await uow.CompleteAsync();
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

        private DateTime ComputeNextRun()
        {
            var tz = GetIstTimeZone();
            var nowIst = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            var scheduleTime = TimeSpan.Parse(
                _config["AppSettings:ScheduleTime"] ?? "08:30:00");
            var nextIst = nowIst.Date.Add(scheduleTime);
            if (nowIst >= nextIst) nextIst = nextIst.AddDays(1);
            var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextIst, tz);
            return TimeZoneInfo.ConvertTimeFromUtc(nextUtc, TimeZoneInfo.Local);
        }
    }
}