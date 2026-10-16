using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/events - public, lists all events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvents()
        {
            var events = await _context.Events
                .Select(e => new
                {
                    e.EventId,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.Distance,
                    EventType = e.EventType.ToString(),
                    e.OrganiserId
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET /api/events/{id} - public, single event with its categories
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(new
            {
                ev.EventId,
                ev.Name,
                ev.Description,
                ev.EventDate,
                ev.Location,
                ev.Distance,
                EventType = ev.EventType.ToString(),
                ev.OrganiserId,
                Categories = ev.Categories.Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    c.Distance,
                    c.EntryFee
                })
            });
        }

        // POST /api/events - Organiser only. Matches the lecturer's demo:
        // parameters come in as query string fields (event_name, description,
        // event_date, location, distance, event_type), not a JSON body.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<object>> CreateEvent(
            [FromQuery(Name = "event_name")] string eventName,
            [FromQuery(Name = "description")] string? description,
            [FromQuery(Name = "event_date")] DateTime eventDate,
            [FromQuery(Name = "location")] string location,
            [FromQuery(Name = "distance")] double distance,
            [FromQuery(Name = "event_type")] EventType eventType)
        {
            var organiserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null || !int.TryParse(organiserIdClaim, out int organiserId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in organiser." });
            }

            var newEvent = new Event
            {
                OrganiserId = organiserId,
                Name = eventName,
                Description = description,
                EventDate = eventDate,
                Location = location,
                Distance = distance,
                EventType = eventType
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = newEvent.EventId }, new
            {
                newEvent.EventId,
                newEvent.Name,
                newEvent.Description,
                newEvent.EventDate,
                newEvent.Location,
                newEvent.Distance,
                EventType = newEvent.EventType.ToString(),
                newEvent.OrganiserId
            });
        }

        // PUT /api/events/{id} - Organiser only, must own the event
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromQuery(Name = "event_name")] string eventName,
            [FromQuery(Name = "description")] string? description,
            [FromQuery(Name = "event_date")] DateTime eventDate,
            [FromQuery(Name = "location")] string location,
            [FromQuery(Name = "distance")] double distance,
            [FromQuery(Name = "event_type")] EventType eventType)
        {
            var organiserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null || !int.TryParse(organiserIdClaim, out int organiserId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in organiser." });
            }

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (existingEvent.OrganiserId != organiserId)
            {
                return Forbid();
            }

            existingEvent.Name = eventName;
            existingEvent.Description = description;
            existingEvent.EventDate = eventDate;
            existingEvent.Location = location;
            existingEvent.Distance = distance;
            existingEvent.EventType = eventType;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Event updated." });
        }

        // DELETE /api/events/{id} - Organiser only, must own the event
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organiserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null || !int.TryParse(organiserIdClaim, out int organiserId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in organiser." });
            }

            var existingEvent = await _context.Events.FindAsync(id);
            if (existingEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (existingEvent.OrganiserId != organiserId)
            {
                return Forbid();
            }

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
