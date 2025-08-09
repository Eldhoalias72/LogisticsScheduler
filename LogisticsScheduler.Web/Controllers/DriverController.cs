using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http.Headers; // Add this using
using Microsoft.AspNetCore.Http; // Add this using

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public DriverController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        // FIX #1: Replace the old GetClient() with this helper method
        private HttpClient GetAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new System.Uri(_apiBaseUrl);
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<IActionResult> Index()
        {
            var driverIdClaim = User.Claims.FirstOrDefault(c => c.Type == "DriverId");
            if (driverIdClaim == null || !int.TryParse(driverIdClaim.Value, out int driverId))
            {
                return Unauthorized();
            }

            // FIX #2: Use the authenticated client
            var client = GetAuthenticatedClient();
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

            // FIX #3: Use the authenticated client
            var client = GetAuthenticatedClient();

            var response = await client.PutAsJsonAsync(
                   $"api/jobs/{jobId}/status",
                   new { Status = status }
               );


            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Failed to update job status.";
            }

            return RedirectToAction("Index");
        }
    }
}