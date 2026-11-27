using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFund.Investment.Application.Orders.Dtos;
using MutualFund.Investment.Domain.Common;
using MySqlConnector;

namespace MutualFund.Investment.Application.Orders.Queries
{
    /// <summary>
    /// Validates Purchase NAV and Units against AMFI historical NAVs in detailedschemes,
    /// detects swapped NAV/Unit fields, unit math mismatches, and same-day duplicates.
    /// </summary>
    public class OrderSanityCheckService
    {
        private readonly string _connString;
        private readonly ILogger<OrderSanityCheckService> _logger;

        public OrderSanityCheckService(
            IConfiguration configuration,
            ILogger<OrderSanityCheckService> logger)
        {
            _connString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        public async Task<Result<PreOrderSanityCheckResultDto>> CheckPreOrderAsync(
            PreOrderSanityCheckRequestDto dto)
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                var navRow = await conn.QueryFirstOrDefaultAsync<NavPointRow>(
                    @"SELECT SchemeCode, NavDate, NAV
                      FROM detailedschemes
                      WHERE SchemeCode = @SchemeCode
                        AND NavDate <= @OrderDate
                      ORDER BY NavDate DESC
                      LIMIT 1",
                    new { dto.SchemeCode, OrderDate = dto.OrderDate.Date });

                var eval = EvaluateTransaction(
                    dto.OrderDate.Date,
                    dto.InvestedAmount,
                    dto.PurchaseNAV,
                    dto.UnitsAllotted,
                    navRow);

                return Result<PreOrderSanityCheckResultDto>.Success(new PreOrderSanityCheckResultDto
                {
                    Severity = eval.Severity,
                    AmfiNav = eval.AmfiNav,
                    AmfiNavDate = eval.AmfiNavDate,
                    VariancePercent = eval.VariancePercent,
                    ExpectedUnits = eval.ExpectedUnits,
                    CalculatedUnits = eval.CalculatedUnits,
                    UnitMathOk = eval.UnitMathOk,
                    LooksSwapped = eval.LooksSwapped,
                    Message = eval.Finding
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pre-order sanity for {SchemeCode}", dto.SchemeCode);
                return Result<PreOrderSanityCheckResultDto>.Failure($"Sanity check failed: {ex.Message}");
            }
        }

        public async Task<Result<OrderSanityAuditSummaryDto>> ScanAllOrdersAsync()
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                var orders = (await conn.QueryAsync<OrderScanRow>(
                    @"SELECT Id, OrderNumber, OrderDate, InvestorUserId, InvestorName,
                             SchemeCode, SchemeName, FolioNumber, InvestedAmount,
                             PurchaseNAV, UnitsAllotted
                      FROM investmentorders
                      ORDER BY OrderDate DESC, Id DESC")).ToList();

                var schemeCodes = orders.Select(o => o.SchemeCode).Distinct().ToList();
                var navLookup = new Dictionary<string, List<NavPointRow>>(StringComparer.OrdinalIgnoreCase);

                if (schemeCodes.Count > 0)
                {
                    var navRows = await conn.QueryAsync<NavPointRow>(
                        @"SELECT SchemeCode, NavDate, NAV
                          FROM detailedschemes
                          WHERE SchemeCode IN @SchemeCodes
                          ORDER BY SchemeCode, NavDate DESC",
                        new { SchemeCodes = schemeCodes });

                    foreach (var n in navRows)
                    {
                        if (!navLookup.TryGetValue(n.SchemeCode, out var list))
                        {
                            list = new List<NavPointRow>();
                            navLookup[n.SchemeCode] = list;
                        }
                        list.Add(n);
                    }
                }

                // Detect same-day duplicates (same Investor + Scheme + Date + Amount)
                var dupGroups = orders
                    .GroupBy(o => $"{o.InvestorUserId}|{o.SchemeCode}|{o.OrderDate:yyyy-MM-dd}|{o.InvestedAmount:F2}")
                    .Where(g => g.Count() > 1)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.OrderNumber).ToList());

                var items = new List<OrderSanityAuditItemDto>(orders.Count);

                foreach (var o in orders)
                {
                    var enteredNav = o.PurchaseNAV ?? 0m;
                    var enteredUnits = o.UnitsAllotted ?? (enteredNav > 0 ? Math.Round(o.InvestedAmount / enteredNav, 6) : 0m);

                    NavPointRow? matchedNav = null;
                    if (navLookup.TryGetValue(o.SchemeCode, out var navList))
                    {
                        matchedNav = navList.FirstOrDefault(n => n.NavDate.Date <= o.OrderDate.Date);
                    }

                    var eval = EvaluateTransaction(
                        o.OrderDate.Date,
                        o.InvestedAmount,
                        enteredNav,
                        o.UnitsAllotted,
                        matchedNav);

                    string? dupOrder = null;
                    var dupKey = $"{o.InvestorUserId}|{o.SchemeCode}|{o.OrderDate:yyyy-MM-dd}|{o.InvestedAmount:F2}";
                    if (dupGroups.TryGetValue(dupKey, out var dupList))
                    {
                        dupOrder = string.Join(", ", dupList.Where(n => n != o.OrderNumber));
                        if (eval.Severity == "OK" || eval.Severity == "NoAmfiData")
                        {
                            eval.Severity = "Warning";
                            eval.Finding = $"Possible same-day duplicate of {dupOrder}. {eval.Finding}";
                        }
                    }

                    items.Add(new OrderSanityAuditItemDto
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        InvestorName = o.InvestorName,
                        SchemeCode = o.SchemeCode,
                        SchemeName = o.SchemeName,
                        FolioNumber = o.FolioNumber,
                        InvestedAmount = o.InvestedAmount,
                        EnteredNav = enteredNav,
                        AmfiNav = eval.AmfiNav,
                        AmfiNavDate = eval.AmfiNavDate,
                        VariancePercent = eval.VariancePercent,
                        EnteredUnits = enteredUnits,
                        ExpectedUnits = eval.ExpectedUnits,
                        Severity = eval.Severity,
                        Finding = eval.Finding,
                        LooksSwapped = eval.LooksSwapped,
                        DuplicateOrderNumber = dupOrder
                    });
                }

