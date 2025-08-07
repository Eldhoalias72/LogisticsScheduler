using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.API.Services; // <-- ADD THIS
using LogisticsScheduler.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService; // <-- ADD THIS
        private const string CacheKey = "dashboard_stats"; // <-- ADD THIS

        // MODIFY THE CONSTRUCTOR
        public DashboardController(AppDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService; // <-- ADD THIS
        }

        // REWRITE THE GetStats METHOD
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DashboardStatsDto>> GetStats()
        {
            // 1. Try to get data from cache
            var cachedStats = await _cacheService.GetData<DashboardStatsDto>(CacheKey);

            if (cachedStats != null)
            {
                // Cache Hit: Return data from cache
                return Ok(cachedStats);
            }

            // Cache Miss: Get data from the database
            var stats = new DashboardStatsDto
            {
                TotalDrivers = await _context.Drivers.CountAsync(),
                ActiveJobs = await _context.Jobs.CountAsync(j => j.Status != "Completed" && j.Status != "Cancelled")
            };

            // 3. Set data in cache for future requests (e.g., for 5 minutes)
            await _cacheService.SetData(CacheKey, stats, TimeSpan.FromMinutes(5));

            return Ok(stats);
        }
    }
}