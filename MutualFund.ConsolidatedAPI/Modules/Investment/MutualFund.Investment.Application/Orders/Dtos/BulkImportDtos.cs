using System.ComponentModel.DataAnnotations;

namespace MutualFund.Investment.Application.Orders.Dtos
{
    public class BulkImportPreviewRowDto
    {
        public int RowNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string InvestorUserId { get; set; } = string.Empty;
        public string InvestorName { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string RawSchemeText { get; set; } = string.Empty;
        public string FolioNumber { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public decimal PurchaseNAV { get; set; }
        public decimal UnitsAllotted { get; set; }
        public string PaymentMode { get; set; } = "Online";
        public decimal? AmfiNav { get; set; }
        public DateTime? AmfiNavDate { get; set; }
        public decimal? VariancePercent { get; set; }
        public string Status { get; set; } = "Ready"; // Ready | Warning | Issue | Duplicate | Unmatched
        public string StatusNote { get; set; } = string.Empty;
        public string? DuplicateOrderNumber { get; set; }
        public bool IncludeByDefault { get; set; }
    }

    public class BulkImportPreviewResponseDto
    {
        public string SourceType { get; set; } = string.Empty; // Excel | CAS
        public string FileName { get; set; } = string.Empty;
        public int TotalParsed { get; set; }
        public int ReadyCount { get; set; }
        public int IssueCount { get; set; }
        public int DuplicateCount { get; set; }
        public int UnmatchedCount { get; set; }
        public List<BulkImportPreviewRowDto> Rows { get; set; } = new();
    }

    public class BulkImportConfirmRowDto
    {
        [Required]
        public DateTime OrderDate { get; set; }

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

        [Range(0.01, double.MaxValue)]
        public decimal InvestedAmount { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal PurchaseNAV { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal UnitsAllotted { get; set; }

        public string PaymentMode { get; set; } = "Online";
    }

    public class BulkImportConfirmRequestDto
    {
        [Required]
        [MinLength(1)]
        public List<BulkImportConfirmRowDto> Rows { get; set; } = new();

        public string? SourceNote { get; set; }
    }

    public class BulkImportConfirmResultDto
    {
        public int OrdersCreated { get; set; }
        public int HoldingsCreated { get; set; }
        public int SnapshotsRecalculated { get; set; }
        public List<string> CreatedOrderNumbers { get; set; } = new();
    }
}
