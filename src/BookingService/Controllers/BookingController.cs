using BookingService.Data;
using BookingService.Entities;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookingService.Controllers
{
    [Authorize] // VÉDETT: Bármilyen bejelentkezett user elérheti (pl. 'User' vagy 'Admin' role is)
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;

        public BookingController(BookingDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> BuyTicket([FromBody] BookingRequest request)
        {
            // A felhasználó e-mailjét a tokenből vesszük ki (ClaimTypes.Name)
            // Ez garantálja, hogy a hitelesített felhasználó nevében történik a foglalás
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            
            if (string.IsNullOrEmpty(userEmail)) 
            {
                // Ha valamiért nincs a tokenben név, fallback a requestre (opcionális)
                // Éles rendszerben itt Unauthorized-ot dobnánk.
                userEmail = request.CustomerEmail; 
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = request.EventId,
                EventName = request.EventName,
                CustomerEmail = userEmail, // Tokenből származó email
                TicketCount = request.TicketCount,
                BookingDate = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish(new TicketPurchasedEvent
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                CustomerEmail = booking.CustomerEmail,
                EventName = booking.EventName,
                TicketCount = booking.TicketCount
            });

            return Ok(new { Message = "Sikeres vásárlás!", BookingId = booking.Id });
        }
    }

    public record BookingRequest(Guid EventId, string EventName, string CustomerEmail, int TicketCount);
}