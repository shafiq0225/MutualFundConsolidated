using Microsoft.AspNetCore.Mvc;
using MutualFund.ConsolidatedAPI.Modules.Messaging.Tools;

namespace MutualFund.ConsolidatedAPI.Modules.Messaging.Controllers
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
