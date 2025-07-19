using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/jobs
        // GET: api/jobs?date=2025-07-20
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobs([FromQuery] DateTime? date)
        {
            var jobsQuery = _context.Jobs
                .Include(j => j.Driver)
                .AsQueryable();

            if (date.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.ScheduledTime.Date == date.Value.Date);
            }

            return await jobsQuery.OrderByDescending(j => j.ScheduledTime).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJobById(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Driver)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
                return NotFound();

            return job;
        }

        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(JobCreateDto dto)
        {
            if (dto.DriverId.HasValue && !await _context.Drivers.AnyAsync(d => d.DriverId == dto.DriverId.Value))
            {
                return BadRequest("Invalid DriverId.");
            }

            var job = new Job
            {
                DriverId = dto.DriverId,
                DeliveryAddress = dto.DeliveryAddress,
                Priority = dto.Priority,
                Status = dto.DriverId.HasValue ? "Assigned" : "Scheduled",
                ScheduledTime = dto.ScheduledTime
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
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

            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateJobStatus(int id, [FromBody] string status)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound();

            job.Status = status;

            var statusLog = new JobStatus
            {
                JobId = job.JobId,
                Status = status,
                TimeStamp = DateTime.UtcNow
            };

            _context.JobStatuses.Add(statusLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private double CalculateDistance(string origin, string destination)
        {
            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
            {
                return double.MaxValue;
            }
            return new Random().NextDouble() * 10;
        }
    }
}