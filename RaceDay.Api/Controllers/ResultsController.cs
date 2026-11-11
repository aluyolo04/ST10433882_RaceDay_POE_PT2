
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db)
    {
        _db = db;
    }

    // Organiser captures or updates a result.
    [HttpPut("enrolments/{enrolmentId}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> SaveResult(
        int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category)
            .ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e =>
                e.EnrolmentId == enrolmentId);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Category.Event.OrganiserId != User.GetUserId())
            return Forbid();

        var allowed = new[] { "Finished", "DNF", "DNS", "DSQ" };

        if (!allowed.Contains(request.Status))
            return BadRequest(new { message = "Invalid result status." });

        TimeSpan? finishTime = null;

        if (request.Status == "Finished")
        {
            if (string.IsNullOrWhiteSpace(request.FinishTime) ||
                !TimeSpan.TryParseExact(
                    request.FinishTime,
                    @"hh\:mm\:ss",
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed) ||
                parsed <= TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Enter a valid finish time in HH:mm:ss format."
                });
            }

            if (!request.Position.HasValue)
                return BadRequest(new
                {
                    message = "A finishing position is required."
                });

            if (request.Position.Value < 1)
                return BadRequest(new
                {
                    message = "Position must be greater than zero."
                });

            finishTime = parsed;
        }

        var result = await _db.Results
            .FirstOrDefaultAsync(r =>
                r.EnrolmentId == enrolmentId);

        if (result == null)
        {
            result = new Result
            {
                EnrolmentId = enrolmentId
            };

            _db.Results.Add(result);
        }

        result.Status = request.Status;
        result.FinishTime = finishTime;
        result.Position = request.Status == "Finished"
            ? request.Position
            : null;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Result saved successfully.",
            result.ResultId,
            result.EnrolmentId,
            result.Status
        });
    }

    // Participant views their own results.
    [HttpGet("mine")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults()
    {
        int userId = User.GetUserId();

        var results = await _db.Results
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Category)
            .ThenInclude(c => c.Event)
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Participant)
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r =>
                r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        return Ok(results.Select(ToResponse));
    }

    // Organiser views results for a category they own.
    [HttpGet("categories/{categoryId}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CategoryResults(int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c =>
                c.CategoryId == categoryId);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != User.GetUserId())
            return Forbid();

        var results = await _db.Results
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Category)
            .ThenInclude(c => c.Event)
            .Include(r => r.Enrolment)
            .ThenInclude(e => e.Participant)
            .Where(r => r.Enrolment.CategoryId == categoryId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(ToResponse));
    }

    private static ResultResponse ToResponse(Result r)
    {
        return new ResultResponse
        {
            ResultId = r.ResultId,
            EnrolmentId = r.EnrolmentId,
            ParticipantId = r.Enrolment.ParticipantId,
            ParticipantName = r.Enrolment.Participant.FullName,
            EventId = r.Enrolment.Category.EventId,
            EventName = r.Enrolment.Category.Event.EventName,
            CategoryId = r.Enrolment.CategoryId,
            CategoryName = r.Enrolment.Category.CategoryName,
            FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss"),
            Position = r.Position,
            Status = r.Status
        };
    }
}
