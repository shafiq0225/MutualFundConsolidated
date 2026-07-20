namespace MutualFundNav.Domain.Entities
{
    public class MarketHoliday : BaseEntity
    {
        public DateTime HolidayDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
