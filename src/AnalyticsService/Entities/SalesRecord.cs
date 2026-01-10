using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AnalyticsService.Entities
{
    public class SalesRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        public Guid BookingId { get; set; }
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int TicketCount { get; set; }
        
        public DateTime PurchaseDate { get; set; }
    }
}