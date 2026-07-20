using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Scheme.Application.UseCases.Queries
{
    public class GetHolidayStatusQuery
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetHolidayStatusQuery> _logger;

        public GetHolidayStatusQuery(IUnitOfWork unitOfWork,
            ILogger<GetHolidayStatusQuery> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<HolidayStatusDto> ExecuteAsync()
        {
            for (int daysBack = 1; daysBack <= 7; daysBack++)
            {
                var dateToCheck = DateTime.Today.AddDays(-daysBack);

                if (dateToCheck.DayOfWeek == DayOfWeek.Saturday ||
                    dateToCheck.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                var holiday = await _unitOfWork.MarketHolidays
                    .GetByDateAsync(dateToCheck);

                if (holiday is not null)
                {
                    var label = daysBack == 1
                        ? "Yesterday"
                        : holiday.HolidayDate.ToString("dd MMM yyyy");

                    _logger.LogInformation(
                        "Holiday detected for {Date} — daysBack={DaysBack}",
                        dateToCheck.ToString("yyyy-MM-dd"), daysBack);

                    return new HolidayStatusDto
                    {
                        IsHoliday = true,
                        HolidayDate = holiday.HolidayDate,
                        Message = $"{label} ({holiday.HolidayDate:dd MMM yyyy}) is a market holiday. " +
                                  $"You are viewing previous trading day records."
                    };
                }
                break;
            }

            return new HolidayStatusDto
            {
                IsHoliday = false,
                Message = "Today is a trading day. Showing latest NAV data."
            };
        }
    }
}