using MutualFund.ConsolidatedAPI.Modules.Messaging.Tools;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MutualFund.ConsolidatedAPI.Modules.Messaging.Workers
{
    public class DailyDigestWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyDigestWorker> _logger;
        private static DateTime _lastSentDate = DateTime.MinValue;

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

                    // 1. Check if today's 05:05 AM IST has passed and we haven't sent today's digest yet
                    if (istNow >= todayTargetTime && _lastSentDate.Date < istNow.Date)
                    {
                        _logger.LogInformation("⏰ Triggering daily digest dispatch for {Date:yyyy-MM-dd} (Current IST Time: {Time:HH:mm:ss})...", istNow.Date, istNow);
                        await RunDispatchAsync(stoppingToken);
                        _lastSentDate = istNow.Date;
                    }

                    // 2. Compute next 05:05 AM IST run time
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
                        if (_lastSentDate.Date < currentIst.Date)
                        {
                            _logger.LogInformation("⏰ Scheduled 05:05 AM IST timer fired. Executing daily digest dispatch...");
                            await RunDispatchAsync(stoppingToken);
                            _lastSentDate = currentIst.Date;
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing RunDispatchAsync in DailyDigestWorker");
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
