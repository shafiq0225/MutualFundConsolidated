namespace MutualFund.Scheme.Domain.Contracts
{
    public record MarketHolidayEvent
    {
        public DateTime HolidayDate { get; init; }
        public string Source { get; init; } = "AMFINAV-App";
        public DateTime PublishedAt { get; init; }
    }
}