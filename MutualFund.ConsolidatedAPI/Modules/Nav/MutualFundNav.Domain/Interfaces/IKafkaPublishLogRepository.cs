using MutualFundNav.Domain.Entities;

namespace MutualFundNav.Domain.Interfaces
{
    public interface IKafkaPublishLogRepository
    {
        Task AddAsync(KafkaPublishLog log);
        Task<IEnumerable<KafkaPublishLog>> GetRecentAsync(int count = 20);
        Task<KafkaPublishLog?> GetLatestAsync();
        Task<IEnumerable<KafkaPublishLog>> GetByNavDateAsync(DateTime navDate);
        Task<IEnumerable<KafkaPublishLog>> GetFailuresAsync(int count = 20);
    }
}