namespace MutualFund.Scheme.Domain.Contracts
{
    public record NavFileProcessedEvent
    {
        public DateTime NavDate { get; init; }
        public string FileContent { get; init; } = string.Empty;
        public int RecordCount { get; init; }
        public DateTime PublishedAt { get; init; }
    }
}