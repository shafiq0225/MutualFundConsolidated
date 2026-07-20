using MutualFund.Investment.Domain.Entities;

namespace MutualFund.Investment.Application.Orders.Dtos
{
    public static class OrderMapper
    {
        public static InvestmentOrderDto ToDto(InvestmentOrder order)
        {
            return new InvestmentOrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,

                InvestorUserId = order.InvestorUserId,
                InvestorName = order.InvestorName,

                SchemeCode = order.SchemeCode,
                SchemeName = order.SchemeName,
                FundName = order.FundName,

                InvestedAmount = order.InvestedAmount,

                PaymentMode = order.PaymentMode.ToString(),
                ChequeNumber = order.ChequeNumber,
                ChequeDate = order.ChequeDate,
                BankName = order.BankName,
                TransactionRef = order.TransactionRef,

                OrderDate = order.OrderDate,
                AssignedDate = order.AssignedDate,
                AssignedStaffName = order.AssignedStaffName,
                SubmittedDate = order.SubmittedDate,
                VerifiedDate = order.VerifiedDate,
                ActivatedDate = order.ActivatedDate,

                Status = order.Status.ToString(),
                StatusCode = (int)order.Status,

                PurchaseNAV = order.PurchaseNAV,
                UnitsAllotted = order.UnitsAllotted,
                FolioNumber = order.FolioNumber,

                HasHolding = order.Holding != null,
                HasStatement = order.Statement != null,

                Notes = order.Notes,
                CreatedByUserId = order.CreatedByUserId,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }

        public static IEnumerable<InvestmentOrderDto> ToDtoList(
            IEnumerable<InvestmentOrder> orders) =>
            orders.Select(ToDto);
    }
}