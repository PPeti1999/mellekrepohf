using AnalyticsService.Entities;
using Contracts; // Feltételezve, hogy a TicketPurchasedEvent ebben a névtérben van
using MassTransit;
using MongoDB.Driver;

namespace AnalyticsService.Consumers
{
    public class TicketPurchasedConsumer : IConsumer<TicketPurchasedEvent>
    {
        private readonly IMongoCollection<SalesRecord> _collection;
        private readonly ILogger<TicketPurchasedConsumer> _logger;

        public TicketPurchasedConsumer(IMongoDatabase database, ILogger<TicketPurchasedConsumer> logger)
        {
            // A "Sales" kollekcióba fogunk írni
            _collection = database.GetCollection<SalesRecord>("Sales");
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TicketPurchasedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation($"[Analytics] Új eladás érkezett! Event: {msg.EventName}, Jegyek: {msg.TicketCount}");

            var record = new SalesRecord
            {
                Id = Guid.NewGuid(),
                BookingId = msg.BookingId,
                EventId = msg.EventId,
                EventName = msg.EventName,
                CustomerEmail = msg.CustomerEmail,
                TicketCount = msg.TicketCount,
                PurchaseDate = DateTime.UtcNow
            };

            await _collection.InsertOneAsync(record);
            
            _logger.LogInformation($"[Analytics] Eladás sikeresen mentve a MongoDB-be. ID: {record.Id}");
        }
    }
}