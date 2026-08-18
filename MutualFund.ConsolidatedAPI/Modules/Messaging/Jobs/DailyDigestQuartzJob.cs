using Quartz;
using Microsoft.Extensions.Logging;
using MutualFund.ConsolidatedAPI.Modules.Messaging.Tools;

namespace MutualFund.ConsolidatedAPI.Modules.Messaging.Jobs
{
    public class DailyDigestQuartzJob : IJob
    {
        private readonly MessagingMcpTools _mcpTools;
        private readonly ILogger<DailyDigestQuartzJob> _logger;

        public DailyDigestQuartzJob(
            MessagingMcpTools mcpTools,
            ILogger<DailyDigestQuartzJob> logger)
        {
            _mcpTools = mcpTools;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("⏰ Quartz Cron Job Fired: Executing Daily Digest Dispatch...");

            try
            {
                var telegramResult = await _mcpTools.SendDailyDigestToTelegramAsync();
                _logger.LogInformation("Telegram Auto Dispatch: {Result}", telegramResult ? "SUCCESS" : "FAILED");

                var whatsAppResult = await _mcpTools.SendDailyDigestToWhatsAppAsync();
                _logger.LogInformation("WhatsApp Auto Dispatch: {Result}", whatsAppResult ? "SUCCESS" : "FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automated Quartz daily digest job execution.");
            }
        }
    }
}
