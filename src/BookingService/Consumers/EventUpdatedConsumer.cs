using Contracts;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;

namespace BookingService.Consumers
{
    public class EventUpdatedConsumer : IConsumer<EventUpdated>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<EventUpdatedConsumer> _logger;

        public EventUpdatedConsumer(IDistributedCache cache, ILogger<EventUpdatedConsumer> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<EventUpdated> context)
        {
            var eventId = context.Message.EventId;

            // FONTOS: Ennek a kulcsnak PONTOSAN egyeznie kell azzal, 
            // amit a BookingController-ben használsz a mentésnél!
            // Ott ez volt: var cacheKey = $"event_{request.EventId}";
            var cacheKey = $"event_stock_{eventId}";

            _logger.LogInformation($"[Cache Invalidation] Esemény változott (ID: {eventId}). Törlés a Redisből: {cacheKey}");

            // ITT TÖRTÉNIK A VARÁZSLAT:
            await _cache.RemoveAsync(cacheKey);
        }
    }
}