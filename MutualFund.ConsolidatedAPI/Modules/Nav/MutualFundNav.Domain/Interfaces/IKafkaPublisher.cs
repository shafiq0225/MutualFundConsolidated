using MutualFundNav.Domain.Contracts;

namespace MutualFundNav.Domain.Interfaces
{
    /// <summary>
    /// Generic Kafka publisher.
    /// PublishAsync never throws — failures are surfaced via <see cref="KafkaPublishResult.IsSuccess"/>.
    /// </summary>
    public interface IKafkaPublisher<T>
    {
        Task<KafkaPublishResult> PublishAsync(
            string topic,
            string key,
            T message,
            CancellationToken ct = default);
    }
}