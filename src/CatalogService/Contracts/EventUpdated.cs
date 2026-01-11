namespace Contracts // <--- NEM CatalogService.Contracts, hanem csak Contracts
{
    public record EventUpdated
    {
        public Guid EventId { get; init; }
    }
}