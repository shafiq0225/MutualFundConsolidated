namespace MutualFund.Scheme.Application.DTOs
{
    public class SchemeDetailsDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundCode { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }

        public decimal CurrentNAV { get; set; }
        public DateTime CurrentNavDate { get; set; }
        public string CurrentNavDateText { get; set; } = string.Empty;

        public decimal PreviousNAV { get; set; }
        public DateTime PreviousNavDate { get; set; }
        public string PreviousNavDateText { get; set; } = string.Empty;

        public decimal DailyChange { get; set; }
        public decimal DailyChangePercent { get; set; }
        public bool IsDailyUp { get; set; }

        public decimal? WeekStartNAV { get; set; }
        public DateTime? WeekStartDate { get; set; }
        public string WeekStartDateText { get; set; } = string.Empty;
        public decimal? WeekReturn { get; set; }
        public decimal? WeekReturnPoints { get; set; }
        public bool IsWeekUp { get; set; }

        public PeriodReturnDto? OneMonth { get; set; }
        public PeriodReturnDto? ThreeMonth { get; set; }
        public PeriodReturnDto? SixMonth { get; set; }
        public PeriodReturnDto? OneYear { get; set; }
        public PeriodReturnDto? ThreeYear { get; set; }

        public IEnumerable<NavPointDto> NavHistory { get; set; } = new List<NavPointDto>();
    }

    public class PeriodReturnDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal StartNAV { get; set; }
        public decimal EndNAV { get; set; }
        public DateTime StartDate { get; set; }
        public decimal ReturnPercent { get; set; }
        public decimal ReturnPoints { get; set; }
        public bool IsPositive { get; set; }
        public bool HasData { get; set; }
    }

    public class NavPointDto
    {
        public DateTime Date { get; set; }
        public decimal NAV { get; set; }
        public string DateText { get; set; } = string.Empty;
    }
}