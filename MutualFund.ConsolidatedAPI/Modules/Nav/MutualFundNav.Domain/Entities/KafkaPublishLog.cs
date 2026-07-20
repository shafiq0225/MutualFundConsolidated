namespace MutualFundNav.Domain.Entities
{
    /// <summary>
    /// Audit record written after every Kafka publish attempt (success or failure).
    /// One row per publish call — covers scheduled runs, missed-run recovery,
    /// manual triggers, upserts, and market-holiday events.
    /// </summary>
    public class KafkaPublishLog : BaseEntity
    {
        /// <summary>Kafka topic name (e.g. "nav-file-processed").</summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>Event type discriminator: "NavFileProcessed" | "MarketHoliday".</summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Kafka message key (NAV date as yyyy-MM-dd).</summary>
        public string MessageKey { get; set; } = string.Empty;

        /// <summary>Serialised JSON payload size in bytes.</summary>
        public long MessageSizeBytes { get; set; }

        /// <summary>True when the broker acknowledged the message.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Exception message when IsSuccess is false.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>UTC timestamp when PublishAsync was called.</summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>Round-trip time from ProduceAsync call to broker ack, in milliseconds.</summary>
        public double ElapsedMs { get; set; }

        /// <summary>
        /// What initiated this publish:
        /// "NavDownloadWorker.Scheduled" | "NavDownloadWorker.MissedRun" |
        /// "NavController.ManualTrigger" | "NavController.ManualUpsert".
        /// </summary>
        public string TriggerSource { get; set; } = string.Empty;

        /// <summary>NAV date this event carries. Null for MarketHoliday events.</summary>
        public DateTime? NavDate { get; set; }

        /// <summary>Kafka partition assigned to the message. Null on failure.</summary>
        public int? Partition { get; set; }

        /// <summary>Kafka offset within the partition. Null on failure.</summary>
        public long? Offset { get; set; }
    }
}