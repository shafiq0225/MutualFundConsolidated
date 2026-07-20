using Microsoft.AspNetCore.Mvc;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class JobsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public JobsController(IUnitOfWork uow) => _uow = uow;

        /// <summary>Returns the last 10 job execution logs.</summary>
        [HttpGet("logs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 10)
        {
            var logs = await _uow.JobLogs.GetRecentAsync(Math.Min(count, 100));
            return Ok(logs.Select(l => new
            {
                l.Id,
                l.JobName,
                startedAt = l.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                completedAt = l.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                l.IsSuccess,
                l.ErrorMessage,
                elapsedSeconds = Math.Round(l.ElapsedSeconds, 2)
            }));
        }

        /// <summary>Returns the most recent job execution.</summary>
        [HttpGet("logs/latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestLog()
        {
            var log = await _uow.JobLogs.GetLatestAsync();
            if (log is null) return NotFound(new { message = "No job logs found" });

            return Ok(new
            {
                log.Id,
                log.JobName,
                startedAt = log.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                completedAt = log.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                log.IsSuccess,
                log.ErrorMessage,
                elapsedSeconds = Math.Round(log.ElapsedSeconds, 2)
            });
        }
    }
}
