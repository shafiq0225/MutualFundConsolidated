using MutualFund.Investment.Domain.Interfaces;
using MutualFund.Investment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace MutualFund.Investment.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InvestmentDbContext _context;
        private IDbContextTransaction? _transaction;

        // ── Repositories (lazy init) ──────────────────────────────
        private IOrderRepository? _orders;
        private IHoldingRepository? _holdings;
        private IPortfolioRepository? _portfolio;
        private IStatementRepository? _statements;

        public UnitOfWork(InvestmentDbContext context)
        {
            _context = context;
        }

        public IOrderRepository Orders =>
            _orders ??= new OrderRepository(_context);

        public IHoldingRepository Holdings =>
            _holdings ??= new HoldingRepository(_context);

        public IPortfolioRepository Portfolio =>
            _portfolio ??= new PortfolioRepository(_context);

        public IStatementRepository Statements =>
            _statements ??= new StatementRepository(_context);

        // ── Save ──────────────────────────────────────────────────
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // ── Transactions ──────────────────────────────────────────
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database
                .BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // ── Dispose ───────────────────────────────────────────────
        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}