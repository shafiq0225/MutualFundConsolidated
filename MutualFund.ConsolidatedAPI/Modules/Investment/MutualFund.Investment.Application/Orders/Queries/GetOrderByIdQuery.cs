using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Orders.Queries
{
    public class GetOrderByIdQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetOrderByIdQuery> _logger;

        public GetOrderByIdQuery(
            IUnitOfWork unitOfWork,
            ILogger<GetOrderByIdQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<OrderDto>> ExecuteAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders
                    .GetByIdAsync(orderId);

                if (order == null)
                    return Result<OrderDto>.Failure(
                        $"Order with ID {orderId} not found.");

                return Result<OrderDto>.Success(new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    InvestorUserId = order.InvestorUserId,
                    InvestorName = order.InvestorName,
                    SchemeCode = order.SchemeCode,
                    SchemeName = order.SchemeName,
                    FundName = order.FundName,
                    InvestedAmount = order.InvestedAmount,
                    PaymentMode = order.PaymentMode.ToString(),
                    ChequeNumber = order.ChequeNumber,
                    ChequeDate = order.ChequeDate,
                    BankName = order.BankName,
                    TransactionRef = order.TransactionRef,
                    OrderDate = order.OrderDate,
                    AssignedDate = order.AssignedDate,
                    AssignedStaffName = order.AssignedStaffName,
                    SubmittedDate = order.SubmittedDate,
                    VerifiedDate = order.VerifiedDate,
                    ActivatedDate = order.ActivatedDate,
                    Status = order.Status.ToString(),
                    StatusCode = (int)order.Status,
                    PurchaseNAV = order.PurchaseNAV,
                    UnitsAllotted = order.UnitsAllotted,
                    FolioNumber = order.FolioNumber,
                    Notes = order.Notes,
                    CreatedByUserId = order.CreatedByUserId,
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt,
                    HasHolding = order.Holding != null,
                    HasStatement = order.Statement != null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get order {OrderId}", orderId);
                return Result<OrderDto>.Failure(
                    $"Failed to retrieve order: {ex.Message}");
            }
        }
    }
}