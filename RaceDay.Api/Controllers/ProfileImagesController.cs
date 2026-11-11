
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile-images")]
[Authorize]
public class ProfileImagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ProfileImageStorageService _storage;

    public ProfileImagesController(
        AppDbContext db,
        ProfileImageStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpPost("mine")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0 ||
            file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Select an image under 5 MB." });

        var allowed = new Dictionary<string, string>
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

        if (!allowed.TryGetValue(
            file.ContentType.ToLowerInvariant(), out var extension))
            return BadRequest(new { message = "Only JPG, PNG and WebP are allowed." });

        // Check the file signature, not just the browser-supplied MIME type.
        var header = new byte[12];
        using (var input = file.OpenReadStream())
        {
            var read = await input.ReadAsync(header.AsMemory());
            if (read < 12)
                return BadRequest(new { message = "Invalid image." });
        }

        bool valid = extension switch
        {
            ".jpg" => header[0] == 0xFF && header[1] == 0xD8
                      && header[2] == 0xFF,
            ".png" => header.AsSpan(0, 8).SequenceEqual(
                new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".webp" => System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                       && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            _ => false
        };

        if (!valid)
            return BadRequest(new { message = "The image format is invalid." });

        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var oldBlob = user.ProfileImageBlobName;

        string blobName;
        await using (var stream = file.OpenReadStream())
        {
            blobName = await _storage.UploadAsync(
                stream, file.ContentType, extension);
        }

        user.ProfileImageBlobName = blobName;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            await _storage.DeleteAsync(blobName);
            throw;
        }

        if (!string.IsNullOrEmpty(oldBlob))
        {
            try { await _storage.DeleteAsync(oldBlob); }
            catch { /* Orphan cleanup can be retried later. */ }
        }

        return Ok(new
        {
            message = "Profile picture uploaded successfully.",
            imageUrl = _storage.GetReadUrl(blobName).ToString()
        });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());

        if (user == null)
            return NotFound();

        if (string.IsNullOrEmpty(user.ProfileImageBlobName))
            return Ok(new { imageUrl = (string?)null });

        return Ok(new
        {
            imageUrl = _storage.GetReadUrl(
                user.ProfileImageBlobName).ToString()
        });
    }
}
