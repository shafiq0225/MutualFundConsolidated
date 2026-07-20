namespace MutualFund.Scheme.Application.DTOs
{
    public class HolidayStatusDto
    {
        public bool IsHoliday { get; set; }
        public DateTime? HolidayDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}