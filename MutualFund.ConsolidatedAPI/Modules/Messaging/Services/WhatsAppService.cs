using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MutualFund.ConsolidatedAPI.Modules.Messaging.Services
{
    public class WhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<WhatsAppService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync(string messageText)
        {
            var provider = _configuration["WhatsApp:Provider"] ?? "UltraMsg";

            if (provider.Equals("UltraMsg", StringComparison.OrdinalIgnoreCase))
            {
                return await SendViaUltraMsgAsync(messageText);
            }
            else if (provider.Equals("GreenApi", StringComparison.OrdinalIgnoreCase))
            {
                return await SendViaGreenApiAsync(messageText);
            }
            else
            {
                return await SendViaTwilioAsync(messageText);
            }
        }

        private async Task<bool> SendViaUltraMsgAsync(string messageText)
        {
            var instanceId = _configuration["WhatsApp:UltraMsgInstanceId"];
            var token = _configuration["WhatsApp:UltraMsgToken"];
            var phone = _configuration["WhatsApp:ToPhone"] ?? "919942353582";

            if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("UltraMsg InstanceId or Token is missing in appsettings.json.");
                return false;
            }

            phone = phone.Replace("+", "").Replace("whatsapp:", "").Trim();

            var url = $"https://api.ultramsg.com/{instanceId}/messages/chat";
            var payload = new
            {
                token = token,
                to = phone,
                body = ConvertHtmlToWhatsAppMarkdown(messageText)
            };

            try
            {
                _logger.LogInformation("Sending WhatsApp message via UltraMsg to {Phone}...", phone);
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp message sent successfully via UltraMsg!");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("UltraMsg send failed. HTTP {StatusCode}: {Error}", response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message via UltraMsg");
                return false;
            }
        }

        private async Task<bool> SendViaGreenApiAsync(string messageText)
        {
            var instanceId = _configuration["WhatsApp:GreenApiInstanceId"];
            var apiToken = _configuration["WhatsApp:GreenApiToken"];
            var phone = _configuration["WhatsApp:ToPhone"] ?? "919942353582";

            if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(apiToken))
            {
                _logger.LogWarning("GreenAPI InstanceId or Token is missing in appsettings.json.");
                return false;
            }

            phone = phone.Replace("+", "").Replace("whatsapp:", "").Trim();
            var chatId = $"{phone}@c.us";

            var url = $"https://api.green-api.com/waInstance{instanceId}/sendMessage/{apiToken}";
            var payload = new
            {
                chatId = chatId,
                message = ConvertHtmlToWhatsAppMarkdown(messageText)
            };

            try
            {
                _logger.LogInformation("Sending WhatsApp message via Green API to {ChatId}...", chatId);
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp message sent successfully via Green API!");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Green API send failed. HTTP {StatusCode}: {Error}", response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message via Green API");
                return false;
            }
        }

        private async Task<bool> SendViaTwilioAsync(string messageText)
        {
            var accountSid = _configuration["WhatsApp:AccountSid"];
            var authToken = _configuration["WhatsApp:AuthToken"];
            var fromPhone = _configuration["WhatsApp:FromPhone"] ?? "whatsapp:+14155238886";
            var toPhone = _configuration["WhatsApp:ToPhone"] ?? "whatsapp:+919942353582";

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken))
            {
                _logger.LogWarning("Twilio AccountSid or AuthToken missing.");
                return false;
            }

            if (!fromPhone.StartsWith("whatsapp:")) fromPhone = $"whatsapp:{fromPhone}";
            if (!toPhone.StartsWith("whatsapp:")) toPhone = $"whatsapp:{toPhone}";

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var authenticationString = $"{accountSid}:{authToken}";
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString)));

            var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("From", fromPhone),
                new KeyValuePair<string, string>("To", toPhone),
                new KeyValuePair<string, string>("Body", ConvertHtmlToWhatsAppMarkdown(messageText))
            };

            request.Content = new FormUrlEncodedContent(keyValues);

            try
            {
                _logger.LogInformation("Sending WhatsApp message via Twilio to {ToPhone}...", toPhone);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message via Twilio");
                return false;
            }
        }

        private static string ConvertHtmlToWhatsAppMarkdown(string htmlText)
        {
            return htmlText
                .Replace("<b>", "*")
                .Replace("</b>", "*")
                .Replace("<code>", "`")
                .Replace("</code>", "`")
                .Replace("<i>", "_")
                .Replace("</i>", "_");
        }
    }
}
