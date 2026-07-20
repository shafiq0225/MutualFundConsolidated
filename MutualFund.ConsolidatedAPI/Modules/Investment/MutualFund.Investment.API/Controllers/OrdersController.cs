using MutualFund.Investment.Application.Orders.Commands;
using MutualFund.Investment.Application.Orders.Dtos;
using MutualFund.Investment.Application.Orders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MutualFund.Investment.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class OrdersController : BaseController
    {
        private readonly CreateOrderCommand _createOrder;
        private readonly UpdateOrderStatusCommand _updateStatus;
        private readonly GetAllOrdersQuery _getAllOrders;
        private readonly GetOrderByIdQuery _getOrderById;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            CreateOrderCommand createOrder,
            UpdateOrderStatusCommand updateStatus,
            GetAllOrdersQuery getAllOrders,
            GetOrderByIdQuery getOrderById,
            ILogger<OrdersController> logger)
        {
            _createOrder = createOrder;
            _updateStatus = updateStatus;
            _getAllOrders = getAllOrders;
            _getOrderById = getOrderById;
            _logger = logger;
        }

        // ── GET /api/orders ────────────────────────────────────────
        /// <summary>
        /// Get all investment orders.
        /// Admin/Employee: all orders.
        /// User: own orders only.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? investorId = null)
        {
            string? filterByInvestor = null;

            // Non-admin/employee and no order.view permission: only see own orders
            if (!CanViewAllOrdersData)
                filterByInvestor = CurrentUserId;
            else if (!string.IsNullOrWhiteSpace(investorId))
                filterByInvestor = investorId;

            // ← investorIdFilter first, statusFilter second
            var result = await _getAllOrders.ExecuteAsync(
                filterByInvestor, status);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/orders/{id} ───────────────────────────────────
        /// <summary>
        /// Get single investment order by Id.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> GetById(int id)
        {
            // ← single int parameter
            var result = await _getOrderById.ExecuteAsync(id);

            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            // Non-admin users can only see own orders
            if (!IsAdmin && !IsEmployee &&
                result.Data!.InvestorUserId != CurrentUserId)
                return Forbid();

            return Ok(result.Data);
        }

        // ── POST /api/orders ───────────────────────────────────────
        /// <summary>
        /// Create new investment order. Admin only.
        /// Head of Family calls Admin → Admin fills this form.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "CanCreateOrder")]
        public async Task<IActionResult> Create(
            [FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ← 2 args: dto + createdByUserId
            var result = await _createOrder.ExecuteAsync(
                dto, CurrentUserId);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Data!.Id },
                result.Data);
        }

        // ── PUT /api/orders/{id}/status ────────────────────────────
        /// <summary>
        /// Update order status.
        /// Requested → Assigned → Submitted → Verified → Active
        /// Admin only.
        /// </summary>
        [HttpPut("{id:int}/status")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ← 3 args: orderId + dto + updatedByUserId
            var result = await _updateStatus.ExecuteAsync(
                id, dto, CurrentUserId);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/orders/investor/{userId} ──────────────────────
        /// <summary>
        /// Get all orders for a specific investor.
        /// Admin/Employee only.
        /// </summary>
        [HttpGet("investor/{userId}")]
        [Authorize(Policy = "CanViewAllOrders")]
        public async Task<IActionResult> GetByInvestor(string userId)
        {
            // ← investorIdFilter = userId, no status filter
            var result = await _getAllOrders.ExecuteAsync(userId, null);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}