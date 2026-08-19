using System.Text;
using Microsoft.Extensions.Logging;
using MutualFund.Investment.Application.Family.Queries;
using MutualFund.Investment.Application.Portfolio.Queries;
using MutualFund.Mcp.API.Services;

namespace MutualFund.Mcp.API.Tools
{
    public class MessagingMcpTools
    {
        private readonly FamilyPortfolioQuery _familyPortfolioQuery;
        private readonly GetAllHoldingsQuery _getAllHoldingsQuery;
        private readonly TelegramService _telegramService;
        private readonly WhatsAppService _whatsAppService;
        private readonly ILogger<MessagingMcpTools> _logger;

        public MessagingMcpTools(
            FamilyPortfolioQuery familyPortfolioQuery,
            GetAllHoldingsQuery getAllHoldingsQuery,
            TelegramService telegramService,
            WhatsAppService whatsAppService,
            ILogger<MessagingMcpTools> logger)
        {
            _familyPortfolioQuery = familyPortfolioQuery;
            _getAllHoldingsQuery = getAllHoldingsQuery;
            _telegramService = telegramService;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a mobile-optimized scheme-wise daily digest message formatted for Telegram and WhatsApp HTML.
        /// </summary>
        public async Task<string> GenerateSchemeWiseDailyDigestAsync()
        {
            _logger.LogInformation("Generating scheme-wise daily digest...");

            var overviewResult = await _familyPortfolioQuery.GetFamilyOverviewAsync();
            var holdingsResult = await _getAllHoldingsQuery.ExecuteAsync();

            var sb = new StringBuilder();

            decimal todayReturnPct = 0;
            if (overviewResult.IsSuccess && overviewResult.Data?.FamilyYesterdayReturn != null)
            {
                todayReturnPct = overviewResult.Data.FamilyYesterdayReturn.ReturnPercent;
            }

            // 1. Header
            sb.AppendLine("📊 <b>MUTUAL FUND DAILY DIGEST</b>");
            sb.AppendLine($"🗓️ <b>{DateTime.Now:ddd, dd MMM yyyy}</b> | AMFI Sync: ✅ <code>05:00 AM</code>");
            sb.AppendLine("\n───────────────────────");
            sb.AppendLine("💼 <b>PORTFOLIO SUMMARY</b>");
            sb.AppendLine("───────────────────────");

            if (overviewResult.IsSuccess && overviewResult.Data != null)
            {
                var p = overviewResult.Data;
                var isGain = p.IsFamilyGain;
                var pnlTag = isGain ? "🟩" : "🟥";
                var pnlStr = isGain ? $"+₹ {p.TotalFamilyGain:N0}" : $"-₹ {Math.Abs(p.TotalFamilyGain):N0}";
                var pnlPctStr = isGain ? $"+{p.TotalFamilyGainPercent:F1}%" : $"{p.TotalFamilyGainPercent:F1}%";

                sb.AppendLine($"💰 <b>Current Value:</b> <code>₹ {p.TotalFamilyCurrentValue:N0}</code>");
                sb.AppendLine($"💵 <b>Invested:</b> <code>₹ {p.TotalFamilyInvested:N0}</code>");
                sb.AppendLine($"💹 <b>Net Profit:</b> {pnlTag} <code>{pnlStr}</code> (<b>{pnlPctStr}</b>)");

                if (p.FamilyYesterdayReturn != null)
                {
                    var dayGain = p.FamilyYesterdayReturn.PeriodGainAmount;
                    var isDayGain = dayGain >= 0;
                    var dayTag = isDayGain ? "🟩" : "🟥";
                    var dayStr = isDayGain ? $"+₹ {dayGain:N2}" : $"-₹ {Math.Abs(dayGain):N2}";
                    var dayPctStr = isDayGain ? $"+{p.FamilyYesterdayReturn.ReturnPercent:F2}%" : $"{p.FamilyYesterdayReturn.ReturnPercent:F2}%";
                    
                    sb.AppendLine($"🚀 <b>Today P&L:</b> {dayTag} <code>{dayStr}</code> (<b>{dayPctStr}</b>)");
                }
            }

            // 2. Scheme-Wise Breakdown (Consolidated by SchemeCode)
            sb.AppendLine("\n───────────────────────");
            sb.AppendLine("📊 <b>SCHEME-WISE BREAKDOWN</b>");
            sb.AppendLine("───────────────────────\n");

            if (holdingsResult.IsSuccess && holdingsResult.Data != null)
            {
                var schemeGroups = holdingsResult.Data
                    .GroupBy(h => !string.IsNullOrWhiteSpace(h.SchemeCode) ? h.SchemeCode.Trim() : h.SchemeName.Trim())
                    .Select(g => new
                    {
                        SchemeCode = g.Key,
                        SchemeName = g.OrderByDescending(x => x.SchemeName.Length).First().SchemeName.Trim(),
                        TotalUnits = g.Sum(x => x.Units),
                        TotalInvested = g.Sum(x => x.InvestedAmount),
                        TotalCurrentValue = g.Sum(x => x.CurrentValue),
                        TotalProfitLoss = g.Sum(x => x.ProfitLoss),
                        LiveNAV = g.First().CurrentNAV,
                        AvgBuyNAV = g.Sum(x => x.Units) > 0 ? g.Sum(x => x.InvestedAmount) / g.Sum(x => x.Units) : 0
                    })
                    .OrderByDescending(x => x.TotalCurrentValue)
                    .ToList();

                int index = 1;
                foreach (var s in schemeGroups)
                {
                    var returnPercent = s.TotalInvested > 0 ? (s.TotalProfitLoss / s.TotalInvested) * 100 : 0;
                    var isGain = s.TotalProfitLoss >= 0;
                    
                    var schemeTag = isGain ? "🟩" : "🟥";
                    var pnlLabel = isGain ? "Net Profit" : "Net Loss";
                    var pnlIcon = isGain ? "💹" : "📉";
                    var pnlStr = isGain ? $"+₹ {s.TotalProfitLoss:N0}" : $"-₹ {Math.Abs(s.TotalProfitLoss):N0}";
                    var pnlPctStr = isGain ? $"+{returnPercent:F1}%" : $"{returnPercent:F1}%";

                    var todaySchemePnl = Math.Round(s.TotalCurrentValue * (todayReturnPct / 100m), 2);
                    var isTodayGain = todaySchemePnl >= 0;
                    var todayTag = isTodayGain ? "🟩" : "🟥";
                    var todayStr = isTodayGain ? $"+₹ {todaySchemePnl:N2}" : $"-₹ {Math.Abs(todaySchemePnl):N2}";
                    var todayPctStr = isTodayGain ? $"+{todayReturnPct:F2}%" : $"{todayReturnPct:F2}%";

                    var safeSchemeName = System.Net.WebUtility.HtmlEncode(s.SchemeName);

                    sb.AppendLine($"{schemeTag} <b>{index++}. {safeSchemeName}</b>");
                    sb.AppendLine($"💰 <b>Current Val:</b> <code>₹ {s.TotalCurrentValue:N0}</code> | 💵 <b>Invested:</b> <code>₹ {s.TotalInvested:N0}</code>");
                    sb.AppendLine($"{pnlIcon} <b>{pnlLabel}:</b> {schemeTag} <code>{pnlStr}</code> (<b>{pnlPctStr}</b>)");
                    sb.AppendLine($"🚀 <b>Today P&L:</b> {todayTag} <code>{todayStr}</code> (<b>{todayPctStr}</b>)");
                    sb.AppendLine($"📊 <code>{s.TotalUnits:N2}</code> units @ Live NAV <code>₹ {s.LiveNAV:F2}</code> (Avg Buy ₹{s.AvgBuyNAV:F2})\n");
                }
            }

            // 3. 5-Day Progressive Ledger
            if (overviewResult.IsSuccess && overviewResult.Data?.ProgressiveDailyReturns?.Any() == true)
            {
                sb.AppendLine("───────────────────────");
                sb.AppendLine("🗓️ <b>5-DAY PROGRESSIVE LEDGER</b>");
                sb.AppendLine("───────────────────────");

                foreach (var day in overviewResult.Data.ProgressiveDailyReturns)
                {
                    var isPos = day.PeriodGainAmount >= 0;
                    var icon = isPos ? "🟩" : "🟥";
                    var dayStr = isPos ? $"+₹ {day.PeriodGainAmount:N2}" : $"-₹ {Math.Abs(day.PeriodGainAmount):N2}";
                    sb.AppendLine($"• <b>{day.Label}</b> : {icon} <code>{dayStr}</code>");
                }
            }

            return sb.ToString();
        }

        public async Task<bool> SendDailyDigestToTelegramAsync()
        {
            var message = await GenerateSchemeWiseDailyDigestAsync();
            return await _telegramService.SendMessageAsync(message);
        }

        public async Task<bool> SendDailyDigestToWhatsAppAsync()
        {
            var message = await GenerateSchemeWiseDailyDigestAsync();
            return await _whatsAppService.SendMessageAsync(message);
        }
    }
}
