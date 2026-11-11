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
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists all categories offered under an event. Public.
    /// </summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{eventId}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                CategoryName = c.CategoryName,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee,
                MaxParticipants = c.MaxParticipants,
                EnrolmentCount = c.Enrolments.Count
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Adds a category (for example "10km" or "Under 20") to an event. Only the owning Organiser can do this.
    /// </summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed or the category name already exists for this event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("events/{eventId}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != User.GetUserId())
            return Forbid();

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name))
            return BadRequest(new { message = "This event already has a category with that name." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(category, 0));
    }

    /// <summary>
    /// Updates a category. Only the owning Organiser can do this.
    /// </summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed, name already used, or max participants is below the current enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPut("categories/{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetUserId())
            return Forbid();

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.CategoryName == name && c.CategoryId != id))
            return BadRequest(new { message = "This event already has a category with that name." });

        var enrolled = await _db.Enrolments.CountAsync(e => e.CategoryId == id);
        if (request.MaxParticipants < enrolled)
            return BadRequest(new { message = "Max participants cannot be lower than the number already enrolled." });

        category.CategoryName = name;
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(category, enrolled));
    }

    /// <summary>
    /// Removes a category and any enrolments and results linked to it. Only the owning Organiser can do this.
    /// </summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments).ThenInclude(e => e.Result)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetUserId())
            return Forbid();

        foreach (var enrolment in category.Enrolments)
        {
            if (enrolment.Result != null)
                _db.Results.Remove(enrolment.Result);
        }
        _db.Enrolments.RemoveRange(category.Enrolments);
        _db.Categories.Remove(category);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryResponse ToResponse(Category category, int enrolmentCount)
    {
        return new CategoryResponse
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants,
            EnrolmentCount = enrolmentCount
        };
    }
}