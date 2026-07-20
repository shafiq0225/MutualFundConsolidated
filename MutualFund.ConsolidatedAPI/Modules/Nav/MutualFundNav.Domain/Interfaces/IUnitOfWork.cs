namespace MutualFundNav.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        INavFileRepository NavFiles { get; }
        IMarketHolidayRepository MarketHolidays { get; }
        IJobExecutionLogRepository JobLogs { get; }
        IKafkaPublishLogRepository KafkaPublishLogs { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}