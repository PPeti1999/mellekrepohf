using BookingService.Data;
using BookingService.Entities;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore; 

namespace BookingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IPublishEndpoint _publishEndpoint;
        // private readonly ILogger<BookingController> _logger; 

        public BookingController(
            BookingDbContext context, 
            IDistributedCache cache, 
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _cache = cache;
            _publishEndpoint = publishEndpoint;
        }
// TODO: {NOERR} - Később implementálni: 
// Ha nincs a Redisben adat, kérjük le a CatalogService-től HTTP-n keresztül (Polly-val).
// Jelenleg: Fake fallback értékkel (100) dolgozunk.
        [HttpPost]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            // 1. Mentés adatbázisba
            booking.Id = Guid.NewGuid();
            booking.BookingDate = DateTime.UtcNow; // UTC idő!
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 2. Üzenet küldése RabbitMQ-ra
          await _publishEndpoint.Publish(new TicketPurchasedEvent
            {
                BookingId = booking.Id,
                EventId = booking.EventId, // <--- EZT A SORT ILLESD BE
                CustomerEmail = booking.CustomerEmail,
                EventName = "Példa Esemény", 
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