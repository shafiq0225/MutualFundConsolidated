using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MutualFund.Scheme.Application.DTOs;
using MutualFund.Scheme.Application.UseCases.Commands;
using MutualFund.Scheme.Application.UseCases.Queries;

namespace MutualFund.Scheme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Whole feature behind one blanket permission — Admin always has it;
    // an Employee needs scheme.manage granted via AuthAPI's
    // PermissionController. Includes the reads (GetAll/GetBySchemeCode/
    // GetApproved) too — "Scheme Enrollment" is an Admin/Employee-only
    // feature end to end, not visible to End Users at all (unlike NAV
    // Comparison / Scheme Details, which are AllRoles in NavComparisonController).
    [Authorize(Policy = "CanManageSchemeEnrollment")]
    public class SchemeEnrollmentController : ControllerBase
    {
        private readonly CreateSchemeEnrollmentCommand _createCommand;
        private readonly UpdateSchemeEnrollmentCommand _updateCommand;
        private readonly GetSchemeEnrollmentsQuery _query;

        public SchemeEnrollmentController(
            CreateSchemeEnrollmentCommand createCommand,
            UpdateSchemeEnrollmentCommand updateCommand,
            GetSchemeEnrollmentsQuery query)
        {
            _createCommand = createCommand;
            _updateCommand = updateCommand;
            _query = query;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _query.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{schemeCode}")]
        public async Task<IActionResult> GetBySchemeCode(string schemeCode)
        {
            var result = await _query.GetBySchemeCodeAsync(schemeCode);
            return Ok(result);
        }

        [HttpGet("approved")]
        public async Task<IActionResult> GetApproved()
        {
            var result = await _query.GetApprovedAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSchemeEnrollmentDto dto)
        {
            var result = await _createCommand.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetBySchemeCode),
                new { schemeCode = result.SchemeCode }, result);
        }

        [HttpPut("{schemeCode}")]
        public async Task<IActionResult> Update(string schemeCode,
            [FromBody] UpdateSchemeEnrollmentDto dto)
        {
            var result = await _updateCommand.ExecuteAsync(schemeCode, dto);
            return Ok(result);
        }
    }
}