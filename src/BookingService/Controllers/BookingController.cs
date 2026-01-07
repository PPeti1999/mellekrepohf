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

namespace BookingService.Controllers
{
    [Authorize] // VÉDETT: Bármilyen bejelentkezett user elérheti (pl. 'User' vagy 'Admin' role is)
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
         private readonly BookingDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly HttpClient _httpClient;
        private readonly ICatalogClient _catalogClient; // <--- Itt az új kliens
        
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
            int? currentStock = null;
            string eventName = "Ismeretlen esemény";

            // --- 1. LÉPÉS: REDIS (Gyorsítótár) ---
            var cachedStockString = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedStockString))
            {
                currentStock = int.Parse(cachedStockString);
                // Opcionális: Ha a név is cache-elve van, azt is kivehetnéd, de most egyszerűsítünk
            }
           else
            {
                // --- 2. LÉPÉS: HTTP FALLBACK (Ha nincs Redisben) ---
                // Itt használjuk az új CatalogClient-et, ami Polly-val védett!
                var eventDto = await _catalogClient.GetEventAsync(request.EventId);

                if (eventDto != null)
                {
                    currentStock = eventDto.AvailableTickets;
                    eventName = eventDto.Name;

                    // --- 3. LÉPÉS: CACHE FELTÖLTÉS (Öngyógyítás) ---
                    // Elmentjük 10 percre, hogy legközelebb gyors legyen
                    await _cache.SetStringAsync(cacheKey, currentStock.ToString(), 
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
                }
            }

            // --- 4. LÉPÉS: VALIDÁCIÓ ---
            if (currentStock == null)
            {
                return BadRequest("A készletinformáció átmenetileg nem elérhető (Catalog is áll és Cache is üres).");
            }

            if (currentStock < request.TicketCount)
            {
                return BadRequest($"Nincs elegendő jegy! Készlet: {currentStock}, Kért: {request.TicketCount}");
            }

            // --- 5. LÉPÉS: MENTÉS + ÜZENETKÜLDÉS (Tranzakcióban!) ---
            // A MassTransit Outbox miatt a Publish és a SaveChanges egy tranzakció lesz!
            
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                BookingDate = DateTime.UtcNow,
                EventId = request.EventId,
                EventName = eventName, // Ezt most vagy a DTO-ból vagy requestből vesszük
                CustomerEmail = userEmail,
                TicketCount = request.TicketCount,
            };

            _context.Bookings.Add(booking);

            // Ez az üzenet először a DB-be kerül, és csak sikeres commit után megy a RabbitMQ-ra
            await _publishEndpoint.Publish(new TicketPurchasedEvent
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                CustomerEmail = booking.CustomerEmail,
                EventName = booking.EventName,
                TicketCount = booking.TicketCount
            });

          await _context.SaveChangesAsync();

            // --- 6. LÉPÉS: OPTIMISTA CACHE FRISSÍTÉS ---
            // Csökkentjük a készletet a cache-ben, hogy a következő user már a kevesebbet lássa
            var newStock = currentStock.Value - request.TicketCount;
            await _cache.SetStringAsync(cacheKey, newStock.ToString(), 
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



         
    

    public record BookingRequest(Guid EventId, string EventName, string CustomerEmail, int TicketCount);
}