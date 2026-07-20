using MutualFund.Investment.Application.Orders.Dtos;
using MutualFund.Investment.Domain.Common;
using MutualFund.Investment.Domain.Entities;
using MutualFund.Investment.Domain.Enums;
using MutualFund.Investment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MutualFund.Investment.Application.Orders.Commands
{
    public class UpdateOrderStatusCommand
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateOrderStatusCommand> _logger;

        public UpdateOrderStatusCommand(
            IUnitOfWork unitOfWork,
            ILogger<UpdateOrderStatusCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<InvestmentOrderDto>> ExecuteAsync(
            int orderId,
            UpdateOrderStatusDto dto,
            string updatedByUserId)
        {
            try
            {
                // ── Find order ─────────────────────────────────────
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order == null)
                    return Result<InvestmentOrderDto>
                        .Failure($"Order with Id {orderId} not found.");

                // ── Parse new status ───────────────────────────────
                // "Active" is deliberately not a valid caller-supplied
                // target — it only happens as an automatic cascade from
                // Verified (see below), matching the design's single
                // "Verify & complete" action with no separate step.
                if (!Enum.TryParse<OrderStatus>(
                        dto.NewStatus, true, out var newStatus)
                    || newStatus == OrderStatus.Active)
                {
                    return Result<InvestmentOrderDto>
                        .Failure($"Invalid status: {dto.NewStatus}. " +
                                 "Valid: Assigned, Submitted, Verified, Cancelled");
                }

                // ── Validate transition ────────────────────────────
                var transitionResult = ValidateTransition(
                    order.Status, newStatus);

                if (!transitionResult.IsSuccess)
                    return Result<InvestmentOrderDto>
                        .Failure(transitionResult.ErrorMessage!);

                _logger.LogInformation(
                    "Order {OrderNumber}: {From} → {To}",
                    order.OrderNumber, order.Status, newStatus);

                // ── Apply status-specific changes ──────────────────
                switch (newStatus)
                {
                    case OrderStatus.Assigned:
                        ApplyAssigned(order, dto);
                        break;

                    case OrderStatus.Submitted:
                        ApplySubmitted(order, dto, updatedByUserId);
                        break;

                    case OrderStatus.Verified:
                        var verifyResult = await ApplyVerifiedAndCascadeActive(
                            order, dto, updatedByUserId);
                        if (!verifyResult.IsSuccess)
                            return Result<InvestmentOrderDto>
                                .Failure(verifyResult.ErrorMessage!);
                        // ApplyVerifiedAndCascadeActive sets the final
                        // status itself (Active), so skip the generic
                        // assignment below for this case.
                        order.UpdatedAt = DateTime.UtcNow;
                        if (!string.IsNullOrWhiteSpace(dto.Notes))
                            order.Notes = dto.Notes;
                        await _unitOfWork.Orders.UpdateAsync(order);
                        await _unitOfWork.CompleteAsync();
                        return Result<InvestmentOrderDto>
                            .Success(OrderMapper.ToDto(order));

                    case OrderStatus.Cancelled:
                        ApplyCancelled(order, dto);
                        break;
                }

                // ── Update status (non-Verified cases) ─────────────
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(dto.Notes))
                    order.Notes = dto.Notes;

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation(
                    "✅ Order {OrderNumber} updated to {Status}",
                    order.OrderNumber, newStatus);

                return Result<InvestmentOrderDto>
                    .Success(OrderMapper.ToDto(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating order {OrderId} status", orderId);
                return Result<InvestmentOrderDto>
                    .Failure($"Failed to update order: {ex.Message}");
            }
        }

        // ── Status Transition Validation ───────────────────────────
        private static Domain.Common.Result ValidateTransition(
            OrderStatus current, OrderStatus next)
        {
            var allowed = new Dictionary<OrderStatus, OrderStatus[]>
            {
                { OrderStatus.Requested, new[] { OrderStatus.Assigned,
                                                 OrderStatus.Cancelled } },
                { OrderStatus.Assigned,  new[] { OrderStatus.Submitted,
                                                 OrderStatus.Cancelled } },
                { OrderStatus.Submitted, new[] { OrderStatus.Verified } },
                { OrderStatus.Verified,  Array.Empty<OrderStatus>() },
                // ↑ Verified is momentary — ApplyVerifiedAndCascadeActive
                // immediately advances to Active in the same call, so no
                // order should ever be found sitting at Verified.
                { OrderStatus.Active,    Array.Empty<OrderStatus>() },
                { OrderStatus.Cancelled, Array.Empty<OrderStatus>() }
            };

            if (!allowed[current].Contains(next))
                return Domain.Common.Result.Failure(
                    $"Cannot move order from '{current}' to '{next}'. " +
                    $"Allowed transitions from '{current}': " +
                    $"{string.Join(", ", allowed[current])}");

            return Domain.Common.Result.Success();
        }

        // ── Assigned ────────────────────────────────────────────────
        private static void ApplyAssigned(
            InvestmentOrder order, UpdateOrderStatusDto dto)
        {
            order.AssignedDate = dto.AssignedDate ?? DateTime.Today;
            order.AssignedStaffName = dto.AssignedStaffName;
            order.Status = OrderStatus.Assigned;
        }

        // ── Submitted ───────────────────────────────────────────────
        private static void ApplySubmitted(
            InvestmentOrder order,
            UpdateOrderStatusDto dto,
            string userId)
        {
            order.SubmittedDate = dto.SubmittedDate ?? DateTime.Today;
            order.SubmittedByUserId = dto.SubmittedByUserId ?? userId;

            // Reference maps to whichever field matches the order's
            // existing payment mode — Cheque uses ChequeNumber,
            // NEFT/RTGS/IMPS use TransactionRef. Set at creation time
            // the mode is already known, so no re-parsing needed here.
            if (!string.IsNullOrWhiteSpace(dto.Reference))
            {
                if (order.PaymentMode == PaymentMode.Cheque)
                    order.ChequeNumber = dto.Reference;
                else
                    order.TransactionRef = dto.Reference;
            }

            order.Status = OrderStatus.Submitted;
        }

        // ── Verified → cascades immediately to Active ───────────────
        private async Task<Domain.Common.Result> ApplyVerifiedAndCascadeActive(
            InvestmentOrder order,
            UpdateOrderStatusDto dto,
            string userId)
        {
            // PurchaseNAV/FolioNumber/UnitsAllotted were already set at
            // order creation per the new design — just sanity-check
            // they're present before activating.
            if (order.PurchaseNAV is null or <= 0)
                return Domain.Common.Result.Failure(
                    "Order is missing a valid Purchase NAV — cannot verify.");

            if (string.IsNullOrWhiteSpace(order.FolioNumber))
                return Domain.Common.Result.Failure(
                    "Order is missing a Folio number — cannot verify.");

            order.VerifiedDate = dto.VerifiedDate ?? DateTime.Today;
            order.VerifiedByUserId = dto.VerifiedByUserId ?? userId;

            _logger.LogInformation(
                "Order {OrderNumber} verified — cascading to Active",
                order.OrderNumber);

            // ── Cascade: create the Holding + mark Active ──────────
            var holdingExists = await _unitOfWork.Holdings
                .ExistsForOrderAsync(order.Id);

            if (!holdingExists)
            {
                var holding = new Holding
                {
                    OrderId = order.Id,
                    InvestorUserId = order.InvestorUserId,
                    InvestorName = order.InvestorName,
                    SchemeCode = order.SchemeCode,
                    SchemeName = order.SchemeName,
                    FundName = order.FundName,
                    FolioNumber = order.FolioNumber!,
                    PurchaseDate = order.OrderDate,
                    PurchaseNAV = order.PurchaseNAV!.Value,
                    InvestedAmount = order.InvestedAmount,
                    Units = order.UnitsAllotted!.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Holdings.AddAsync(holding);

                _logger.LogInformation(
                    "✅ Holding created for order {OrderNumber}: " +
                    "{Units} units of {Scheme}",
                    order.OrderNumber,
                    holding.Units,
                    holding.SchemeName);
            }

            order.ActivatedDate = DateTime.Today;
            order.Status = OrderStatus.Active;

            return Domain.Common.Result.Success();
        }

        // ── Cancelled ────────────────────────────────────────────────
        private void ApplyCancelled(
            InvestmentOrder order,
            UpdateOrderStatusDto dto)
        {
            order.Status = OrderStatus.Cancelled;
            _logger.LogWarning(
                "Order {OrderNumber} cancelled. Reason: {Notes}",
                order.OrderNumber, dto.Notes);
        }
    }
}
