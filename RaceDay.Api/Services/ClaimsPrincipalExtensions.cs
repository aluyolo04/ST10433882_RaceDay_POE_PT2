using System.Security.Claims;

namespace RaceDay.Api.Services;

public static class ClaimsPrincipalExtensions
{
    // Reads the user id from the login token
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}