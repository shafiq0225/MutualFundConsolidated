using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Domain.Exceptions;
using MutualFund.Scheme.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Scheme.Application.UseCases.Queries
{
    public class GetNavComparisonQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetNavComparisonQuery> _logger;

        public GetNavComparisonQuery(IUnitOfWork unitOfWork,
            ILogger<GetNavComparisonQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<NavComparisonResponseDto> ExecuteDailyAsync()
        {
            var tradingDates = await _unitOfWork.DetailedSchemes
                .GetLastTradingDatesAsync(2);

            if (tradingDates.Count == 0)
                throw new NavDataNotFoundException(DateTime.Today, DateTime.Today);

            var endDate = tradingDates[0].Date;
            var startDate = tradingDates.Count == 1 ? endDate : tradingDates[1].Date;

            return await ExecuteAsync(startDate, endDate);
        }

        public async Task<NavComparisonResponseDto> ExecuteAsync(
            DateTime startDate, DateTime endDate)
        {
            var records = await _unitOfWork.DetailedSchemes
                .GetByDateRangeWithPreviousAsync(startDate, endDate);

            var recordList = records.ToList();

            if (recordList.Count == 0)
                throw new NavDataNotFoundException(startDate, endDate);

            var grouped = recordList.GroupBy(r => r.SchemeCode).ToList();
            var schemes = new List<SchemeComparisonDto>();

            foreach (var group in grouped)
            {
                var navByDate = group.ToDictionary(r => r.NavDate.Date, r => r.Nav);
                var orderedDates = navByDate.Keys.OrderBy(d => d).ToList();
                var history = new List<NavHistoryDto>();

                foreach (var date in orderedDates)
                {
                    var currentNav = navByDate[date];
                    var previousDate = orderedDates
                        .Where(d => d < date)
                        .OrderByDescending(d => d)
                        .FirstOrDefault();

                    string percentage;
                    bool isGrowth;

                    if (previousDate == default || !navByDate.ContainsKey(previousDate))
                    {
                        percentage = "100.00";
                        isGrowth = true;
                    }
                    else
                    {
                        var previousNav = navByDate[previousDate];
                        if (previousNav == 0)
                        {
                            percentage = "100.00";
                            isGrowth = true;
                        }
                        else
                        {
                            var change = ((currentNav - previousNav) / previousNav) * 100;
                            percentage = change.ToString("F2");
                            isGrowth = currentNav > previousNav;
                        }
                    }

                    if (date >= startDate.Date)
                    {
                        history.Add(new NavHistoryDto
                        {
                            Date = date,
                            Nav = currentNav,
                            Percentage = percentage,
                            IsTradingHoliday = false,
                            IsGrowth = isGrowth
                        });
                    }
                }

                var first = group.OrderBy(r => r.NavDate)
                    .First(r => r.NavDate.Date >= startDate.Date);

                schemes.Add(new SchemeComparisonDto
                {
                    FundName = first.FundName,
                    SchemeCode = first.SchemeCode,
                    SchemeName = first.SchemeName,
                    History = history
                });
            }

            var latestDate = endDate.Date;
            var ranked = schemes
                .OrderByDescending(s =>
                {
                    var latest = s.History.FirstOrDefault(h => h.Date.Date == latestDate);
                    if (latest == null) return decimal.MinValue;
                    return decimal.TryParse(latest.Percentage, out var p)
                        ? p : decimal.MinValue;
                })
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
                ranked[i].Rank = i + 1;

            return new NavComparisonResponseDto
            {
                StartDate = startDate,
                EndDate = endDate,
                Message = $"Retrieved {ranked.Count} scheme(s) successfully.",
                Schemes = ranked
            };
        }
    }
}