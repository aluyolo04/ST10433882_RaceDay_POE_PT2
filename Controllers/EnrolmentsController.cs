using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Enrols the logged-in Participant into a category. Records the participant, event and category.
    /// </summary>
    /// <response code="201">Enrolment created.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("categories/{id}/enrol")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        var userId = User.GetUserId();
        var participant = await _db.Users.FindAsync(userId);
        if (participant == null)
            return Unauthorized();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id && e.ParticipantId == userId))
            return Conflict(new { message = "You are already enrolled in this category." });

        var enrolled = await _db.Enrolments.CountAsync(e => e.CategoryId == id);
        if (enrolled >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment
        {
            CategoryId = id,
            ParticipantId = userId,
            Category = category,
            Participant = participant
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(enrolment));
    }

    /// <summary>
    /// Lists all enrolments for a user. Users see their own; Admins can see anyone's.
    /// </summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Trying to view someone else's enrolments.</response>
    /// <response code="404">User does not exist.</response>
    [HttpGet("users/{id}/enrolments")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForUser(int id)
    {
        if (User.GetUserId() != id && !User.IsInRole("Admin"))
            return Forbid();

        if (!await _db.Users.AnyAsync(u => u.UserId == id))
            return NotFound(new { message = "User not found." });

        var enrolments = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.ParticipantId == id)
            .OrderBy(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse));
    }

    /// <summary>
    /// Lists everyone enrolled in a category. Only the Organiser who owns the event can do this.
    /// </summary>
    /// <response code="200">List of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpGet("categories/{id}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetUserId())
            return Forbid();

        var enrolments = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Where(e => e.CategoryId == id)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse));
    }

    /// <summary>
    /// Cancels an enrolment. The Participant who made it, or the Organiser of the event, can do this.
    /// </summary>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the participant or the event's organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    [HttpDelete("enrolments/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        var userId = User.GetUserId();
        var isParticipant = enrolment.ParticipantId == userId;
        var isOrganiser = enrolment.Category.Event.OrganiserId == userId;

        if (!isParticipant && !isOrganiser)
            return Forbid();

        if (enrolment.Result != null)
            _db.Results.Remove(enrolment.Result);

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static EnrolmentResponse ToResponse(Enrolment enrolment)
    {
        return new EnrolmentResponse
        {
            EnrolmentId = enrolment.EnrolmentId,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = enrolment.Participant.FullName,
            EventId = enrolment.Category.EventId,
            EventName = enrolment.Category.Event.EventName,
            EventDate = enrolment.Category.Event.EventDate,
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category.CategoryName,
            EnrolmentDate = enrolment.EnrolmentDate,
            Status = enrolment.Status
        };
    }
}