namespace MutualFundNav.Domain.Contracts
{
    public record NavFileProcessedEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime NavDate { get; init; }
        public string FileContent { get; init; } = string.Empty;
        public int RecordCount { get; init; }
        public string Checksum { get; init; } = string.Empty;
        public DateTime PublishedAt { get; init; } = DateTime.UtcNow;
        public string Source { get; init; } = "MutualFundNav-API";
    }
}
