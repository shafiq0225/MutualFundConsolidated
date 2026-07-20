using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MutualFund.Scheme.Domain.Contracts;
using MutualFund.Scheme.Domain.Entities;
using MutualFund.Scheme.Domain.Helpers;
using MutualFund.Scheme.Domain.Interfaces;
using MutualFund.Scheme.Domain.Exceptions;

namespace MutualFund.Scheme.Infrastructure.Consumers
{
    public class NavFileKafkaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<NavFileKafkaConsumer> _logger;

        public NavFileKafkaConsumer(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<NavFileKafkaConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Yield immediately so the generic host's StartAsync() returns right away
            // instead of blocking on this thread until the first Kafka message arrives.
            // Without this, ConsumerBuilder.Build()/Subscribe()/Consume() below all run
            // synchronously on the host startup thread (since there's no earlier await),
            // which prevents Kestrel from ever starting to accept requests.
            await Task.Yield();

            var bootstrapServers = _config["Kafka:BootstrapServers"] ?? "127.0.0.1:9092";
            var topic = _config["Kafka:Topics:NavFileProcessed"] ?? "nav-file-processed";
            var groupId = _config["Kafka:ConsumerGroups:NavFile"] ?? "scheme-api-nav-consumer";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            var kafkaSection = _config.GetSection("Kafka");
            if (!string.IsNullOrEmpty(kafkaSection["SaslUsername"]))
                consumerConfig.SaslUsername = kafkaSection["SaslUsername"];
            if (!string.IsNullOrEmpty(kafkaSection["SaslPassword"]))
                consumerConfig.SaslPassword = kafkaSection["SaslPassword"];
            if (!string.IsNullOrEmpty(kafkaSection["SaslMechanism"]) && 
                Enum.TryParse<SaslMechanism>(kafkaSection["SaslMechanism"], true, out var mechanism))
                consumerConfig.SaslMechanism = mechanism;
            if (!string.IsNullOrEmpty(kafkaSection["SecurityProtocol"]) && 
                Enum.TryParse<SecurityProtocol>(kafkaSection["SecurityProtocol"], true, out var protocol))
                consumerConfig.SecurityProtocol = protocol;
            if (!string.IsNullOrEmpty(kafkaSection["EnableSslCertificateVerification"]) && 
                bool.TryParse(kafkaSection["EnableSslCertificateVerification"], out var verify))
                consumerConfig.EnableSslCertificateVerification = verify;

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(topic);

            _logger.LogInformation(
                "NavFileKafkaConsumer started — Topic: {Topic} GroupId: {GroupId}",
                topic, groupId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null) continue;

                    var navEvent = JsonSerializer.Deserialize<NavFileProcessedEvent>(
                        result.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (navEvent is null)
                    {
                        _logger.LogWarning("Failed to deserialize NavFileProcessedEvent — skipping");
                        consumer.Commit(result);
                        continue;
                    }

                    _logger.LogInformation(
                        "Received NavFileProcessedEvent for {Date} with {Count} records",
                        navEvent.NavDate.ToString("yyyy-MM-dd"), navEvent.RecordCount);

                    await ProcessNavEventAsync(navEvent, stoppingToken);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing NavFileProcessedEvent");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            consumer.Close();
            _logger.LogInformation("NavFileKafkaConsumer stopped");
        }

        private async Task ProcessNavEventAsync(
            NavFileProcessedEvent message, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                var approvedSchemes = await unitOfWork.SchemeEnrollments
                    .GetApprovedSchemesAsync();

                var approvedCodes = approvedSchemes
                    .Select(s => s.SchemeCode)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (approvedCodes.Count == 0)
                {
                    _logger.LogWarning(
                        "No approved schemes found for {Date} — skipping",
                        message.NavDate.ToString("yyyy-MM-dd"));
                    return;
                }

                var lines = message.FileContent.Split(
                    new[] { '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries);

                var toInsert = new List<DetailedScheme>();
                var receivedAt = DateTime.Now;
                string currentFundName = string.Empty;
                string currentFundCode = string.Empty;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (!trimmed.Contains(';'))
                    {
                        currentFundName = trimmed;
                        currentFundCode = FundCodeGenerator.Generate(currentFundName);
                        continue;
                    }

                    var parts = trimmed.Split(';');
                    if (parts.Length < 6) continue;

                    var schemeCode = parts[0].Trim();
                    if (!approvedCodes.Contains(schemeCode)) continue;

                    if (await unitOfWork.DetailedSchemes
                            .ExistsBySchemeCodeAndDateAsync(schemeCode, message.NavDate))
                    {
                        _logger.LogInformation(
                            "Already exists — SchemeCode={Code} NavDate={Date}",
                            schemeCode, message.NavDate.ToString("yyyy-MM-dd"));
                        continue;
                    }

                    if (!decimal.TryParse(parts[4].Trim(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var nav))
                    {
                        _logger.LogWarning(
                            "Invalid NAV value for SchemeCode={Code} — skipping", schemeCode);
                        continue;
                    }

                    var enrollment = approvedSchemes
                        .First(s => s.SchemeCode == schemeCode);

                    toInsert.Add(new DetailedScheme
                    {
                        FundCode = currentFundCode,
                        FundName = currentFundName,
                        SchemeCode = schemeCode,
                        SchemeName = parts[3].Trim(),
                        IsApproved = enrollment.IsApproved,
                        Nav = nav,
                        NavDate = message.NavDate.Date,
                        ReceivedAt = receivedAt
                    });
                }

                if (toInsert.Count == 0)
                {
                    _logger.LogInformation(
                        "No new records to insert for {Date}",
                        message.NavDate.ToString("yyyy-MM-dd"));
                    return;
                }

                await unitOfWork.DetailedSchemes.AddRangeAsync(toInsert);
                await unitOfWork.CompleteAsync();

                _logger.LogInformation(
                    "Inserted {Count} records into DetailedScheme for {Date}",
                    toInsert.Count, message.NavDate.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "NavFileKafkaConsumer failed for NavDate={Date}",
                    message.NavDate.ToString("yyyy-MM-dd"));
                throw new NavConsumerException(
                    $"Failed to process NAV file for {message.NavDate:yyyy-MM-dd}",
                    message.NavDate, ex);
            }
        }
    }
}