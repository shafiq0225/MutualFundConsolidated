using MutualFund.Investment.Application.Orders.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Enums;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Orders.Commands
{
    public class CreateOrderCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateOrderCommand> _logger;

        public CreateOrderCommand(
            IUnitOfWork unitOfWork,
            ILogger<CreateOrderCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<InvestmentOrderDto>> ExecuteAsync(
            CreateOrderDto dto,
            string createdByUserId)
        {
            try
            {
                // ── Validate ───────────────────────────────────────
                var validation = Validate(dto);
                if (!validation.IsSuccess)
                    return Result<InvestmentOrderDto>
                        .Failure(validation.ErrorMessage!);

                // ── Parse PaymentMode ──────────────────────────────
                if (!Enum.TryParse<PaymentMode>(
                        dto.PaymentMode, true, out var paymentMode))
                    return Result<InvestmentOrderDto>
                        .Failure($"Invalid payment mode: {dto.PaymentMode}. " +
                                 "Valid values: Cheque, NEFT, RTGS, IMPS, Online");

                // ── Validate cheque details ────────────────────────
                if (paymentMode == PaymentMode.Cheque)
                {
                    if (string.IsNullOrWhiteSpace(dto.ChequeNumber))
                        return Result<InvestmentOrderDto>
                            .Failure("Cheque number is required for Cheque payments.");

                    if (dto.ChequeDate == null)
                        return Result<InvestmentOrderDto>
                            .Failure("Cheque date is required for Cheque payments.");

                    if (string.IsNullOrWhiteSpace(dto.BankName))
                        return Result<InvestmentOrderDto>
                            .Failure("Bank name is required for Cheque payments.");
                }

                // ── Validate NEFT/RTGS/IMPS details ───────────────
                if (paymentMode is PaymentMode.NEFT
                                or PaymentMode.RTGS
                                or PaymentMode.IMPS)
                {
                    if (string.IsNullOrWhiteSpace(dto.TransactionRef))
                        return Result<InvestmentOrderDto>
                            .Failure("Transaction reference is required " +
                                     "for NEFT/RTGS/IMPS payments.");
                }

                // ── Generate Order Number ──────────────────────────
                var orderNumber = await _unitOfWork.Orders
                    .GenerateOrderNumberAsync();

                _logger.LogInformation(
                    "Creating order {OrderNumber} for investor {Investor} " +
                    "— Scheme: {Scheme} — Amount: {Amount}",
                    orderNumber,
                    dto.InvestorName,
                    dto.SchemeName,
                    dto.InvestedAmount);

                // ── Create Entity ──────────────────────────────────
                var order = new InvestmentOrder
                {
                    OrderNumber = orderNumber,
                    InvestorUserId = dto.InvestorUserId,
                    InvestorName = dto.InvestorName,
                    SchemeCode = dto.SchemeCode,
                    SchemeName = dto.SchemeName,
                    FundName = dto.FundName,
                    InvestedAmount = dto.InvestedAmount,
                    PaymentMode = paymentMode,
                    ChequeNumber = dto.ChequeNumber,
                    ChequeDate = dto.ChequeDate,
                    BankName = dto.BankName,
                    TransactionRef = dto.TransactionRef,
                    OrderDate = dto.OrderDate.Date,
                    Status = OrderStatus.Requested,
                    PurchaseNAV = dto.PurchaseNAV,
                    FolioNumber = dto.FolioNumber,
                    UnitsAllotted = dto.PurchaseNAV > 0
                        ? Math.Round(dto.InvestedAmount / dto.PurchaseNAV, 6)
                        : null,
                    Notes = dto.Notes,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // ── Save ───────────────────────────────────────────
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation(
                    "✅ Order created: {OrderNumber} (Id: {Id})",
                    order.OrderNumber, order.Id);

                return Result<InvestmentOrderDto>
                    .Success(OrderMapper.ToDto(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating order for investor {Investor}",
                    dto.InvestorName);
                return Result<InvestmentOrderDto>
                    .Failure($"Failed to create order: {ex.Message}");
            }
        }

        // ── Private Validation ─────────────────────────────────────
        private static Domain.Common.Result Validate(CreateOrderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.InvestorUserId))
                return Domain.Common.Result.Failure(
                    "Investor is required.");

            if (string.IsNullOrWhiteSpace(dto.SchemeCode))
                return Domain.Common.Result.Failure(
                    "Scheme code is required.");

            if (string.IsNullOrWhiteSpace(dto.SchemeName))
                return Domain.Common.Result.Failure(
                    "Scheme name is required.");

            if (dto.InvestedAmount <= 0)
                return Domain.Common.Result.Failure(
                    "Invested amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.PaymentMode))
                return Domain.Common.Result.Failure(
                    "Payment mode is required.");

            if (dto.PurchaseNAV <= 0)
                return Domain.Common.Result.Failure(
                    "Purchase NAV must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.FolioNumber))
                return Domain.Common.Result.Failure(
                    "Folio number is required.");

            return Domain.Common.Result.Success();
        }
    }
}