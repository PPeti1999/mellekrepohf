using BookingService.Data;
using BookingService.Entities;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using BookingService.Services;
using BookingService.Models;

namespace BookingService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
         private readonly BookingDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly HttpClient _httpClient;
        private readonly ICatalogClient _catalogClient; 
        
        public BookingController(
            BookingDbContext context,
            IDistributedCache cache,
            IPublishEndpoint publishEndpoint,
            IHttpClientFactory httpClientFactory,
            ICatalogClient catalogClient)
        {
            _context = context;
            _cache = cache;
            _catalogClient = catalogClient; ;
            _publishEndpoint = publishEndpoint;
            _httpClient = httpClientFactory.CreateClient("catalog");
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
        {
           // User email kinyerése
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value ?? request.CustomerEmail;
            string cacheKey = $"event_stock_{request.EventId}";
             CatalogEventDto? eventData = null;

            //  REDIS 
            var cachedDataString = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedDataString))
            {
              // adatcsekk
             eventData = JsonSerializer.Deserialize<CatalogEventDto>(cachedDataString);
            }
          // HTTP FALLBACK 
            if (eventData == null)
            {
                eventData = await _catalogClient.GetEventAsync(request.EventId);

                if (eventData != null)
                {
                    // Ha sikerült lekérni elmentjük  a Redisbe
                    var jsonOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(eventData), jsonOptions);
                }
            }
                // ha mindig semmi akk üres
            if (eventData == null)
            {
                return BadRequest("A készletinformáció átmenetileg nem elérhető.");
            }

            if (eventData.AvailableTickets < request.TicketCount)
            {
                return BadRequest($"Nincs elegendő jegy! Készlet: {eventData.AvailableTickets}");
            }

            // MENTÉS + ÜZENETKÜLDÉS 
            // A MassTransit Outbox miatt a Publish és a SaveChanges egy tranzakció lesz
            
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingDate = DateTime.UtcNow,
                EventId = request.EventId,
                EventName = eventData.Name, 
                CustomerEmail = userEmail,
                TicketCount = request.TicketCount,
            };

            _context.Bookings.Add(booking);

            //  üzenet először a DB-be kerül, és csak sikeres commit után megy a RabbitMQ-ra
            await _publishEndpoint.Publish(new TicketPurchasedEvent
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                CustomerEmail = booking.CustomerEmail,
                EventName = booking.EventName,
                TicketCount = booking.TicketCount
            });

          await _context.SaveChangesAsync();

            //chache frissítés
            eventData.AvailableTickets -= request.TicketCount;
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(eventData), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

            return Ok(new { Message = "Sikeres foglalás!", BookingId = booking.Id });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            return await _context.Bookings.ToListAsync();
           // return Ok(new { Message = "Sikeres vásárlás!", BookingId = booking.Id });
        }
    }



         
    

   public record BookingRequest(Guid EventId, string CustomerEmail, int TicketCount);
}