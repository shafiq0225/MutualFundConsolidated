using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Statements.Queries
{
    public class DownloadStatementResult
    {
        public Stream FileStream { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
    }

    public class DownloadStatementQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlobStorageService _blobStorage;
        private readonly ILogger<DownloadStatementQuery> _logger;

        public DownloadStatementQuery(
            IUnitOfWork unitOfWork,
            IBlobStorageService blobStorage,
            ILogger<DownloadStatementQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _blobStorage = blobStorage;
            _logger = logger;
        }

        /// <summary>
        /// Returns the PDF file stream for browser display.
        /// No download — opens in browser (read-only view).
        /// </summary>
        public async Task<Result<DownloadStatementResult>> ExecuteAsync(
            int statementId)
        {
            try
            {
                // ── Find statement ─────────────────────────────────
                var statement = await _unitOfWork.Statements
                    .GetByIdAsync(statementId);

                if (statement == null)
                    return Result<DownloadStatementResult>
                        .Failure($"Statement with Id {statementId} not found.");

                _logger.LogInformation(
                    "Downloading statement {Id} — {File}",
                    statementId, statement.FileName);

                // ── Download from Blob Storage ─────────────────────
                var stream = await _blobStorage
                    .DownloadAsync(statement.FilePath);

                return Result<DownloadStatementResult>.Success(
                    new DownloadStatementResult
                    {
                        FileStream = stream,
                        FileName = statement.FileName,
                        ContentType = "application/pdf"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error downloading statement {Id}", statementId);
                return Result<DownloadStatementResult>
                    .Failure($"Failed to download statement: {ex.Message}");
            }
        }
    }
}