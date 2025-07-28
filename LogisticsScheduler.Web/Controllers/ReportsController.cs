using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public ReportsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        // FIX #1: Add the helper method to create a client with the JWT attached
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

        public async Task<IActionResult> Index()
        {
            // FIX #2: Use the authenticated client for the API call
            var client = GetAuthenticatedClient();
            var report = new List<DriverReportViewModel>();

            var response = await client.GetAsync("api/reports/driver-performance");

            if (response.IsSuccessStatusCode)
            {
                report = await response.Content.ReadFromJsonAsync<List<DriverReportViewModel>>();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "An error occurred while fetching reports.");
            }

            return View(report);
        }
    }
}