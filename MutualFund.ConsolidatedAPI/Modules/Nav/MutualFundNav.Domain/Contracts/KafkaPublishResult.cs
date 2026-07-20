namespace MutualFundNav.Domain.Contracts
{
    /// <summary>
    /// Returned by <see cref="MutualFundNav.Domain.Interfaces.IKafkaPublisher{T}.PublishAsync"/>
    /// so callers can log the full outcome without needing to catch exceptions themselves.
    /// The publisher absorbs delivery exceptions and surfaces them here instead.
    /// </summary>
    public sealed class KafkaPublishResult
    {
        public bool IsSuccess { get; init; }
        public string? ErrorMessage { get; init; }
        public int? Partition { get; init; }
        public long? Offset { get; init; }
        public double ElapsedMs { get; init; }
        public long MessageSizeBytes { get; init; }

        public static KafkaPublishResult Succeeded(int partition, long offset, double elapsedMs, long sizeBytes) =>
            new() { IsSuccess = true, Partition = partition, Offset = offset, ElapsedMs = elapsedMs, MessageSizeBytes = sizeBytes };

        public static KafkaPublishResult Failed(string error, double elapsedMs, long sizeBytes) =>
            new() { IsSuccess = false, ErrorMessage = error, ElapsedMs = elapsedMs, MessageSizeBytes = sizeBytes };
    }
}