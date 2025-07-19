using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        // REFACTORED: Inject IHttpClientFactory, not AppDbContext
        public ReportsController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiBaseUrl);

            var report = new List<DriverReportViewModel>();

            // Call the new API endpoint
            var response = await client.GetAsync("api/reports/driver-performance");

            if (response.IsSuccessStatusCode)
            {
                // Deserialize directly into the ViewModel, as the structure matches the DTO
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