namespace Contracts 
{
    public record TicketPurchasedEvent
    {
        public Guid BookingId { get; init; }
        public string CustomerEmail { get; init; } = string.Empty;
        public string EventName { get; init; } = string.Empty;
        public int TicketCount { get; init; }
    }
}