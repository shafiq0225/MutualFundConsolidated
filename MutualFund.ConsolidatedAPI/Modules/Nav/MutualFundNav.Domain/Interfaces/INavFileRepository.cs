using MutualFundNav.Domain.Entities;

namespace MutualFundNav.Domain.Interfaces
{
    public interface INavFileRepository
    {
        Task<bool> ExistsByDateAsync(DateTime date);
        Task AddAsync(NavFile navFile);
        Task UpdateAsync(NavFile navFile);
        Task<NavFile?> GetByDateAsync(DateTime date);
        Task<IEnumerable<DateTime>> GetAllDatesAsync();
        Task<DateTime?> GetLatestDateAsync();
        Task<NavFile?> GetLatestAsync();

        /// <summary>
        /// Returns the NAV file record whose DownloadedAt timestamp falls on
        /// the specified date (UTC). Used by GET /api/nav/content?downloadedAt=...
        /// </summary>
        Task<NavFile?> GetByDownloadedAtAsync(DateTime downloadedAt);

        /// <summary>
        /// Returns all NAV records ordered newest-first WITHOUT the FileContent
        /// field populated (set to empty string). Safe to serialise � no MB-sized payload.
        /// Used by GET /api/nav/history and the Angular UI.
        /// </summary>
        Task<IEnumerable<NavFile>> GetAllSummariesAsync();
    }
}