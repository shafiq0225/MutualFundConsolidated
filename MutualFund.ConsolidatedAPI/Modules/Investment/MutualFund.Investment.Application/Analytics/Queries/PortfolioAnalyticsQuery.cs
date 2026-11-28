using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFund.Investment.Application.Analytics.Dtos;
using MutualFund.Investment.Domain.Common;
using MySqlConnector;

namespace MutualFund.Investment.Application.Analytics.Queries
{
    /// <summary>
    /// Computes the 4-tab BI Analytics Hub (SIP Step-Up, Year-Wise Analytics,
    /// Monthly-Wise Ledger Matrix, and Head-to-Head Comparison) from Active orders,
    /// Active holdings, and latest PortfolioSnapshots.
    /// </summary>
    public class PortfolioAnalyticsQuery
    {
        private readonly string _connString;
        private readonly ILogger<PortfolioAnalyticsQuery> _logger;

        public PortfolioAnalyticsQuery(
            IConfiguration configuration,
            ILogger<PortfolioAnalyticsQuery> logger)
        {
            _connString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        public async Task<Result<AnalyticsHubResponseDto>> ExecuteAsync(
            string? investorUserId = null,
            string? alternatePan = null)
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                var hasFilter = !string.IsNullOrWhiteSpace(investorUserId)
                    && !string.Equals(investorUserId, "all", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(investorUserId, "family", StringComparison.OrdinalIgnoreCase);

                var filterClause = hasFilter
                    ? "AND (InvestorUserId = @InvestorId OR (@AltPan <> '' AND InvestorUserId = @AltPan))"
                    : string.Empty;

                var param = new
                {
                    InvestorId = investorUserId ?? string.Empty,
                    AltPan = alternatePan ?? string.Empty
                };

                var orders = (await conn.QueryAsync<OrderRow>(
                    $@"SELECT SchemeCode, SchemeName, OrderDate, InvestedAmount
                       FROM investmentorders
                       WHERE Status = 'Active' {filterClause}
                       ORDER BY OrderDate ASC", param)).ToList();

                var holdings = (await conn.QueryAsync<HoldingRow>(
                    $@"SELECT h.Id, h.SchemeCode, h.SchemeName, h.InvestedAmount,
                              COALESCE(ps.CurrentValue, h.InvestedAmount) AS CurrentValue
                       FROM holdings h
                       LEFT JOIN (
                           SELECT s1.HoldingId, s1.CurrentValue
                           FROM portfoliosnapshots s1
                           INNER JOIN (
                               SELECT HoldingId, MAX(SnapshotDate) AS MaxDate
                               FROM portfoliosnapshots
                               GROUP BY HoldingId
                           ) s2 ON s1.HoldingId = s2.HoldingId AND s1.SnapshotDate = s2.MaxDate
                       ) ps ON ps.HoldingId = h.Id
                       WHERE h.IsActive = 1 {(hasFilter ? "AND (h.InvestorUserId = @InvestorId OR (@AltPan <> '' AND h.InvestorUserId = @AltPan))" : string.Empty)}",
                    param)).ToList();

                var years = orders
                    .Select(o => o.OrderDate.Year)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList();

                var currentCalendarYear = DateTime.UtcNow.AddHours(5.5).Year;
                var yearMonthsElapsed = new Dictionary<int, int>();
                foreach (var yr in years)
                {
                    if (yr < currentCalendarYear)
                    {
                        yearMonthsElapsed[yr] = 12;
                    }
                    else
                    {
                        var maxMonth = orders.Where(o => o.OrderDate.Year == yr).Max(o => o.OrderDate.Month);
                        yearMonthsElapsed[yr] = Math.Max(1, maxMonth);
                    }
                }

                // Canonical scheme name per SchemeCode (latest/longest)
                var schemeNames = orders
                    .GroupBy(o => o.SchemeCode)
                    .ToDictionary(g => g.Key, g => g.First().SchemeName);
                foreach (var h in holdings)
                {
                    if (!schemeNames.ContainsKey(h.SchemeCode))
                        schemeNames[h.SchemeCode] = h.SchemeName;
                }

                // Order schemes by total invested descending
                var schemeOrder = orders
                    .GroupBy(o => o.SchemeCode)
                    .Select(g => new { SchemeCode = g.Key, Total = g.Sum(x => x.InvestedAmount) })
                    .OrderByDescending(x => x.Total)
                    .Select(x => x.SchemeCode)
                    .ToList();

                var dto = new AnalyticsHubResponseDto
                {
                    Years = years,
                    YearMonthsElapsed = yearMonthsElapsed,
                    SipStepUp = BuildSipStepUp(orders, years, yearMonthsElapsed, schemeOrder, schemeNames),
                    YearWise = BuildYearWise(orders, years, yearMonthsElapsed, schemeOrder, schemeNames),
                    MonthlyLedger = BuildMonthlyLedger(orders, years, schemeOrder, schemeNames),
                    HeadToHead = BuildHeadToHead(holdings)
                };

                dto.TotalInvested = holdings.Sum(h => h.InvestedAmount);
                dto.TotalMarketValue = holdings.Sum(h => h.CurrentValue);
                dto.TotalProfitLoss = dto.TotalMarketValue - dto.TotalInvested;
                dto.OverallReturnPercent = dto.TotalInvested > 0
                    ? Math.Round((dto.TotalProfitLoss / dto.TotalInvested) * 100m, 2)
                    : 0m;

                return Result<AnalyticsHubResponseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing portfolio analytics");
                return Result<AnalyticsHubResponseDto>.Failure("Failed to compute portfolio analytics.");
            }
        }

        public async Task<Result<List<AnalyticsMonthOrderDto>>> GetMonthOrdersAsync(
            int year,
            int month,
            string? schemeCode = null,
            string? investorUserId = null,
            string? alternatePan = null)
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                var hasInvestor = !string.IsNullOrWhiteSpace(investorUserId)
                    && !string.Equals(investorUserId, "all", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(investorUserId, "family", StringComparison.OrdinalIgnoreCase);

                var hasScheme = !string.IsNullOrWhiteSpace(schemeCode);

                var sql = $@"
                    SELECT Id, OrderNumber, OrderDate, InvestorName, SchemeName,
                           COALESCE(FolioNumber, '') AS FolioNumber,
                           InvestedAmount, PurchaseNAV, UnitsAllotted, PaymentMode
                    FROM investmentorders
                    WHERE Status = 'Active'
                      AND YEAR(OrderDate) = @Year
                      AND MONTH(OrderDate) = @Month
                      {(hasScheme ? "AND SchemeCode = @SchemeCode" : string.Empty)}
                      {(hasInvestor ? "AND (InvestorUserId = @InvestorId OR (@AltPan <> '' AND InvestorUserId = @AltPan))" : string.Empty)}
                    ORDER BY OrderDate DESC, Id DESC";

                var list = (await conn.QueryAsync<AnalyticsMonthOrderDto>(sql, new
                {
                    Year = year,
                    Month = month,
                    SchemeCode = schemeCode ?? string.Empty,
                    InvestorId = investorUserId ?? string.Empty,
                    AltPan = alternatePan ?? string.Empty
                })).ToList();

                return Result<List<AnalyticsMonthOrderDto>>.Success(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching analytics month orders");
                return Result<List<AnalyticsMonthOrderDto>>.Failure("Failed to load month orders.");
            }
        }

        private static SipStepUpSummaryDto BuildSipStepUp(
            List<OrderRow> orders,
            List<int> years,
            Dictionary<int, int> yearMonthsElapsed,
            List<string> schemeOrder,
            Dictionary<string, string> schemeNames)
        {
            var summary = new SipStepUpSummaryDto
            {
                Years = years,
                FirstYear = years.FirstOrDefault(),
                CurrentYear = years.LastOrDefault(),
                PrevYear = years.Count >= 2 ? years[^2] : years.FirstOrDefault()
            };

            if (!years.Any()) return summary;

            foreach (var code in schemeOrder)
            {
                var schemeOrders = orders.Where(o => o.SchemeCode == code).ToList();
                var row = BuildStepUpRow(
                    code,
                    schemeNames[code],
                    schemeOrders,
                    years,
                    yearMonthsElapsed,
                    summary.FirstYear,
                    summary.PrevYear,
                    summary.CurrentYear);
                summary.Rows.Add(row);
            }

            summary.Totals = BuildStepUpRow(
                "TOTAL",
                "PORTFOLIO TOTAL",
                orders,
                years,
                yearMonthsElapsed,
                summary.FirstYear,
                summary.PrevYear,
                summary.CurrentYear);

            return summary;
        }

        private static SipStepUpSchemeRowDto BuildStepUpRow(
            string code,
            string name,
            List<OrderRow> orders,
            List<int> years,
            Dictionary<int, int> yearMonthsElapsed,
            int firstYear,
            int prevYear,
            int currentYear)
        {
            var yearlyTotals = new Dictionary<int, decimal>();
            var avgMonthly = new Dictionary<int, decimal>();

            foreach (var yr in years)
            {
                var sum = orders.Where(o => o.OrderDate.Year == yr).Sum(o => o.InvestedAmount);
                yearlyTotals[yr] = Math.Round(sum, 2);
                var months = yearMonthsElapsed.TryGetValue(yr, out var m) && m > 0 ? m : 12;
                avgMonthly[yr] = Math.Round(sum / months, 2);
            }

            var prevInvested = yearlyTotals.GetValueOrDefault(prevYear);
            var currInvested = yearlyTotals.GetValueOrDefault(currentYear);
            var prevAvg = avgMonthly.GetValueOrDefault(prevYear);
            var currAvg = avgMonthly.GetValueOrDefault(currentYear);

            // Compare First -> Prev if >= 3 years (matching Excel Table 2: 2024 -> 2025), else Prev -> Curr
            var baseYear = years.Count >= 3 ? firstYear : prevYear;
            var compYear = years.Count >= 3 ? prevYear : currentYear;
            var baseInv = yearlyTotals.GetValueOrDefault(baseYear);
            var compInv = yearlyTotals.GetValueOrDefault(compYear);
            var stepUpRs = compInv - baseInv;
            var stepUpPct = baseInv > 0 ? Math.Round((stepUpRs / baseInv) * 100m, 2) : 0m;

            var currAnnualised = Math.Round(currAvg * 12m, 2);
            var currVsPrevAnnualPct = prevInvested > 0
                ? Math.Round(((currAnnualised - prevInvested) / prevInvested) * 100m, 2)
                : 0m;

            var baseAvg = avgMonthly.GetValueOrDefault(baseYear);
            var compAvg = avgMonthly.GetValueOrDefault(compYear);
            var monthlyIncRs = compAvg - baseAvg;
            var monthlyIncPct = baseAvg > 0 ? Math.Round((monthlyIncRs / baseAvg) * 100m, 2) : 0m;

            return new SipStepUpSchemeRowDto
            {
                SchemeCode = code,
                SchemeName = name,
                YearlyTotals = yearlyTotals,
                GrandTotal = Math.Round(orders.Sum(o => o.InvestedAmount), 2),
                AvgMonthlyByYear = avgMonthly,
                PrevToCurrStepUpRs = Math.Round(stepUpRs, 2),
                PrevToCurrStepUpPercent = stepUpPct,
                CurrVsPrevAnnualisedPercent = currVsPrevAnnualPct,
                MonthlyIncrementRs = Math.Round(monthlyIncRs, 2),
                MonthlyIncrementPercent = monthlyIncPct,
                AnnualisedRunRate = currAnnualised
            };
        }

        private static YearWiseAnalyticsDto BuildYearWise(
            List<OrderRow> orders,
            List<int> years,
            Dictionary<int, int> yearMonthsElapsed,
            List<string> schemeOrder,
            Dictionary<string, string> schemeNames)
        {
            var result = new YearWiseAnalyticsDto();
            var grandTotal = orders.Sum(o => o.InvestedAmount);
            var currentYear = DateTime.UtcNow.AddHours(5.5).Year;
            decimal cumulative = 0m;

            foreach (var yr in years)
            {
                var yrOrders = orders.Where(o => o.OrderDate.Year == yr).ToList();
                var yrSum = Math.Round(yrOrders.Sum(o => o.InvestedAmount), 2);
                cumulative += yrSum;

                result.Summary.Add(new YearWiseSummaryRowDto
                {
                    Year = yr,
                    InvestedAmount = yrSum,
                    ShareOfTotalPercent = grandTotal > 0 ? Math.Round((yrSum / grandTotal) * 100m, 2) : 0m,
                    CumulativeInvested = Math.Round(cumulative, 2),
                    Status = yr < currentYear ? "Completed" : "Active / In Progress"
                });

                var months = yearMonthsElapsed.TryGetValue(yr, out var m) && m > 0 ? m : 12;
                var breakdown = new YearSchemeBreakdownDto
                {
                    Year = yr,
                    MonthsElapsed = months,
                    TotalInvested = yrSum,
                    AvgMonthlySip = Math.Round(yrSum / months, 2)
                };

                foreach (var code in schemeOrder)
                {
                    var schemeSum = Math.Round(yrOrders.Where(o => o.SchemeCode == code).Sum(o => o.InvestedAmount), 2);
                    breakdown.Schemes.Add(new YearSchemeItemDto
                    {
                        SchemeCode = code,
                        SchemeName = schemeNames[code],
                        InvestedAmount = schemeSum,
                        ShareOfYearPercent = yrSum > 0 ? Math.Round((schemeSum / yrSum) * 100m, 2) : 0m,
                        AvgMonthlySip = Math.Round(schemeSum / months, 2)
                    });
                }

                result.Breakdowns.Add(breakdown);
            }

            return result;
        }

        private static List<MonthlyLedgerYearDto> BuildMonthlyLedger(
            List<OrderRow> orders,
            List<int> years,
            List<string> schemeOrder,
            Dictionary<string, string> schemeNames)
        {
            var list = new List<MonthlyLedgerYearDto>();
            foreach (var yr in years.OrderByDescending(y => y))
            {
                var yrOrders = orders.Where(o => o.OrderDate.Year == yr).ToList();
                var yrDto = new MonthlyLedgerYearDto
                {
                    Year = yr,
                    YearTotal = Math.Round(yrOrders.Sum(o => o.InvestedAmount), 2)
                };

                for (int m = 1; m <= 12; m++)
                {
                    yrDto.MonthlyTotals[m - 1] = Math.Round(
                        yrOrders.Where(o => o.OrderDate.Month == m).Sum(o => o.InvestedAmount), 2);
                }

                foreach (var code in schemeOrder)
                {
                    var sOrders = yrOrders.Where(o => o.SchemeCode == code).ToList();
                    var row = new MonthlyLedgerSchemeRowDto
                    {
                        SchemeCode = code,
                        SchemeName = schemeNames[code],
                        YearTotal = Math.Round(sOrders.Sum(o => o.InvestedAmount), 2)
                    };
                    for (int m = 1; m <= 12; m++)
                    {
                        var monthOrders = sOrders.Where(o => o.OrderDate.Month == m).ToList();
                        row.Months[m - 1] = Math.Round(monthOrders.Sum(o => o.InvestedAmount), 2);
                        row.OrderCounts[m - 1] = monthOrders.Count;
                    }
                    yrDto.Schemes.Add(row);
                }

                list.Add(yrDto);
            }

            return list;
        }

        private static HeadToHeadComparisonDto BuildHeadToHead(List<HoldingRow> holdings)
        {
            var result = new HeadToHeadComparisonDto();
            if (!holdings.Any()) return result;

            var totalInv = holdings.Sum(h => h.InvestedAmount);
            var totalVal = holdings.Sum(h => h.CurrentValue);
            var totalPnl = totalVal - totalInv;

            var groupedSchemes = holdings
                .GroupBy(h => h.SchemeCode)
                .Select(g =>
                {
                    var inv = g.Sum(x => x.InvestedAmount);
                    var val = g.Sum(x => x.CurrentValue);
                    var pnl = val - inv;
                    var name = g.First().SchemeName;
                    var (detailCat, groupCat) = ClassifyScheme(name);

                    return new HeadToHeadSchemeRowDto
                    {
                        SchemeCode = g.Key,
                        SchemeName = name,
                        AssetCategory = detailCat,
                        AssetClassGroup = groupCat,
                        InvestedCapital = Math.Round(inv, 2),
                        MarketValue = Math.Round(val, 2),
                        ProfitLoss = Math.Round(pnl, 2),
                        ReturnPercent = inv > 0 ? Math.Round((pnl / inv) * 100m, 2) : 0m,
                        PortfolioWeightPercent = totalInv > 0 ? Math.Round((inv / totalInv) * 100m, 2) : 0m,
                        ValuationSharePercent = totalVal > 0 ? Math.Round((val / totalVal) * 100m, 2) : 0m,
                        ProfitSharePercent = totalPnl != 0 ? Math.Round((pnl / totalPnl) * 100m, 2) : 0m
                    };
                })
                .OrderByDescending(x => x.InvestedCapital)
                .ToList();

            result.Schemes = groupedSchemes;

            var catOrder = new[]
            {
                "Equity (Mid Cap & Multi Cap)",
                "Debt & Fixed Income",
                "Commodities & Precious Metals",
                "Tax Saver - ELSS (80C)",
                "Hybrid"
            };

            result.Categories = groupedSchemes
                .GroupBy(s => s.AssetClassGroup)
                .Select(g =>
                {
                    var inv = g.Sum(x => x.InvestedCapital);
                    var val = g.Sum(x => x.MarketValue);
                    var pnl = val - inv;
                    return new HeadToHeadCategoryRowDto
                    {
                        AssetCategory = g.Key,
                        SchemeCount = g.Count(),
                        InvestedCapital = Math.Round(inv, 2),
                        MarketValue = Math.Round(val, 2),
                        ProfitLoss = Math.Round(pnl, 2),
                        ReturnPercent = inv > 0 ? Math.Round((pnl / inv) * 100m, 2) : 0m,
                        CategorySharePercent = totalInv > 0 ? Math.Round((inv / totalInv) * 100m, 2) : 0m
                    };
                })
                .OrderBy(c =>
                {
                    var idx = Array.IndexOf(catOrder, c.AssetCategory);
                    return idx >= 0 ? idx : 99;
                })
                .ToList();

            return result;
        }

        private static (string DetailCategory, string GroupCategory) ClassifyScheme(string schemeName)
        {
            var s = (schemeName ?? string.Empty).ToLowerInvariant();
            if (s.Contains("elss") || s.Contains("tax saver"))
                return ("Tax Saver - ELSS (80C)", "Tax Saver - ELSS (80C)");

            if (s.Contains("gold") && s.Contains("silver"))
                return ("Commodities - Gold & Silver", "Commodities & Precious Metals");
            if (s.Contains("gold") && s.Contains("etf"))
                return ("Commodities - Gold ETF", "Commodities & Precious Metals");
            if (s.Contains("gold") || s.Contains("silver"))
                return ("Commodities - Gold", "Commodities & Precious Metals");

            if (s.Contains("debt") || s.Contains("bond") || s.Contains("liquid") || s.Contains("gilt") || s.Contains("psu") || s.Contains("income"))
                return (s.Contains("psu") ? "Debt - Banking & PSU" : "Debt & Fixed Income", "Debt & Fixed Income");

            if (s.Contains("hybrid") || s.Contains("balance"))
                return ("Hybrid", "Hybrid");

            if (s.Contains("midcap") || s.Contains("mid cap"))
                return ("Equity - Mid Cap", "Equity (Mid Cap & Multi Cap)");
            if (s.Contains("multi cap") || s.Contains("multicap"))
                return ("Equity - Multi Cap", "Equity (Mid Cap & Multi Cap)");
            if (s.Contains("small cap") || s.Contains("smallcap"))
                return ("Equity - Small Cap", "Equity (Mid Cap & Multi Cap)");
            if (s.Contains("large cap") || s.Contains("largecap"))
                return ("Equity - Large Cap", "Equity (Mid Cap & Multi Cap)");

            return ("Equity", "Equity (Mid Cap & Multi Cap)");
        }

        private class OrderRow
        {
            public string SchemeCode { get; set; } = string.Empty;
            public string SchemeName { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public decimal InvestedAmount { get; set; }
        }

        private class HoldingRow
        {
            public int Id { get; set; }
            public string SchemeCode { get; set; } = string.Empty;
            public string SchemeName { get; set; } = string.Empty;
            public decimal InvestedAmount { get; set; }
            public decimal CurrentValue { get; set; }
        }
    }
}
