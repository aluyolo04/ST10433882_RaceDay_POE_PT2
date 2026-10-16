using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Gets a user's profile. Users can view their own profile; Admins can view any.
    /// </summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Trying to view someone else's profile.</response>
    /// <response code="404">User does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int id)
    {
        if (User.GetUserId() != id && !User.IsInRole("Admin"))
            return Forbid();

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(ToProfile(user));
    }

    /// <summary>
    /// Updates the logged-in user's own profile (full name and email).
    /// </summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Validation failed or email already in use.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Trying to update someone else's profile.</response>
    /// <response code="404">User does not exist.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(int id, UpdateProfileRequest request)
    {
        if (User.GetUserId() != id)
            return Forbid();

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != id))
            return BadRequest(new { message = "That email is already in use." });

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(ToProfile(user));
    }

    private static UserProfileResponse ToProfile(User user)
    {
        return new UserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.RoleName,
            CreatedAt = user.CreatedAt
        };
    }
}