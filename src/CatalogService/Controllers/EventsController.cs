using CatalogService.Data;
using Event = CatalogService.Entities.Event;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MassTransit;
using Contracts; // <--- EZT NE FELEJTSD EL!

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
       private readonly CatalogDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint; 
        public EventsController(CatalogDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(Guid id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return NotFound();
            return evt;
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event evt)
        {
            evt.Id = Guid.NewGuid();
            evt.Date = DateTime.SpecifyKind(evt.Date, DateTimeKind.Utc);
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEvent), new { id = evt.Id }, evt);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, Event updatedEvent)
        {
            if (id != updatedEvent.Id)
            {
                return BadRequest("Az ID nem egyezik az URL-ben és a Body-ban.");
            }

            // db friss
            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound();
            }

          
            existingEvent.Name = updatedEvent.Name;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.Price = updatedEvent.Price;
            existingEvent.AvailableTickets = updatedEvent.AvailableTickets;

            try
            {
                await _context.SaveChangesAsync();

                //  szól  BookingServicenek, hogy törölje a cache
                await _publishEndpoint.Publish(new EventUpdated { EventId = id });
                
                Console.WriteLine($"[Catalog] EventUpdated üzenet elküldve. ID: {id}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Events.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return NoContent(); // Vagy Ok()
        }
         // Segédfüggvény az UpdateEvent-hez
        private bool EventExists(Guid id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}