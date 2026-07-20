using System.Security.Cryptography;
using System.Text;
using MutualFundNav.Domain.Common;
using MutualFundNav.Domain.Contracts;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Enums;
using MutualFundNav.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFundNav.Application.UseCases.Commands
{
    /// <summary>
    /// Downloads NAV data and UPSERTS into the database.
    /// If data already exists for the date it is REPLACED with the latest download.
    /// Always publishes to Kafka after a successful upsert and persists a
    /// <see cref="KafkaPublishLog"/> row regardless of publish outcome.
    /// Used by the manual "force refresh" endpoint.
    /// </summary>
    public class UpsertNavCommand
    {
        private readonly INavDownloadService _downloadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKafkaPublisher<NavFileProcessedEvent> _kafkaPublisher;
        private readonly ILogger<UpsertNavCommand> _logger;

        public UpsertNavCommand(
            INavDownloadService downloadService,
            IUnitOfWork unitOfWork,
            IKafkaPublisher<NavFileProcessedEvent> kafkaPublisher,
            ILogger<UpsertNavCommand> logger)
        {
            _downloadService = downloadService;
            _unitOfWork = unitOfWork;
            _kafkaPublisher = kafkaPublisher;
            _logger = logger;
        }

        /// <param name="targetDate">NAV date to upsert.</param>
        /// <param name="kafkaTopic">Topic to publish to.</param>
        /// <param name="triggerSource">Identifies the caller — stored in KafkaPublishLog.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Result<UpsertNavResult>> ExecuteAsync(
            DateTime targetDate,
            string kafkaTopic = "nav-file-processed",
            string triggerSource = "Unknown",
            CancellationToken ct = default)
        {
            _logger.LogInformation("Upsert NAV for {Date}", targetDate.ToString("yyyy-MM-dd"));

            // ── Download ──────────────────────────────────────────────────
            var (status, content, errorMessage, recordCount) =
                await _downloadService.DownloadNavDataAsync();

            if (status != DownloadStatus.Success)
            {
                _logger.LogError("Download failed: {Error}", errorMessage);
                return Result<UpsertNavResult>.Failure(errorMessage ?? "Download failed");
            }

            var checksum = ComputeChecksum(content);
            bool wasReplaced = false;

            // ── Upsert (transactional) ────────────────────────────────────
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var existing = await _unitOfWork.NavFiles.GetByDateAsync(targetDate);

                if (existing is not null)
                {
                    existing.FileContent = content;
                    existing.FileSizeBytes = (long)Encoding.UTF8.GetByteCount(content);
                    existing.RecordCount = recordCount;
                    existing.Checksum = checksum;
                    existing.DownloadedAt = DateTime.UtcNow;
                    await _unitOfWork.NavFiles.UpdateAsync(existing);
                    wasReplaced = true;
                    _logger.LogInformation("Replaced existing NAV for {Date}",
                        targetDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    await _unitOfWork.NavFiles.AddAsync(new NavFile
                    {
                        NavDate = targetDate,
                        FileContent = content,
                        FileSizeBytes = (long)Encoding.UTF8.GetByteCount(content),
                        RecordCount = recordCount,
                        Checksum = checksum,
                        DownloadedAt = DateTime.UtcNow
                    });
                    _logger.LogInformation("Inserted new NAV for {Date}",
                        targetDate.ToString("yyyy-MM-dd"));
                }

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync(); // disposes + nulls _transaction
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Upsert failed for {Date}", targetDate.ToString("yyyy-MM-dd"));
                return Result<UpsertNavResult>.Failure($"Storage failed: {ex.Message}");
            }

            // ── Publish to Kafka ──────────────────────────────────────────
            var messageKey = targetDate.ToString("yyyy-MM-dd");

            var publishResult = await _kafkaPublisher.PublishAsync(
                topic: kafkaTopic,
                key: messageKey,
                message: new NavFileProcessedEvent
                {
                    NavDate = targetDate,
                    FileContent = content,
                    RecordCount = recordCount,
                    Checksum = checksum,
                    PublishedAt = DateTime.UtcNow
                },
                ct: ct);

            if (!publishResult.IsSuccess)
            {
                _logger.LogWarning(
                    "NAV upserted but Kafka publish failed for {Date}: {Error}",
                    targetDate.ToString("yyyy-MM-dd"), publishResult.ErrorMessage);
            }
            else
            {
                _logger.LogInformation(
                    "Published NavFileProcessedEvent (upsert) for {Date} — partition={P} offset={O}",
                    targetDate.ToString("yyyy-MM-dd"),
                    publishResult.Partition, publishResult.Offset);
            }

            // ── Persist Kafka publish log ─────────────────────────────────
            try
            {
                await _unitOfWork.KafkaPublishLogs.AddAsync(new KafkaPublishLog
                {
                    Topic = kafkaTopic,
                    EventType = "NavFileProcessed",
                    MessageKey = messageKey,
                    MessageSizeBytes = publishResult.MessageSizeBytes,
                    IsSuccess = publishResult.IsSuccess,
                    ErrorMessage = publishResult.ErrorMessage,
                    PublishedAt = DateTime.UtcNow,
                    ElapsedMs = publishResult.ElapsedMs,
                    TriggerSource = triggerSource,
                    NavDate = targetDate,
                    Partition = publishResult.Partition,
                    Offset = publishResult.Offset
                });
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to persist KafkaPublishLog for {Date}",
                    targetDate.ToString("yyyy-MM-dd"));
            }

            return Result<UpsertNavResult>.Success(new UpsertNavResult
            {
                Date = targetDate,
                WasReplaced = wasReplaced,
                RecordCount = recordCount,
                Checksum = checksum,
                KafkaPublished = publishResult.IsSuccess,
                KafkaErrorMessage = publishResult.ErrorMessage
            });
        }

        private static string ComputeChecksum(string content)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    public class UpsertNavResult
    {
        public DateTime Date { get; set; }
        public bool WasReplaced { get; set; }
        public int RecordCount { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public bool KafkaPublished { get; set; }
        public string? KafkaErrorMessage { get; set; }
    }
}