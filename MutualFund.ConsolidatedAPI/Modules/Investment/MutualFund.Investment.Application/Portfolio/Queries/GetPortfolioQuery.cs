using MutualFund.Investment.Application.Portfolio.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Portfolio.Queries
{
    public class GetPortfolioQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPortfolioQuery> _logger;

        public GetPortfolioQuery(
            IUnitOfWork unitOfWork,
            ILogger<GetPortfolioQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Returns the portfolio report for one investor.
        /// Equivalent to the Excel sheet the staff used to prepare.
        /// </summary>
        public async Task<Result<PortfolioReportDto>> ExecuteAsync(
            string investorUserId,
            string investorName,
            DateTime? asOfDate = null)
        {
            try
            {
                var reportDate = asOfDate?.Date ?? DateTime.Today;

                // ── Get active holdings ────────────────────────────
                var holdings = await _unitOfWork.Holdings
                    .GetByInvestorAsync(investorUserId);

                var holdingList = holdings.ToList();

                if (!holdingList.Any())
                {
                    // Return empty report — not an error
                    return Result<PortfolioReportDto>.Success(
                        new PortfolioReportDto
                        {
                            InvestorUserId = investorUserId,
                            InvestorName = investorName,
                            ReportDate = reportDate,
                            Holdings = new List<PortfolioRowDto>()
                        });
                }

                // ── Get latest snapshot per holding ────────────────
                var rows = new List<PortfolioRowDto>();

                foreach (var holding in holdingList)
                {
                    var snapshot = await _unitOfWork.Portfolio
                        .GetLatestByHoldingAsync(holding.Id);

                    rows.Add(PortfolioMapper.ToRowDto(holding, snapshot));
                }

                // ── Calculate summary ──────────────────────────────
                var totalInvested = rows.Sum(r => r.InvestedAmount);
                var totalCurrentValue = rows.Sum(r => r.TotalAmount);
                var totalPL = totalCurrentValue - totalInvested;
                var overallPercent = totalInvested > 0
                    ? Math.Round((totalPL / totalInvested) * 100, 4)
                    : 0;

                var report = new PortfolioReportDto
                {
                    InvestorUserId = investorUserId,
                    InvestorName = investorName,
                    ReportDate = reportDate,
                    TotalInvested = totalInvested,
                    TotalCurrentValue = totalCurrentValue,
                    TotalProfitLoss = totalPL,
                    OverallReturnPercent = overallPercent,
                    IsOverallProfit = totalPL >= 0,
                    TotalHoldings = holdingList.Count,
                    Holdings = rows.OrderBy(r => r.SchemeName)
                };

                _logger.LogInformation(
                    "Portfolio for {Investor}: {Holdings} holdings, " +
                    "Invested={Invested}, Current={Current}, P&L={PL}",
                    investorName,
                    holdingList.Count,
                    totalInvested,
                    totalCurrentValue,
                    totalPL);

                return Result<PortfolioReportDto>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting portfolio for investor {Id}",
                    investorUserId);
                return Result<PortfolioReportDto>
                    .Failure($"Failed to get portfolio: {ex.Message}");
            }
        }
    }
}