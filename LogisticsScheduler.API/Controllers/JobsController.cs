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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetAllJobs()
        {
            return await _context.Jobs
                .Include(j => j.Driver)
                .Include(j => j.JobStatuses)
                .Include(j => j.Feedback)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJobById(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.Driver)
                .Include(j => j.JobStatuses)
                .Include(j => j.Feedback)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
                return NotFound();

            return job;
        }

        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(JobCreateDto dto)
        {
            var job = new Job
            {
                DriverId = dto.DriverId,
                DeliveryAddress = dto.DeliveryAddress,
                Priority = dto.Priority,
                Status = dto.Status,
                ScheduledTime = dto.ScheduledTime
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetJobById), new { id = job.JobId }, job);
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
                TimeStamp = DateTime.Now
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
    }
}
