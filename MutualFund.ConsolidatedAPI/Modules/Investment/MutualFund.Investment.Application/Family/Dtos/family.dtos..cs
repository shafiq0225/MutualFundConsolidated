namespace MutualFund.Investment.Application.Family.Dtos
{
    // ── Screen 1 ───────────────────────────────────────────────────

    public class FamilyOverviewDto
    {
        public decimal TotalFamilyInvested { get; set; }
        public decimal TotalFamilyCurrentValue { get; set; }
        public decimal TotalFamilyGain { get; set; }
        public decimal TotalFamilyGainPercent { get; set; }
        public bool IsFamilyGain { get; set; }

        public int TotalMembers { get; set; }
        public int TotalSchemes { get; set; }

        public int EquitySchemeCount { get; set; }
        public int DebtSchemeCount { get; set; }
        public int HybridSchemeCount { get; set; }

        public QuickReturnDto? FamilyYesterdayReturn { get; set; }

        public DateTime ReportDate { get; set; }

        public IEnumerable<MemberSummaryDto> Members { get; set; }
            = new List<MemberSummaryDto>();
    }

    public class MemberSummaryDto
    {
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;

        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalGain { get; set; }
        public decimal TotalGainPercent { get; set; }
        public bool IsGain { get; set; }

        public int SchemeCount { get; set; }
        public int HoldingCount { get; set; }
        public string CategorySummary { get; set; } = string.Empty;

        // ── Period returns ─────────────────────────────────────────
        public QuickReturnDto? DayBefore { get; set; }   // D-2
        public QuickReturnDto? Yesterday { get; set; }   // Yesterday
        public QuickReturnDto? ThisWeek { get; set; }   // 7 days — required by new Passbook design
        public QuickReturnDto? OneMonth { get; set; }   // 1M
        public QuickReturnDto? OneYear { get; set; }   // 1Y
        public QuickReturnDto? ThreeYear { get; set; }   // 3Y
        public QuickReturnDto? FiveYear { get; set; }   // 5Y ← NEW
    }

    // ── Screen 2 ───────────────────────────────────────────────────

    public class MemberHoldingsDto
    {
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;

        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalGain { get; set; }
        public decimal TotalGainPercent { get; set; }
        public bool IsGain { get; set; }

        public IEnumerable<HoldingCardDto> Holdings { get; set; }
            = new List<HoldingCardDto>();
    }

    public class HoldingCardDto
    {
        public int HoldingId { get; set; }
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;

        public decimal InvestedAmount { get; set; }
        public decimal Units { get; set; }
        public decimal PurchaseNAV { get; set; }
        public decimal CurrentNAV { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal Gain { get; set; }
        public decimal GainPercent { get; set; }
        public bool IsGain { get; set; }

        public QuickReturnDto? DayBefore { get; set; }
        public QuickReturnDto? Yesterday { get; set; }   // required by new Passbook design
        public QuickReturnDto? ThisWeek { get; set; }   // 7 days — required by new Passbook design
        public QuickReturnDto? OneMonth { get; set; }
        public QuickReturnDto? SixMonth { get; set; }
        public QuickReturnDto? OneYear { get; set; }
        public QuickReturnDto? ThreeYear { get; set; }   // ← NEW
        public QuickReturnDto? FiveYear { get; set; }   // ← NEW
    }

    // ── Shared return DTO ──────────────────────────────────────────

    public class QuickReturnDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal ReturnPercent { get; set; }
        public decimal PeriodGainAmount { get; set; }   // ← NEW: ₹ P&L for the period
        public decimal CagrPercent { get; set; }   // ← NEW: annualised CAGR
        public bool IsPositive { get; set; }
        public bool HasData { get; set; }

        /// <summary>
        /// True when full history was unavailable and the
        /// earliest available NAV was used as the period start.
        /// </summary>
        public bool IsPartialPeriod { get; set; }   // ← NEW
        public string ActualFromDate { get; set; } = string.Empty; // ← NEW
    }

    // ── Screen 3 ───────────────────────────────────────────────────

    public class HoldingSchemeDetailDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public bool IsApproved { get; set; }

        public decimal InvestedAmount { get; set; }
        public decimal Units { get; set; }
        public decimal AvgBuyNAV { get; set; }

        public decimal CurrentNAV { get; set; }
        public string CurrentNavDateText { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal TotalGain { get; set; }
        public decimal TotalGainPercent { get; set; }
        public bool IsTotalGain { get; set; }

        public decimal PreviousNAV { get; set; }
        public string PreviousNavDateText { get; set; } = string.Empty;
        public decimal DailyChange { get; set; }
        public decimal DailyChangePercent { get; set; }
        public bool IsDailyUp { get; set; }

        public decimal? WeekStartNAV { get; set; }
        public string WeekStartDateText { get; set; } = string.Empty;
        public decimal? WeekReturn { get; set; }
        public decimal? WeekGainAmount { get; set; }
        public bool IsWeekUp { get; set; }

        public PeriodDetailDto? OneMonth { get; set; }
        public PeriodDetailDto? ThreeMonth { get; set; }
        public PeriodDetailDto? SixMonth { get; set; }
        public PeriodDetailDto? OneYear { get; set; }
        public PeriodDetailDto? ThreeYear { get; set; }
        public PeriodDetailDto? FiveYear { get; set; }   // ← NEW
    }

    public class PeriodDetailDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal ReturnPercent { get; set; }
        public decimal PeriodGainAmount { get; set; }   // ← NEW
        public decimal CagrPercent { get; set; }   // ← NEW
        public bool IsPositive { get; set; }
        public bool HasData { get; set; }
        public bool IsPartialPeriod { get; set; }   // ← NEW
        public string ActualFromDate { get; set; } = string.Empty; // ← NEW
    }
}