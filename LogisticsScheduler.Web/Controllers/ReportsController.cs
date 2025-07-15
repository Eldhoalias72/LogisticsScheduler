using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsScheduler.Data;
using LogisticsScheduler.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers.ToListAsync();

            var report = new List<DriverReportViewModel>();

            foreach (var driver in drivers)
            {
                var jobs = await _context.Jobs.Where(j => j.DriverId == driver.DriverId).ToListAsync();
                var jobIds = jobs.Select(j => j.JobId).ToList();

                var feedbacks = await _context.Feedbacks.Where(f => jobIds.Contains(f.JobId)).ToListAsync();

                var reportItem = new DriverReportViewModel
                {
                    DriverId = driver.DriverId,
                    DriverName = driver.Name,
                    TotalJobs = jobs.Count,
                    AverageTimeliness = feedbacks.Any() ? feedbacks.Average(f => f.Timeliness) : 0,
                    AverageProductCondition = feedbacks.Any() ? feedbacks.Average(f => f.ProductCondition) : 0,
                    AverageStaffBehaviour = feedbacks.Any() ? feedbacks.Average(f => f.StaffBehaviour) : 0
                };

                report.Add(reportItem);
            }

            return View(report);
        }
    }
}
