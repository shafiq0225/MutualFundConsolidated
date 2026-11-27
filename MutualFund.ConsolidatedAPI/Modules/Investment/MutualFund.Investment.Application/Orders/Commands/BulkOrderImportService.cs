using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MutualFund.Investment.Application.Orders.Dtos;
using MutualFund.Investment.Domain.Common;
using MySqlConnector;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MutualFund.Investment.Application.Orders.Commands
{
    /// <summary>
    /// Handles bulk order import from password-protected CAMS + KFintech Detailed CAS PDFs,
    /// with scheme/investor matching, duplicate detection, AMFI NAV sanity check,
    /// and transactional batch creation.
    /// </summary>
    public class BulkOrderImportService
    {
        private readonly string _connString;
        private readonly ILogger<BulkOrderImportService> _logger;

        public BulkOrderImportService(
            IConfiguration configuration,
            ILogger<BulkOrderImportService> logger)
        {
            _connString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        public async Task<Result<BulkImportPreviewResponseDto>> PreviewCasPdfAsync(
            Stream pdfStream,
            string fileName,
            string? password)
        {
            try
            {
                var lines = new List<string>();
                using (var doc = PdfDocument.Open(pdfStream, new ParsingOptions { Password = password ?? string.Empty }))
                {
                    foreach (var page in doc.GetPages())
                    {
                        lines.AddRange(ExtractLinesFromPage(page));
                    }
                }

                var rawRows = ParseCasLines(lines);
                return await BuildPreviewResponseAsync("CAS", fileName, rawRows);
            }
            catch (Exception ex) when (
                ex.GetType().Name.Contains("Encrypted", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
            {
                return Result<BulkImportPreviewResponseDto>.Failure(
                    "Invalid or missing CAS PDF password. Please enter the password used to open this PDF.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse CAS PDF {File}", fileName);
                return Result<BulkImportPreviewResponseDto>.Failure($"Failed to parse CAS PDF: {ex.Message}");
            }
        }

        public async Task<Result<BulkImportConfirmResultDto>> ConfirmImportAsync(
            BulkImportConfirmRequestDto dto,
            string createdByUserId)
        {
            if (dto.Rows == null || dto.Rows.Count == 0)
                return Result<BulkImportConfirmResultDto>.Failure("No rows selected for import.");

            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                var year = DateTime.UtcNow.Year;
                var prefix = $"ORD-{year}-";
                var lastOrderNo = await conn.QueryFirstOrDefaultAsync<string?>(
                    @"SELECT OrderNumber FROM investmentorders
                      WHERE OrderNumber LIKE @Prefix
                      ORDER BY Id DESC LIMIT 1",
                    new { Prefix = prefix + "%" }, tx);

                int nextSeq = 1;
                if (!string.IsNullOrWhiteSpace(lastOrderNo))
                {
                    var parts = lastOrderNo.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var lastSeq))
                        nextSeq = lastSeq + 1;
                }

                var schemeCodes = dto.Rows.Select(r => r.SchemeCode).Distinct().ToList();
                var latestNavs = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                if (schemeCodes.Count > 0)
                {
                    var navRows = await conn.QueryAsync<(string SchemeCode, decimal NAV)>(
                        @"SELECT ds.SchemeCode, ds.NAV
                          FROM detailedschemes ds
                          INNER JOIN (
                              SELECT SchemeCode, MAX(NavDate) AS MaxDate
                              FROM detailedschemes
                              WHERE SchemeCode IN @Codes
                              GROUP BY SchemeCode
                          ) latest ON ds.SchemeCode = latest.SchemeCode AND ds.NavDate = latest.MaxDate",
                        new { Codes = schemeCodes }, tx);
                    foreach (var nr in navRows) latestNavs[nr.SchemeCode] = nr.NAV;
                }

                var createdOrderNumbers = new List<string>();
                int holdingsCreated = 0;
                int snapshotsCreated = 0;
                var today = DateTime.Today;

                foreach (var row in dto.Rows)
                {
                    var orderNumber = $"{prefix}{nextSeq:D4}";
                    nextSeq++;

                    var units = row.UnitsAllotted > 0
                        ? Math.Round(row.UnitsAllotted, 6)
                        : Math.Round(row.InvestedAmount / row.PurchaseNAV, 6);

                    var now = DateTime.UtcNow;
                    var orderDate = row.OrderDate.Date;

                    var orderId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO investmentorders
                            (OrderNumber, InvestorUserId, InvestorName, SchemeCode, SchemeName, FundName,
                             InvestedAmount, PaymentMode, OrderDate, Status, AssignedDate, SubmittedDate,
                             SubmittedByUserId, VerifiedDate, VerifiedByUserId, PurchaseNAV, UnitsAllotted,
                             FolioNumber, ActivatedDate, Notes, CreatedByUserId, CreatedAt, UpdatedAt)
                          VALUES
                            (@OrderNumber, @InvestorUserId, @InvestorName, @SchemeCode, @SchemeName, @FundName,
                             @InvestedAmount, @PaymentMode, @OrderDate, 'Active', @OrderDate, @OrderDate,
                             @CreatedByUserId, @OrderDate, @CreatedByUserId, @PurchaseNAV, @UnitsAllotted,
                             @FolioNumber, @OrderDate, @Notes, @CreatedByUserId, @Now, @Now);
                          SELECT LAST_INSERT_ID();",
                        new
                        {
                            OrderNumber = orderNumber,
                            row.InvestorUserId,
                            row.InvestorName,
                            row.SchemeCode,
                            row.SchemeName,
                            row.FundName,
                            row.InvestedAmount,
                            PaymentMode = string.IsNullOrWhiteSpace(row.PaymentMode) ? "Online" : row.PaymentMode,
                            OrderDate = orderDate,
                            CreatedByUserId = createdByUserId,
                            row.PurchaseNAV,
                            UnitsAllotted = units,
                            row.FolioNumber,
                            Notes = dto.SourceNote ?? "Bulk Imported",
                            Now = now
                        }, tx);

                    var holdingId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO holdings
                            (OrderId, InvestorUserId, InvestorName, SchemeCode, SchemeName, FundName,
                             FolioNumber, PurchaseDate, PurchaseNAV, InvestedAmount, Units, IsActive, CreatedAt)
                          VALUES
                            (@OrderId, @InvestorUserId, @InvestorName, @SchemeCode, @SchemeName, @FundName,
                             @FolioNumber, @PurchaseDate, @PurchaseNAV, @InvestedAmount, @Units, 1, @Now);
                          SELECT LAST_INSERT_ID();",
                        new
                        {
                            OrderId = orderId,
                            row.InvestorUserId,
                            row.InvestorName,
                            row.SchemeCode,
                            row.SchemeName,
                            row.FundName,
                            row.FolioNumber,
                            PurchaseDate = orderDate,
                            row.PurchaseNAV,
                            row.InvestedAmount,
                            Units = units,
                            Now = now
                        }, tx);

                    holdingsCreated++;
                    createdOrderNumbers.Add(orderNumber);

                    if (latestNavs.TryGetValue(row.SchemeCode, out var currentNav) && currentNav > 0)
                    {
                        var currentValue = Math.Round(units * currentNav, 2);
                        var pnl = Math.Round(currentValue - row.InvestedAmount, 2);
                        var pnlPct = row.InvestedAmount > 0
                            ? Math.Round((pnl / row.InvestedAmount) * 100m, 4)
                            : 0m;

                        await conn.ExecuteAsync(
                            @"INSERT INTO portfoliosnapshots
                                (HoldingId, InvestorUserId, InvestorName, SchemeCode, SchemeName, FundName,
                                 SnapshotDate, CurrentNAV, InvestedAmount, CurrentValue, ProfitLoss, ProfitLossPercent, CreatedAt)
                              VALUES
                                (@HoldingId, @InvestorUserId, @InvestorName, @SchemeCode, @SchemeName, @FundName,
                                 @SnapshotDate, @CurrentNAV, @InvestedAmount, @CurrentValue, @ProfitLoss, @ProfitLossPercent, @Now)",
                            new
                            {
                                HoldingId = holdingId,
                                row.InvestorUserId,
                                row.InvestorName,
                                row.SchemeCode,
                                row.SchemeName,
                                row.FundName,
                                SnapshotDate = today,
                                CurrentNAV = currentNav,
                                row.InvestedAmount,
                                CurrentValue = currentValue,
                                ProfitLoss = pnl,
                                ProfitLossPercent = pnlPct,
                                Now = now
                            }, tx);

                        snapshotsCreated++;
                    }
                }

                await tx.CommitAsync();

                _logger.LogInformation("Bulk import committed: {Orders} orders, {Holdings} holdings, {Snaps} snapshots",
                    createdOrderNumbers.Count, holdingsCreated, snapshotsCreated);

                return Result<BulkImportConfirmResultDto>.Success(new BulkImportConfirmResultDto
                {
                    OrdersCreated = createdOrderNumbers.Count,
                    HoldingsCreated = holdingsCreated,
                    SnapshotsRecalculated = snapshotsCreated,
                    CreatedOrderNumbers = createdOrderNumbers
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Bulk import failed and rolled back");
                return Result<BulkImportConfirmResultDto>.Failure($"Bulk import failed: {ex.Message}");
            }
        }

        // ── Preview Builder (Matching + Duplicate + AMFI Sanity Check) ──

        private async Task<Result<BulkImportPreviewResponseDto>> BuildPreviewResponseAsync(
            string sourceType,
            string fileName,
            List<RawParsedRow> rawRows)
        {
            using var conn = new MySqlConnection(_connString);
            await conn.OpenAsync();

            var investors = (await conn.QueryAsync<InvestorLookupRow>(
                @"SELECT Id, PanNumber, FirstName, LastName, Email
                  FROM users
                  WHERE IsActive = 1 AND Role = 3")).ToList();

            var schemes = (await conn.QueryAsync<SchemeLookupRow>(
                @"SELECT SchemeCode, SchemeName, FundName
                  FROM schemeenrollments
                  WHERE IsApproved = 1")).ToList();

            var existingOrders = (await conn.QueryAsync<ExistingOrderKeyRow>(
                @"SELECT OrderNumber, InvestorUserId, InvestorName, SchemeCode, FolioNumber, OrderDate, InvestedAmount, UnitsAllotted
                  FROM investmentorders")).ToList();

            var schemeCodes = schemes.Select(s => s.SchemeCode).Distinct().ToList();
            var navLookup = new Dictionary<string, List<NavLookupRow>>(StringComparer.OrdinalIgnoreCase);
            if (schemeCodes.Count > 0)
            {
                var navRows = await conn.QueryAsync<NavLookupRow>(
                    @"SELECT SchemeCode, NavDate, NAV
                      FROM detailedschemes
                      WHERE SchemeCode IN @Codes
                      ORDER BY SchemeCode, NavDate DESC",
                    new { Codes = schemeCodes });

                foreach (var n in navRows)
                {
                    if (!navLookup.TryGetValue(n.SchemeCode, out var list))
                    {
                        list = new List<NavLookupRow>();
                        navLookup[n.SchemeCode] = list;
                    }
                    list.Add(n);
                }
            }

            var previewRows = new List<BulkImportPreviewRowDto>();

            foreach (var raw in rawRows)
            {
                var inv = MatchInvestor(raw.InvestorHint, investors);
                if (inv == null && !string.IsNullOrWhiteSpace(raw.FolioNumber))
                {
                    var folioOwner = existingOrders.FirstOrDefault(e =>
                        !string.IsNullOrWhiteSpace(e.FolioNumber) &&
                        string.Equals(e.FolioNumber.Trim(), raw.FolioNumber.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (folioOwner != null)
                    {
                        inv = investors.FirstOrDefault(i =>
                            string.Equals(i.PanOrId, folioOwner.InvestorUserId, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(i.Id, folioOwner.InvestorUserId, StringComparison.OrdinalIgnoreCase));
                    }
                }
                var sch = MatchScheme(raw.SchemeHint, schemes);

                var units = raw.UnitsAllotted.HasValue && raw.UnitsAllotted.Value > 0
                    ? Math.Round(raw.UnitsAllotted.Value, 6)
                    : (raw.PurchaseNAV > 0 ? Math.Round(raw.InvestedAmount / raw.PurchaseNAV, 6) : 0m);

                var item = new BulkImportPreviewRowDto
                {
                    RowNumber = raw.RowNumber,
                    OrderDate = raw.OrderDate.Date,
                    InvestorUserId = inv?.PanOrId ?? string.Empty,
                    InvestorName = inv?.FullName ?? raw.InvestorHint,
                    SchemeCode = sch?.SchemeCode ?? string.Empty,
                    SchemeName = sch?.SchemeName ?? string.Empty,
                    FundName = sch?.FundName ?? string.Empty,
                    RawSchemeText = raw.SchemeHint,
                    FolioNumber = raw.FolioNumber,
                    InvestedAmount = Math.Round(raw.InvestedAmount, 2),
                    PurchaseNAV = Math.Round(raw.PurchaseNAV, 4),
                    UnitsAllotted = units,
                    PaymentMode = raw.PaymentMode
                };

                if (inv == null || sch == null)
                {
                    item.Status = "Unmatched";
                    item.StatusNote = inv == null && sch == null
                        ? $"Unmatched Investor ('{raw.InvestorHint}') & Scheme ('{raw.SchemeHint}')"
                        : inv == null
                            ? $"Unmatched Investor ('{raw.InvestorHint}')"
                            : $"Scheme not found/approved ('{raw.SchemeHint}')";
                    item.IncludeByDefault = false;
                    previewRows.Add(item);
                    continue;
                }

                // Duplicate detection: same Investor + Scheme + OrderDate + (Amount or Units close)
                var dup = existingOrders.FirstOrDefault(e =>
                    string.Equals(e.InvestorUserId, item.InvestorUserId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.SchemeCode, item.SchemeCode, StringComparison.OrdinalIgnoreCase) &&
                    e.OrderDate.Date == item.OrderDate.Date &&
                    (Math.Abs(e.InvestedAmount - item.InvestedAmount) <= 1.0m ||
                     (e.UnitsAllotted.HasValue && Math.Abs(e.UnitsAllotted.Value - item.UnitsAllotted) <= 0.05m)));

                if (dup != null)
                {
                    item.Status = "Duplicate";
                    item.DuplicateOrderNumber = dup.OrderNumber;
                    item.StatusNote = $"Already exists ({dup.OrderNumber})";
                    item.IncludeByDefault = false;
                }
                else if (navLookup.TryGetValue(item.SchemeCode, out var navList))
                {
                    var navRow = navList.FirstOrDefault(n => n.NavDate.Date <= item.OrderDate.Date);
                    var isRecentAmfi = navRow != null && navRow.NAV > 0 && (item.OrderDate.Date - navRow.NavDate.Date).TotalDays <= 4;

                    if (isRecentAmfi && navRow != null)
                    {
                        item.AmfiNav = navRow.NAV;
                        item.AmfiNavDate = navRow.NavDate;
                        var varPct = Math.Round(((item.PurchaseNAV - navRow.NAV) / navRow.NAV) * 100m, 2);
                        item.VariancePercent = varPct;

                        if (Math.Abs(varPct) > 5m)
                        {
                            if (sourceType == "CAS" && navRow.NavDate.Date != item.OrderDate.Date)
                            {
                                item.Status = "Warning";
                                item.StatusNote = $"CAS NAV vs older AMFI ({navRow.NAV:F4} on {navRow.NavDate:dd MMM})";
                                item.IncludeByDefault = true;
                            }
                            else
                            {
                                item.Status = "Issue";
                                item.StatusNote = $"NAV differs by {(varPct >= 0 ? "+" : "")}{varPct:F2}% vs AMFI ({navRow.NAV:F4} on {navRow.NavDate:dd MMM})";
                                item.IncludeByDefault = false;
                            }
                        }
                        else if (Math.Abs(varPct) > 1m)
                        {
                            item.Status = "Warning";
                            item.StatusNote = $"Minor NAV drift {(varPct >= 0 ? "+" : "")}{varPct:F2}% vs AMFI ({navRow.NAV:F4} on {navRow.NavDate:dd MMM})";
                            item.IncludeByDefault = true;
                        }
                        else
                        {
                            item.Status = "Ready";
                            item.StatusNote = $"Matched AMFI NAV ({navRow.NAV:F4} on {navRow.NavDate:dd MMM})";
                            item.IncludeByDefault = true;
                        }
                    }
                    else
                    {
                        item.Status = "Ready";
                        item.StatusNote = "Matched Investor & Scheme (AMFI NAV for this date not in DB)";
                        item.IncludeByDefault = true;
                    }
                }
                else
                {
                    item.Status = "Ready";
                    item.StatusNote = "Matched Investor & Scheme";
                    item.IncludeByDefault = true;
                }

                previewRows.Add(item);
            }

            return Result<BulkImportPreviewResponseDto>.Success(new BulkImportPreviewResponseDto
            {
                SourceType = sourceType,
                FileName = fileName,
                TotalParsed = previewRows.Count,
                ReadyCount = previewRows.Count(r => r.Status == "Ready" || r.Status == "Warning"),
                IssueCount = previewRows.Count(r => r.Status == "Issue"),
                DuplicateCount = previewRows.Count(r => r.Status == "Duplicate"),
                UnmatchedCount = previewRows.Count(r => r.Status == "Unmatched"),
                Rows = previewRows
            });
        }

        // ── CAS PDF Extraction & Parsing Helpers ──────────────────────

        private static List<string> ExtractLinesFromPage(Page page)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) return new List<string>();

            var rows = new List<(double Y, List<Word> Words)>();
            foreach (var w in words.OrderByDescending(x => x.BoundingBox.Bottom).ThenBy(x => x.BoundingBox.Left))
            {
                var y = w.BoundingBox.Bottom;
                var existing = rows.FirstOrDefault(r => Math.Abs(r.Y - y) <= 3.0);
                if (existing.Words != null)
                {
                    existing.Words.Add(w);
                }
                else
                {
                    rows.Add((y, new List<Word> { w }));
                }
            }

            return rows
                .OrderByDescending(r => r.Y)
                .Select(r => string.Join(" ", r.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();
        }

        private static List<RawParsedRow> ParseCasLines(List<string> lines)
        {
            var results = new List<RawParsedRow>();
            var investorHints = new List<string>();
            string currentFolio = string.Empty;
            string currentScheme = string.Empty;
            bool unitsBeforeNav = true; // CAMS CAS standard: Amount | Units | NAV | Unit Balance

            var panRegex = new Regex(@"\bPAN\s*[:\-]?\s*([A-Z]{5}[0-9]{4}[A-Z])\b", RegexOptions.IgnoreCase);
            var maskedPanRegex = new Regex(@"\b([A-Z]{2}[X*]{4,6}[0-9A-Z]{2})\b", RegexOptions.IgnoreCase);
            var emailRegex = new Regex(@"\b([A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})\b");
            var folioRegex = new Regex(@"Folio\s*No\.?\s*[:\-]?\s*([A-Za-z0-9\/\-]+)", RegexOptions.IgnoreCase);
            var schemeHeaderRegex = new Regex(@"^([A-Z0-9]{2,10}\s*[-–]\s*)?(.+?)\s*[-–]?\s*ISIN\s*[:\-]?\s*(INF[A-Z0-9]{9})", RegexOptions.IgnoreCase);
            var txDateRegex = new Regex(@"^(\d{2}[-/][A-Za-z0-9]{2,3}[-/]\d{4})\s+(.+)$");
            var numRegex = new Regex(@"-?\d{1,3}(?:,\d{3})*(?:\.\d+)?|-?\d+(?:\.\d+)?");

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();

                // Detect table header column order ("Amount ... Units ... NAV" vs "Amount ... NAV ... Units")
                if (line.Contains("Amount", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains("NAV", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains("Unit", StringComparison.OrdinalIgnoreCase))
                {
                    int idxNav = line.IndexOf("NAV", StringComparison.OrdinalIgnoreCase);
                    int idxUnits = line.IndexOf("Unit", StringComparison.OrdinalIgnoreCase);
                    if (idxNav >= 0 && idxUnits >= 0)
                        unitsBeforeNav = idxUnits < idxNav;
                }

                var panMatch = panRegex.Match(line);
                if (panMatch.Success)
                    investorHints.Add(panMatch.Groups[1].Value.ToUpperInvariant());

                var maskedMatch = maskedPanRegex.Match(line);
                if (maskedMatch.Success)
                    investorHints.Add(maskedMatch.Groups[1].Value.ToUpperInvariant());

                var emailMatch = emailRegex.Match(line);
                if (emailMatch.Success)
                    investorHints.Add(emailMatch.Groups[1].Value.Trim());

                var folioMatch = folioRegex.Match(line);
                if (folioMatch.Success)
                {
                    currentFolio = folioMatch.Groups[1].Value.Trim();
                }

                var schemeMatch = schemeHeaderRegex.Match(line);
                if (schemeMatch.Success)
                {
                    currentScheme = schemeMatch.Groups[2].Value.Trim();
                    continue;
                }

                // Alternate CAMS scheme line format: "123456 - Scheme Name (Advisor: ...)" or "Scheme: ..."
                if (line.StartsWith("Scheme", StringComparison.OrdinalIgnoreCase) && line.Contains("ISIN", StringComparison.OrdinalIgnoreCase))
                {
                    var cleaned = Regex.Replace(line, @"^Scheme\s*[:\-]?\s*", "", RegexOptions.IgnoreCase);
                    cleaned = Regex.Replace(cleaned, @"\s*ISIN.*$", "", RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrWhiteSpace(cleaned))
                        currentScheme = cleaned;
                    continue;
                }

                var txMatch = txDateRegex.Match(line);
                if (!txMatch.Success) continue;

                var dateStr = txMatch.Groups[1].Value;
                var rest = txMatch.Groups[2].Value;

                if (!TryParseDate(dateStr, out var txDate)) continue;

                // Skip non-purchase / informational lines
                var lowerRest = rest.ToLowerInvariant();
                if (lowerRest.Contains("opening balance") ||
                    lowerRest.Contains("closing balance") ||
                    lowerRest.Contains("stamp duty") ||
                    lowerRest.Contains("gross systematic") ||
                    lowerRest.Contains("redemption") ||
                    lowerRest.Contains("switch out") ||
                    lowerRest.Contains("switch-out") ||
                    lowerRest.Contains("stt") ||
                    lowerRest.Contains("valuation"))
                {
                    continue;
                }

                // Extract numbers from the right side of the transaction line
                var numMatches = numRegex.Matches(rest)
                    .Cast<Match>()
                    .Select(m => decimal.TryParse(m.Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : (decimal?)null)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (numMatches.Count < 3) continue;

                decimal amount = 0, nav = 0, units = 0;
                bool matchedMath = false;

                // CAMS CAS transaction table ends with: Amount, Units, NAV, UnitBalance (4 numbers)
                // or Amount, NAV, Units, UnitBalance (when header has NAV before Units)
                if (numMatches.Count >= 4)
                {
                    var a = numMatches[^4];
                    var col2 = numMatches[^3];
                    var col3 = numMatches[^2];
                    var u = unitsBeforeNav ? col2 : col3;
                    var n = unitsBeforeNav ? col3 : col2;
                    if (a > 0 && n > 0 && u > 0 && Math.Abs((a / n) - u) <= Math.Max(0.1m, u * 0.02m))
                    {
                        amount = a; nav = n; units = u;
                        matchedMath = true;
                    }
                }

                if (!matchedMath && numMatches.Count >= 3)
                {
                    var a = numMatches[^3];
                    var col2 = numMatches[^2];
                    var col3 = numMatches[^1];
                    var u = unitsBeforeNav ? col2 : col3;
                    var n = unitsBeforeNav ? col3 : col2;
                    if (a > 0 && n > 0 && u > 0 && Math.Abs((a / n) - u) <= Math.Max(0.1m, u * 0.02m))
                    {
                        amount = a; nav = n; units = u;
                        matchedMath = true;
                    }
                }

                if (!matchedMath) continue;

                results.Add(new RawParsedRow
                {
                    RowNumber = results.Count + 1,
                    OrderDate = txDate,
                    InvestorHint = string.Join("|", investorHints.Distinct(StringComparer.OrdinalIgnoreCase)),
                    SchemeHint = currentScheme,
                    FolioNumber = currentFolio,
                    InvestedAmount = amount,
                    PurchaseNAV = nav,
                    UnitsAllotted = units,
                    PaymentMode = "Online"
                });
            }

            return results;
        }

        private static InvestorLookupRow? MatchInvestor(string hint, List<InvestorLookupRow> investors)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return investors.Count == 1 ? investors[0] : null;

            var hints = hint.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var h in hints)
            {
                var exact = investors.FirstOrDefault(i =>
                    string.Equals(i.PanNumber, h, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.Id, h, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.Email, h, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(i.FullName, h, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                // Masked PAN match e.g. "DPXXXXXX4L" -> starts with "DP" and ends with "4L"
                if (h.Length == 10 && h.Contains('X', StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = h[..2];
                    var suffix = h[^2..];
                    var maskedMatch = investors.FirstOrDefault(i =>
                        !string.IsNullOrWhiteSpace(i.PanNumber) &&
                        i.PanNumber.Length == 10 &&
                        i.PanNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        i.PanNumber.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                    if (maskedMatch != null) return maskedMatch;
                }

                var partial = investors.FirstOrDefault(i => i.FullName.Contains(h, StringComparison.OrdinalIgnoreCase));
                if (partial != null) return partial;
            }

            return null;
        }

        private static SchemeLookupRow? MatchScheme(string hint, List<SchemeLookupRow> schemes)
        {
            if (string.IsNullOrWhiteSpace(hint)) return null;
            var h = hint.Trim();

            var byCode = schemes.FirstOrDefault(s =>
                string.Equals(s.SchemeCode, h, StringComparison.OrdinalIgnoreCase));
            if (byCode != null) return byCode;

            var byExact = schemes.FirstOrDefault(s =>
                string.Equals(s.SchemeName, h, StringComparison.OrdinalIgnoreCase));
            if (byExact != null) return byExact;

            // Token-overlap fuzzy match (handles e.g. "Motilal Oswal Gold and Silver Passive Fund of Funds - Regular Growth" vs "...(Regular Plan)")
            var hintTokens = Tokenize(h);
            SchemeLookupRow? best = null;
            int bestScore = 0;

            foreach (var s in schemes)
            {
                var schTokens = Tokenize(s.SchemeName);
                int overlap = hintTokens.Count(t => schTokens.Contains(t));
                if (overlap > bestScore && overlap >= Math.Min(3, hintTokens.Count))
                {
                    bestScore = overlap;
                    best = s;
                }
            }

            return best;
        }

        private static HashSet<string> Tokenize(string text)
        {
            var cleaned = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9\s]", " ");
            var stop = new HashSet<string> { "plan", "option", "fund", "funds", "of", "and", "the" };
            return cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1 && !stop.Contains(t))
                .ToHashSet();
        }

        private static bool TryParseDate(string input, out DateTime date)
        {
            var formats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "dd-MMM-yyyy", "dd/MMM/yyyy", "d-M-yyyy", "d/M/yyyy" };
            return DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                   || DateTime.TryParse(input.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private class RawParsedRow
        {
            public int RowNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public string InvestorHint { get; set; } = string.Empty;
            public string SchemeHint { get; set; } = string.Empty;
            public string FolioNumber { get; set; } = string.Empty;
            public decimal InvestedAmount { get; set; }
            public decimal PurchaseNAV { get; set; }
            public decimal? UnitsAllotted { get; set; }
            public string PaymentMode { get; set; } = "Online";
        }

        private class InvestorLookupRow
        {
            public string Id { get; set; } = string.Empty;
            public string PanNumber { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FullName => $"{FirstName} {LastName}".Trim();
            public string PanOrId => !string.IsNullOrWhiteSpace(PanNumber) ? PanNumber : Id;
        }

        private class SchemeLookupRow
        {
            public string SchemeCode { get; set; } = string.Empty;
            public string SchemeName { get; set; } = string.Empty;
            public string FundName { get; set; } = string.Empty;
        }

        private class ExistingOrderKeyRow
        {
            public string OrderNumber { get; set; } = string.Empty;
            public string InvestorUserId { get; set; } = string.Empty;
            public string InvestorName { get; set; } = string.Empty;
            public string SchemeCode { get; set; } = string.Empty;
            public string? FolioNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal InvestedAmount { get; set; }
            public decimal? UnitsAllotted { get; set; }
        }

        private class NavLookupRow
        {
            public string SchemeCode { get; set; } = string.Empty;
            public DateTime NavDate { get; set; }
            public decimal NAV { get; set; }
        }
    }
}
