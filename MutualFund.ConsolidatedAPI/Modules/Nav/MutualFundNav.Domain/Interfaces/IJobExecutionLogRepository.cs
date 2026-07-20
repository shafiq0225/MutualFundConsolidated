using MutualFundNav.Domain.Entities;

namespace MutualFundNav.Domain.Interfaces
{
    public interface IJobExecutionLogRepository
    {
        Task AddAsync(JobExecutionLog log);
        Task<IEnumerable<JobExecutionLog>> GetRecentAsync(int count = 10);
        Task<JobExecutionLog?> GetLatestAsync();
    }
}
