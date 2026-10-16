using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/events/{eventId}/categories - public
        [HttpGet("events/{eventId}/categories")]
        public async Task<ActionResult<IEnumerable<object>>> GetCategoriesForEvent(int eventId)
        {
            bool eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
            if (!eventExists)
            {
                return NotFound(new { message = "Event not found." });
            }

            var categories = await _context.Categories
                .Where(c => c.EventId == eventId)
                .Select(c => new
                {
                    c.CategoryId,
                    c.EventId,
                    c.Name,
                    c.Distance,
                    c.EntryFee
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET /api/categories/{id} - public
        [HttpGet("categories/{id}")]
        public async Task<ActionResult<object>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            return Ok(new
            {
                category.CategoryId,
                category.EventId,
                category.Name,
                category.Distance,
                category.EntryFee
            });
        }

        // POST /api/events/{eventId}/categories - Organiser owner only
        [HttpPost("events/{eventId}/categories")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<object>> CreateCategory(
            int eventId,
            [FromQuery(Name = "name")] string name,
            [FromQuery(Name = "distance")] decimal distance,
            [FromQuery(Name = "entry_fee")] decimal entryFee)
        {
            var organiserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null || !int.TryParse(organiserIdClaim, out int organiserId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in organiser." });
            }

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            if (ev.OrganiserId != organiserId)
            {
                return Forbid();
            }

            var category = new Category
            {
                EventId = eventId,
                Name = name,
                Distance = distance,
                EntryFee = entryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Created($"/api/categories/{category.CategoryId}", new
            {
                category.CategoryId,
                category.EventId,
                category.Name,
                category.Distance,
                category.EntryFee
            });
        }

        // PUT /api/categories/{id} - Organiser owner of parent event only
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            [FromQuery(Name = "name")] string name,
            [FromQuery(Name = "distance")] decimal distance,
            [FromQuery(Name = "entry_fee")] decimal entryFee)
        {
            var organiserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null || !int.TryParse(organiserIdClaim, out int organiserId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in organiser." });
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.Event == null || category.Event.OrganiserId != organiserId)
            {
                return Forbid();
            }

            category.Name = name;
            category.Distance = distance;
            category.EntryFee = entryFee;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated." });
        }

        // DELETE /api/categories/{id} - Organiser owner of parent event only
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var organiserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (organiserIdClaim == null || !int.TryParse(organiserIdClaim, out int organiserId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in organiser." });
            }

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            if (category.Event == null || category.Event.OrganiserId != organiserId)
            {
                return Forbid();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}