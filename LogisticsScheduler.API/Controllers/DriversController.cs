using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.API.Services; // Added
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace LogisticsScheduler.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService; // Added
        private const string DashboardCacheKey = "dashboard_stats"; // Added

        // Modified constructor to inject the cache service
        public DriversController(AppDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService; // Added
        }

        // GET methods do not change data, so no changes are needed here.
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers([FromQuery] bool? isAvailable)
        {
            var query = _context.Drivers.AsQueryable();

            if (isAvailable.HasValue)
            {
                query = query.Where(d => d.IsAvailable == isAvailable.Value);
            }

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<Driver>> GetDriverById(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
                return NotFound();
            return driver;
        }

        [HttpGet("{driverId}/jobs")]
        [Authorize(Roles = "Driver")]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobsForDriver(int driverId)
        {
            var driverExists = await _context.Drivers.AnyAsync(d => d.DriverId == driverId);
            if (!driverExists)
            {
                return NotFound("Driver not found.");
            }

            var jobs = await _context.Jobs
                .Where(j => j.DriverId == driverId)
                .OrderByDescending(j => j.ScheduledTime)
                .ToListAsync();

            return Ok(jobs);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Driver>> CreateDriver(DriverCreateDto dto)
        {
            if (await _context.Drivers.AnyAsync(d => d.Username == dto.Username))
            {
                return BadRequest(new { message = "Username already exists." });
            }

            var driver = new Driver
            {
                Name = dto.Name,
                Location = dto.Location,
                IsAvailable = dto.IsAvailable,
                VehicleCapacity = dto.VehicleCapacity,
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            // Invalidate the cache because a new driver was added
            await _cacheService.RemoveData(DashboardCacheKey);

            return CreatedAtAction(nameof(GetDriverById), new { id = driver.DriverId }, new { driver.DriverId, driver.Name });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDriver(int id, Driver driver)
        {
            if (id != driver.DriverId)
                return BadRequest();

            _context.Entry(driver).State = EntityState.Modified;
            await _context.SaveChangesAsync();



            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
                return NotFound();

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();

            // Invalidate the cache because a driver was removed
            await _cacheService.RemoveData(DashboardCacheKey);

            return NoContent();
        }
    }
}