using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;

namespace LogisticsScheduler.Web.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Submit(int jobId)
        {
            ViewBag.JobId = jobId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Submit([Bind("JobId,Timeliness,ProductCondition,StaffBehaviour,Comments")] Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();
                return RedirectToAction("ThankYou");
            }

            ViewBag.JobId = feedback.JobId;
            return View(feedback);
        }


        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
