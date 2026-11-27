using System.ComponentModel.DataAnnotations;

namespace MutualFund.Investment.Application.Orders.Dtos
{
    public class PreOrderSanityCheckRequestDto
    {
        [Required]
        public string SchemeCode { get; set; } = string.Empty;

        [Required]
        public DateTime OrderDate { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal InvestedAmount { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal PurchaseNAV { get; set; }

        public decimal? UnitsAllotted { get; set; }
    }

    public class PreOrderSanityCheckResultDto
    {
        public string Severity { get; set; } = "OK"; // Critical | Warning | OK | NoAmfiData
        public decimal? AmfiNav { get; set; }
        public DateTime? AmfiNavDate { get; set; }
        public decimal? VariancePercent { get; set; }
        public decimal? ExpectedUnits { get; set; }
        public decimal CalculatedUnits { get; set; }
        public bool UnitMathOk { get; set; }
        public bool LooksSwapped { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OrderSanityAuditItemDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string InvestorName { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string? FolioNumber { get; set; }
        public decimal InvestedAmount { get; set; }
        public decimal EnteredNav { get; set; }
        public decimal? AmfiNav { get; set; }
        public DateTime? AmfiNavDate { get; set; }
        public decimal? VariancePercent { get; set; }
        public decimal EnteredUnits { get; set; }
        public decimal? ExpectedUnits { get; set; }
        public string Severity { get; set; } = "OK";
        public string Finding { get; set; } = string.Empty;
        public bool LooksSwapped { get; set; }
        public string? DuplicateOrderNumber { get; set; }
    }

    public class OrderSanityAuditSummaryDto
    {
        public int TotalScanned { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int HealthyCount { get; set; }
        public int NoAmfiDataCount { get; set; }
        public List<OrderSanityAuditItemDto> Items { get; set; } = new();
    }
}
