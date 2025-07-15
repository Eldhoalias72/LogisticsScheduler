using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly AppDbContext _context;

        public DriverController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get DriverId from Claims
            var driverIdClaim = User.Claims.FirstOrDefault(c => c.Type == "DriverId");

            if (driverIdClaim == null || !int.TryParse(driverIdClaim.Value, out int driverId))
            {
                return Unauthorized();
            }

            var jobs = await _context.Jobs
                .Where(j => j.DriverId == driverId)
                .OrderByDescending(j => j.ScheduledTime)
                .ToListAsync();

            ViewBag.DriverId = driverId;
            return View(jobs);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int jobId, string status)
        {
            var driverIdClaim = User.Claims.FirstOrDefault(c => c.Type == "DriverId");

            if (driverIdClaim == null || !int.TryParse(driverIdClaim.Value, out int driverId))
            {
                return Unauthorized();
            }

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId && j.DriverId == driverId);
            if (job == null)
            {
                return NotFound();
            }

            job.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
