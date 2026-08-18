using Microsoft.AspNetCore.Mvc;
using MutualFund.Mcp.API.Tools;

namespace MutualFund.Mcp.API.Controllers
{
    [ApiController]
    [Route("api/mcp/digest")]
    public class McpMessagingController : ControllerBase
    {
        private readonly MessagingMcpTools _mcpTools;

        public McpMessagingController(MessagingMcpTools mcpTools)
        {
            _mcpTools = mcpTools;
        }

        /// <summary>
        /// Previews the mobile-optimized scheme-wise daily digest message for WhatsApp & Telegram.
        /// </summary>
        [HttpGet("preview")]
        public async Task<IActionResult> PreviewDailyDigest()
        {
            var messageText = await _mcpTools.GenerateSchemeWiseDailyDigestAsync();
            return Ok(new
            {
                service = "messaging-mcp",
                timestamp = DateTime.UtcNow,
                formattedMessage = messageText
            });
        }

        /// <summary>
        /// Sends the daily digest push notification to Telegram or WhatsApp.
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendDailyDigest([FromQuery] string channel = "telegram")
        {
            if (channel.ToLower() == "telegram")
            {
                var success = await _mcpTools.SendDailyDigestToTelegramAsync();
                return Ok(new
                {
                    status = success ? "SUCCESS" : "FAILED",
                    channel = "telegram",
                    deliveredAt = DateTime.UtcNow
                });
            }
            else if (channel.ToLower() == "whatsapp")
            {
                var success = await _mcpTools.SendDailyDigestToWhatsAppAsync();
                return Ok(new
                {
                    status = success ? "SUCCESS" : "FAILED",
                    channel = "whatsapp",
                    deliveredAt = DateTime.UtcNow
                });
            }

            return BadRequest(new { status = "FAILED", message = $"Channel '{channel}' is not supported. Use 'telegram' or 'whatsapp'." });
        }
    }
}
