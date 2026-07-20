using MutualFund.Investment.Domain.Entities;

namespace MutualFund.Investment.Application.Portfolio.Dtos
{
    public static class PortfolioMapper
    {
        public static HoldingDto ToHoldingDto(
            Holding holding,
            PortfolioSnapshot? snapshot = null)
        {
            return new HoldingDto
            {
                Id = holding.Id,
                OrderId = holding.OrderId,
                OrderNumber = holding.Order?.OrderNumber ?? string.Empty,

                InvestorUserId = holding.InvestorUserId,
                InvestorName = holding.InvestorName,

                SchemeCode = holding.SchemeCode,
                SchemeName = holding.SchemeName,
                FundName = holding.FundName,
                FolioNumber = holding.FolioNumber,

                PurchaseDate = holding.PurchaseDate,
                PurchaseYear = holding.PurchaseDate.Year,
                PurchaseNAV = holding.PurchaseNAV,
                InvestedAmount = holding.InvestedAmount,
                Units = holding.Units,

                CurrentNAV = snapshot?.CurrentNAV ?? 0,
                CurrentValue = snapshot?.CurrentValue ?? 0,
                ProfitLoss = snapshot?.ProfitLoss ?? 0,
                ProfitLossPercent = snapshot?.ProfitLossPercent ?? 0,
                IsProfit = snapshot?.ProfitLoss >= 0,
                LastUpdatedDate = snapshot?.SnapshotDate,
                IsActive = holding.IsActive
            };
        }

        public static PortfolioRowDto ToRowDto(
            Holding holding,
            PortfolioSnapshot? snapshot)
        {
            var currentNAV = snapshot?.CurrentNAV ?? 0;
            var totalAmount = snapshot?.CurrentValue ?? 0;
            var profitLoss = snapshot?.ProfitLoss ?? 0;
            var percentage = snapshot?.ProfitLossPercent ?? 0;

            return new PortfolioRowDto
            {
                SchemeCode = holding.SchemeCode,
                SchemeName = holding.SchemeName,
                FundName = holding.FundName,
                FolioNumber = holding.FolioNumber,

                PurchaseDate = holding.PurchaseDate,
                PurchaseDateText = holding.PurchaseDate
                    .ToString("dd MMM yyyy"),
                Year = holding.PurchaseDate.Year,

                InvestedAmount = holding.InvestedAmount,
                PurchaseNAV = holding.PurchaseNAV,
                Units = holding.Units,

                CurrentNAV = currentNAV,
                TotalAmount = totalAmount,
                ProfitLoss = profitLoss,
                Percentage = percentage,
                IsProfit = profitLoss >= 0,

                SnapshotDate = snapshot?.SnapshotDate
            };
        }
    }
}