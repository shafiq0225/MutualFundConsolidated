namespace MutualFund.Scheme.Domain.Entities
{
    public class DetailedScheme
    {
        public int Id { get; set; }
        public string FundCode { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string SchemeCode { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public decimal Nav { get; set; }
        public DateTime NavDate { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}