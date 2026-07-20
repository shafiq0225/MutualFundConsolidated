using MutualFund.Investment.Domain.Interfaces;
using Dapper;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Infrastructure.Services
{
    public class NavRateService : INavRateService
    {
        private readonly string _connectionString;
        private readonly ILogger<NavRateService> _logger;

        public NavRateService(
            IConfiguration configuration,
            ILogger<NavRateService> logger)
        {
            _connectionString = configuration
                .GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        /// <summary>
        /// Fetches latest NAV for each scheme from DetailedSchemes table.
        /// This table is populated daily by App 2 (SchemeAPI)
        /// when it consumes the NavFileProcessedEvent from App 1.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetLatestNavAsync(
            List<string> schemeCodes)
        {
            var result = new Dictionary<string, decimal>();

            if (!schemeCodes.Any()) return result;

            try
            {
                using var connection = new MySqlConnection(_connectionString);

                // Get the latest NAV per scheme from DetailedSchemes
                // DetailedSchemes is App 2's table — shared DB
                var sql = @"
                    SELECT ds.SchemeCode, ds.NAV
                    FROM DetailedSchemes ds
                    INNER JOIN (
                        SELECT SchemeCode, MAX(NavDate) AS LatestDate
                        FROM DetailedSchemes
                        WHERE SchemeCode IN @SchemeCodes
                        GROUP BY SchemeCode
                    ) latest
                        ON  ds.SchemeCode = latest.SchemeCode
                        AND ds.NavDate    = latest.LatestDate";

                var rows = await connection.QueryAsync<NavRow>(
                    sql, new { SchemeCodes = schemeCodes });

                foreach (var row in rows)
                    result[row.SchemeCode] = row.NAV;

                _logger.LogInformation(
                    "Fetched NAV for {Count}/{Total} schemes " +
                    "from DetailedSchemes",
                    result.Count, schemeCodes.Count);

                // Log any missing schemes
                var missing = schemeCodes
                    .Except(result.Keys)
                    .ToList();

                if (missing.Any())
                    _logger.LogWarning(
                        "No NAV found for {Count} schemes: {Schemes}",
                        missing.Count,
                        string.Join(", ", missing.Take(5)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching NAV from DetailedSchemes");
            }

            return result;
        }

        private class NavRow
        {
            public string SchemeCode { get; set; } = string.Empty;
            public decimal NAV { get; set; }
        }
    }
}