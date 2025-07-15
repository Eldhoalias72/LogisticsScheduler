using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Feedback>> SubmitFeedback(FeedbackCreateDto dto)
        {
            var feedback = new Feedback
            {
                JobId = dto.JobId,
                Timeliness = dto.Timeliness,
                ProductCondition = dto.ProductCondition,
                StaffBehaviour = dto.StaffBehaviour,
                Comments = dto.Comments
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.FeedbackId }, feedback);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Feedback>> GetFeedbackById(int id)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.Job)
                .FirstOrDefaultAsync(f => f.FeedbackId == id);

            if (feedback == null)
                return NotFound();

            return feedback;
        }

        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<Feedback>> GetFeedbackByJobId(int jobId)
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.Job)
                .FirstOrDefaultAsync(f => f.JobId == jobId);

            if (feedback == null)
                return NotFound();

            return feedback;
        }
    }
}
