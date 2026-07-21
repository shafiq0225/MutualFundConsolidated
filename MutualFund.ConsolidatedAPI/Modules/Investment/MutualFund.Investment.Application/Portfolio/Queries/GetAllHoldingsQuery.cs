using MutualFund.Investment.Application.Portfolio.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Portfolio.Queries
{
    public class GetAllHoldingsQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllHoldingsQuery> _logger;

        public GetAllHoldingsQuery(
            IUnitOfWork unitOfWork,
            ILogger<GetAllHoldingsQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Admin: get all active holdings with latest P&L.
        /// </summary>
        public async Task<Result<IEnumerable<HoldingDto>>> ExecuteAsync()
        {
            try
            {
                var holdingsList = (await _unitOfWork.Holdings.GetAllActiveAsync()).ToList();
                var holdingIds = holdingsList.Select(h => h.Id);
                var snapshotsDict = await _unitOfWork.Portfolio.GetLatestForHoldingsAsync(holdingIds);

                var result = holdingsList.Select(holding =>
                    PortfolioMapper.ToHoldingDto(
                        holding,
                        snapshotsDict.TryGetValue(holding.Id, out var snapshot) ? snapshot : null
                    )
                ).ToList();

                return Result<IEnumerable<HoldingDto>>
                    .Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all holdings");
                return Result<IEnumerable<HoldingDto>>
                    .Failure($"Failed to fetch holdings: {ex.Message}");
            }
        }
    }
}