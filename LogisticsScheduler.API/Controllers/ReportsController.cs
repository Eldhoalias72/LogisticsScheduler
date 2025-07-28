using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogisticsScheduler.API.Controllers
{
    [Authorize(Roles = "Admin")]
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
            var driversWithData = await _context.Drivers
                .Include(driver => driver.Jobs)
                    .ThenInclude(job => job.Feedback)
                .AsNoTracking() 
                .ToListAsync();

            var report = driversWithData.Select(driver =>
            {
                
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