namespace AnalyticsService.Contracts;

public record TicketPurchasedEvent(Guid EventId, string UserId, int SeatNumber);