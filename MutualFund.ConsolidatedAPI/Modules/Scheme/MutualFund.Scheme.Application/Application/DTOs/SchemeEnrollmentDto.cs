namespace MutualFund.Scheme.Application.DTOs
{
    public class SchemeEnrollmentDto
    {
        public int Id { get; set; }
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateSchemeEnrollmentDto
    {
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }

    public class UpdateSchemeEnrollmentDto
    {
        public string SchemeName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }
}