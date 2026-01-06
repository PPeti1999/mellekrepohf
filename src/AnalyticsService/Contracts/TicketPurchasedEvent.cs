namespace Contracts
{
    // FONTOS: A névtérnek (Contracts) és a mezőknek egyeznie kell a BookingService-ével!
    public record TicketPurchasedEvent
    {
        public Guid BookingId { get; init; }
        public Guid EventId { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public string EventName { get; init; } = string.Empty;
        public int TicketCount { get; init; }
    }
}