                // Order Critical first, then Warning, then NoAmfiData, then OK
                var severityRank = new Dictionary<string, int>
                {
                    ["Critical"] = 0,
                    ["Warning"] = 1,
                    ["NoAmfiData"] = 2,
                    ["OK"] = 3
                };

                var ordered = items
                    .OrderBy(i => severityRank.TryGetValue(i.Severity, out var r) ? r : 9)
                    .ThenByDescending(i => i.OrderDate)
                    .ToList();

                return Result<OrderSanityAuditSummaryDto>.Success(new OrderSanityAuditSummaryDto
                {
                    TotalScanned = ordered.Count,
                    CriticalCount = ordered.Count(i => i.Severity == "Critical"),
                    WarningCount = ordered.Count(i => i.Severity == "Warning"),
                    HealthyCount = ordered.Count(i => i.Severity == "OK"),
                    NoAmfiDataCount = ordered.Count(i => i.Severity == "NoAmfiData"),
                    Items = ordered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning orders for sanity check");
                return Result<OrderSanityAuditSummaryDto>.Failure($"Order scan failed: {ex.Message}");
            }
        }

        private static EvalResult EvaluateTransaction(
            DateTime orderDate,
            decimal investedAmount,
            decimal enteredNav,
            decimal? enteredUnitsOpt,
            NavPointRow? navRow)
        {
            var calcUnits = enteredNav > 0 ? Math.Round(investedAmount / enteredNav, 6) : 0m;
            var enteredUnits = enteredUnitsOpt ?? calcUnits;
            bool unitMathOk = enteredNav > 0 && Math.Abs(enteredUnits - calcUnits) <= 0.05m;

            if (enteredNav <= 0)
            {
                return new EvalResult
                {
                    CalculatedUnits = 0,
                    UnitMathOk = false,
                    Severity = "Critical",
                    Finding = "Purchase NAV is missing or zero."
                };
            }

            if (navRow == null || navRow.NAV <= 0)
            {
                return new EvalResult
                {
                    CalculatedUnits = calcUnits,
                    UnitMathOk = unitMathOk,
                    Severity = unitMathOk ? "NoAmfiData" : "Critical",
                    Finding = unitMathOk
                        ? $"No AMFI NAV record found on or before {orderDate:dd MMM yyyy} in DetailedSchemes."
                        : "Unit math mismatch (InvestedAmount / PurchaseNAV does not match Units)."
                };
            }

            var amfiNav = navRow.NAV;
            var expectedUnits = Math.Round(investedAmount / amfiNav, 6);
            var variancePct = Math.Round(((enteredNav - amfiNav) / amfiNav) * 100m, 2);
            var absVar = Math.Abs(variancePct);
            var looksSwapped = Math.Abs(enteredNav - expectedUnits) <= Math.Max(0.05m, expectedUnits * 0.02m);

            string severity;
            string finding;

            if (looksSwapped)
            {
                severity = "Critical";
                finding = $"Entered NAV ({enteredNav:F4}) matches expected Units ({expectedUnits:F4}) — looks like Units were entered in the Purchase NAV field.";
            }
            else if (absVar > 5m)
            {
                severity = "Critical";
                finding = $"Entered NAV differs by {(variancePct >= 0 ? "+" : "")}{variancePct:F2}% from AMFI NAV ({amfiNav:F4} on {navRow.NavDate:dd MMM yyyy}).";
            }
            else if (!unitMathOk)
            {
                severity = "Critical";
                finding = $"Unit math mismatch: {investedAmount:F2} ÷ {enteredNav:F4} = {calcUnits:F4}, but recorded Units = {enteredUnits:F4}.";
            }
            else if (absVar > 1m)
            {
                severity = "Warning";
                finding = $"Entered NAV differs by {(variancePct >= 0 ? "+" : "")}{variancePct:F2}% from AMFI NAV ({amfiNav:F4} on {navRow.NavDate:dd MMM yyyy}).";
            }
            else
            {
                severity = "OK";
                finding = $"Verified: within ±1% of AMFI NAV ({amfiNav:F4} on {navRow.NavDate:dd MMM yyyy}).";
            }

            return new EvalResult
            {
                AmfiNav = amfiNav,
                AmfiNavDate = navRow.NavDate,
                VariancePercent = variancePct,
                ExpectedUnits = expectedUnits,
                CalculatedUnits = calcUnits,
                UnitMathOk = unitMathOk,
                LooksSwapped = looksSwapped,
                Severity = severity,
                Finding = finding
            };
        }

        private class EvalResult
        {
            public decimal? AmfiNav { get; set; }
            public DateTime? AmfiNavDate { get; set; }
            public decimal? VariancePercent { get; set; }
            public decimal? ExpectedUnits { get; set; }
            public decimal CalculatedUnits { get; set; }
            public bool UnitMathOk { get; set; }
            public bool LooksSwapped { get; set; }
            public string Severity { get; set; } = "OK";
            public string Finding { get; set; } = string.Empty;
        }

        private class NavPointRow
        {
            public string SchemeCode { get; set; } = string.Empty;
            public DateTime NavDate { get; set; }
            public decimal NAV { get; set; }
        }

        private class OrderScanRow
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public string InvestorUserId { get; set; } = string.Empty;
            public string InvestorName { get; set; } = string.Empty;
            public string SchemeCode { get; set; } = string.Empty;
            public string SchemeName { get; set; } = string.Empty;
            public string? FolioNumber { get; set; }
            public decimal InvestedAmount { get; set; }
            public decimal? PurchaseNAV { get; set; }
            public decimal? UnitsAllotted { get; set; }
        }
    }
}
