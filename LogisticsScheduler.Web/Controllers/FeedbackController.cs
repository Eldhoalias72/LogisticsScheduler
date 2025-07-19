using LogisticsScheduler.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace LogisticsScheduler.Web.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public FeedbackController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiBaseUrl = configuration["ApiBaseUrl"];
        }

        [HttpGet]
        public IActionResult Submit(int jobId)
        {
            ViewBag.JobId = jobId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit([Bind("JobId,Timeliness,ProductCondition,StaffBehaviour,Comments")] FeedbackCreateDto feedbackDto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.JobId = feedbackDto.JobId;
                return View(feedbackDto);
            }

            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/feedback", feedbackDto);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("ThankYou");
            }

            ModelState.AddModelError("", "Failed to submit feedback.");
            ViewBag.JobId = feedbackDto.JobId;
            return View(feedbackDto);
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
