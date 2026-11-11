
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiClient _api;

        public AccountController(RaceDayApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _api.PostAsync<LoginRequest, AuthResponse>(
                    "api/auth/login",
                    new LoginRequest
                    {
                        Email = model.Email,
                        Password = model.Password
                    });

                if (!result.Success || result.Data == null)
                {
                    ModelState.AddModelError("", result.Error);
                    return View(model);
                }

                var user = result.Data;

                HttpContext.Session.SetString("Token", user.Token);
                HttpContext.Session.SetString("UserId", user.UserId.ToString());
                HttpContext.Session.SetString("FullName", user.FullName);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role);

                if (user.Role == "Organiser")
                    return RedirectToAction("Dashboard", "Organiser");

                return RedirectToAction("Index", "Participant");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("",
                    "Unable to connect to RaceDay. Please start the API.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role != "Organiser" &&
                model.Role != "Participant")
            {
                ModelState.AddModelError("Role", "Invalid account type.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _api.PostAsync<RegisterRequest, JsonElement>(
                    "api/auth/register",
                    new RegisterRequest
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        Password = model.Password,
                        Role = model.Role
                    });

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.Error);
                    return View(model);
                }

                TempData["Success"] =
                    "Account created successfully. Please log in.";

                return RedirectToAction("Login");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError("",
                    "Unable to connect to RaceDay API.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
