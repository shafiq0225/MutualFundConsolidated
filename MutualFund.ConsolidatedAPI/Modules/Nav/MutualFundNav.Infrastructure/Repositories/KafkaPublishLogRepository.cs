using Microsoft.EntityFrameworkCore;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Infrastructure.Data;

namespace MutualFundNav.Infrastructure.Repositories
{
    public class KafkaPublishLogRepository : IKafkaPublishLogRepository
    {
        private readonly ApplicationDbContext _context;

        public KafkaPublishLogRepository(ApplicationDbContext context) => _context = context;

        public async Task AddAsync(KafkaPublishLog log) =>
            await _context.KafkaPublishLogs.AddAsync(log);

        public async Task<IEnumerable<KafkaPublishLog>> GetRecentAsync(int count = 20) =>
            await _context.KafkaPublishLogs
                .OrderByDescending(l => l.PublishedAt)
                .Take(count)
                .ToListAsync();

        public async Task<KafkaPublishLog?> GetLatestAsync() =>
            await _context.KafkaPublishLogs
                .OrderByDescending(l => l.PublishedAt)
                .FirstOrDefaultAsync();

        public async Task<IEnumerable<KafkaPublishLog>> GetByNavDateAsync(DateTime navDate) =>
            await _context.KafkaPublishLogs
                .Where(l => l.NavDate.HasValue && l.NavDate.Value.Date == navDate.Date)
                .OrderByDescending(l => l.PublishedAt)
                .ToListAsync();

        public async Task<IEnumerable<KafkaPublishLog>> GetFailuresAsync(int count = 20) =>
            await _context.KafkaPublishLogs
                .Where(l => !l.IsSuccess)
                .OrderByDescending(l => l.PublishedAt)
                .Take(count)
                .ToListAsync();
    }
}