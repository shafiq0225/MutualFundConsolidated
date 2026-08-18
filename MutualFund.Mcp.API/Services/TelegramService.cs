using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MutualFund.Mcp.API.Services
{
    public class TelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TelegramService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync(string messageText)
        {
            var botToken = _configuration["Telegram:BotToken"];
            var chatId = _configuration["Telegram:ChatId"];

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Telegram BotToken or ChatId is not configured.");
                return false;
            }

            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = messageText,
                parse_mode = "HTML"
            };

            try
            {
                _logger.LogInformation("Sending Telegram message to ChatId {ChatId}...", chatId);
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Telegram message sent successfully!");
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send Telegram message. HTTP {StatusCode}: {Body}", response.StatusCode, errorBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Telegram API");
                return false;
            }
        }
    }
}
