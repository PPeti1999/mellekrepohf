using Contracts;
using MassTransit;

namespace NotificationWorker
{
    public class NotificationConsumer : IConsumer<TicketPurchasedEvent>
    {
        private readonly ILogger<NotificationConsumer> _logger;

        public NotificationConsumer(ILogger<NotificationConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<TicketPurchasedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation($"[EMAIL SZIMULÁCIÓ] Küldés ide: {msg.CustomerEmail}. Üzenet: Sikeres foglalás {msg.TicketCount} db jegyre a '{msg.EventName}' eseményre.");
            return Task.CompletedTask;
        }
    }
}