using Microsoft.EntityFrameworkCore;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Infrastructure.Data;

namespace MutualFundNav.Infrastructure.Repositories
{
    public class JobExecutionLogRepository : IJobExecutionLogRepository
    {
        private readonly ApplicationDbContext _context;

        public JobExecutionLogRepository(ApplicationDbContext context) => _context = context;

        public async Task AddAsync(JobExecutionLog log) =>
            await _context.JobExecutionLogs.AddAsync(log);

        public async Task<IEnumerable<JobExecutionLog>> GetRecentAsync(int count = 10) =>
            await _context.JobExecutionLogs
                .OrderByDescending(l => l.StartedAt)
                .Take(count)
                .ToListAsync();

        public async Task<JobExecutionLog?> GetLatestAsync() =>
            await _context.JobExecutionLogs
                .OrderByDescending(l => l.StartedAt)
                .FirstOrDefaultAsync();
    }
}
