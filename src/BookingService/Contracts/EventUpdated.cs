namespace Contracts // <--- NEM BookingService.Contracts, hanem csak Contracts
{
    public record EventUpdated
    {
        public Guid EventId { get; init; }
    }
}