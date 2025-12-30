using CatalogService.Data;
using CatalogService.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

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
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(Guid id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return NotFound();
            return evt;
        }

        // POST: api/events
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event evt)
        {
            evt.Id = Guid.NewGuid();
            evt.Date = DateTime.SpecifyKind(evt.Date, DateTimeKind.Utc);
            _context.Events.Add(evt);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEvent), new { id = evt.Id }, evt);
        }
    }
}