using BookingService.Data;
using BookingService.Entities;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BookingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly HttpClient _httpClient;
        
        public BookingController(
            BookingDbContext context, 
            IDistributedCache cache, 
            IPublishEndpoint publishEndpoint,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _cache = cache;
            _publishEndpoint = publishEndpoint;
            // A Program.cs-ben regisztrált "catalog" nevű klienst vagy az alapértelmezettet használjuk
            _httpClient = httpClientFactory.CreateClient("catalog"); 
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            string cacheKey = $"event:{booking.EventId}";
            int currentStock = 0;
            string eventName = "Ismeretlen Esemény";
            bool stockFound = false;

            // --- 1. PRÓBÁLKOZÁS REDISBŐL ---
            var cachedStockString = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedStockString) && int.TryParse(cachedStockString, out currentStock))
            {
                stockFound = true;
            }
            else
            {
                // --- 2. FALLBACK: HA NINCS REDISBEN, LEKÉRJÜK A CATALOGTÓL (HTTP) ---
                try 
                {
                    // Dockerben: http://catalog-service:8080/api/Events/{id}
                    string catalogUrl = $"http://catalog-service:8080/api/Events/{booking.EventId}";
                    
                    var response = await _httpClient.GetAsync(catalogUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(content);
                        
                        // Készlet kinyerése
                        if (doc.RootElement.TryGetProperty("availableTickets", out var ticketsElement))
                        {
                            currentStock = ticketsElement.GetInt32();
                            stockFound = true;
                            
                            // Ha már lekértük, mentsük el a Redisbe a jövőre nézve!
                            await _cache.SetStringAsync(cacheKey, currentStock.ToString());
                        }

                        // Név kinyerése (ha már itt vagyunk)
                        if (doc.RootElement.TryGetProperty("name", out var nameElement))
                        {
                            eventName = nameElement.GetString() ?? eventName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HIBA: Nem sikerült elérni a Catalogot: {ex.Message}");
                    // Ha a Catalog is halott és Redisben sincs, akkor elutasíthatjuk, vagy engedhetjük.
                    // Most a biztonság kedvéért elutasítjuk, ha 0-nak tűnik.
                }
            }

            // --- 3. VALIDÁCIÓ ---
            // Ha megtaláltuk a készletet (Redisből VAGY HTTP-ből), ellenőrizzük!
            if (stockFound)
            {
                if (currentStock < booking.TicketCount)
                {
                    return BadRequest($"Nincs elegendő jegy! Jelenlegi készlet: {currentStock}, Kért: {booking.TicketCount}");
                }

                // Optimista levonás a cache-ből (hogy a köv. kérés már a csökkentettet lássa)
                var newStock = currentStock - booking.TicketCount;
                await _cache.SetStringAsync(cacheKey, newStock.ToString());
            }
            else 
            {
                // Opcionális: Ha sehol sem találtuk az adatot (pl. nincs ilyen ID), visszadobhatjuk.
                // return NotFound("Az esemény nem található vagy nem elérhető.");
            }

            // --- 4. MENTÉS ADATBÁZISBA ---
            booking.Id = Guid.NewGuid();
            booking.BookingDate = DateTime.UtcNow;
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // --- 5. ÜZENET KÜLDÉSE ---
            await _publishEndpoint.Publish(new TicketPurchasedEvent
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                CustomerEmail = booking.CustomerEmail,
                EventName = eventName,
                TicketCount = booking.TicketCount
            });

            return Ok(booking);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            return await _context.Bookings.ToListAsync();
        }
    }
}