using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MutualFund.Scheme.Domain.Contracts;
using MutualFund.Scheme.Domain.Entities;
using MutualFund.Scheme.Domain.Interfaces;

namespace MutualFund.Scheme.Infrastructure.Consumers
{
    public class MarketHolidayKafkaConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<MarketHolidayKafkaConsumer> _logger;

        public MarketHolidayKafkaConsumer(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<MarketHolidayKafkaConsumer> logger)
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
            var topic = _config["Kafka:Topics:MarketHoliday"] ?? "market-holidays";
            var groupId = _config["Kafka:ConsumerGroups:MarketHoliday"] ?? "scheme-api-holiday-consumer";

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
                "MarketHolidayKafkaConsumer started — Topic: {Topic} GroupId: {GroupId}",
                topic, groupId);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null) continue;

                    var holidayEvent = JsonSerializer.Deserialize<MarketHolidayEvent>(
                        result.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (holidayEvent is null)
                    {
                        _logger.LogWarning(
                            "Failed to deserialize MarketHolidayEvent — skipping");
                        consumer.Commit(result);
                        continue;
                    }

                    _logger.LogInformation(
                        "Received MarketHolidayEvent for {Date}",
                        holidayEvent.HolidayDate.ToString("yyyy-MM-dd"));

                    await ProcessHolidayEventAsync(holidayEvent);
                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MarketHolidayEvent");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            consumer.Close();
            _logger.LogInformation("MarketHolidayKafkaConsumer stopped");
        }

        private async Task ProcessHolidayEventAsync(MarketHolidayEvent holidayEvent)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var holidayDate = holidayEvent.HolidayDate.Date;

            if (await unitOfWork.MarketHolidays.ExistsByDateAsync(holidayDate))
            {
                _logger.LogInformation(
                    "MarketHoliday already stored for {Date} — skipping",
                    holidayDate.ToString("yyyy-MM-dd"));
                return;
            }

            await unitOfWork.MarketHolidays.AddAsync(new MarketHoliday
            {
                HolidayDate = holidayDate,
                Source = holidayEvent.Source,
                ReceivedAt = DateTime.UtcNow
            });

            await unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "MarketHoliday stored for {Date}",
                holidayDate.ToString("yyyy-MM-dd"));
        }
    }
}