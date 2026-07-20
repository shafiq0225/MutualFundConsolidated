using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFundNav.Domain.Contracts;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.Infrastructure.Services
{
    public class KafkaPublisher<T> : IKafkaPublisher<T>, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaPublisher<T>> _logger;

        public KafkaPublisher(IConfiguration configuration, ILogger<KafkaPublisher<T>> logger)
        {
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "127.0.0.1:9092",
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000,
                CompressionType = CompressionType.Snappy,
                BrokerAddressFamily = BrokerAddressFamily.V4,
                MessageMaxBytes = 20_971_520   // 20 MB — AMFI NAV file is ~1.6 MB uncompressed
            };
            var kafkaSection = configuration.GetSection("Kafka");
            if (!string.IsNullOrEmpty(kafkaSection["SaslUsername"]))
                config.SaslUsername = kafkaSection["SaslUsername"];
            if (!string.IsNullOrEmpty(kafkaSection["SaslPassword"]))
                config.SaslPassword = kafkaSection["SaslPassword"];
            if (!string.IsNullOrEmpty(kafkaSection["SaslMechanism"]) && 
                Enum.TryParse<SaslMechanism>(kafkaSection["SaslMechanism"], true, out var mechanism))
                config.SaslMechanism = mechanism;
            if (!string.IsNullOrEmpty(kafkaSection["SecurityProtocol"]) && 
                Enum.TryParse<SecurityProtocol>(kafkaSection["SecurityProtocol"], true, out var protocol))
                config.SecurityProtocol = protocol;
            if (!string.IsNullOrEmpty(kafkaSection["EnableSslCertificateVerification"]) && 
                bool.TryParse(kafkaSection["EnableSslCertificateVerification"], out var verify))
                config.EnableSslCertificateVerification = verify;

            _logger.LogInformation("Kafka Init: User={User}, PwdLength={Len}, PwdPrefix={Prefix}", 
                config.SaslUsername, 
                config.SaslPassword?.Length ?? 0, 
                config.SaslPassword?.Substring(0, Math.Min(5, config.SaslPassword?.Length ?? 0)));

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                    _logger.LogError("Kafka producer error [{Code}]: {Reason}", e.Code, e.Reason))
                .Build();
        }

        /// <summary>
        /// Publishes <paramref name="message"/> to Kafka and returns a <see cref="KafkaPublishResult"/>.
        /// This method never throws — delivery failures are captured in the result so callers
        /// can persist them to <c>KafkaPublishLogs</c> without an additional try/catch.
        /// </summary>
        public async Task<KafkaPublishResult> PublishAsync(
            string topic, string key, T message, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(message);
            var sizeBytes = (long)Encoding.UTF8.GetByteCount(json);
            var kafkaMsg = new Message<string, string> { Key = key, Value = json };
            var sw = Stopwatch.StartNew();

            try
            {
                var result = await _producer.ProduceAsync(topic, kafkaMsg, ct);
                sw.Stop();

                _logger.LogInformation(
                    "Kafka publish OK | topic={Topic} | partition={P} | offset={O} | key={Key} | {Bytes} bytes | {Ms}ms",
                    result.Topic, result.Partition.Value, result.Offset.Value, key,
                    sizeBytes, (int)sw.Elapsed.TotalMilliseconds);

                return KafkaPublishResult.Succeeded(
                    result.Partition.Value,
                    result.Offset.Value,
                    sw.Elapsed.TotalMilliseconds,
                    sizeBytes);
            }
            catch (Exception ex)
            {
                sw.Stop();

                _logger.LogError(ex,
                    "Kafka publish FAILED | topic={Topic} | key={Key} | {Bytes} bytes | {Ms}ms",
                    topic, key, sizeBytes, (int)sw.Elapsed.TotalMilliseconds);

                return KafkaPublishResult.Failed(
                    ex.Message,
                    sw.Elapsed.TotalMilliseconds,
                    sizeBytes);
            }
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}