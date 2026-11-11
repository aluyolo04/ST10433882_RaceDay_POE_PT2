
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
[RoleAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? search, string? type)
    {
        var result = await _api.GetAsync<List<EventViewModel>>(
            "api/events?upcomingOnly=true");

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return View(new List<EventViewModel>());
        }

        var events = result.Data ?? new();

        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e =>
                e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e =>
                e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();

        ViewBag.Search = search;
        ViewBag.Type = type;

        return View(events);
    }

    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await _api.GetAsync<EventViewModel>($"api/events/{id}");

        if (!ev.Success || ev.Data == null)
            return NotFound();

        var categories = await _api.GetAsync<List<CategoryViewModel>>(
            $"api/events/{id}/categories");

        if (!categories.Success)
            TempData["Error"] = categories.Error;

        return View(new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result = await _api.PostEmptyAsync<EnrolmentViewModel>(
            $"api/categories/{categoryId}/enrol");

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "You have successfully enrolled!"
                : result.Error;

        return RedirectToAction(nameof(EventDetails), new { id = eventId });
    }

    public async Task<IActionResult> MyEnrolments()
    {
        int.TryParse(HttpContext.Session.GetString("UserId"), out int userId);

        var result = await _api.GetAsync<List<EnrolmentViewModel>>(
            $"api/users/{userId}/enrolments");

        if (!result.Success)
            TempData["Error"] = result.Error;

        return View(result.Data ?? new());
    }

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var response = await _api.GetAsync<List<RaceResultViewModel>>(
            "api/results/mine");

        if (!response.Success)
        {
            TempData["Error"] = response.Error;
            return View(new List<RaceResultViewModel>());
        }

        return View(response.Data ?? new List<RaceResultViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!int.TryParse(
            HttpContext.Session.GetString("UserId"), out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var response = await _api.GetAsync<UserProfileViewModel>(
            $"api/users/{userId}");

        if (!response.Success || response.Data == null)
        {
            TempData["Error"] = response.Error;
            return RedirectToAction(nameof(Index));
        }

        var image = await _api.GetAsync<ProfileImageViewModel>(
            "api/profile-images/mine");

        ViewBag.ProfileImageUrl = image.Success
            ? image.Data?.ImageUrl
            : null;

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!int.TryParse(
            HttpContext.Session.GetString("UserId"), out int userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var request = new
        {
            model.FullName,
            model.Email
        };

        var response = await _api.PutAsync<object, UserProfileViewModel>(
            $"api/users/{userId}", request);

        if (!response.Success || response.Data == null)
        {
            ModelState.AddModelError("", response.Error);
            return View(model);
        }

        HttpContext.Session.SetString(
            "FullName", response.Data.FullName);

        HttpContext.Session.SetString(
            "Email", response.Data.Email);

        TempData["Success"] = "Profile updated successfully!";

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0 ||
            file.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Choose an image smaller than 5 MB.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.UploadImageAsync<ProfileImageViewModel>(
            "api/profile-images/mine", file);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Profile picture updated!"
                : result.Error;

        return RedirectToAction(nameof(Profile));
    }


}
