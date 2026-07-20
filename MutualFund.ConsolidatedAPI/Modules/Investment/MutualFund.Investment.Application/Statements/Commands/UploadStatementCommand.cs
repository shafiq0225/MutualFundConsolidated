using MutualFund.Investment.Application.Statements.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Statements.Commands
{
    public class UploadStatementCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlobStorageService _blobStorage;
        private readonly ILogger<UploadStatementCommand> _logger;

        public UploadStatementCommand(
            IUnitOfWork unitOfWork,
            IBlobStorageService blobStorage,
            ILogger<UploadStatementCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _blobStorage = blobStorage;
            _logger = logger;
        }

        /// <summary>
        /// Admin uploads the PDF statement received from MF company.
        /// PDF stored in Azure Blob Storage.
        /// URL saved in InvestmentStatements table.
        /// </summary>
        public async Task<Result<InvestmentStatementDto>> ExecuteAsync(
            UploadStatementDto dto,
            string uploadedByUserId)
        {
            try
            {
                // ── Validate ───────────────────────────────────────
                var validation = await ValidateAsync(dto);
                if (!validation.IsSuccess)
                    return Result<InvestmentStatementDto>
                        .Failure(validation.ErrorMessage!);

                // ── Get the order ──────────────────────────────────
                var order = await _unitOfWork.Orders
                    .GetByIdAsync(dto.OrderId);

                if (order == null)
                    return Result<InvestmentStatementDto>
                        .Failure($"Order with Id {dto.OrderId} not found.");

                _logger.LogInformation(
                    "Uploading statement for order {OrderNumber} — " +
                    "File: {FileName} ({Size} bytes)",
                    order.OrderNumber,
                    dto.FileName,
                    dto.FileSizeBytes);

                // ── Upload PDF to Blob Storage ──────────────────────
                string blobUrl;
                try
                {
                    // Use order number as part of blob name
                    // for easy identification
                    var blobFileName =
                        $"{order.OrderNumber}_{dto.FileName}";

                    blobUrl = await _blobStorage.UploadAsync(
                        dto.FileStream,
                        blobFileName,
                        dto.ContentType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to upload PDF to storage " +
                        "for order {OrderNumber}",
                        order.OrderNumber);
                    return Result<InvestmentStatementDto>
                        .Failure("Failed to upload file. Please try again.");
                }

                // ── Save statement record ──────────────────────────
                var statement = new InvestmentStatement
                {
                    OrderId = dto.OrderId,
                    InvestorUserId = order.InvestorUserId,
                    InvestorName = order.InvestorName,
                    StatementDate = dto.StatementDate,
                    FilePath = blobUrl,
                    FileName = dto.FileName,
                    FileSizeBytes = dto.FileSizeBytes,
                    UploadedByUserId = uploadedByUserId,
                    UploadedAt = DateTime.UtcNow,
                    Notes = dto.Notes
                };

                await _unitOfWork.Statements.AddAsync(statement);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation(
                    "✅ Statement uploaded for order {OrderNumber}: " +
                    "Id={Id} File={File}",
                    order.OrderNumber,
                    statement.Id,
                    dto.FileName);

                // ── Load with navigation properties ────────────────
                var saved = await _unitOfWork.Statements
                    .GetByIdAsync(statement.Id);

                return Result<InvestmentStatementDto>
                    .Success(StatementMapper.ToDto(saved!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error uploading statement for order {OrderId}",
                    dto.OrderId);
                return Result<InvestmentStatementDto>
                    .Failure($"Upload failed: {ex.Message}");
            }
        }

        // ── Validation ─────────────────────────────────────────────
        private async Task<Domain.Common.Result> ValidateAsync(
            UploadStatementDto dto)
        {
            if (dto.OrderId <= 0)
                return Domain.Common.Result.Failure(
                    "Valid order Id is required.");

            if (dto.StatementDate == default)
                return Domain.Common.Result.Failure(
                    "Statement date is required.");

            if (dto.StatementDate > DateTime.Today)
                return Domain.Common.Result.Failure(
                    "Statement date cannot be in the future.");

            if (string.IsNullOrWhiteSpace(dto.FileName))
                return Domain.Common.Result.Failure(
                    "File name is required.");

            // Only PDF files allowed
            var extension = Path.GetExtension(dto.FileName).ToLower();
            if (extension != ".pdf")
                return Domain.Common.Result.Failure(
                    "Only PDF files are allowed.");

            // Max file size: 10 MB
            const long maxSize = 10 * 1024 * 1024;
            if (dto.FileSizeBytes > maxSize)
                return Domain.Common.Result.Failure(
                    "File size must not exceed 10 MB.");

            // Check if statement already exists for this order
            var exists = await _unitOfWork.Statements
                .ExistsForOrderAsync(dto.OrderId);

            if (exists)
                return Domain.Common.Result.Failure(
                    "A statement has already been uploaded for this order. " +
                    "Only one statement per order is allowed.");

            return Domain.Common.Result.Success();
        }
    }
}