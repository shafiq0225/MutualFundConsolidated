using System.Security.Cryptography;
using System.Text;
using MutualFundNav.Domain.Common;
using MutualFundNav.Domain.Contracts;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Enums;
using MutualFundNav.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MutualFund.Investment.Application.Portfolio.Commands;

namespace MutualFundNav.Application.UseCases.Commands
{
    /// <summary>
    /// Downloads NAV data for <paramref name="targetDate"/> and inserts it if it does not already
    /// exist (idempotent). Publishes a <see cref="NavFileProcessedEvent"/> to Kafka and persists
    /// a <see cref="KafkaPublishLog"/> row regardless of publish outcome.
    /// </summary>
    public class DownloadAndStoreNavCommand
    {
        private readonly INavDownloadService _downloadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IKafkaPublisher<NavFileProcessedEvent> _kafkaPublisher;
        private readonly ILogger<DownloadAndStoreNavCommand> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DownloadAndStoreNavCommand(
            INavDownloadService downloadService,
            IUnitOfWork unitOfWork,
            IKafkaPublisher<NavFileProcessedEvent> kafkaPublisher,
            ILogger<DownloadAndStoreNavCommand> logger,
            IServiceProvider serviceProvider)
        {
            _downloadService = downloadService;
            _unitOfWork = unitOfWork;
            _kafkaPublisher = kafkaPublisher;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<Result<bool>> ExecuteAsync(
            DateTime targetDate,
            string kafkaTopic = "nav-file-processed",
            CancellationToken ct = default,
            string triggerSource = "Unknown",
            bool allowReprocess = false)
        {
            _logger.LogInformation("Checking if NAV data exists for {Date}",
                targetDate.ToString("yyyy-MM-dd"));

            var existingNavFile = await _unitOfWork.NavFiles.GetByDateAsync(targetDate);

            // ── Idempotency check ──────────────────────────────────────────
            if (existingNavFile is not null)
            {
                if (!allowReprocess)
                {
                    _logger.LogInformation("NAV data already exists for {Date} — skipping",
                        targetDate.ToString("yyyy-MM-dd"));
                    return Result<bool>.Success(false);
                }

                _logger.LogInformation("NAV data already exists for {Date} — re-publishing stored content",
                    targetDate.ToString("yyyy-MM-dd"));

                await PublishNavFileAsync(
                    targetDate,
                    existingNavFile.FileContent,
                    existingNavFile.RecordCount,
                    existingNavFile.Checksum,
                    kafkaTopic,
                    ct,
                    triggerSource);

                await DirectSyncDetailedSchemesAsync(targetDate, existingNavFile.FileContent);
                await AutoCalculatePortfolioSnapshotsAsync(targetDate);

                return Result<bool>.Success(true);
            }

            // ── Download ───────────────────────────────────────────────────
            _logger.LogInformation("Downloading NAV data for {Date}",
                targetDate.ToString("yyyy-MM-dd"));

            var (status, content, errorMessage, recordCount) =
                await _downloadService.DownloadNavDataAsync();

            if (status != DownloadStatus.Success)
            {
                _logger.LogError("Download failed: {Error}", errorMessage);
                return Result<bool>.Failure(errorMessage ?? "Download failed");
            }

            var checksum = ComputeChecksum(content);

            // ── Store (transactional) ──────────────────────────────────────
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var navFile = new NavFile
                {
                    NavDate = targetDate,
                    FileContent = content,
                    FileSizeBytes = (long)Encoding.UTF8.GetByteCount(content),
                    RecordCount = recordCount,
                    Checksum = checksum,
                    DownloadedAt = DateTime.UtcNow
                };

                await _unitOfWork.NavFiles.AddAsync(navFile);
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();  // disposes + nulls _transaction

                _logger.LogInformation(
                    "Stored NAV file for {Date} — {Size} bytes, {Records} records",
                    targetDate.ToString("yyyy-MM-dd"), navFile.FileSizeBytes, recordCount);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Storage failed for {Date}", targetDate.ToString("yyyy-MM-dd"));
                return Result<bool>.Failure($"Storage failed: {ex.Message}");
            }

            await PublishNavFileAsync(
                targetDate,
                content,
                recordCount,
                checksum,
                kafkaTopic,
                ct,
                triggerSource);

            await DirectSyncDetailedSchemesAsync(targetDate, content);
            await AutoCalculatePortfolioSnapshotsAsync(targetDate);

            return Result<bool>.Success(true);
        }

