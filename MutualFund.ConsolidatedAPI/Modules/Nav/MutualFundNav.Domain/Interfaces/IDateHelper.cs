namespace MutualFundNav.Domain.Interfaces
{
    public interface IDateHelper
    {
        Task<DateTime> GetTargetNavDateAsync();
        Task<bool> IsTradingDayAsync(DateTime date);
    }
}
