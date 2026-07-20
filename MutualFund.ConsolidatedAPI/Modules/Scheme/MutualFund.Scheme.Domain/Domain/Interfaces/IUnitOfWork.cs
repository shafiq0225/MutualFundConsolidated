namespace MutualFund.Scheme.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ISchemeEnrollmentRepository SchemeEnrollments { get; }
        IDetailedSchemeRepository DetailedSchemes { get; }
        IMarketHolidayRepository MarketHolidays { get; }
        Task<int> CompleteAsync();
    }
}