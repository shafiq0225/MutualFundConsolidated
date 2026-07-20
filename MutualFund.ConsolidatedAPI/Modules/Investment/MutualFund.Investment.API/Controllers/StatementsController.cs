using MutualFund.Investment.Application.Statements.Commands;
using MutualFund.Investment.Application.Statements.Dtos;
using MutualFund.Investment.Application.Statements.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MutualFund.Investment.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class StatementsController : BaseController
    {
        private readonly UploadStatementCommand _uploadStatement;
        private readonly GetStatementsQuery _getStatements;
        private readonly DownloadStatementQuery _downloadStatement;
        private readonly ILogger<StatementsController> _logger;

        public StatementsController(
            UploadStatementCommand uploadStatement,
            GetStatementsQuery getStatements,
            DownloadStatementQuery downloadStatement,
            ILogger<StatementsController> logger)
        {
            _uploadStatement = uploadStatement;
            _getStatements = getStatements;
            _downloadStatement = downloadStatement;
            _logger = logger;
        }

        // ── GET /api/statements ────────────────────────────────────
        /// <summary>
        /// Get all statements.
        /// Admin: all statements.
        /// User: own statements only.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> GetAll()
        {
            if (CanViewAllOrdersData)
            {
                var result = await _getStatements.GetAllAsync();

                if (!result.IsSuccess)
                    return BadRequest(new { error = result.ErrorMessage });

                return Ok(result.Data);
            }
            else
            {
                // Regular user — own statements only
                var result = await _getStatements
                    .GetByInvestorAsync(CurrentUserId);

                if (!result.IsSuccess)
                    return BadRequest(new { error = result.ErrorMessage });

                return Ok(result.Data);
            }
        }

        // ── GET /api/statements/investor/{userId} ──────────────────
        /// <summary>
        /// Get statements for a specific investor.
        /// Admin/Employee only.
        /// </summary>
        [HttpGet("investor/{userId}")]
        [Authorize(Policy = "CanViewAllOrders")]
        public async Task<IActionResult> GetByInvestor(string userId)
        {
            var result = await _getStatements
                .GetByInvestorAsync(userId);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/statements/order/{orderId} ────────────────────
        /// <summary>
        /// Get statement for a specific order.
        /// </summary>
        [HttpGet("order/{orderId:int}")]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var result = await _getStatements.GetByOrderAsync(orderId);

            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ── GET /api/statements/{id}/view ──────────────────────────
        /// <summary>
        /// Stream PDF for in-browser viewing (read-only).
        /// Opens PDF in browser tab — no download.
        /// </summary>
        [HttpGet("{id:int}/view")]
        [Authorize(Policy = "CanViewOrders")]
        public async Task<IActionResult> ViewStatement(int id)
        {
            var result = await _downloadStatement.ExecuteAsync(id);

            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            var file = result.Data!;

            // inline = open in browser (not downloaded)
            Response.Headers.Append(
                "Content-Disposition",
                $"inline; filename=\"{file.FileName}\"");

            return File(file.FileStream, file.ContentType);
        }

        // ── POST /api/statements/upload ────────────────────────────
        /// <summary>
        /// Admin uploads PDF statement from MF company.
        /// One statement per investment order.
        /// </summary>
        [HttpPost("upload")]
        [Authorize(Policy = "AdminOnly")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
        public async Task<IActionResult> Upload(
            [FromForm] int orderId,
            [FromForm] DateTime statementDate,
            [FromForm] string? notes,
            IFormFile file)
        {
            // ── Validate file ──────────────────────────────────────
            if (file == null || file.Length == 0)
                return BadRequest(new
                {
                    error = "Please select a PDF file to upload."
                });

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
                return BadRequest(new
                {
                    error = "Only PDF files are allowed."
                });

            // ── Build DTO ──────────────────────────────────────────
            var dto = new UploadStatementDto
            {
                OrderId = orderId,
                StatementDate = statementDate,
                Notes = notes,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                FileStream = file.OpenReadStream()
            };

            // ── Execute ────────────────────────────────────────────
            var result = await _uploadStatement.ExecuteAsync(
                dto, CurrentUserId);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
    }
}