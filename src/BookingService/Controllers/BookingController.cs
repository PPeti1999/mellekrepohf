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
    // 1. DTO Létrehozása: Csak ezeket kérjük be a Swaggertől/Vásárlótól
    public record BookingRequest(Guid EventId, int TicketCount, string CustomerEmail);

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
            _httpClient = httpClientFactory.CreateClient("catalog"); 
        }

        [HttpPost]
        // 2. Itt már a DTO-t fogadjuk a teljes Entity helyett
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
        {
            // --- A "BEJELENTKEZÉS" SZIMULÁLÁSA ---
            // A request.CustomerEmail mezővel mondod meg, hogy épp ki vagy.
            
            string cacheKey = $"event:{request.EventId}";
            int currentStock = 0;
            string eventName = "Ismeretlen Esemény";
            bool stockFound = false;

            // --- 1. REDIS CHECK ---
            var cachedStockString = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedStockString) && int.TryParse(cachedStockString, out currentStock))
            {
                stockFound = true;
            }
            else
            {
                // --- 2. HTTP FALLBACK (Catalog) ---
                try 
                {
                    string catalogUrl = $"http://catalog-service:8080/api/Events/{request.EventId}";
                    var response = await _httpClient.GetAsync(catalogUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(content);
                        
                        if (doc.RootElement.TryGetProperty("availableTickets", out var ticketsElement))
                        {
                            currentStock = ticketsElement.GetInt32();
                            stockFound = true;
                            await _cache.SetStringAsync(cacheKey, currentStock.ToString());
                        }
                        if (doc.RootElement.TryGetProperty("name", out var nameElement))
                        {
                            eventName = nameElement.GetString() ?? eventName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HIBA: Nem sikerült elérni a Catalogot: {ex.Message}");
                }
            }

            // --- 3. VALIDÁCIÓ ---
            if (stockFound)
            {
                if (currentStock < request.TicketCount)
                {
                    return BadRequest($"Nincs elegendő jegy! Készlet: {currentStock}, Kért: {request.TicketCount}");
                }

                // Optimista cache frissítés
                var newStock = currentStock - request.TicketCount;
                await _cache.SetStringAsync(cacheKey, newStock.ToString());
            }

            // --- 4. MENTÉS (ENTITY LÉTREHOZÁSA) ---
            // Itt hozzuk létre a végleges adatbázis objektumot
            var booking = new Booking
            {
                Id = Guid.NewGuid(),              // Mi generáljuk
                BookingDate = DateTime.UtcNow,    // Mi generáljuk
                EventId = request.EventId,
                TicketCount = request.TicketCount,
                CustomerEmail = request.CustomerEmail // Ez a "User ID"
            };
            
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

            return Ok(new { Message = "Sikeres foglalás!", BookingId = booking.Id, User = booking.CustomerEmail });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            return await _context.Bookings.ToListAsync();
        }
    }
}