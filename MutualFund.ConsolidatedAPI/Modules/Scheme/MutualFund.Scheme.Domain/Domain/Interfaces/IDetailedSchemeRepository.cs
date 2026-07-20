using MutualFund.Scheme.Domain.Entities;

namespace MutualFund.Scheme.Domain.Interfaces
{
    public interface IDetailedSchemeRepository
    {
        Task<bool> ExistsBySchemeCodeAndDateAsync(string schemeCode, DateTime navDate);
        Task AddRangeAsync(IEnumerable<DetailedScheme> schemes);
        Task UpdateApprovalByFundCodeAsync(string fundCode, bool isApproved);
        Task<IEnumerable<string>> GetSchemeCodesByFundCodeAsync(string fundCode);
        Task<IEnumerable<DetailedScheme>> GetByDateRangeWithPreviousAsync(DateTime startDate, DateTime endDate);
        Task<List<DateTime>> GetLastTradingDatesAsync(int count);
        Task<IEnumerable<DetailedScheme>> GetNavHistoryBySchemeCodeAsync(string schemeCode, DateTime fromDate);
    }
}