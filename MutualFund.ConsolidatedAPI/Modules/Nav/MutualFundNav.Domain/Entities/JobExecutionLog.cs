namespace MutualFundNav.Domain.Entities
{
    public class JobExecutionLog : BaseEntity
    {
        public string JobName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
        public double ElapsedSeconds { get; set; }
    }
}
