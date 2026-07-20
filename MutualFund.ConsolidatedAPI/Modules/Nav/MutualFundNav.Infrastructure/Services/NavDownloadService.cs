using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFundNav.Domain.Enums;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.Infrastructure.Services
{
    public class NavDownloadService : INavDownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NavDownloadService> _logger;

        public NavDownloadService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<NavDownloadService> logger)
        {
            _httpClient    = httpClient;
            _configuration = configuration;
            _logger        = logger;
        }

        public async Task<(DownloadStatus Status, string Content, string? ErrorMessage, int RecordCount)>
            DownloadNavDataAsync()
        {
            var url        = _configuration["AppSettings:NavSourceUrl"]
                             ?? "https://portal.amfiindia.com/spages/NAVAll.txt";
            var retryCount = _configuration.GetValue<int>("AppSettings:RetryCount", 3);
            var retryDelay = _configuration.GetValue<int>("AppSettings:RetryDelaySeconds", 10);

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    _logger.LogInformation("NAV download attempt {Attempt}/{Max}", attempt, retryCount);

                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var content     = await response.Content.ReadAsStringAsync();
                    var lines       = content.Split(new[] { '\n', '\r' },
                                          StringSplitOptions.RemoveEmptyEntries);
                    var recordCount = Math.Max(0, lines.Length - 1);

                    _logger.LogInformation("Download succeeded — {Records} records", recordCount);
                    return (DownloadStatus.Success, content, null, recordCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed", attempt);
                    if (attempt < retryCount)
                        await Task.Delay(TimeSpan.FromSeconds(retryDelay));
                    else
                        return (DownloadStatus.Failed, string.Empty, ex.Message, 0);
                }
            }

            return (DownloadStatus.Failed, string.Empty, "Unknown error after retries", 0);
        }
    }
}
