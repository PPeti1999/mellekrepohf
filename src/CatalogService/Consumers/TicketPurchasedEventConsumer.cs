using CatalogService.Data;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Consumers
{
    public class TicketPurchasedEventConsumer : IConsumer<TicketPurchasedEvent>
    {
        private readonly CatalogDbContext _context;
        private readonly ILogger<TicketPurchasedEventConsumer> _logger;

        public TicketPurchasedEventConsumer(CatalogDbContext context, ILogger<TicketPurchasedEventConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TicketPurchasedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation($"[Catalog] Jegyvásárlás történt! Esemény ID: {msg.EventId}, Darab: {msg.TicketCount}. Készlet frissítése...");

            // Megkeressük az eseményt
            var evt = await _context.Events.FindAsync(msg.EventId);

            if (evt != null)
            {
                // Levonjuk a jegyeket
                evt.AvailableTickets -= msg.TicketCount;
                
                // Biztonsági ellenőrzés, ne legyen negatív
                if (evt.AvailableTickets < 0) evt.AvailableTickets = 0;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"[Catalog] Készlet frissítve. Új készlet: {evt.AvailableTickets}");
            }
            else
            {
                _logger.LogError($"[Catalog] HIBA: Nem található esemény ezzel az ID-vel: {msg.EventId}");
            }
        }
    }
}