using Microsoft.Extensions.Configuration;
using Quartz;

namespace MutualFund.Investment.Infrastructure.BackgroundJobs
{
    public static class QuartzConfiguration
    {
        public static void ConfigureJobs(
            IServiceCollectionQuartzConfigurator quartz,
            IConfiguration configuration)
        {
            // ── Portfolio Snapshot Job ─────────────────────────────
            var jobKey = new JobKey(
                "PortfolioSnapshotJob",
                "Investment");

            quartz.AddJob<PortfolioSnapshotJob>(opts =>
                opts.WithIdentity(jobKey)
                    .WithDescription(
                        "Calculates daily P&L for all active holdings."));

            // ── Parse schedule time from config ────────────────────
            var scheduleTime = configuration[
                "AppSettings:SnapshotScheduleTime"] ?? "9:00:00";

            var parts = scheduleTime.Split(':');
            var hour = int.Parse(parts[0]);
            var minute = int.Parse(parts[1]);

            // ── Cron trigger ───────────────────────────────────────
            quartz.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("PortfolioSnapshotTrigger", "Investment")
                .WithDescription(
                    $"Fires daily at {hour:D2}:{minute:D2}")
                .WithCronSchedule(
                    $"0 {minute} {hour} * * ?",
                    x => x.InTimeZone(GetIstTimeZone())
                          .WithMisfireHandlingInstructionFireAndProceed())
                .StartNow());
        }

        private static TimeZoneInfo GetIstTimeZone()
        {
            // India Standard Time
            // Windows ID differs from Linux ID
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "India Standard Time");   // ← Windows
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Kolkata");          // ← Linux / Azure
            }
        }
    }
}