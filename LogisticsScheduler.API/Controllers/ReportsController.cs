using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogisticsScheduler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("driver-performance")]
        public async Task<ActionResult<IEnumerable<DriverReportDto>>> GetDriverPerformanceReport()
        {
            // Step 1: Fetch all drivers with their jobs and related feedback in a single DB call.
            // This is efficient and avoids the SQL translation error.
            var driversWithData = await _context.Drivers
                .Include(driver => driver.Jobs)
                    .ThenInclude(job => job.Feedback)
                .AsNoTracking() // Use AsNoTracking for read-only queries to improve performance
                .ToListAsync();

            // Step 2: Now that all data is in memory, build the report. This is fast and reliable.
            var report = driversWithData.Select(driver =>
            {
                // Get a list of all non-null feedback records for the current driver's jobs
                var feedbacks = driver.Jobs
                                      .Select(job => job.Feedback)
                                      .Where(feedback => feedback != null)
                                      .ToList();

                return new DriverReportDto
                {
                    DriverId = driver.DriverId,
                    DriverName = driver.Name,
                    TotalJobs = driver.Jobs.Count(),
                    AverageTimeliness = feedbacks.Any() ? feedbacks.Average(f => f.Timeliness) : 0,
                    AverageProductCondition = feedbacks.Any() ? feedbacks.Average(f => f.ProductCondition) : 0,
                    AverageStaffBehaviour = feedbacks.Any() ? feedbacks.Average(f => f.StaffBehaviour) : 0
                };
            }).ToList();

            return Ok(report);
        }
    }
}