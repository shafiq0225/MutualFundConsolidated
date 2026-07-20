namespace MutualFund.Investment.Domain.Interfaces
{
    /// <summary>
    /// Fetches latest NAV rates from DetailedSchemes table
    /// (populated by App 2 — SchemeAPI).
    /// Interface in Domain, implementation in Infrastructure.
    /// </summary>
    public interface INavRateService
    {
        Task<Dictionary<string, decimal>> GetLatestNavAsync(
            List<string> schemeCodes);
    }
}