namespace MutualFund.Scheme.Domain.Entities
{
    public class MarketHoliday
    {
        public int Id { get; set; }
        public DateTime HolidayDate { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
}