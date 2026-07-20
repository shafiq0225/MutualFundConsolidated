using Microsoft.EntityFrameworkCore.Storage;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Infrastructure.Repositories;

namespace MutualFundNav.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            NavFiles = new NavFileRepository(_context);
            MarketHolidays = new MarketHolidayRepository(_context);
            JobLogs = new JobExecutionLogRepository(_context);
            KafkaPublishLogs = new KafkaPublishLogRepository(_context);
        }

        public INavFileRepository NavFiles { get; }
        public IMarketHolidayRepository MarketHolidays { get; }
        public IJobExecutionLogRepository JobLogs { get; }
        public IKafkaPublishLogRepository KafkaPublishLogs { get; }

        public async Task<int> CompleteAsync() =>
            await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync() =>
            _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitTransactionAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync(); // BUG FIX: dispose after commit
                _transaction = null;               // BUG FIX: null out so next SaveChanges is clean
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync(); // BUG FIX: dispose after rollback
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}