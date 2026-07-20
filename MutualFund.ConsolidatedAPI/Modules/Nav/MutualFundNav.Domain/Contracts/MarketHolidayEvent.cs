namespace MutualFundNav.Domain.Contracts
{
    public record MarketHolidayEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime HolidayDate { get; init; }
        public string Source { get; init; } = "MutualFundNav-API";
        public DateTime PublishedAt { get; init; } = DateTime.UtcNow;
    }
}