        private async Task PublishNavFileAsync(
            DateTime targetDate,
            string content,
            int recordCount,
            string checksum,
            string kafkaTopic,
            CancellationToken ct,
            string triggerSource)
        {
            var config = _serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var kafkaEnabled = config?.GetValue<bool>("Kafka:Enabled", true) ?? true;
            if (!kafkaEnabled)
            {
                _logger.LogInformation("Kafka is disabled in configuration. Skipping Kafka event publication.");
                return;
            }

            // ── Publish to Kafka ───────────────────────────────────────────
            // PublishAsync never throws — failures come back in the result.
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
                    "Kafka publish failed for {Date}: {Error}",
                    targetDate.ToString("yyyy-MM-dd"), publishResult.ErrorMessage);
            }

            // ── Persist Kafka publish log ──────────────────────────────────
            // Uses the same UoW (DbContext) after transaction is committed + disposed.
            // Saved as a plain (non-transactional) SaveChanges — if this fails it
            // does not affect the already-committed NAV data.
            try
            {
                var kafkaLog = new KafkaPublishLog
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
                };

                await _unitOfWork.KafkaPublishLogs.AddAsync(kafkaLog);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to persist KafkaPublishLog for {Date}",
                    targetDate.ToString("yyyy-MM-dd"));
            }
        }

        private static string ComputeChecksum(string content)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private async Task DirectSyncDetailedSchemesAsync(DateTime targetDate, string fileContent)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var schemeUow = scope.ServiceProvider.GetService<MutualFund.Scheme.Domain.Interfaces.IUnitOfWork>();
                if (schemeUow == null)
                {
                    _logger.LogWarning("Scheme IUnitOfWork is not available. Skipping direct sync.");
                    return;
                }

                _logger.LogInformation("Directly synchronizing detailed schemes from NAV file in-process...");

                var approvedSchemes = await schemeUow.SchemeEnrollments.GetApprovedSchemesAsync();
                var approvedCodes = approvedSchemes.Select(s => s.SchemeCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (approvedCodes.Count == 0)
                {
                    _logger.LogInformation("No approved schemes found for direct sync.");
                    return;
                }

                var lines = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var toInsert = new List<MutualFund.Scheme.Domain.Entities.DetailedScheme>();
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
                        currentFundCode = MutualFund.Scheme.Domain.Helpers.FundCodeGenerator.Generate(currentFundName);
                        continue;
                    }

                    var parts = trimmed.Split(';');
                    if (parts.Length < 6) continue;

                    var schemeCode = parts[0].Trim();
                    if (!approvedCodes.Contains(schemeCode)) continue;

                    if (await schemeUow.DetailedSchemes.ExistsBySchemeCodeAndDateAsync(schemeCode, targetDate))
                    {
                        continue;
                    }

                    if (!decimal.TryParse(parts[4].Trim(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var nav))
                    {
                        continue;
                    }

                    var enrollment = approvedSchemes.First(s => s.SchemeCode == schemeCode);

                    toInsert.Add(new MutualFund.Scheme.Domain.Entities.DetailedScheme
                    {
                        FundCode = currentFundCode,
                        FundName = currentFundName,
                        SchemeCode = schemeCode,
                        SchemeName = parts[3].Trim(),
                        IsApproved = enrollment.IsApproved,
                        Nav = nav,
                        NavDate = targetDate.Date,
                        ReceivedAt = receivedAt
                    });
                }

                if (toInsert.Count > 0)
                {
                    await schemeUow.DetailedSchemes.AddRangeAsync(toInsert);
                    await schemeUow.CompleteAsync();
                    _logger.LogInformation("Directly inserted {Count} records into DetailedSchemes.", toInsert.Count);
                }
                else
                {
                    _logger.LogInformation("No new detailed schemes to insert.");
                }

                // Always invalidate navcomparison_daily cache after sync
                var memoryCache = _serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                memoryCache?.Remove("navcomparison_daily");
                _logger.LogInformation("Invalidated 'navcomparison_daily' cache after NAV sync.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform direct in-process scheme synchronization.");
            }
        }

        private async Task AutoCalculatePortfolioSnapshotsAsync(DateTime targetDate)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var snapshotCmd = scope.ServiceProvider.GetService<CalculateSnapshotCommand>();
                if (snapshotCmd != null)
                {
                    _logger.LogInformation("Auto-triggering Portfolio Snapshot calculation for {Date}...", targetDate.ToString("yyyy-MM-dd"));
                    await snapshotCmd.ExecuteAsync(targetDate);
                    _logger.LogInformation("Portfolio Snapshot calculation completed automatically after NAV sync.");
                }
                else
                {
                    _logger.LogWarning("CalculateSnapshotCommand was not resolved in DI scope.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-trigger portfolio snapshot calculation after NAV sync.");
            }
        }
    }
}