using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace LogisticsScheduler.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            var stats = new DashboardStatsDto
            {
                TotalDrivers = await _context.Drivers.CountAsync(),
                ActiveJobs = await _context.Jobs.CountAsync(j => j.Status != "Completed" && j.Status != "Cancelled")
            };
            return Ok(stats);
        }
    }
}