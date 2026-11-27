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
        private readonly OrderCorrectionService? _correctionService;
        private readonly OrderSanityCheckService? _sanityCheckService;
        private readonly BulkOrderImportService? _bulkImportService;

        public OrdersController(
            CreateOrderCommand createOrder,
            UpdateOrderStatusCommand updateStatus,
            GetAllOrdersQuery getAllOrders,
            GetOrderByIdQuery getOrderById,
            ILogger<OrdersController> logger,
            OrderCorrectionService? correctionService = null,
            OrderSanityCheckService? sanityCheckService = null,
            BulkOrderImportService? bulkImportService = null)
        {
            _createOrder = createOrder;
            _updateStatus = updateStatus;
            _getAllOrders = getAllOrders;
            _getOrderById = getOrderById;
            _logger = logger;
            _correctionService = correctionService;
            _sanityCheckService = sanityCheckService;
            _bulkImportService = bulkImportService;
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

        // ── POST /api/orders/correction-impact ─────────────────────
        /// <summary>
        /// Preview dependent holdings, snapshots, and statements for selected orders.
        /// Admin only.
        /// </summary>
        [HttpPost("correction-impact")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetCorrectionImpact(
            [FromBody] OrderCorrectionImpactRequestDto dto)
        {
            if (_correctionService == null)
                return StatusCode(500, new { error = "OrderCorrectionService not configured." });

            var result = await _correctionService.GetImpactAsync(dto.OrderIds);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── PUT /api/orders/{id}/admin-edit ────────────────────────
        /// <summary>
        /// Edit an existing order (including scheme, investor, date, NAV, units, folio)
        /// and cascade updates to linked Holding and PortfolioSnapshots.
        /// Admin only.
        /// </summary>
        [HttpPut("{id:int}/admin-edit")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminEditOrder(
            int id,
            [FromBody] AdminEditOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_correctionService == null)
                return StatusCode(500, new { error = "OrderCorrectionService not configured." });

            var result = await _correctionService.EditOrderAsync(
                id, dto, CurrentUserId, CurrentUserName);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── POST /api/orders/admin-reverse ─────────────────────────
        /// <summary>
        /// Reverse/delete one or more orders and cascade-clean linked holdings,
        /// portfolio snapshots, and statements in a single transaction.
        /// Admin only.
        /// </summary>
        [HttpPost("admin-reverse")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AdminReverseOrders(
            [FromBody] AdminReverseOrdersDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_correctionService == null)
                return StatusCode(500, new { error = "OrderCorrectionService not configured." });

            var result = await _correctionService.ReverseOrdersAsync(
                dto, CurrentUserId, CurrentUserName);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/orders/correction-logs ────────────────────────
        /// <summary>
        /// Get Admin order correction &amp; reversal audit logs.
        /// Admin only.
        /// </summary>
        [HttpGet("correction-logs")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetCorrectionLogs([FromQuery] int limit = 100)
        {
            if (_correctionService == null)
                return StatusCode(500, new { error = "OrderCorrectionService not configured." });

            var result = await _correctionService.GetLogsAsync(limit);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/orders/sanity-audit ───────────────────────────
        /// <summary>
        /// Scan all orders against DetailedSchemes AMFI NAVs and unit math.
        /// </summary>
        [HttpGet("sanity-audit")]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> ScanOrdersSanity()
        {
            if (_sanityCheckService == null)
                return StatusCode(500, new { error = "OrderSanityCheckService not configured." });

            var result = await _sanityCheckService.ScanAllOrdersAsync();
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── POST /api/orders/sanity-check ──────────────────────────
        /// <summary>
        /// Verify a draft order's Purchase NAV and Units against AMFI historical NAV.
        /// </summary>
        [HttpPost("sanity-check")]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> CheckPreOrderSanity(
            [FromBody] PreOrderSanityCheckRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_sanityCheckService == null)
                return StatusCode(500, new { error = "OrderSanityCheckService not configured." });

            var result = await _sanityCheckService.CheckPreOrderAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── POST /api/orders/bulk-import/preview-cas ───────────────
        /// <summary>
        /// Parse and validate a CAMS/KFintech Detailed CAS PDF before importing.
        /// </summary>
        [HttpPost("bulk-import/preview-cas")]
        [Authorize(Policy = "CanCreateOrder")]
        public async Task<IActionResult> PreviewBulkCas(
            IFormFile file,
            [FromForm] string? password = null)
        {
            if (_bulkImportService == null)
                return StatusCode(500, new { error = "BulkOrderImportService not configured." });

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please upload a non-empty CAS PDF file." });

            using var stream = file.OpenReadStream();
            var result = await _bulkImportService.PreviewCasPdfAsync(stream, file.FileName, password);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── POST /api/orders/bulk-import/confirm ───────────────────
        /// <summary>
        /// Commit validated bulk import rows as Active orders, Holdings, and Snapshots.
        /// </summary>
        [HttpPost("bulk-import/confirm")]
        [Authorize(Policy = "CanCreateOrder")]
        public async Task<IActionResult> ConfirmBulkImport(
            [FromBody] BulkImportConfirmRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (_bulkImportService == null)
                return StatusCode(500, new { error = "BulkOrderImportService not configured." });

            var result = await _bulkImportService.ConfirmImportAsync(dto, CurrentUserId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}