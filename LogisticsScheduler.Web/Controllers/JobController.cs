using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class JobController : Controller
    {
        private readonly AppDbContext _context;

        public JobController(AppDbContext context)
        {
            _context = context;
        }

        // Schedule view with calendar/timeline
        public async Task<IActionResult> Schedule(DateTime? date)
        {
            var jobsQuery = _context.Jobs
                .Include(j => j.Driver)
                .AsQueryable();

            if (date.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.ScheduledTime.Date == date.Value.Date);
                ViewBag.SelectedDate = date.Value;
            }
            else
            {
                ViewBag.SelectedDate = null;
            }

            var jobs = await jobsQuery.ToListAsync();
            ViewBag.Drivers = await _context.Drivers.Where(d => d.IsAvailable).ToListAsync();

            return View(jobs);
        }



        // Manual job assignment
        [HttpGet]
        public async Task<IActionResult> Assign()
        {
            ViewBag.Drivers = await _context.Drivers.Where(d => d.IsAvailable).ToListAsync();
            return View(new Job { Status = "Scheduled" });
        }


        [HttpPost]
        public async Task<IActionResult> Assign(Job job)
        {
            if (ModelState.IsValid)
            {
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction("Schedule");
            }

            ViewBag.Drivers = await _context.Drivers
                .Where(d => d.IsAvailable)
                .ToListAsync();

            return View(job);
        }

        // Auto-assign jobs based on criteria
        [HttpPost]
        public async Task<IActionResult> AutoAssign(int jobId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();

            // Find best driver based on proximity, availability, and capacity
            var suitableDrivers = await _context.Drivers
                .Where(d => d.IsAvailable)
                .OrderBy(d => CalculateDistance(d.Location, job.DeliveryAddress)) // Simplified
                .ToListAsync();

            if (suitableDrivers.Any())
            {
                job.DriverId = suitableDrivers.First().DriverId;
                job.Status = "Assigned";
                _context.Update(job);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Schedule");
        }

        // Job reassignment
        [HttpPost]
        public async Task<IActionResult> Reassign(int jobId, int driverId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();

            job.DriverId = driverId;
            _context.Update(job);
            await _context.SaveChangesAsync();

            return RedirectToAction("Schedule");
        }

        // Simplified distance calculation (replace with Google Maps API)
        private double CalculateDistance(string origin, string destination)
        {
            // This is a placeholder - implement actual Google Maps API integration
            return new Random().NextDouble() * 10;
        }
    }
}