using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        // REFACTORED: Inject IHttpClientFactory, not AppDbContext
        public DriverController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        private HttpClient GetClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new System.Uri(_apiBaseUrl);
            return client;
        }

        public async Task<IActionResult> Index()
        {
            var driverIdClaim = User.Claims.FirstOrDefault(c => c.Type == "DriverId");
            if (driverIdClaim == null || !int.TryParse(driverIdClaim.Value, out int driverId))
            {
                return Unauthorized();
            }

            var client = GetClient();
            var response = await client.GetAsync($"api/drivers/{driverId}/jobs");

            var jobs = new List<Job>();
            if (response.IsSuccessStatusCode)
            {
                jobs = await response.Content.ReadFromJsonAsync<List<Job>>();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Could not retrieve your jobs from the server.");
            }

            ViewBag.DriverId = driverId;
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int jobId, string status)
        {
            var driverIdClaim = User.Claims.FirstOrDefault(c => c.Type == "DriverId");
            if (driverIdClaim == null) return Unauthorized();

            var client = GetClient();

            // The API endpoint for this is in JobsController, which is correct.
            var response = await client.PutAsJsonAsync($"api/jobs/{jobId}/status", status);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Failed to update job status.";
            }

            return RedirectToAction("Index");
        }
    }
}