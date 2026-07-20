using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Orders.Queries
{
    public class GetOrdersByInvestorQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetOrdersByInvestorQuery> _logger;

        public GetOrdersByInvestorQuery(
            IUnitOfWork unitOfWork,
            ILogger<GetOrdersByInvestorQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<OrderSummaryDto>>>
            ExecuteAsync(string investorUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(investorUserId))
                    return Result<IEnumerable<OrderSummaryDto>>
                        .Failure("Investor ID is required.");

                var orders = await _unitOfWork.Orders
                    .GetByInvestorAsync(investorUserId);

                var result = orders.Select(o => new OrderSummaryDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    InvestorName = o.InvestorName,
                    SchemeName = o.SchemeName,
                    FundName = o.FundName,
                    InvestedAmount = o.InvestedAmount,
                    PaymentMode = o.PaymentMode.ToString(),
                    Status = o.Status.ToString(),
                    StatusCode = (int)o.Status,
                    OrderDate = o.OrderDate,
                    HasStatement = o.Statement != null,
                    CreatedAt = o.CreatedAt
                });

                return Result<IEnumerable<OrderSummaryDto>>
                    .Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to get orders for investor {InvestorId}",
                    investorUserId);
                return Result<IEnumerable<OrderSummaryDto>>
                    .Failure($"Failed to retrieve orders: {ex.Message}");
            }
        }
    }
}