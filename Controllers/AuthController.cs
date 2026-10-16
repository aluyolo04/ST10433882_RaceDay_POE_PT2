using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;

    public AuthController(AppDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    /// <summary>
    /// Registers a new account as an Organiser or a Participant.
    /// </summary>
    /// <remarks>Send fullName, email, password (min 8 characters) and role ("Organiser" or "Participant").</remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed or role is not allowed.</response>
    /// <response code="409">Email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        string roleName;
        if (string.Equals(request.Role.Trim(), "Organiser", StringComparison.OrdinalIgnoreCase))
            roleName = "Organiser";
        else if (string.Equals(request.Role.Trim(), "Participant", StringComparison.OrdinalIgnoreCase))
            roleName = "Participant";
        else
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "That email is already registered." });

        var role = await _db.Roles.FirstAsync(r => r.RoleName == roleName);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.RoleId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            userId = user.UserId,
            fullName = user.FullName,
            email = user.Email,
            role = role.RoleName
        });
    }

    /// <summary>
    /// Logs in and returns a JWT token containing the user's role.
    /// </summary>
    /// <remarks>Send email and password. Use the returned token in the Authorize button.</remarks>
    /// <response code="200">Login successful, token returned.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var (token, expires) = _tokens.CreateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            Expires = expires,
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.RoleName
        });
    }
}
