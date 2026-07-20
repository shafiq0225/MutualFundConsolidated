using MutualFund.Investment.Application.Statements.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Statements.Queries
{
    public class GetStatementsQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetStatementsQuery> _logger;

        public GetStatementsQuery(
            IUnitOfWork unitOfWork,
            ILogger<GetStatementsQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Get all statements — Admin view.
        /// </summary>
        public async Task<Result<IEnumerable<InvestmentStatementDto>>>
            GetAllAsync()
        {
            try
            {
                var statements = await _unitOfWork.Statements
                    .GetAllAsync();

                return Result<IEnumerable<InvestmentStatementDto>>
                    .Success(StatementMapper.ToDtoList(statements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all statements");
                return Result<IEnumerable<InvestmentStatementDto>>
                    .Failure($"Failed to fetch statements: {ex.Message}");
            }
        }

        /// <summary>
        /// Get statements for a specific investor.
        /// </summary>
        public async Task<Result<IEnumerable<InvestmentStatementDto>>>
            GetByInvestorAsync(string investorUserId)
        {
            try
            {
                var statements = await _unitOfWork.Statements
                    .GetByInvestorAsync(investorUserId);

                return Result<IEnumerable<InvestmentStatementDto>>
                    .Success(StatementMapper.ToDtoList(statements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching statements for investor {Id}",
                    investorUserId);
                return Result<IEnumerable<InvestmentStatementDto>>
                    .Failure($"Failed to fetch statements: {ex.Message}");
            }
        }

        /// <summary>
        /// Get statement for a specific order.
        /// </summary>
        public async Task<Result<InvestmentStatementDto>>
            GetByOrderAsync(int orderId)
        {
            try
            {
                var statement = await _unitOfWork.Statements
                    .GetByOrderIdAsync(orderId);

                if (statement == null)
                    return Result<InvestmentStatementDto>
                        .Failure($"No statement found for order Id {orderId}.");

                return Result<InvestmentStatementDto>
                    .Success(StatementMapper.ToDto(statement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching statement for order {OrderId}", orderId);
                return Result<InvestmentStatementDto>
                    .Failure($"Failed to fetch statement: {ex.Message}");
            }
        }
    }
}