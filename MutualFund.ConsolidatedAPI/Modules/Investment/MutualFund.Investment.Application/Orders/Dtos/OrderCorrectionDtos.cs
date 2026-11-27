using System.ComponentModel.DataAnnotations;

namespace MutualFund.Investment.Application.Orders.Dtos
{
    public class AdminEditOrderDto
    {
        [Required]
        public string InvestorUserId { get; set; } = string.Empty;

        [Required]
        public string InvestorName { get; set; } = string.Empty;

        [Required]
        public string SchemeCode { get; set; } = string.Empty;

        [Required]
        public string SchemeName { get; set; } = string.Empty;

        public string FundName { get; set; } = string.Empty;

        [Required]
        public string FolioNumber { get; set; } = string.Empty;

        [Required]
        public DateTime OrderDate { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal InvestedAmount { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal PurchaseNAV { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal? UnitsAllotted { get; set; }

        public string? Reason { get; set; }
    }

    public class AdminReverseOrdersDto
    {
        [Required]
        [MinLength(1)]
        public List<int> OrderIds { get; set; } = new();

        [Required]
        [MinLength(3)]
        public string Reason { get; set; } = string.Empty;
    }

    public class OrderCorrectionImpactRequestDto
    {
        [Required]
        public List<int> OrderIds { get; set; } = new();
    }

    public class OrderImpactItemDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public int? HoldingId { get; set; }
        public int SnapshotCount { get; set; }
        public int StatementCount { get; set; }
    }

    public class OrderCorrectionImpactDto
    {
        public int OrderCount { get; set; }
        public int HoldingCount { get; set; }
        public int SnapshotCount { get; set; }
        public int StatementCount { get; set; }
        public List<OrderImpactItemDto> Items { get; set; } = new();
    }

    public class AdminEditOrderResultDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int? HoldingId { get; set; }
        public int SnapshotsRecalculated { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;
    }

    public class AdminReverseOrdersResultDto
    {
        public int OrdersRemoved { get; set; }
        public int HoldingsRemoved { get; set; }
        public int SnapshotsRemoved { get; set; }
        public int StatementsRemoved { get; set; }
    }

    public class OrderCorrectionLogDto
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string AdminUserId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string ChangeSummary { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
