using Microsoft.EntityFrameworkCore;
using MutualFundNav.Domain.Entities;
using MutualFundNav.Domain.Interfaces;
using MutualFundNav.Infrastructure.Data;

namespace MutualFundNav.Infrastructure.Repositories
{
    public class NavFileRepository : INavFileRepository
    {
        private readonly ApplicationDbContext _context;

        public NavFileRepository(ApplicationDbContext context) => _context = context;

        public async Task<bool> ExistsByDateAsync(DateTime date) =>
            await _context.NavFiles.AnyAsync(f => f.NavDate.Date == date.Date);

        public async Task AddAsync(NavFile navFile) =>
            await _context.NavFiles.AddAsync(navFile);

        public Task UpdateAsync(NavFile navFile)
        {
            navFile.UpdatedAt = DateTime.UtcNow;
            _context.NavFiles.Update(navFile);
            return Task.CompletedTask;
        }

        public async Task<NavFile?> GetByDateAsync(DateTime date) =>
            await _context.NavFiles
                .FirstOrDefaultAsync(f => f.NavDate.Date == date.Date);

        public async Task<IEnumerable<DateTime>> GetAllDatesAsync() =>
            await _context.NavFiles
                .OrderByDescending(f => f.NavDate)
                .Select(f => f.NavDate)
                .ToListAsync();

        public async Task<DateTime?> GetLatestDateAsync() =>
            await _context.NavFiles
                .MaxAsync(f => (DateTime?)f.NavDate);

        public async Task<NavFile?> GetLatestAsync() =>
            await _context.NavFiles
                .OrderByDescending(f => f.NavDate)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Finds the record downloaded on the given UTC calendar date.
        /// Matches on the date part of DownloadedAt, not the exact timestamp.
        /// </summary>
        public async Task<NavFile?> GetByDownloadedAtAsync(DateTime downloadedAt) =>
            await _context.NavFiles
                .Where(f => f.DownloadedAt.Date == downloadedAt.Date)
                .OrderByDescending(f => f.DownloadedAt)
                .FirstOrDefaultAsync();

        /// <summary>
        /// Returns all records ordered newest-first without FileContent loaded into memory.
        /// EF projects only the summary columns so the query never materialises MB-sized strings.
        /// </summary>
        public async Task<IEnumerable<NavFile>> GetAllSummariesAsync() =>
            await _context.NavFiles
                .OrderByDescending(f => f.NavDate)
                .Select(f => new NavFile
                {
                    Id = f.Id,
                    NavDate = f.NavDate,
                    FileSizeBytes = f.FileSizeBytes,
                    RecordCount = f.RecordCount,
                    Checksum = f.Checksum,
                    DownloadedAt = f.DownloadedAt,
                    IsHoliday = f.IsHoliday,
                    FileContent = string.Empty   // excluded intentionally
                })
                .ToListAsync();
    }
}