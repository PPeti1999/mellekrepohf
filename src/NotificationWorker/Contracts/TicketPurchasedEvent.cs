namespace Contracts
{
    public record TicketPurchasedEvent
    {
        public Guid BookingId { get; init; }
        
        // EZT A SORT ADD HOZZÁ:
        public Guid EventId { get; init; } 
        
        public string CustomerEmail { get; init; } = string.Empty;
        public string EventName { get; init; } = string.Empty;
        public int TicketCount { get; init; }
    }
}