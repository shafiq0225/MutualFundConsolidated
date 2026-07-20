using MutualFund.Investment.Application.Portfolio.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Portfolio.Queries
{
    public class GetFamilyPortfolioQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetFamilyPortfolioQuery> _logger;
        private readonly GetPortfolioQuery _portfolioQuery;

        public GetFamilyPortfolioQuery(
            IUnitOfWork unitOfWork,
            ILogger<GetFamilyPortfolioQuery> logger,
            GetPortfolioQuery portfolioQuery)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _portfolioQuery = portfolioQuery;
        }

        /// <summary>
        /// Returns combined portfolio for all investors.
        /// Phase 1: All users see all investments.
        /// Phase 2: filtered by family group membership.
        /// </summary>
        public async Task<Result<FamilyPortfolioDto>> ExecuteAsync(
            DateTime? asOfDate = null)
        {
            try
            {
                var reportDate = asOfDate?.Date ?? DateTime.Today;

                // ── Get all active holdings ────────────────────────
                var allHoldings = await _unitOfWork.Holdings
                    .GetAllActiveAsync();

                // ── Group by investor ──────────────────────────────
                var investorGroups = allHoldings
                    .GroupBy(h => new
                    {
                        h.InvestorUserId,
                        h.InvestorName
                    })
                    .ToList();

                if (!investorGroups.Any())
                {
                    return Result<FamilyPortfolioDto>.Success(
                        new FamilyPortfolioDto
                        {
                            ReportDate = reportDate,
                            InvestorPortfolios = new List<PortfolioReportDto>()
                        });
                }

                // ── Build per-investor reports ─────────────────────
                var investorReports = new List<PortfolioReportDto>();

                foreach (var group in investorGroups)
                {
                    var result = await _portfolioQuery.ExecuteAsync(
                        group.Key.InvestorUserId,
                        group.Key.InvestorName,
                        reportDate);

                    if (result.IsSuccess && result.Data != null)
                        investorReports.Add(result.Data);
                }

                // ── Calculate family grand total ───────────────────
                var familyInvested = investorReports
                    .Sum(r => r.TotalInvested);
                var familyCurrentValue = investorReports
                    .Sum(r => r.TotalCurrentValue);
                var familyPL = familyCurrentValue - familyInvested;
                var familyPercent = familyInvested > 0
                    ? Math.Round((familyPL / familyInvested) * 100, 4)
                    : 0;

                var familyPortfolio = new FamilyPortfolioDto
                {
                    ReportDate = reportDate,
                    TotalFamilyInvested = familyInvested,
                    TotalFamilyCurrentValue = familyCurrentValue,
                    TotalFamilyProfitLoss = familyPL,
                    FamilyReturnPercent = familyPercent,
                    IsFamilyProfit = familyPL >= 0,
                    InvestorPortfolios = investorReports
                        .OrderBy(r => r.InvestorName)
                };

                _logger.LogInformation(
                    "Family portfolio: {Investors} investors, " +
                    "FamilyInvested={Invested}, Current={Current}, " +
                    "P&L={PL} ({Pct}%)",
                    investorReports.Count,
                    familyInvested,
                    familyCurrentValue,
                    familyPL,
                    familyPercent);

                return Result<FamilyPortfolioDto>.Success(familyPortfolio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting family portfolio");
                return Result<FamilyPortfolioDto>
                    .Failure($"Failed to get family portfolio: {ex.Message}");
            }
        }
    }
}