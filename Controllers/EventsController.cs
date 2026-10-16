using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists all events. Public. Use upcomingOnly=true to hide past events.
    /// </summary>
    /// <response code="200">List of events.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.Include(e => e.Organiser).AsQueryable();

        if (upcomingOnly)
            query = query.Where(e => e.EventDate >= DateTime.Today);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToResponse));
    }

    /// <summary>
    /// Gets the details of one event. Public.
    /// </summary>
    /// <response code="200">Event details.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        return Ok(ToResponse(ev));
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <remarks>eventType must be Run, Walk or Cycle.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (!TryGetEventType(request.EventType, out var eventType))
            return BadRequest(new { message = "eventType must be Run, Walk or Cycle." });

        var ev = new Event
        {
            OrganiserId = User.GetUserId(),
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value.Date,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = eventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToResponse(ev));
    }

    /// <summary>
    /// Updates an event. Only the Organiser who created it can do this.
    /// </summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetUserId())
            return Forbid();

        if (!TryGetEventType(request.EventType, out var eventType))
            return BadRequest(new { message = "eventType must be Run, Walk or Cycle." });

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value.Date;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = eventType;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>
    /// Deletes an event together with its categories, enrolments and results. Only the owning Organiser can do this.
    /// </summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
                    .ThenInclude(en => en.Result)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetUserId())
            return Forbid();

        foreach (var category in ev.Categories)
        {
            foreach (var enrolment in category.Enrolments)
            {
                if (enrolment.Result != null)
                    _db.Results.Remove(enrolment.Result);
            }
            _db.Enrolments.RemoveRange(category.Enrolments);
        }
        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static bool TryGetEventType(string value, out string eventType)
    {
        eventType = string.Empty;
        foreach (var allowed in new[] { "Run", "Walk", "Cycle" })
        {
            if (string.Equals(value.Trim(), allowed, StringComparison.OrdinalIgnoreCase))
            {
                eventType = allowed;
                return true;
            }
        }
        return false;
    }

    private static EventResponse ToResponse(Event ev)
    {
        return new EventResponse
        {
            EventId = ev.EventId,
            OrganiserId = ev.OrganiserId,
            OrganiserName = ev.Organiser.FullName,
            EventName = ev.EventName,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            CreatedAt = ev.CreatedAt
        };
    }
}