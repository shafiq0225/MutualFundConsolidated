using Quartz;
using MutualFund.Mcp.API.Tools;

namespace MutualFund.Mcp.API.Jobs
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
                // Send to Telegram
                var telegramResult = await _mcpTools.SendDailyDigestToTelegramAsync();
                _logger.LogInformation("Telegram Auto Dispatch: {Result}", telegramResult ? "SUCCESS" : "FAILED");

                // Send to WhatsApp
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
