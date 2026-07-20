using Microsoft.AspNetCore.Mvc;
using MutualFundNav.Domain.Interfaces;

namespace MutualFundNav.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class KafkaController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public KafkaController(IUnitOfWork uow) => _uow = uow;

        /// <summary>Returns the most recent Kafka publish logs.</summary>
        [HttpGet("logs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50)
        {
            var logs = await _uow.KafkaPublishLogs.GetRecentAsync(Math.Min(count, 100));
            return Ok(logs.Select(l => new
            {
                l.Id,
                l.Topic,
                l.EventType,
                l.MessageKey,
                l.MessageSizeBytes,
                l.IsSuccess,
                l.ErrorMessage,
                publishedAt = l.PublishedAt.ToString("o"),
                elapsedMs = Math.Round(l.ElapsedMs, 2),
                l.TriggerSource,
                navDate = l.NavDate?.ToString("yyyy-MM-dd"),
                l.Partition,
                l.Offset,
                createdAt = l.CreatedAt.ToString("o"),
                updatedAt = l.UpdatedAt?.ToString("o")
            }));
        }

        /// <summary>Returns the latest Kafka publish log entry.</summary>
        [HttpGet("logs/latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestLog()
        {
            var log = await _uow.KafkaPublishLogs.GetLatestAsync();
            if (log is null) return NotFound(new { message = "No Kafka publish logs found" });

            return Ok(new
            {
                log.Id,
                log.Topic,
                log.EventType,
                log.MessageKey,
                log.MessageSizeBytes,
                log.IsSuccess,
                log.ErrorMessage,
                publishedAt = log.PublishedAt.ToString("o"),
                elapsedMs = Math.Round(log.ElapsedMs, 2),
                log.TriggerSource,
                navDate = log.NavDate?.ToString("yyyy-MM-dd"),
                log.Partition,
                log.Offset
            });
        }
    }
}
