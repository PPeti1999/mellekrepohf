using CatalogService.Data;
using CatalogService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // <--- EZT NE FELEJTSD EL!

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly CatalogDbContext _context;

        public EventsController(CatalogDbContext context)
        {
            _context = context;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events.ToListAsync();
        }

        // GET: api/events/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(Guid id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return NotFound();
            return evt;
        }

        // POST: api/events
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

        // --- Meglévő esemény módosítása (PUT) ---
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, Event updatedEvent)
        {
            if (id != updatedEvent.Id)
            {
                return BadRequest("Az URL-ben lévő ID nem egyezik a küldött objektum ID-jával.");
            }

            // Mivel a FindAsync trackeli az entitást, és mi nem akarunk teljes update-et a kliens minden mezőjéből (vagy pont azt akarunk),
            // a legtisztább, ha lekérjük a meglévőt és frissítjük a mezőit.
            var existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            // Adatok frissítése
            existingEvent.Name = updatedEvent.Name;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.AvailableTickets = updatedEvent.AvailableTickets;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.Price = updatedEvent.Price;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
         // Segédfüggvény az UpdateEvent-hez
        private bool EventExists(Guid id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}