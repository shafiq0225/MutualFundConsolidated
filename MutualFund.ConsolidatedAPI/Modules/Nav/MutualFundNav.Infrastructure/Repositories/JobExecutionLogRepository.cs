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

        public async Task<bool> HasJobRunOnDateAsync(string jobNamePrefix, DateTime dateIst)
        {
            var tz = GetIstTimeZone();
            var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(dateIst.Date, tz);
            var endOfDayUtc = startOfDayUtc.AddDays(1);

            return await _context.JobExecutionLogs
                .AnyAsync(l => l.JobName.StartsWith(jobNamePrefix)
                               && l.IsSuccess
                               && l.StartedAt >= startOfDayUtc
                               && l.StartedAt < endOfDayUtc);
        }

        private static TimeZoneInfo GetIstTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
                catch { return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromHours(5.5), "India Standard Time", "India Standard Time"); }
            }
        }
    }
}
