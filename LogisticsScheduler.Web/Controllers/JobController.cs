using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Data.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using System.Linq; // Add this using statement

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class JobController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public JobController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

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

        // MODIFIED: This action is the key to the fix
        public async Task<IActionResult> Schedule(DateTime? date)
        {
            var client = GetAuthenticatedClient();

            // Build the request URI without any cache-busting parameters
            string requestUri = "api/jobs";
            if (date.HasValue)
            {
                requestUri += $"?date={date.Value:yyyy-MM-dd}";
                ViewBag.SelectedDate = date.Value;
            }

            // This request will now hit the Redis cache on the API if the data is available
            var jobsResponse = await client.GetAsync(requestUri);
            var jobs = jobsResponse.IsSuccessStatusCode
                ? await jobsResponse.Content.ReadFromJsonAsync<List<Job>>()
                : new List<Job>();

            // The rest of the method remains the same
            var driversResponse = await client.GetAsync("api/drivers?isAvailable=true");
            ViewBag.Drivers = driversResponse.IsSuccessStatusCode
                ? await driversResponse.Content.ReadFromJsonAsync<List<Driver>>()
                : new List<Driver>();

            return View(jobs);
        }


        // No changes are needed in the methods below, as they all redirect to the now-fixed Schedule action.

        [HttpGet]
        public async Task<IActionResult> Assign()
        {
            var client = GetAuthenticatedClient();
            var driversResponse = await client.GetAsync("api/drivers?isAvailable=true");

            ViewBag.Drivers = driversResponse.IsSuccessStatusCode
                ? await driversResponse.Content.ReadFromJsonAsync<List<Driver>>()
                : new List<Driver>();

            return View(new Job());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(Job job)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate drivers on validation failure
                var clientForRepopulate = GetAuthenticatedClient();
                var driversResponseForRepopulate = await clientForRepopulate.GetAsync("api/drivers?isAvailable=true");
                ViewBag.Drivers = driversResponseForRepopulate.IsSuccessStatusCode
                    ? await driversResponseForRepopulate.Content.ReadFromJsonAsync<List<Driver>>()
                    : new List<Driver>();
                return View(job);
            }

            var jobCreateDto = new
            {
                DriverId = job.DriverId == 0 ? (int?)null : job.DriverId,
                job.PickupAddress,
                job.DeliveryAddress,
                job.Priority,
                job.Status,
                job.ScheduledTime,
                job.CustomerName,
                job.CustomerEmail,
                job.CustomerNumber
            };

            var client = GetAuthenticatedClient();
            var response = await client.PostAsJsonAsync("api/jobs", jobCreateDto);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "New job has been scheduled successfully!";
                return RedirectToAction("Schedule");
            }

            ModelState.AddModelError(string.Empty, "An error occurred while creating the job.");
            var driversResponse = await client.GetAsync("api/drivers?isAvailable=true");
            ViewBag.Drivers = driversResponse.IsSuccessStatusCode
                ? await driversResponse.Content.ReadFromJsonAsync<List<Driver>>()
                : new List<Driver>();

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoAssign(int jobId)
        {
            var client = GetAuthenticatedClient();
            var response = await client.PostAsync($"api/jobs/{jobId}/auto-assign", null);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Failed to auto-assign job. No suitable drivers may be available.";
            }

            return RedirectToAction("Schedule");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(int jobId, int driverId)
        {
            var client = GetAuthenticatedClient();
            var response = await client.PutAsync($"api/jobs/{jobId}/reassign/{driverId}", null);

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Failed to reassign job.";
            }
            return RedirectToAction("Schedule");
        }
    }
}