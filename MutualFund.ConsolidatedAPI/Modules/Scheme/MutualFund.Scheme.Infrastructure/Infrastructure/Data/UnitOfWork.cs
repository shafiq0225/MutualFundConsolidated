using MutualFund.Scheme.Domain.Interfaces;
using MutualFund.Scheme.Infrastructure.Repositories;

namespace MutualFund.Scheme.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            SchemeEnrollments = new SchemeEnrollmentRepository(_context);
            DetailedSchemes = new DetailedSchemeRepository(_context);
            MarketHolidays = new MarketHolidayRepository(_context);
        }

        public ISchemeEnrollmentRepository SchemeEnrollments { get; }
        public IDetailedSchemeRepository DetailedSchemes { get; }
        public IMarketHolidayRepository MarketHolidays { get; }

        public async Task<int> CompleteAsync() =>
            await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}