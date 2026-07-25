using MutualFund.Investment.Application.Family.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Interfaces;
using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Family.Queries
{
    public class FamilyPortfolioQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _connString;
        private readonly ILogger<FamilyPortfolioQuery> _logger;

        // 5 years + 30-day buffer so the last fetch always has enough history
        private const int NavHistoryDays = 1856;

        public FamilyPortfolioQuery(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<FamilyPortfolioQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _connString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        // ── Screen 1: Family Overview ──────────────────────────────
        public async Task<Result<FamilyOverviewDto>> GetFamilyOverviewAsync()
        {
            try
            {
                var holdings = await _unitOfWork.Holdings.GetAllActiveGroupedAsync();
                var holdingList = holdings.ToList();

                if (!holdingList.Any())
                    return Result<FamilyOverviewDto>.Success(
                        new FamilyOverviewDto { ReportDate = DateTime.Today });

                var snapshotMap = await GetLatestSnapshotsAsync(
                    holdingList.Select(h => h.Id).ToList());
                var navHistoryMap = await GetNavHistoryAsync(
                    holdingList.Select(h => h.SchemeCode).Distinct().ToList());

                var memberGroups = holdingList
                    .GroupBy(h => new { h.InvestorUserId, h.InvestorName })
                    .ToList();

                var members = new List<MemberSummaryDto>();

                foreach (var group in memberGroups)
                {
                    decimal invested = 0, currentValue = 0;
                    var schemes = new HashSet<string>();

                    foreach (var h in group)
                    {
                        invested += h.InvestedAmount;
                        schemes.Add(h.SchemeCode);
                        currentValue += snapshotMap.TryGetValue(h.Id, out var snap)
                            ? snap.currentValue
                            : h.InvestedAmount;
                    }

                    var gain = currentValue - invested;
                    var gainPct = invested > 0
                        ? Math.Round((gain / invested) * 100, 4) : 0;

                    var mh = group.ToList();

                    members.Add(new MemberSummaryDto
                    {
                        InvestorUserId = group.Key.InvestorUserId,
                        InvestorName = group.Key.InvestorName,
                        Initials = GetInitials(group.Key.InvestorName),
                        TotalInvested = invested,
                        TotalCurrentValue = currentValue,
                        TotalGain = Math.Round(gain, 2),
                        TotalGainPercent = gainPct,
                        IsGain = gain >= 0,
                        SchemeCount = schemes.Count,
                        HoldingCount = group.Count(),
                        CategorySummary = GetCategorySummary(mh),

                        DayBefore = CalcMemberPreviousTwoNavsReturn("D-2", mh, navHistoryMap),
                        Yesterday = CalcMemberLastTwoNavsReturn("Yest", mh, navHistoryMap),
                        ThisWeek = CalcMemberPeriodReturn("1W", 7, 0m, mh, snapshotMap, navHistoryMap),
                        OneMonth = CalcMemberPeriodReturn("1M", 30, 0m, mh, snapshotMap, navHistoryMap),
                        OneYear = CalcMemberPeriodReturn("1Y", 365, 1m, mh, snapshotMap, navHistoryMap),
                        ThreeYear = CalcMemberPeriodReturn("3Y", 1095, 3m, mh, snapshotMap, navHistoryMap),
                        FiveYear = CalcMemberPeriodReturn("5Y", 1825, 5m, mh, snapshotMap, navHistoryMap),
                        ProgressiveDailyReturns = CalcProgressiveDailyReturnsForHoldings(mh, navHistoryMap)
                    });
                }

                var totalInvested = members.Sum(m => m.TotalInvested);
                var totalValue = members.Sum(m => m.TotalCurrentValue);
                var totalGain = totalValue - totalInvested;
                var totalGainPct = totalInvested > 0
                    ? Math.Round((totalGain / totalInvested) * 100, 4) : 0;

                var familyYesterday = CalcMemberLastTwoNavsReturn(
                    "Yest", holdingList, navHistoryMap);
                var (eq, dbt, hyb) = GetSchemeCategoryCounts(holdingList);

                return Result<FamilyOverviewDto>.Success(new FamilyOverviewDto
                {
                    TotalFamilyInvested = totalInvested,
                    TotalFamilyCurrentValue = totalValue,
                    TotalFamilyGain = Math.Round(totalGain, 2),
                    TotalFamilyGainPercent = totalGainPct,
                    IsFamilyGain = totalGain >= 0,
                    TotalMembers = members.Count,
                    TotalSchemes = holdingList
                        .Select(h => h.SchemeCode).Distinct().Count(),
                    EquitySchemeCount = eq,
                    DebtSchemeCount = dbt,
                    HybridSchemeCount = hyb,
                    FamilyYesterdayReturn = familyYesterday,
                    ProgressiveDailyReturns = CalcProgressiveDailyReturnsForHoldings(holdingList, navHistoryMap),
                    ReportDate = DateTime.Today,
                    Members = members.OrderBy(m => m.InvestorName)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting family overview");
                return Result<FamilyOverviewDto>.Failure($"Failed: {ex.Message}");
            }
        }

        // ── Screen 2: Member Holdings ──────────────────────────────
        public async Task<Result<MemberHoldingsDto>> GetMemberHoldingsAsync(
            string investorUserId)
        {
            try
            {
                var holdings = await _unitOfWork.Holdings
                    .GetAllByInvestorActiveAsync(investorUserId);
                var holdingList = holdings.ToList();

                if (!holdingList.Any())
                    return Result<MemberHoldingsDto>.Failure(
                        "No active holdings found for this investor.");

                var snapshotMap = await GetLatestSnapshotsAsync(
                    holdingList.Select(h => h.Id).ToList());
                var navHistoryMap = await GetNavHistoryAsync(
                    holdingList.Select(h => h.SchemeCode).Distinct().ToList());

                var cards = new List<HoldingCardDto>();

                foreach (var h in holdingList)
                {
                    var hasSnap = snapshotMap.TryGetValue(h.Id, out var snap);
                    var currentNAV = hasSnap ? snap.currentNAV : 0;
                    var currentValue = hasSnap ? snap.currentValue : h.InvestedAmount;
                    var gain = currentValue - h.InvestedAmount;
                    var gainPct = h.InvestedAmount > 0
                        ? Math.Round((gain / h.InvestedAmount) * 100, 4) : 0;

                    navHistoryMap.TryGetValue(h.SchemeCode, out var navHistory);

                    cards.Add(new HoldingCardDto
                    {
                        HoldingId = h.Id,
                        SchemeCode = h.SchemeCode,
                        SchemeName = h.SchemeName,
                        FundName = h.FundName,
                        FolioNumber = h.FolioNumber,
                        OrderNumber = h.Order?.OrderNumber ?? string.Empty,
                        InvestedAmount = h.InvestedAmount,
                        Units = h.Units,
                        PurchaseNAV = h.PurchaseNAV,
                        CurrentNAV = currentNAV,
                        CurrentValue = Math.Round(currentValue, 2),
                        Gain = Math.Round(gain, 2),
                        GainPercent = gainPct,
                        IsGain = gain >= 0,

                        DayBefore = CalcQuickReturnPreviousTwoNavs("D-2", navHistory, h.Units),
                        Yesterday = CalcQuickReturnLastTwoNavs("Yest", navHistory, h.Units),
                        ThisWeek = CalcQuickReturn("1W", 7, 0m, navHistory, currentNAV, h.Units),
                        OneMonth = CalcQuickReturn("1M", 30, 0m, navHistory, currentNAV, h.Units),
                        SixMonth = CalcQuickReturn("6M", 180, 0m, navHistory, currentNAV, h.Units),
                        OneYear = CalcQuickReturn("1Y", 365, 1m, navHistory, currentNAV, h.Units),
                        ThreeYear = CalcQuickReturn("3Y", 1095, 3m, navHistory, currentNAV, h.Units),
                        FiveYear = CalcQuickReturn("5Y", 1825, 5m, navHistory, currentNAV, h.Units),
                        ProgressiveDailyReturns = CalcProgressiveDailyReturnsForScheme(navHistory, h.Units)
                    });
                }

                var totalInvested = cards.Sum(c => c.InvestedAmount);
                var totalValue = cards.Sum(c => c.CurrentValue);
                var totalGain = totalValue - totalInvested;
                var totalGainPct = totalInvested > 0
                    ? Math.Round((totalGain / totalInvested) * 100, 4) : 0;

                var first = holdingList.First();

                return Result<MemberHoldingsDto>.Success(new MemberHoldingsDto
                {
                    InvestorUserId = first.InvestorUserId,
                    InvestorName = first.InvestorName,
                    Initials = GetInitials(first.InvestorName),
                    TotalInvested = totalInvested,
                    TotalCurrentValue = totalValue,
                    TotalGain = Math.Round(totalGain, 2),
                    TotalGainPercent = totalGainPct,
                    IsGain = totalGain >= 0,
                    Holdings = cards.OrderBy(c => c.SchemeName)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting holdings for {UserId}", investorUserId);
                return Result<MemberHoldingsDto>.Failure($"Failed: {ex.Message}");
            }
        }

        // ── Screen 3: Scheme Detail ────────────────────────────────
        public async Task<Result<HoldingSchemeDetailDto>> GetSchemeDetailAsync(
            string investorUserId, string schemeCode)
        {
            try
            {
                var holdings = await _unitOfWork.Holdings
                    .GetAllByInvestorActiveAsync(investorUserId);
                var holding = holdings.FirstOrDefault(h => h.SchemeCode == schemeCode);

                if (holding == null)
                    return Result<HoldingSchemeDetailDto>.Failure(
                        $"No holding found for scheme {schemeCode}.");

                var snapshot = await _unitOfWork.Portfolio.GetLatestByHoldingAsync(holding.Id);
                var currentNAV = snapshot?.CurrentNAV ?? 0;
                var currentValue = snapshot?.CurrentValue ?? holding.InvestedAmount;

                var navHistoryMap = await GetNavHistoryAsync(new List<string> { schemeCode });
                navHistoryMap.TryGetValue(schemeCode, out var navHistory);
                var navList = navHistory ?? new List<NavRecord>();

                // Daily change
                var previousNav = navList.Count >= 2 ? navList[1].NAV : 0;
                var previousNavDate = navList.Count >= 2 ? navList[1].NavDate : DateTime.MinValue;
                var dailyChange = currentNAV > 0 && previousNav > 0
                    ? currentNAV - previousNav : 0;
                var dailyChangePct = previousNav > 0
                    ? Math.Round((dailyChange / previousNav) * 100, 4) : 0;

                // This week
                var latestNavDate = navList.Count > 0 ? navList[0].NavDate : DateTime.Today;
                var monday = GetThisMonday(latestNavDate);
                var weekRecord = navList
                    .Where(n => n.NavDate.Date <= monday.Date)
                    .OrderByDescending(n => n.NavDate)
                    .FirstOrDefault();

                decimal? weekReturn = null, weekGainAmount = null;
                if (weekRecord != null && weekRecord.NAV > 0 && currentNAV > 0)
                {
                    weekReturn = Math.Round(
                        ((currentNAV - weekRecord.NAV) / weekRecord.NAV) * 100, 4);
                    weekGainAmount = Math.Round(
                        holding.Units * (currentNAV - weekRecord.NAV), 2);
                }

                var totalGain = currentValue - holding.InvestedAmount;
                var totalGainPct = holding.InvestedAmount > 0
                    ? Math.Round((totalGain / holding.InvestedAmount) * 100, 4) : 0;
                var avgBuyNAV = holding.Units > 0
                    ? Math.Round(holding.InvestedAmount / holding.Units, 4) : 0;

                return Result<HoldingSchemeDetailDto>.Success(new HoldingSchemeDetailDto
                {
                    SchemeCode = holding.SchemeCode,
                    SchemeName = holding.SchemeName,
                    FundName = holding.FundName,
                    FolioNumber = holding.FolioNumber,
                    OrderNumber = holding.Order?.OrderNumber ?? string.Empty,
                    IsApproved = true,
                    InvestedAmount = holding.InvestedAmount,
                    Units = holding.Units,
                    AvgBuyNAV = avgBuyNAV,
                    CurrentNAV = currentNAV,
                    CurrentNavDateText = navList.Count > 0
                        ? navList[0].NavDate.ToString("dd MMM yyyy") : string.Empty,
                    CurrentValue = Math.Round(currentValue, 2),
                    TotalGain = Math.Round(totalGain, 2),
                    TotalGainPercent = totalGainPct,
                    IsTotalGain = totalGain >= 0,
                    PreviousNAV = previousNav,
                    PreviousNavDateText = previousNavDate != DateTime.MinValue
                        ? previousNavDate.ToString("dd MMM yyyy") : string.Empty,
                    DailyChange = Math.Round(dailyChange, 4),
                    DailyChangePercent = dailyChangePct,
                    IsDailyUp = dailyChange >= 0,
                    WeekStartNAV = weekRecord?.NAV,
                    WeekStartDateText = weekRecord?.NavDate.ToString("dd MMM yyyy") ?? string.Empty,
                    WeekReturn = weekReturn,
                    WeekGainAmount = weekGainAmount,
                    IsWeekUp = weekReturn.GetValueOrDefault() >= 0,

                    OneMonth = CalcPeriodDetail("1 Month", 30, 0m, navList, currentNAV, holding.Units),
                    ThreeMonth = CalcPeriodDetail("3 Month", 90, 0m, navList, currentNAV, holding.Units),
                    SixMonth = CalcPeriodDetail("6 Month", 180, 0m, navList, currentNAV, holding.Units),
                    OneYear = CalcPeriodDetail("1 Year", 365, 1m, navList, currentNAV, holding.Units),
                    ThreeYear = CalcPeriodDetail("3 Year", 1095, 3m, navList, currentNAV, holding.Units),
                    FiveYear = CalcPeriodDetail("5 Year", 1825, 5m, navList, currentNAV, holding.Units),
                    ProgressiveDailyReturns = CalcProgressiveDailyReturnsForScheme(navList, holding.Units)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting scheme detail for {UserId}/{Code}",
                    investorUserId, schemeCode);
                return Result<HoldingSchemeDetailDto>.Failure($"Failed: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calculates return % and P&L between the last two available NAV dates
        /// (e.g. Wednesday vs Tuesday, or Friday vs Thursday on weekends/Mondays).
        /// </summary>
        private QuickReturnDto CalcMemberLastTwoNavsReturn(
            string label,
            List<Domain.Entities.Holding> holdings,
            Dictionary<string, List<NavRecord>> navHistoryMap)
        {
            decimal totalLatestVal = 0;
            decimal totalPreviousVal = 0;
            int covered = 0;
            DateTime previousNavDate = DateTime.MinValue;

            foreach (var h in holdings)
            {
                if (!navHistoryMap.TryGetValue(h.SchemeCode, out var navList)
                    || navList == null || !navList.Any())
                    continue;

                var nav1 = navList[0].NAV;
                var nav2 = navList.Count >= 2 ? navList[1].NAV : navList[0].NAV;

                if (navList.Count >= 2 && navList[1].NavDate > previousNavDate)
                {
                    previousNavDate = navList[1].NavDate;
                }
                else if (previousNavDate == DateTime.MinValue && navList[0].NavDate != DateTime.MinValue)
                {
                    previousNavDate = navList[0].NavDate;
                }

                totalLatestVal += h.Units * nav1;
                totalPreviousVal += h.Units * nav2;
                covered++;
            }

            if (covered == 0 || totalPreviousVal <= 0)
                return new QuickReturnDto { Label = label, HasData = false };

            var gain = totalLatestVal - totalPreviousVal;
            var pct = Math.Round((gain / totalPreviousVal) * 100, 4);

            return new QuickReturnDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = 0,
                IsPositive = gain >= 0,
                HasData = true,
                IsPartialPeriod = false,
                ActualFromDate = previousNavDate != DateTime.MinValue ? previousNavDate.ToString("dd MMM yyyy") : string.Empty
            };
        }

        /// <summary>
        /// Calculates return % and P&L between the 2nd and 3rd available NAV dates (Day Before Yesterday).
        /// </summary>
        private QuickReturnDto CalcMemberPreviousTwoNavsReturn(
            string label,
            List<Domain.Entities.Holding> holdings,
            Dictionary<string, List<NavRecord>> navHistoryMap)
        {
            decimal totalVal2 = 0;
            decimal totalVal3 = 0;
            int covered = 0;
            DateTime date3 = DateTime.MinValue;

            foreach (var h in holdings)
            {
                if (!navHistoryMap.TryGetValue(h.SchemeCode, out var navList)
                    || navList == null || !navList.Any())
                    continue;

                var nav2 = navList.Count >= 2 ? navList[1].NAV : navList[0].NAV;
                var nav3 = navList.Count >= 3 ? navList[2].NAV : (navList.Count >= 2 ? navList[1].NAV : navList[0].NAV);

                if (navList.Count >= 3 && navList[2].NavDate > date3)
                {
                    date3 = navList[2].NavDate;
                }

                totalVal2 += h.Units * nav2;
                totalVal3 += h.Units * nav3;
                covered++;
            }

            if (covered == 0 || totalVal3 <= 0)
                return new QuickReturnDto { Label = label, HasData = false };

            var gain = totalVal2 - totalVal3;
            var pct = Math.Round((gain / totalVal3) * 100, 4);

            return new QuickReturnDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = 0,
                IsPositive = gain >= 0,
                HasData = true,
                IsPartialPeriod = false,
                ActualFromDate = date3 != DateTime.MinValue ? date3.ToString("dd MMM yyyy") : string.Empty
            };
        }

        /// <summary>
        /// Calculates return % and P&L between the last two available NAV dates for a single scheme holding.
        /// </summary>
        private QuickReturnDto CalcQuickReturnLastTwoNavs(
            string label,
            List<NavRecord>? navList,
            decimal units)
        {
            if (navList == null || !navList.Any() || units <= 0)
                return new QuickReturnDto { Label = label, HasData = false };

            var nav1 = navList[0].NAV;
            var nav2 = navList.Count >= 2 ? navList[1].NAV : navList[0].NAV;

            var latestVal = units * nav1;
            var previousVal = units * nav2;
            var gain = latestVal - previousVal;
            var pct = previousVal > 0
                ? Math.Round((gain / previousVal) * 100, 4) : 0;

            return new QuickReturnDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = 0,
                IsPositive = gain >= 0,
                HasData = true,
                IsPartialPeriod = false,
                ActualFromDate = navList.Count >= 2 ? navList[1].NavDate.ToString("dd MMM yyyy") : navList[0].NavDate.ToString("dd MMM yyyy")
            };
        }

        /// <summary>
        /// Calculates return % and P&L between the 2nd and 3rd available NAV dates for a single scheme holding.
        /// </summary>
        private QuickReturnDto CalcQuickReturnPreviousTwoNavs(
            string label,
            List<NavRecord>? navList,
            decimal units)
        {
            if (navList == null || !navList.Any() || units <= 0)
                return new QuickReturnDto { Label = label, HasData = false };

            var nav2 = navList.Count >= 2 ? navList[1].NAV : navList[0].NAV;
            var nav3 = navList.Count >= 3 ? navList[2].NAV : (navList.Count >= 2 ? navList[1].NAV : navList[0].NAV);

            var val2 = units * nav2;
            var val3 = units * nav3;
            var gain = val2 - val3;
            var pct = val3 > 0
                ? Math.Round((gain / val3) * 100, 4) : 0;

            return new QuickReturnDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = 0,
                IsPositive = gain >= 0,
                HasData = true,
                IsPartialPeriod = false,
                ActualFromDate = navList.Count >= 3 ? navList[2].NavDate.ToString("dd MMM yyyy") : string.Empty
            };
        }

        /// <summary>
        /// Calculates the progressive daily return records for a list of holdings based on the 
        /// day of week of the latest available NAV data (1 to 6 records maximum).
        /// </summary>
        private List<QuickReturnDto> CalcProgressiveDailyReturnsForHoldings(
            List<Domain.Entities.Holding> holdings,
            Dictionary<string, List<NavRecord>> navHistoryMap)
        {
            var list = new List<QuickReturnDto>();
            if (holdings == null || !holdings.Any()) return list;

            DateTime latestNavDate = DateTime.MinValue;
            foreach (var h in holdings)
            {
                if (navHistoryMap.TryGetValue(h.SchemeCode, out var navs) && navs != null && navs.Any())
                {
                    if (navs[0].NavDate > latestNavDate) latestNavDate = navs[0].NavDate;
                }
            }

            if (latestNavDate == DateTime.MinValue) return list;

            int nRecords;
            switch (latestNavDate.DayOfWeek)
            {
                case DayOfWeek.Monday: nRecords = 2; break;
                case DayOfWeek.Tuesday: nRecords = 3; break;
                case DayOfWeek.Wednesday: nRecords = 4; break;
                case DayOfWeek.Thursday: nRecords = 5; break;
                case DayOfWeek.Friday: nRecords = 6; break;
                default: nRecords = 6; break;
            }

            for (int k = 0; k < nRecords; k++)
            {
                decimal valNew = 0;
                decimal valOld = 0;
                int covered = 0;
                DateTime newDate = DateTime.MinValue;
                DateTime oldDate = DateTime.MinValue;

                foreach (var h in holdings)
                {
                    if (!navHistoryMap.TryGetValue(h.SchemeCode, out var navs) || navs == null || navs.Count <= k)
                        continue;

                    var navNew = navs[k].NAV;
                    var navOld = (k + 1 < navs.Count) ? navs[k + 1].NAV : navs[k].NAV;

                    if (navs[k].NavDate > newDate) newDate = navs[k].NavDate;
                    if (k + 1 < navs.Count && navs[k + 1].NavDate > oldDate) oldDate = navs[k + 1].NavDate;

                    valNew += h.Units * navNew;
                    valOld += h.Units * navOld;
                    covered++;
                }

                if (covered == 0 || valOld <= 0) break;

                var gain = valNew - valOld;
                var pct = Math.Round((gain / valOld) * 100, 4);

                string oldStr = oldDate != DateTime.MinValue ? oldDate.ToString("dd MMM") : string.Empty;
                string newStr = newDate != DateTime.MinValue ? newDate.ToString("ddd, dd MMM") : string.Empty;

                string label = newStr;

                list.Add(new QuickReturnDto
                {
                    Label = label,
                    ReturnPercent = pct,
                    PeriodGainAmount = Math.Round(gain, 2),
                    CagrPercent = 0,
                    IsPositive = gain >= 0,
                    HasData = true,
                    IsPartialPeriod = false,
                    ActualFromDate = oldStr
                });
            }

            return list;
        }

        private List<QuickReturnDto> CalcProgressiveDailyReturnsForScheme(
            List<NavRecord>? navs,
            decimal units)
        {
            var list = new List<QuickReturnDto>();
            if (navs == null || !navs.Any() || units <= 0) return list;

            var latestNavDate = navs[0].NavDate;

            int nRecords;
            switch (latestNavDate.DayOfWeek)
            {
                case DayOfWeek.Monday: nRecords = 2; break;
                case DayOfWeek.Tuesday: nRecords = 3; break;
                case DayOfWeek.Wednesday: nRecords = 4; break;
                case DayOfWeek.Thursday: nRecords = 5; break;
                case DayOfWeek.Friday: nRecords = 6; break;
                default: nRecords = 6; break;
            }

            for (int k = 0; k < nRecords; k++)
            {
                if (k >= navs.Count) break;

                var navNew = navs[k].NAV;
                var navOld = (k + 1 < navs.Count) ? navs[k + 1].NAV : navs[k].NAV;

                var valNew = units * navNew;
                var valOld = units * navOld;
                if (valOld <= 0) break;

                var gain = valNew - valOld;
                var pct = Math.Round((gain / valOld) * 100, 4);

                string oldStr = (k + 1 < navs.Count) ? navs[k + 1].NavDate.ToString("dd MMM") : navs[k].NavDate.ToString("dd MMM");
                string newStr = navs[k].NavDate.ToString("ddd, dd MMM");

                string label = newStr;

                list.Add(new QuickReturnDto
                {
                    Label = label,
                    ReturnPercent = pct,
                    PeriodGainAmount = Math.Round(gain, 2),
                    CagrPercent = 0,
                    IsPositive = gain >= 0,
                    HasData = true,
                    IsPartialPeriod = false,
                    ActualFromDate = oldStr
                });
            }

            return list;
        }

        /// <summary>
        /// Calculates period return across all holdings belonging to one member.
        /// Fallback: if no NAV exists at the target date, the earliest available
        /// NAV record is used so the user always sees a value.
        /// </summary>
        private QuickReturnDto CalcMemberPeriodReturn(
            string label,
            int daysBack,
            decimal years,                         // 0 = skip CAGR
            List<Domain.Entities.Holding> holdings,
            Dictionary<int, (decimal currentNAV, decimal currentValue)> snapshotMap,
            Dictionary<string, List<NavRecord>> navHistoryMap)
        {
            decimal totalCurrentValue = 0;
            decimal totalPeriodAgoValue = 0;
            int covered = 0;
            var earliestDateUsed = DateTime.MinValue;
            bool isPartial = false;

            foreach (var h in holdings)
            {
                totalCurrentValue += snapshotMap.TryGetValue(h.Id, out var snap)
                    ? snap.currentValue : h.InvestedAmount;

                if (!navHistoryMap.TryGetValue(h.SchemeCode, out var navList)
                    || navList == null || !navList.Any())
                    continue;

                var targetDate = DateTime.Today.AddDays(-daysBack);

                // Primary: find NAV at or before the target date
                var record = navList
                    .Where(n => n.NavDate.Date <= targetDate.Date)
                    .OrderByDescending(n => n.NavDate)
                    .FirstOrDefault();

                // ── FALLBACK ────────────────────────────────────────
                // No data that far back — use the earliest available NAV
                if (record == null || record.NAV <= 0)
                {
                    record = navList
                        .OrderBy(n => n.NavDate)
                        .FirstOrDefault(n => n.NAV > 0);

                    if (record == null) continue;
                    isPartial = true;
                }
                // ───────────────────────────────────────────────────

                if (record.NavDate > earliestDateUsed)
                    earliestDateUsed = record.NavDate;

                totalPeriodAgoValue += h.Units * record.NAV;
                covered++;
            }

            if (covered == 0 || totalPeriodAgoValue <= 0)
                return new QuickReturnDto { Label = label, HasData = false };

            var gain = totalCurrentValue - totalPeriodAgoValue;
            var pct = Math.Round((gain / totalPeriodAgoValue) * 100, 4);

            // CAGR — only meaningful for periods ≥ 1 year
            decimal cagr = 0;
            if (years > 0 && totalPeriodAgoValue > 0)
            {
                // Use actual elapsed years when partial period
                var actualYears = isPartial && earliestDateUsed != DateTime.MinValue
                    ? Math.Max(
                        (decimal)(DateTime.Today - earliestDateUsed).TotalDays / 365m,
                        0.01m)
                    : years;

                cagr = Math.Round(
                    (decimal)(Math.Pow(
                        (double)(totalCurrentValue / totalPeriodAgoValue),
                        (double)(1m / actualYears)) - 1) * 100, 4);
            }

            return new QuickReturnDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = cagr,
                IsPositive = pct >= 0,
                HasData = true,
                IsPartialPeriod = isPartial,
                ActualFromDate = isPartial && earliestDateUsed != DateTime.MinValue
                    ? earliestDateUsed.ToString("dd MMM yyyy")
                    : string.Empty
            };
        }

        /// <summary>
        /// Calculates period return for a single scheme holding.
        /// Same fallback logic as CalcMemberPeriodReturn.
        /// </summary>
        private QuickReturnDto CalcQuickReturn(
            string label,
            int daysBack,
            decimal years,
            List<NavRecord>? navList,
            decimal currentNAV,
            decimal units)
        {
            if (navList == null || !navList.Any() || currentNAV <= 0)
                return new QuickReturnDto { Label = label, HasData = false };

            var targetDate = DateTime.Today.AddDays(-daysBack);

            var record = navList
                .Where(n => n.NavDate.Date <= targetDate.Date)
                .OrderByDescending(n => n.NavDate)
                .FirstOrDefault();

            bool isPartial = false;

            // ── FALLBACK ────────────────────────────────────────────
            if (record == null || record.NAV <= 0)
            {
                record = navList
                    .OrderBy(n => n.NavDate)
                    .FirstOrDefault(n => n.NAV > 0);

                if (record == null)
                    return new QuickReturnDto { Label = label, HasData = false };

                isPartial = true;
            }
            // ───────────────────────────────────────────────────────

            var periodAgoValue = units * record.NAV;
            var currentValue = units * currentNAV;
            var gain = currentValue - periodAgoValue;
            var pct = periodAgoValue > 0
                ? Math.Round((gain / periodAgoValue) * 100, 4) : 0;

            decimal cagr = 0;
            if (years > 0 && periodAgoValue > 0)
            {
                var actualYears = isPartial
                    ? Math.Max(
                        (decimal)(DateTime.Today - record.NavDate).TotalDays / 365m,
                        0.01m)
                    : years;

                cagr = Math.Round(
                    (decimal)(Math.Pow(
                        (double)(currentValue / periodAgoValue),
                        (double)(1m / actualYears)) - 1) * 100, 4);
            }

            return new QuickReturnDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = cagr,
                IsPositive = pct >= 0,
                HasData = true,
                IsPartialPeriod = isPartial,
                ActualFromDate = isPartial
                    ? record.NavDate.ToString("dd MMM yyyy")
                    : string.Empty
            };
        }

        /// <summary>
        /// Calculates period return for the scheme detail screen.
        /// Same fallback logic — uses earliest available NAV if history is short.
        /// </summary>
        private PeriodDetailDto CalcPeriodDetail(
            string label,
            int daysBack,
            decimal years,
            List<NavRecord> navList,
            decimal currentNAV,
            decimal units)
        {
            if (!navList.Any() || currentNAV <= 0)
                return new PeriodDetailDto { Label = label, HasData = false };

            var latestDate = navList[0].NavDate;
            var targetDate = latestDate.AddDays(-daysBack);

            var record = navList
                .Where(n => n.NavDate.Date <= targetDate.Date)
                .OrderByDescending(n => n.NavDate)
                .FirstOrDefault();

            bool isPartial = false;

            // ── FALLBACK ────────────────────────────────────────────
            if (record == null || record.NAV <= 0)
            {
                record = navList
                    .OrderBy(n => n.NavDate)
                    .FirstOrDefault(n => n.NAV > 0);

                if (record == null)
                    return new PeriodDetailDto { Label = label, HasData = false };

                isPartial = true;
            }
            // ───────────────────────────────────────────────────────

            var periodAgoValue = units * record.NAV;
            var currentValue = units * currentNAV;
            var gain = currentValue - periodAgoValue;
            var pct = periodAgoValue > 0
                ? Math.Round((gain / periodAgoValue) * 100, 4) : 0;

            decimal cagr = 0;
            if (years > 0 && periodAgoValue > 0)
            {
                var actualYears = isPartial
                    ? Math.Max(
                        (decimal)(latestDate - record.NavDate).TotalDays / 365m,
                        0.01m)
                    : years;

                cagr = Math.Round(
                    (decimal)(Math.Pow(
                        (double)(currentValue / periodAgoValue),
                        (double)(1m / actualYears)) - 1) * 100, 4);
            }

            return new PeriodDetailDto
            {
                Label = label,
                ReturnPercent = pct,
                PeriodGainAmount = Math.Round(gain, 2),
                CagrPercent = cagr,
                IsPositive = pct >= 0,
                HasData = true,
                IsPartialPeriod = isPartial,
                ActualFromDate = isPartial
                    ? record.NavDate.ToString("dd MMM yyyy")
                    : string.Empty
            };
        }

        // ── DB helpers ─────────────────────────────────────────────

        private async Task<Dictionary<int, (decimal currentNAV, decimal currentValue)>>
            GetLatestSnapshotsAsync(List<int> holdingIds)
        {
            var result = new Dictionary<int, (decimal, decimal)>();
            using var conn = new MySqlConnection(_connString);

            var sql = @"
                SELECT ps.HoldingId, ps.CurrentNAV, ps.CurrentValue
                FROM portfoliosnapshots ps
                INNER JOIN (
                    SELECT HoldingId, MAX(SnapshotDate) AS LatestDate
                    FROM portfoliosnapshots
                    WHERE HoldingId IN @HoldingIds
                    GROUP BY HoldingId
                ) latest
                    ON ps.HoldingId     = latest.HoldingId
                    AND ps.SnapshotDate = latest.LatestDate";

            var rows = await conn.QueryAsync<dynamic>(sql, new { HoldingIds = holdingIds });
            foreach (var r in rows)
                result[(int)r.HoldingId] = ((decimal)r.CurrentNAV, (decimal)r.CurrentValue);

            return result;
        }

        private async Task<Dictionary<string, List<NavRecord>>>
            GetNavHistoryAsync(List<string> schemeCodes)
        {
            var result = new Dictionary<string, List<NavRecord>>();
            if (!schemeCodes.Any()) return result;

            using var conn = new MySqlConnection(_connString);

            // ── Extended to 5Y + buffer ────────────────────────────
            var fromDate = DateTime.Today.AddDays(-NavHistoryDays);

            var sql = @"
                SELECT SchemeCode, NAV, NavDate
                FROM detailedschemes
                WHERE SchemeCode IN @SchemeCodes
                  AND NavDate   >= @FromDate
                ORDER BY SchemeCode, NavDate DESC";

            var rows = await conn.QueryAsync<NavRecord>(
                sql, new { SchemeCodes = schemeCodes, FromDate = fromDate });

            foreach (var r in rows)
            {
                if (!result.ContainsKey(r.SchemeCode))
                    result[r.SchemeCode] = new List<NavRecord>();
                result[r.SchemeCode].Add(r);
            }

            return result;
        }

        // ── Category / initials helpers ────────────────────────────

        private static (int equity, int debt, int hybrid) GetSchemeCategoryCounts(
            List<Domain.Entities.Holding> holdings)
        {
            var distinct = holdings
                .GroupBy(h => h.SchemeCode)
                .Select(g => g.First().SchemeName.ToLower());

            int equity = 0, debt = 0, hybrid = 0;
            foreach (var name in distinct)
            {
                if (name.Contains("hybrid") || name.Contains("balance")) hybrid++;
                else if (name.Contains("debt") || name.Contains("bond") ||
                         name.Contains("liquid") || name.Contains("gilt") ||
                         name.Contains("psu") || name.Contains("income")) debt++;
                else equity++;
            }
            return (equity, debt, hybrid);
        }

        private static string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
        }

        private static string GetCategorySummary(List<Domain.Entities.Holding> holdings)
        {
            var cats = new HashSet<string>();
            foreach (var h in holdings)
            {
                var n = h.SchemeName.ToLower();
                if (n.Contains("equity") || n.Contains("large") ||
                         n.Contains("mid") || n.Contains("small")) cats.Add("Equity");
                else if (n.Contains("debt") || n.Contains("bond") ||
                         n.Contains("liquid") || n.Contains("psu")) cats.Add("Debt");
                else if (n.Contains("hybrid") || n.Contains("balance")) cats.Add("Hybrid");
                else cats.Add("Equity");
            }
            return string.Join(" + ", cats.OrderBy(c => c));
        }

        private static DateTime GetThisMonday(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        private class NavRecord
        {
            public string SchemeCode { get; set; } = string.Empty;
            public decimal NAV { get; set; }
            public DateTime NavDate { get; set; }
        }
    }
}