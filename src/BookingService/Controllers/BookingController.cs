using BookingService.Data;
using BookingService.Entities;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using System.Text.Json; // Kell a JSON feldolgozáshoz

namespace BookingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly HttpClient _httpClient; // <--- Új mező
        private readonly IConfiguration _configuration; // Kell a címekhez

        public BookingController(
            BookingDbContext context, 
            IDistributedCache cache, 
            IPublishEndpoint publishEndpoint,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _cache = cache;
            _publishEndpoint = publishEndpoint;
            _httpClient = httpClientFactory.CreateClient(); // Kliens létrehozása
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            // 0. Esemény adatainak lekérése a CatalogService-től
            string eventName = "Ismeretlen Esemény";
            
            try 
            {
                // Dockerben a service neve: http://catalog-service:8080
                // Lokálisan (fejlesztéskor): http://localhost:5001 (ezt configból kéne, de most egyszerűsítünk)
                string catalogUrl = "http://catalog-service:8080/api/Events/" + booking.EventId;
                
                // Ha nem Dockerben vagyunk, akkor localhost (ez csak a biztonság kedvéért)
                // if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true") 
                //    catalogUrl = "http://localhost:5001/api/Events/" + booking.EventId;

                var response = await _httpClient.GetAsync(catalogUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // Feltételezzük, hogy a CatalogService egy JSON-t ad vissza, amiben van "name" mező
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("name", out var nameElement))
                    {
                        eventName = nameElement.GetString() ?? eventName;
                    }
                }
            }
            catch (Exception ex)
            {
                // Ha nem sikerül elérni a Catalogot, nem állunk meg, csak logolunk (vagy ignorálunk)
                Console.WriteLine($"Hiba az esemény lekérdezésekor: {ex.Message}");
            }

            // 1. Mentés adatbázisba
            booking.Id = Guid.NewGuid();
            booking.BookingDate = DateTime.UtcNow;
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 2. Üzenet küldése RabbitMQ-ra (Most már a VALÓS névvel!)
            await _publishEndpoint.Publish(new TicketPurchasedEvent
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                CustomerEmail = booking.CustomerEmail,
                EventName = eventName, // <--- ITT HASZNÁLJUK A LEKÉRT NEVET
                TicketCount = booking.TicketCount
            });

            return Ok(booking);
        }

        // GET: api/booking
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings()
        {
            return await _context.Bookings.ToListAsync();
        }
    }
}