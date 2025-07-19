using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http.Json;
using LogisticsScheduler.Data.Models; 
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

        // The constructor is now clean and only has the dependencies it needs.
        public AdminController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        public async Task<IActionResult> Dashboard()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

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
            }

            return View();
        }

        public IActionResult AddDriver()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDriver(Driver driver, string Password) // Assuming the view sends these two
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError("Password", "Password is required.");
                return View(driver);
            }

            var client = _httpClientFactory.CreateClient();

            // Create the DTO to send to the API, including the raw password
            var driverDto = new
            {
                driver.Name,
                driver.Location,
                driver.IsAvailable,
                driver.VehicleCapacity,
                driver.Username,
                Password 
            };

            var response = await client.PostAsJsonAsync($"{_apiBaseUrl}/api/drivers", driverDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = $"Driver '{driver.Name}' added successfully!";
                return RedirectToAction("Dashboard");
            }

            var errorResponse = await response.Content.ReadFromJsonAsync<object>();
            ModelState.AddModelError(string.Empty, $"Failed to add driver: {errorResponse}");
            return View(driver);
        }
    }
}