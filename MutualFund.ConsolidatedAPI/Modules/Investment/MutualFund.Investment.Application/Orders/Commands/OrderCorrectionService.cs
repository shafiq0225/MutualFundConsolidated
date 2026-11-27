using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFund.Investment.Application.Orders.Dtos;
using MutualFund.Investment.Domain.Common;
using MySqlConnector;

namespace MutualFund.Investment.Application.Orders.Commands
{
    /// <summary>
    /// Handles Admin order editing, cascade reversal/deletion, impact preview,
    /// and persistent audit logging in a single transactional boundary.
    /// </summary>
    public class OrderCorrectionService
    {
        private readonly string _connString;
        private readonly ILogger<OrderCorrectionService> _logger;

        public OrderCorrectionService(
            IConfiguration configuration,
            ILogger<OrderCorrectionService> logger)
        {
            _connString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        public async Task<Result<OrderCorrectionImpactDto>> GetImpactAsync(List<int> orderIds)
        {
            if (orderIds == null || orderIds.Count == 0)
                return Result<OrderCorrectionImpactDto>.Success(new OrderCorrectionImpactDto());

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                var orders = (await conn.QueryAsync<OrderSummaryRow>(
                    @"SELECT Id, OrderNumber, SchemeName, InvestedAmount
                      FROM investmentorders
                      WHERE Id IN @Ids",
                    new { Ids = orderIds })).ToList();

                var holdings = (await conn.QueryAsync<HoldingOrderRow>(
                    @"SELECT Id, OrderId FROM holdings WHERE OrderId IN @Ids",
                    new { Ids = orderIds })).ToList();

                var holdingByOrder = holdings.ToDictionary(h => h.OrderId, h => h.Id);
                var holdingIds = holdings.Select(h => h.Id).ToList();

                var snapCounts = new Dictionary<int, int>();
                if (holdingIds.Count > 0)
                {
                    var rows = await conn.QueryAsync<GroupCountRow>(
                        @"SELECT HoldingId AS KeyId, COUNT(*) AS Cnt
                          FROM portfoliosnapshots
                          WHERE HoldingId IN @HoldingIds
                          GROUP BY HoldingId",
                        new { HoldingIds = holdingIds });
                    foreach (var r in rows) snapCounts[r.KeyId] = r.Cnt;
                }

                var stmtCounts = new Dictionary<int, int>();
                var stmtRows = await conn.QueryAsync<GroupCountRow>(
                    @"SELECT OrderId AS KeyId, COUNT(*) AS Cnt
                      FROM investmentstatements
                      WHERE OrderId IN @Ids
                      GROUP BY OrderId",
                    new { Ids = orderIds });
                foreach (var r in stmtRows) stmtCounts[r.KeyId] = r.Cnt;

                var items = orders.Select(o =>
                {
                    int? hid = holdingByOrder.TryGetValue(o.Id, out var h) ? h : null;
                    return new OrderImpactItemDto
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        SchemeName = o.SchemeName,
                        InvestedAmount = o.InvestedAmount,
                        HoldingId = hid,
                        SnapshotCount = hid.HasValue && snapCounts.TryGetValue(hid.Value, out var sc) ? sc : 0,
                        StatementCount = stmtCounts.TryGetValue(o.Id, out var stc) ? stc : 0
                    };
                }).ToList();

                return Result<OrderCorrectionImpactDto>.Success(new OrderCorrectionImpactDto
                {
                    OrderCount = items.Count,
                    HoldingCount = items.Count(i => i.HoldingId.HasValue),
                    SnapshotCount = items.Sum(i => i.SnapshotCount),
                    StatementCount = items.Sum(i => i.StatementCount),
                    Items = items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing order correction impact");
                return Result<OrderCorrectionImpactDto>.Failure($"Failed to compute impact: {ex.Message}");
            }
        }

        public async Task<Result<AdminEditOrderResultDto>> EditOrderAsync(
            int orderId,
            AdminEditOrderDto dto,
            string adminUserId,
            string adminName)
        {
            if (dto.InvestedAmount <= 0 || dto.PurchaseNAV <= 0)
                return Result<AdminEditOrderResultDto>.Failure("Invested amount and Purchase NAV must be greater than zero.");

            var units = dto.UnitsAllotted.HasValue && dto.UnitsAllotted.Value > 0
                ? Math.Round(dto.UnitsAllotted.Value, 6)
                : Math.Round(dto.InvestedAmount / dto.PurchaseNAV, 6);

            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();
            await EnsureAuditTableAsync(conn);

            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var existing = await conn.QueryFirstOrDefaultAsync<ExistingOrderRow>(
                    @"SELECT Id, OrderNumber, InvestorUserId, InvestorName, SchemeCode, SchemeName, FundName,
                             FolioNumber, OrderDate, InvestedAmount, PurchaseNAV, UnitsAllotted
                      FROM investmentorders WHERE Id = @Id",
                    new { Id = orderId }, tx);

                if (existing == null)
                {
                    await tx.RollbackAsync();
                    return Result<AdminEditOrderResultDto>.Failure($"Order ID {orderId} not found.");
                }

                await conn.ExecuteAsync(
                    @"UPDATE investmentorders
                      SET InvestorUserId = @InvestorUserId,
                          InvestorName = @InvestorName,
                          SchemeCode = @SchemeCode,
                          SchemeName = @SchemeName,
                          FundName = @FundName,
                          FolioNumber = @FolioNumber,
                          OrderDate = @OrderDate,
                          InvestedAmount = @InvestedAmount,
                          PurchaseNAV = @PurchaseNAV,
                          UnitsAllotted = @UnitsAllotted,
                          UpdatedAt = @UpdatedAt
                      WHERE Id = @Id",
                    new
                    {
                        Id = orderId,
                        dto.InvestorUserId,
                        dto.InvestorName,
                        dto.SchemeCode,
                        dto.SchemeName,
                        dto.FundName,
                        dto.FolioNumber,
                        OrderDate = dto.OrderDate.Date,
                        dto.InvestedAmount,
                        dto.PurchaseNAV,
                        UnitsAllotted = units,
                        UpdatedAt = DateTime.UtcNow
                    }, tx);

                int? holdingId = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT Id FROM holdings WHERE OrderId = @OrderId LIMIT 1",
                    new { OrderId = orderId }, tx);

                int snapshotsRecalculated = 0;
                if (holdingId.HasValue)
                {
                    await conn.ExecuteAsync(
                        @"UPDATE holdings
                          SET InvestorUserId = @InvestorUserId,
                              InvestorName = @InvestorName,
                              SchemeCode = @SchemeCode,
                              SchemeName = @SchemeName,
                              FundName = @FundName,
                              FolioNumber = @FolioNumber,
                              PurchaseDate = @PurchaseDate,
                              PurchaseNAV = @PurchaseNAV,
                              InvestedAmount = @InvestedAmount,
                              Units = @Units
                          WHERE Id = @HoldingId",
                        new
                        {
                            HoldingId = holdingId.Value,
                            dto.InvestorUserId,
                            dto.InvestorName,
                            dto.SchemeCode,
                            dto.SchemeName,
                            dto.FundName,
                            dto.FolioNumber,
                            PurchaseDate = dto.OrderDate.Date,
                            dto.PurchaseNAV,
                            dto.InvestedAmount,
                            Units = units
                        }, tx);

                    var snapshots = (await conn.QueryAsync<SnapshotRow>(
                        @"SELECT Id, SnapshotDate, CurrentNAV
                          FROM portfoliosnapshots
                          WHERE HoldingId = @HoldingId",
                        new { HoldingId = holdingId.Value }, tx)).ToList();

                    if (snapshots.Count > 0)
                    {
                        var navHistory = (await conn.QueryAsync<NavHistoryRow>(
                            @"SELECT NavDate, NAV
                              FROM detailedschemes
                              WHERE SchemeCode = @SchemeCode
                              ORDER BY NavDate DESC",
                            new { dto.SchemeCode }, tx)).ToList();

                        foreach (var snap in snapshots)
                        {
                            decimal currentNav = snap.CurrentNAV;
                            if (navHistory.Count > 0)
                            {
                                var matched = navHistory.FirstOrDefault(n => n.NavDate.Date <= snap.SnapshotDate.Date);
                                if (matched != null && matched.NAV > 0)
                                    currentNav = matched.NAV;
                                else if (existing.SchemeCode != dto.SchemeCode)
                                    currentNav = navHistory[0].NAV;
                            }

                            var currentVal = Math.Round(units * currentNav, 2);
                            var pnl = Math.Round(currentVal - dto.InvestedAmount, 2);
                            var pnlPct = dto.InvestedAmount > 0
                                ? Math.Round((pnl / dto.InvestedAmount) * 100m, 4)
                                : 0m;

                            await conn.ExecuteAsync(
                                @"UPDATE portfoliosnapshots
                                  SET InvestorUserId = @InvestorUserId,
                                      InvestorName = @InvestorName,
                                      SchemeCode = @SchemeCode,
                                      SchemeName = @SchemeName,
                                      FundName = @FundName,
                                      InvestedAmount = @InvestedAmount,
                                      CurrentNAV = @CurrentNAV,
                                      CurrentValue = @CurrentValue,
                                      ProfitLoss = @ProfitLoss,
                                      ProfitLossPercent = @ProfitLossPercent
                                  WHERE Id = @Id",
                                new
                                {
                                    snap.Id,
                                    dto.InvestorUserId,
                                    dto.InvestorName,
                                    dto.SchemeCode,
                                    dto.SchemeName,
                                    dto.FundName,
                                    dto.InvestedAmount,
                                    CurrentNAV = currentNav,
                                    CurrentValue = currentVal,
                                    ProfitLoss = pnl,
                                    ProfitLossPercent = pnlPct
                                }, tx);

                            snapshotsRecalculated++;
                        }
                    }
                }

                var diffs = new List<string>();
                if (existing.SchemeCode != dto.SchemeCode)
                    diffs.Add($"Scheme: {existing.SchemeName} → {dto.SchemeName}");
                if (existing.InvestorUserId != dto.InvestorUserId)
                    diffs.Add($"Investor: {existing.InvestorName} → {dto.InvestorName}");
                if (existing.OrderDate.Date != dto.OrderDate.Date)
                    diffs.Add($"Date: {existing.OrderDate:yyyy-MM-dd} → {dto.OrderDate:yyyy-MM-dd}");
                if (existing.InvestedAmount != dto.InvestedAmount)
                    diffs.Add($"Amount: {existing.InvestedAmount:F2} → {dto.InvestedAmount:F2}");
                if (existing.PurchaseNAV != dto.PurchaseNAV)
                    diffs.Add($"NAV: {existing.PurchaseNAV:F4} → {dto.PurchaseNAV:F4}");
                if (existing.UnitsAllotted != units)
                    diffs.Add($"Units: {existing.UnitsAllotted:F4} → {units:F4}");
                if (existing.FolioNumber != dto.FolioNumber)
                    diffs.Add($"Folio: {existing.FolioNumber} → {dto.FolioNumber}");
                if (snapshotsRecalculated > 0)
                    diffs.Add($"Recalculated {snapshotsRecalculated} snapshot(s)");

                var changeSummary = diffs.Count > 0 ? string.Join("; ", diffs) : "Order fields re-saved";
                var reason = string.IsNullOrWhiteSpace(dto.Reason) ? "Admin order correction" : dto.Reason.Trim();

                await InsertAuditLogAsync(conn, tx, orderId, existing.OrderNumber, "EDITED",
                    adminUserId, adminName, changeSummary, reason);

                await tx.CommitAsync();

                _logger.LogInformation("Admin {Admin} edited order {OrderNumber}: {Summary}",
                    adminName, existing.OrderNumber, changeSummary);

                return Result<AdminEditOrderResultDto>.Success(new AdminEditOrderResultDto
                {
                    OrderId = orderId,
                    OrderNumber = existing.OrderNumber,
                    HoldingId = holdingId,
                    SnapshotsRecalculated = snapshotsRecalculated,
                    ChangeSummary = changeSummary
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to edit order {OrderId}", orderId);
                return Result<AdminEditOrderResultDto>.Failure($"Edit failed: {ex.Message}");
            }
        }

        public async Task<Result<AdminReverseOrdersResultDto>> ReverseOrdersAsync(
            AdminReverseOrdersDto dto,
            string adminUserId,
            string adminName)
        {
            if (dto.OrderIds == null || dto.OrderIds.Count == 0)
                return Result<AdminReverseOrdersResultDto>.Failure("At least one order must be selected.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return Result<AdminReverseOrdersResultDto>.Failure("A reversal reason is required for the audit log.");

            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();
            await EnsureAuditTableAsync(conn);

            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var orders = (await conn.QueryAsync<OrderNumberRow>(
                    @"SELECT Id, OrderNumber FROM investmentorders WHERE Id IN @Ids",
                    new { Ids = dto.OrderIds }, tx)).ToList();

                if (orders.Count == 0)
                {
                    await tx.RollbackAsync();
                    return Result<AdminReverseOrdersResultDto>.Failure("No matching orders found.");
                }

                var foundOrderIds = orders.Select(o => o.Id).ToList();
                var holdingIds = (await conn.QueryAsync<int>(
                    @"SELECT Id FROM holdings WHERE OrderId IN @Ids",
                    new { Ids = foundOrderIds }, tx)).ToList();

                int snapshotsRemoved = 0;
                if (holdingIds.Count > 0)
                {
                    snapshotsRemoved = await conn.ExecuteAsync(
                        @"DELETE FROM portfoliosnapshots WHERE HoldingId IN @HoldingIds",
                        new { HoldingIds = holdingIds }, tx);
                }

                int statementsRemoved = await conn.ExecuteAsync(
                    @"DELETE FROM investmentstatements WHERE OrderId IN @Ids",
                    new { Ids = foundOrderIds }, tx);

                int holdingsRemoved = 0;
                if (holdingIds.Count > 0)
                {
                    holdingsRemoved = await conn.ExecuteAsync(
                        @"DELETE FROM holdings WHERE Id IN @HoldingIds",
                        new { HoldingIds = holdingIds }, tx);
                }

                int ordersRemoved = await conn.ExecuteAsync(
                    @"DELETE FROM investmentorders WHERE Id IN @Ids",
                    new { Ids = foundOrderIds }, tx);

                var orderNos = string.Join(", ", orders.Select(o => o.OrderNumber));
                if (orderNos.Length > 240) orderNos = orderNos[..237] + "...";

                var summary = $"Removed {ordersRemoved} order(s), {holdingsRemoved} holding(s), {snapshotsRemoved} snapshot(s), {statementsRemoved} statement(s)";
                await InsertAuditLogAsync(conn, tx, orders.Count == 1 ? orders[0].Id : null,
                    orderNos, "REVERSED", adminUserId, adminName, summary, dto.Reason.Trim());

                await tx.CommitAsync();

                _logger.LogInformation("Admin {Admin} reversed orders [{Orders}]: {Summary}",
                    adminName, orderNos, summary);

                return Result<AdminReverseOrdersResultDto>.Success(new AdminReverseOrdersResultDto
                {
                    OrdersRemoved = ordersRemoved,
                    HoldingsRemoved = holdingsRemoved,
                    SnapshotsRemoved = snapshotsRemoved,
                    StatementsRemoved = statementsRemoved
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to reverse orders");
                return Result<AdminReverseOrdersResultDto>.Failure($"Reversal failed: {ex.Message}");
            }
        }

        public async Task<Result<List<OrderCorrectionLogDto>>> GetLogsAsync(int limit = 100)
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureAuditTableAsync(conn);

                var rows = (await conn.QueryAsync<OrderCorrectionLogDto>(
                    @"SELECT Id, OrderId, OrderNumber, ActionType, AdminUserId, AdminName, ChangeSummary, Reason, CreatedAt
                      FROM ordercorrectionlogs
                      ORDER BY CreatedAt DESC
                      LIMIT @Limit",
                    new { Limit = limit })).ToList();

                return Result<List<OrderCorrectionLogDto>>.Success(rows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load order correction logs");
                return Result<List<OrderCorrectionLogDto>>.Failure($"Failed to load audit logs: {ex.Message}");
            }
        }

        private static async Task EnsureAuditTableAsync(MySqlConnection conn)
        {
            await conn.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS ordercorrectionlogs (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    OrderId INT NULL,
                    OrderNumber VARCHAR(255) NOT NULL,
                    ActionType VARCHAR(32) NOT NULL,
                    AdminUserId VARCHAR(128) NOT NULL,
                    AdminName VARCHAR(255) NOT NULL,
                    ChangeSummary TEXT NOT NULL,
                    Reason VARCHAR(500) NOT NULL,
                    CreatedAt DATETIME NOT NULL
                )");
        }

        private static async Task InsertAuditLogAsync(
            MySqlConnection conn,
            MySqlTransaction tx,
            int? orderId,
            string orderNumber,
            string actionType,
            string adminUserId,
            string adminName,
            string changeSummary,
            string reason)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO ordercorrectionlogs
                    (OrderId, OrderNumber, ActionType, AdminUserId, AdminName, ChangeSummary, Reason, CreatedAt)
                  VALUES
                    (@OrderId, @OrderNumber, @ActionType, @AdminUserId, @AdminName, @ChangeSummary, @Reason, @CreatedAt)",
                new
                {
                    OrderId = orderId,
                    OrderNumber = orderNumber,
                    ActionType = actionType,
                    AdminUserId = adminUserId,
                    AdminName = adminName,
                    ChangeSummary = changeSummary,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                }, tx);
        }

        private class ExistingOrderRow
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public string InvestorUserId { get; set; } = string.Empty;
            public string InvestorName { get; set; } = string.Empty;
            public string SchemeCode { get; set; } = string.Empty;
            public string SchemeName { get; set; } = string.Empty;
            public string FundName { get; set; } = string.Empty;
            public string? FolioNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal InvestedAmount { get; set; }
            public decimal? PurchaseNAV { get; set; }
            public decimal? UnitsAllotted { get; set; }
        }

        private class OrderSummaryRow
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
            public string SchemeName { get; set; } = string.Empty;
            public decimal InvestedAmount { get; set; }
        }

        private class HoldingOrderRow
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
        }

        private class GroupCountRow
        {
            public int KeyId { get; set; }
            public int Cnt { get; set; }
        }

        private class SnapshotRow
        {
            public int Id { get; set; }
            public DateTime SnapshotDate { get; set; }
            public decimal CurrentNAV { get; set; }
        }

        private class NavHistoryRow
        {
            public DateTime NavDate { get; set; }
            public decimal NAV { get; set; }
        }

        private class OrderNumberRow
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; } = string.Empty;
        }
    }
}
