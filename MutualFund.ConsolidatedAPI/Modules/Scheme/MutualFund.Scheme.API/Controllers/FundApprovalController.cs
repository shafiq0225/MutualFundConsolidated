using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MutualFund.Scheme.Application.UseCases.Commands;
using MutualFund.Scheme.Domain.Exceptions;

namespace MutualFund.Scheme.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Fund approval is part of the same "Scheme Enrollment" feature bucket
    // (one blanket switch), not a separately-grantable permission.
    [Authorize(Policy = "CanManageSchemeEnrollment")]
    public class FundApprovalController : ControllerBase
    {
        private readonly UpdateFundApprovalCommand _command;

        public FundApprovalController(UpdateFundApprovalCommand command)
        {
            _command = command;
        }

        [HttpPut("{fundCode}")]
        public async Task<IActionResult> UpdateFundApproval(
            string fundCode, [FromQuery] bool isApproved)
        {
            if (string.IsNullOrWhiteSpace(fundCode))
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "fundCode", new[] { "FundCode is required." } }
                });

            var count = await _command.ExecuteAsync(fundCode, isApproved);

            return Ok(new
            {
                FundCode = fundCode,
                IsApproved = isApproved,
                SchemesAffected = count,
                Message = $"Successfully updated {count} scheme(s)"
            });
        }
    }
}