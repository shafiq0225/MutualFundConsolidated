namespace MutualFund.Investment.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IOrderRepository Orders { get; }
        IHoldingRepository Holdings { get; }
        IPortfolioRepository Portfolio { get; }
        IStatementRepository Statements { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}