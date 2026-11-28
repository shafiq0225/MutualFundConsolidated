namespace MutualFund.Investment.Application.Analytics.Dtos
{
    public class AnalyticsHubResponseDto
    {
        public List<int> Years { get; set; } = new();
        public Dictionary<int, int> YearMonthsElapsed { get; set; } = new();

        public decimal TotalInvested { get; set; }
        public decimal TotalMarketValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal OverallReturnPercent { get; set; }

        public SipStepUpSummaryDto SipStepUp { get; set; } = new();
        public YearWiseAnalyticsDto YearWise { get; set; } = new();
        public List<MonthlyLedgerYearDto> MonthlyLedger { get; set; } = new();
        public HeadToHeadComparisonDto HeadToHead { get; set; } = new();
    }

    public class SipStepUpSummaryDto
    {
        public List<int> Years { get; set; } = new();
        public int FirstYear { get; set; }
        public int PrevYear { get; set; }
        public int CurrentYear { get; set; }
        public List<SipStepUpSchemeRowDto> Rows { get; set; } = new();
        public SipStepUpSchemeRowDto Totals { get; set; } = new();
    }

    public class SipStepUpSchemeRowDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public Dictionary<int, decimal> YearlyTotals { get; set; } = new();
        public decimal GrandTotal { get; set; }
        public Dictionary<int, decimal> AvgMonthlyByYear { get; set; } = new();
        public decimal PrevToCurrStepUpRs { get; set; }
        public decimal PrevToCurrStepUpPercent { get; set; }
        public decimal CurrVsPrevAnnualisedPercent { get; set; }
        public decimal MonthlyIncrementRs { get; set; }
        public decimal MonthlyIncrementPercent { get; set; }
        public decimal AnnualisedRunRate { get; set; }
    }

    public class YearWiseAnalyticsDto
    {
        public List<YearWiseSummaryRowDto> Summary { get; set; } = new();
        public List<YearSchemeBreakdownDto> Breakdowns { get; set; } = new();
    }

    public class YearWiseSummaryRowDto
    {
        public int Year { get; set; }
        public decimal InvestedAmount { get; set; }
        public decimal ShareOfTotalPercent { get; set; }
        public decimal CumulativeInvested { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class YearSchemeBreakdownDto
    {
        public int Year { get; set; }
        public int MonthsElapsed { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal AvgMonthlySip { get; set; }
        public List<YearSchemeItemDto> Schemes { get; set; } = new();
    }

    public class YearSchemeItemDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public decimal ShareOfYearPercent { get; set; }
        public decimal AvgMonthlySip { get; set; }
    }

    public class MonthlyLedgerYearDto
    {
        public int Year { get; set; }
        public decimal YearTotal { get; set; }
        public decimal[] MonthlyTotals { get; set; } = new decimal[12];
        public List<MonthlyLedgerSchemeRowDto> Schemes { get; set; } = new();
    }

    public class MonthlyLedgerSchemeRowDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public decimal[] Months { get; set; } = new decimal[12];
        public int[] OrderCounts { get; set; } = new int[12];
        public decimal YearTotal { get; set; }
    }

    public class HeadToHeadComparisonDto
    {
        public List<HeadToHeadSchemeRowDto> Schemes { get; set; } = new();
        public List<HeadToHeadCategoryRowDto> Categories { get; set; } = new();
    }

    public class HeadToHeadSchemeRowDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string AssetCategory { get; set; } = string.Empty;
        public string AssetClassGroup { get; set; } = string.Empty;
        public decimal InvestedCapital { get; set; }
        public decimal MarketValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ReturnPercent { get; set; }
        public decimal PortfolioWeightPercent { get; set; }
        public decimal ValuationSharePercent { get; set; }
        public decimal ProfitSharePercent { get; set; }
    }

    public class HeadToHeadCategoryRowDto
    {
        public string AssetCategory { get; set; } = string.Empty;
        public int SchemeCount { get; set; }
        public decimal InvestedCapital { get; set; }
        public decimal MarketValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ReturnPercent { get; set; }
        public decimal CategorySharePercent { get; set; }
    }

    public class AnalyticsMonthOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string InvestorName { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public decimal? PurchaseNAV { get; set; }
        public decimal? UnitsAllotted { get; set; }
        public string PaymentMode { get; set; } = string.Empty;
    }
}
