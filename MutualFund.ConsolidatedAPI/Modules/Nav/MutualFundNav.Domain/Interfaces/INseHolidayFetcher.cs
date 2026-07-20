namespace MutualFundNav.Domain.Interfaces
{
    public interface INseHolidayFetcher
    {
        Task<List<DateTime>> FetchHolidaysForYearAsync(int year);
        Task<HashSet<DateTime>> FetchAllHolidaysAsync();
        Task RefreshHolidaysAsync();
    }
}
