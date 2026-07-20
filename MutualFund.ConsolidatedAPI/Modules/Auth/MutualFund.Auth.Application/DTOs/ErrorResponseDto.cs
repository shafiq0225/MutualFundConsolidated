namespace MutualFund.Auth.Application.DTOs
{
    public class ErrorResponseDto
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; set; }
    }
}