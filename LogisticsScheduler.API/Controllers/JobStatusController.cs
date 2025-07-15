using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using LogisticsScheduler.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobStatusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobStatusController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobStatus>>> GetAll()
        {
            return await _context.JobStatuses.Include(js => js.Job).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobStatus>> GetById(int id)
        {
            var js = await _context.JobStatuses.FindAsync(id);
            if (js == null) return NotFound();
            return js;
        }

        [HttpPost]
        public async Task<ActionResult<JobStatus>> Create(JobStatusCreateDto dto)
        {
            var js = new JobStatus
            {
                JobId = dto.JobId,
                Status = dto.Status,
                TimeStamp = dto.TimeStamp
            };
            _context.JobStatuses.Add(js);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = js.UpdateId }, js);
        }
    }
}
