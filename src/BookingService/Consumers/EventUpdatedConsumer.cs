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
            var cacheKey = $"event_stock_{eventId}";

            _logger.LogInformation($"[Cache Invalidation] Esemény változott (ID: {eventId}). Törlés a Redisből: {cacheKey}");
            await _cache.RemoveAsync(cacheKey);
        }
    }
}