using MutualFund.ConsolidatedAPI.Modules.Messaging.Tools;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Domain.Entities;

namespace MutualFund.ConsolidatedAPI.Modules.Messaging.Workers
{
    public class DailyDigestWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyDigestWorker> _logger;

        public DailyDigestWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<DailyDigestWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 DailyDigestWorker background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var istZone = GetIstTimeZone();
                    var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                    var todayTargetTime = istNow.Date.AddHours(5).AddMinutes(5); // 05:05 AM IST

                    // 1. Check database log to verify if today's digest has already been sent
                    bool alreadySentToday = false;
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        alreadySentToday = await uow.JobLogs.HasJobRunOnDateAsync("DailyDigestWorker", istNow.Date);
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogWarning(dbEx, "Failed to query JobExecutionLog for DailyDigestWorker — assuming not sent today.");
                    }

                    // 2. If 05:05 AM IST has passed today AND it hasn't been sent yet today, trigger dispatch
                    if (istNow >= todayTargetTime && !alreadySentToday)
                    {
                        _logger.LogInformation("⏰ Triggering daily digest dispatch for {Date:yyyy-MM-dd} (Current IST Time: {Time:HH:mm:ss})...", istNow.Date, istNow);
                        await RunDispatchAsync(stoppingToken);
                    }
                    else if (alreadySentToday)
                    {
                        _logger.LogInformation("ℹ️ Daily digest for {Date:yyyy-MM-dd} has already been dispatched today. Skipping duplicate dispatch.", istNow.Date);
                    }

                    // 3. Compute next 05:05 AM IST run time
                    var nextRun = todayTargetTime;
                    if (istNow >= todayTargetTime)
                    {
                        nextRun = todayTargetTime.AddDays(1);
                    }

                    var delay = nextRun - istNow;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

                    _logger.LogInformation("🗓️ Next Daily Digest scheduled for {NextRun} IST (in {Delay:hh\\:mm\\:ss})", nextRun, delay);

                    // Wait until next scheduled time (or until cancellation)
                    await Task.Delay(delay, stoppingToken);

                    // Execute when timer wakes up
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        var currentIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                        bool ranOnWakeup = false;
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                            ranOnWakeup = await uow.JobLogs.HasJobRunOnDateAsync("DailyDigestWorker", currentIst.Date);
                        }
                        catch (Exception dbEx)
                        {
                            _logger.LogWarning(dbEx, "Failed to query JobExecutionLog on timer wakeup.");
                        }

                        if (!ranOnWakeup)
                        {
                            _logger.LogInformation("⏰ Scheduled 05:05 AM IST timer fired. Executing daily digest dispatch...");
                            await RunDispatchAsync(stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DailyDigestWorker execution loop. Retrying in 60 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }

            _logger.LogInformation("DailyDigestWorker background service stopped.");
        }

        private async Task RunDispatchAsync(CancellationToken stoppingToken)
        {
            var startedAt = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool success = false;
            string? error = null;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mcpTools = scope.ServiceProvider.GetRequiredService<MessagingMcpTools>();

                _logger.LogInformation("Sending Telegram daily digest...");
                var tgResult = await mcpTools.SendDailyDigestToTelegramAsync();
                _logger.LogInformation("Telegram Daily Digest result: {Result}", tgResult ? "SUCCESS" : "FAILED");

                _logger.LogInformation("Sending WhatsApp daily digest...");
                var waResult = await mcpTools.SendDailyDigestToWhatsAppAsync();
                _logger.LogInformation("WhatsApp Daily Digest result: {Result}", waResult ? "SUCCESS" : "FAILED");

                success = tgResult || waResult;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _logger.LogError(ex, "Error executing RunDispatchAsync in DailyDigestWorker");
            }
            finally
            {
                stopwatch.Stop();
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    await uow.JobLogs.AddAsync(new JobExecutionLog
                    {
                        JobName = "DailyDigestWorker.Dispatch",
                        StartedAt = startedAt,
                        CompletedAt = DateTime.UtcNow,
                        IsSuccess = success,
                        ErrorMessage = error,
                        ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
                        Details = "Daily digest dispatch to Telegram & WhatsApp"
                    });
                    await uow.CompleteAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to persist JobExecutionLog for DailyDigestWorker");
                }
            }
        }

        private static TimeZoneInfo GetIstTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time");
                }
            }
        }
    }
}
