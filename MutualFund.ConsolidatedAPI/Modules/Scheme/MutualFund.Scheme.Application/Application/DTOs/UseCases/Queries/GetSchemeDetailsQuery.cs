using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Domain.Entities;
using MutualFund.Scheme.Domain.Exceptions;
using MutualFund.Scheme.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Scheme.Application.UseCases.Queries
{
    public class GetSchemeDetailsQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetSchemeDetailsQuery> _logger;

        public GetSchemeDetailsQuery(IUnitOfWork unitOfWork,
            ILogger<GetSchemeDetailsQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SchemeDetailsDto> ExecuteAsync(string schemeCode)
        {
            _logger.LogInformation(
                "Fetching scheme details for: {SchemeCode}", schemeCode);

            var fromDate = DateTime.Today.AddDays(-1105);
            var history = await _unitOfWork.DetailedSchemes
                .GetNavHistoryBySchemeCodeAsync(schemeCode, fromDate);

            var navList = history.ToList();

            if (!navList.Any())
                throw new NotFoundException("Scheme", schemeCode);

            var latest = navList[0];
            var previous = navList.Count > 1 ? navList[1] : null;

            decimal dailyChange = 0;
            decimal dailyChangePct = 0;

            if (previous != null && previous.Nav > 0)
            {
                dailyChange = latest.Nav - previous.Nav;
                dailyChangePct = Math.Round((dailyChange / previous.Nav) * 100, 4);
            }

            var monday = GetThisMonday(latest.NavDate);
            var weekRecord = navList
                .Where(n => n.NavDate.Date <= monday.Date)
                .OrderByDescending(n => n.NavDate)
                .FirstOrDefault();

            decimal? weekReturn = null;
            decimal? weekReturnPoints = null;

            if (weekRecord != null && weekRecord.Nav > 0)
            {
                weekReturnPoints = Math.Round(latest.Nav - weekRecord.Nav, 4);
                weekReturn = Math.Round(
                    (weekReturnPoints.Value / weekRecord.Nav) * 100, 4);
            }

            var oneMonth = CalcReturn("1 Month", 30, navList, latest.Nav, latest.NavDate);
            var threeMonth = CalcReturn("3 Month", 90, navList, latest.Nav, latest.NavDate);
            var sixMonth = CalcReturn("6 Month", 180, navList, latest.Nav, latest.NavDate);
            var oneYear = CalcReturn("1 Year", 365, navList, latest.Nav, latest.NavDate);
            var threeYear = CalcReturn("3 Year", 1095, navList, latest.Nav, latest.NavDate);

            var sparkline = navList
                .Take(30)
                .OrderBy(n => n.NavDate)
                .Select(n => new NavPointDto
                {
                    Date = n.NavDate,
                    NAV = n.Nav,
                    DateText = n.NavDate.ToString("dd MMM")
                })
                .ToList();

            return new SchemeDetailsDto
            {
                SchemeCode = latest.SchemeCode,
                SchemeName = latest.SchemeName,
                FundCode = latest.FundCode,
                FundName = latest.FundName,
                IsApproved = latest.IsApproved,

                CurrentNAV = latest.Nav,
                CurrentNavDate = latest.NavDate,
                CurrentNavDateText = latest.NavDate.ToString("dd MMM yyyy"),

                PreviousNAV = previous?.Nav ?? 0,
                PreviousNavDate = previous?.NavDate ?? DateTime.MinValue,
                PreviousNavDateText = previous?.NavDate.ToString("dd MMM yyyy") ?? string.Empty,

                DailyChange = Math.Round(dailyChange, 4),
                DailyChangePercent = dailyChangePct,
                IsDailyUp = dailyChange >= 0,

                WeekStartNAV = weekRecord?.Nav,
                WeekStartDate = weekRecord?.NavDate,
                WeekStartDateText = weekRecord?.NavDate.ToString("dd MMM yyyy") ?? string.Empty,
                WeekReturn = weekReturn,
                WeekReturnPoints = weekReturnPoints,
                IsWeekUp = weekReturn.GetValueOrDefault() >= 0,

                OneMonth = oneMonth,
                ThreeMonth = threeMonth,
                SixMonth = sixMonth,
                OneYear = oneYear,
                ThreeYear = threeYear,

                NavHistory = sparkline
            };
        }

        private PeriodReturnDto CalcReturn(string label, int daysBack,
            List<DetailedScheme> navList, decimal currentNav, DateTime currentDate)
        {
            var targetDate = currentDate.AddDays(-daysBack);
            var periodRecord = navList
                .Where(n => n.NavDate.Date <= targetDate.Date)
                .OrderByDescending(n => n.NavDate)
                .FirstOrDefault();

            if (periodRecord == null || periodRecord.Nav <= 0)
                return new PeriodReturnDto { Label = label, HasData = false };

            var returnPoints = Math.Round(currentNav - periodRecord.Nav, 4);
            var returnPercent = Math.Round((returnPoints / periodRecord.Nav) * 100, 4);

            return new PeriodReturnDto
            {
                Label = label,
                StartNAV = periodRecord.Nav,
                EndNAV = currentNav,
                StartDate = periodRecord.NavDate,
                ReturnPercent = returnPercent,
                ReturnPoints = returnPoints,
                IsPositive = returnPercent >= 0,
                HasData = true
            };
        }

        private static DateTime GetThisMonday(DateTime referenceDate)
        {
            var date = referenceDate.Date;
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff);
        }
    }
}