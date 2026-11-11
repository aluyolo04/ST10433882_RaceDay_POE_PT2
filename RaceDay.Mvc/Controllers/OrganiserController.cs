
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
[RoleAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Dashboard()
    {
        var result = await _api.GetAsync<List<EventViewModel>>("api/events");

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return View(new OrganiserDashboardViewModel());
        }

        int.TryParse(HttpContext.Session.GetString("UserId"), out int userId);

        var events = result.Data?
            .Where(e => e.OrganiserId == userId)
            .ToList() ?? new();

        return View(new OrganiserDashboardViewModel
        {
            Events = events
        });
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventViewModel
        {
            EventDate = DateTime.Today.AddDays(7)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventViewModel model)
    {
        if (!ModelState.IsValid)
            return View("EventForm", model);

        var result = await _api.PostAsync<EventViewModel, EventViewModel>(
            "api/events", model);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error);
            return View("EventForm", model);
        }

        TempData["Success"] = "Event created successfully!";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetAsync<EventViewModel>($"api/events/{id}");

        if (!result.Success || result.Data == null)
            return NotFound();

        if (!IsOwner(result.Data))
            return Forbid();

        return View("EventForm", result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventViewModel model)
    {
        if (!ModelState.IsValid)
            return View("EventForm", model);

        var existing = await _api.GetAsync<EventViewModel>($"api/events/{id}");

        if (!existing.Success || existing.Data == null)
            return NotFound();

        if (!IsOwner(existing.Data))
            return Forbid();

        var result = await _api.PutAsync<EventViewModel, EventViewModel>(
            $"api/events/{id}", model);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error);
            return View("EventForm", model);
        }

        TempData["Success"] = "Event updated successfully!";
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var existing = await _api.GetAsync<EventViewModel>($"api/events/{id}");

        if (!existing.Success || existing.Data == null)
            return NotFound();

        if (!IsOwner(existing.Data))
            return Forbid();

        var deleted = await _api.DeleteAsync($"api/events/{id}");

        TempData[deleted ? "Success" : "Error"] =
            deleted ? "Event deleted." : "Could not delete event.";

        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await _api.GetAsync<EventViewModel>($"api/events/{id}");

        if (!ev.Success || ev.Data == null)
            return NotFound();

        if (!IsOwner(ev.Data))
            return Forbid();

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
    public async Task<IActionResult> AddCategory(
        int eventId, CategoryViewModel model)
    {
        var ev = await _api.GetAsync<EventViewModel>($"api/events/{eventId}");

        if (!ev.Success || ev.Data == null)
            return NotFound();

        if (!IsOwner(ev.Data))
            return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter valid category details.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        var result = await _api.PostAsync<CategoryViewModel, CategoryViewModel>(
            $"api/events/{eventId}/categories", model);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "Category added!" : result.Error;

        return RedirectToAction(nameof(EventDetails), new { id = eventId });
    }

    public async Task<IActionResult> Enrolments(int id)
    {
        var result = await _api.GetAsync<List<EnrolmentViewModel>>(
            $"api/categories/{id}/enrolments");

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Dashboard));
        }

        ViewBag.CategoryId = id;
        return View(result.Data ?? new());
    }

    private bool IsOwner(EventViewModel ev)
    {
        return int.TryParse(
            HttpContext.Session.GetString("UserId"), out int userId)
            && ev.OrganiserId == userId;
    }

    [HttpGet]
    public async Task<IActionResult> CaptureResults(int id)
    {
        // Verify ownership through the existing API.
        var enrolments = await _api.GetAsync<List<EnrolmentViewModel>>(
            $"api/categories/{id}/enrolments");

        if (!enrolments.Success)
        {
            TempData["Error"] = enrolments.Error;
            return RedirectToAction(nameof(Dashboard));
        }

        var results = await _api.GetAsync<List<RaceResultViewModel>>(
            $"api/results/categories/{id}");

        if (!results.Success)
        {
            TempData["Error"] = results.Error;
            return RedirectToAction(nameof(Dashboard));
        }

        ViewBag.CategoryId = id;
        ViewBag.Results = results.Data ?? new List<RaceResultViewModel>();

        return View(enrolments.Data ?? new List<EnrolmentViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(
        CaptureResultViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter valid result details.";
            return RedirectToAction(nameof(CaptureResults),
                new { id = model.CategoryId });
        }

        var allowedStatuses = new[] { "Finished", "DNF", "DNS", "DSQ" };

        if (!allowedStatuses.Contains(model.Status))
        {
            TempData["Error"] = "Invalid result status.";
            return RedirectToAction(nameof(CaptureResults),
                new { id = model.CategoryId });
        }

        var request = new
        {
            model.FinishTime,
            model.Position,
            model.Status
        };

        var response = await _api.PutAsync<object, System.Text.Json.JsonElement>(
            $"api/results/enrolments/{model.EnrolmentId}",
            request);

        TempData[response.Success ? "Success" : "Error"] =
            response.Success
                ? "Race result saved successfully!"
                : response.Error;

        return RedirectToAction(nameof(CaptureResults),
            new { id = model.CategoryId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int eventId)
    {
        var categories = await _api.GetAsync<List<CategoryViewModel>>(
            $"api/events/{eventId}/categories");

        if (!categories.Success)
        {
            TempData["Error"] = categories.Error;
            return RedirectToAction(nameof(Dashboard));
        }

        var category = categories.Data?
            .FirstOrDefault(c => c.CategoryId == id);

        if (category == null)
            return NotFound();

        // Verify ownership before displaying the editing page.
        var ev = await _api.GetAsync<EventViewModel>(
            $"api/events/{eventId}");

        if (!ev.Success || ev.Data == null || !IsOwner(ev.Data))
            return Forbid();

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryViewModel model)
    {
        var ev = await _api.GetAsync<EventViewModel>(
            $"api/events/{model.EventId}");

        if (!ev.Success || ev.Data == null || !IsOwner(ev.Data))
            return Forbid();

        if (!ModelState.IsValid)
            return View(model);

        var request = new
        {
            model.CategoryName,
            model.DistanceKm,
            model.EntryFee,
            model.MaxParticipants
        };

        var result = await _api.PutAsync<object, CategoryViewModel>(
            $"api/categories/{model.CategoryId}", request);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        TempData["Success"] = "Category updated successfully!";

        return RedirectToAction(nameof(EventDetails),
            new { id = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var ev = await _api.GetAsync<EventViewModel>(
            $"api/events/{eventId}");

        if (!ev.Success || ev.Data == null || !IsOwner(ev.Data))
            return Forbid();

        var deleted = await _api.DeleteAsync($"api/categories/{id}");

        TempData[deleted ? "Success" : "Error"] =
            deleted
                ? "Category deleted successfully."
                : "Could not delete category.";

        return RedirectToAction(nameof(EventDetails),
            new { id = eventId });
    }

}
