using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http.Json;
using LogisticsScheduler.Data.Models;
using System.Net.Http.Headers; 
using Microsoft.AspNetCore.Http; 
public class DashboardStatsDto
{
    public int TotalDrivers { get; set; }
    public int ActiveJobs { get; set; }
}

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public AdminController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        //  method to create a client with the JWT attached
        private HttpClient GetAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<IActionResult> Dashboard()
        {
            //  Use the authenticated client for the API call
            var client = GetAuthenticatedClient();
            var response = await client.GetAsync("api/dashboard/stats");

            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
                ViewBag.TotalDrivers = stats.TotalDrivers;
                ViewBag.ActiveJobs = stats.ActiveJobs;
            }
            else
            {
                ViewBag.TotalDrivers = "N/A";
                ViewBag.ActiveJobs = "N/A";
                TempData["ErrorMessage"] = "Could not load dashboard stats. Please try again later.";
            }

            return View();
        }

        public IActionResult AddDriver()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDriver(Driver driver, string Password)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
                return View(driver);
            }

            // Use the authenticated client for the API call
            var client = GetAuthenticatedClient();

            var driverDto = new
            {
                driver.Name,
                driver.Location,
                driver.IsAvailable,
                driver.VehicleCapacity,
                driver.Username,
                Password
            };

            var response = await client.PostAsJsonAsync($"api/drivers", driverDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = $"Driver '{driver.Name}' added successfully!";
                return RedirectToAction("Dashboard");
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Failed to add driver: {errorResponse}");
            return View(driver);
        }
    }
}