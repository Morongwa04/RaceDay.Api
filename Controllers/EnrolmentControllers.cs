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
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST /api/enrolments - Participant only
        [HttpPost("enrolments")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<object>> CreateEnrolment(
            [FromQuery(Name = "category_id")] int categoryId)
        {
            var participantIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (participantIdClaim == null || !int.TryParse(participantIdClaim, out int participantId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in participant." });
            }

            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound(new { message = "Category not found." });
            }

            bool alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantId == participantId &&
                e.CategoryId == categoryId &&
                e.Status == "Confirmed");

            if (alreadyEnrolled)
            {
                return Conflict(new { message = "You are already enrolled in this category." });
            }

            var enrolment = new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentId }, new
            {
                enrolment.EnrolmentId,
                enrolment.ParticipantId,
                enrolment.CategoryId,
                enrolment.EnrolmentDate,
                enrolment.Status
            });
        }

        // GET /api/enrolments/my - Participant only, their own enrolments
        [HttpGet("enrolments/my")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyEnrolments()
        {
            var participantIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (participantIdClaim == null || !int.TryParse(participantIdClaim, out int participantId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in participant." });
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == participantId)
                .Include(e => e.Category)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.CategoryId,
                    CategoryName = e.Category!.Name,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET /api/events/{eventId}/enrolments - Organiser only, must own the event
        [HttpGet("events/{eventId}/enrolments")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<IEnumerable<object>>> GetEnrolmentsForEvent(int eventId)
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

            var enrolments = await _context.Enrolments
                .Where(e => e.Category!.EventId == eventId)
                .Include(e => e.Category)
                .Include(e => e.Participant)
                .Select(e => new
                {
                    e.EnrolmentId,
                    ParticipantName = e.Participant!.FirstName + " " + e.Participant.Surname,
                    CategoryName = e.Category!.Name,
                    e.EnrolmentDate,
                    e.Status
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET /api/enrolments/{id} - owner participant or the organiser of that event
        [HttpGet("enrolments/{id}")]
        [Authorize]
        public async Task<ActionResult<object>> GetEnrolment(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in user." });
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            bool isOwner = enrolment.ParticipantId == userId;
            bool isOrganiserOfEvent = role == "Organiser" &&
                enrolment.Category?.Event?.OrganiserId == userId;

            if (!isOwner && !isOrganiserOfEvent)
            {
                return Forbid();
            }

            return Ok(new
            {
                enrolment.EnrolmentId,
                enrolment.ParticipantId,
                enrolment.CategoryId,
                enrolment.EnrolmentDate,
                enrolment.Status
            });
        }

        // DELETE /api/enrolments/{id} - Participant only, must own the enrolment
        [HttpDelete("enrolments/{id}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var participantIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (participantIdClaim == null || !int.TryParse(participantIdClaim, out int participantId))
            {
                return Unauthorized(new { message = "Could not identify the logged-in participant." });
            }

            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null)
            {
                return NotFound(new { message = "Enrolment not found." });
            }

            if (enrolment.ParticipantId != participantId)
            {
                return Forbid();
            }

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrolment cancelled." });
        }
    }
}