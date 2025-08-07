using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.API.Services;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private const string DashboardCacheKey = "dashboard_stats";

        public JobsController(AppDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        // MODIFIED: This is the corrected GET method
        // In LogisticsScheduler.API/Controllers/JobsController.cs

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobDto>>> GetJobs([FromQuery] DateTime? date)
        {
            var cacheKey = date.HasValue ? $"jobs:{date.Value:yyyy-MM-dd}" : "jobs:all";

            var cachedJobs = await _cacheService.GetData<List<JobDto>>(cacheKey);
            if (cachedJobs != null)
            {
                return Ok(cachedJobs);
            }

            var jobsQuery = _context.Jobs.AsQueryable();

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                jobsQuery = jobsQuery.Where(j => j.ScheduledTime >= startDate && j.ScheduledTime < endDate);
            }

            // Project the database models into DTOs using Select()
            var jobs = await jobsQuery
                .OrderByDescending(j => j.ScheduledTime)
                .Select(j => new JobDto
                {
                    JobId = j.JobId,
                    PickupAddress = j.PickupAddress,
                    DeliveryAddress = j.DeliveryAddress,
                    Priority = j.Priority,
                    Status = j.Status,
                    ScheduledTime = j.ScheduledTime,
                    CustomerName = j.CustomerName,
                    CustomerEmail = j.CustomerEmail,
                    CustomerNumber = j.CustomerNumber,
                    // Only map the driver fields you need
                    Driver = j.Driver == null ? null : new DriverDto
                    {
                        DriverId = j.Driver.DriverId,
                        Name = j.Driver.Name
                    }
                })
                .ToListAsync();

            await _cacheService.SetData(cacheKey, jobs, TimeSpan.FromMinutes(5));

            return Ok(jobs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJobById(int id)
        {
            var cacheKey = $"job:{id}";
            var cachedJob = await _cacheService.GetData<Job>(cacheKey);

            if (cachedJob != null)
            {
                return Ok(cachedJob);
            }

            var job = await _context.Jobs
                .Include(j => j.Driver)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
            {
                return NotFound();
            }

            await _cacheService.SetData(cacheKey, job, TimeSpan.FromMinutes(5));

            return Ok(job);
        }

        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(JobCreateDto dto)
        {
            var job = new Job
            {
                DriverId = dto.DriverId,
                PickupAddress = dto.PickupAddress,
                DeliveryAddress = dto.DeliveryAddress,
                Priority = dto.Priority,
                Status = dto.DriverId.HasValue ? "Assigned" : "Scheduled",
                ScheduledTime = dto.ScheduledTime,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerNumber = dto.CustomerNumber
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            await InvalidateJobCaches(job);

            return CreatedAtAction(nameof(GetJobById), new { id = job.JobId }, job);
        }

        [HttpPost("{id}/auto-assign")]
        public async Task<IActionResult> AutoAssignJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound("Job not found.");
            if (job.DriverId.HasValue) return BadRequest("Job is already assigned.");

            var suitableDriver = await _context.Drivers
                .Where(d => d.IsAvailable)
                .OrderBy(d => CalculateDistance(d.Location, job.DeliveryAddress))
                .FirstOrDefaultAsync();

            if (suitableDriver != null)
            {
                job.DriverId = suitableDriver.DriverId;
                job.Status = "Assigned";
                _context.Update(job);
                await _context.SaveChangesAsync();

                await InvalidateJobCaches(job);

                await _context.Entry(job).Reference(j => j.Driver).LoadAsync();

                return Ok(job);
            }

            return NotFound("No suitable drivers available.");
        }

        [HttpPut("{jobId}/reassign/{driverId}")]
        public async Task<IActionResult> ReassignJob(int jobId, int driverId)
        {
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound("Job not found.");

            var driverExists = await _context.Drivers.AnyAsync(d => d.DriverId == driverId);
            if (!driverExists) return BadRequest("Driver not found.");

            job.DriverId = driverId;
            job.Status = "Assigned";
            _context.Update(job);
            await _context.SaveChangesAsync();

            await InvalidateJobCaches(job);

            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] string status)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            job.Status = status;
            var statusLog = new JobStatus
            {
                JobId = job.JobId,
                Status = status,
                TimeStamp = DateTime.UtcNow
            };

            _context.JobStatuses.Add(statusLog);
            await _context.SaveChangesAsync();

            await InvalidateJobCaches(job);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            await InvalidateJobCaches(job);

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task InvalidateJobCaches(Job job)
        {
            var tasks = new List<Task>
            {
                _cacheService.RemoveData($"job:{job.JobId}"),
                _cacheService.RemoveData("jobs:all"),
                _cacheService.RemoveData($"jobs:{job.ScheduledTime:yyyy-MM-dd}"),
                _cacheService.RemoveData(DashboardCacheKey)
            };
            await Task.WhenAll(tasks);
        }

        private double CalculateDistance(string? origin, string? destination)
        {
            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
            {
                return double.MaxValue;
            }
            return new Random().NextDouble() * 10;
        }
    }
}