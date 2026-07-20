namespace MutualFund.Scheme.Application.DTOs
{
    public class NavComparisonResponseDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SchemeComparisonDto> Schemes { get; set; } = new();
    }

    public class SchemeComparisonDto
    {
        public string FundName { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public List<NavHistoryDto> History { get; set; } = new();
        public int Rank { get; set; }
    }

    public class NavHistoryDto
    {
        public DateTime Date { get; set; }
        public decimal Nav { get; set; }
        public string Percentage { get; set; } = "0.00";
        public bool IsTradingHoliday { get; set; }
        public bool IsGrowth { get; set; }
    }
}