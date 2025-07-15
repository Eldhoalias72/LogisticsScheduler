using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DriversController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Driver>>> GetAllDrivers()
        {
            return await _context.Drivers.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Driver>> GetDriverById(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
                return NotFound();
            return driver;
        }

        [HttpPost]
        public async Task<ActionResult<Driver>> CreateDriver(DriverCreateDto dto)
        {
            var driver = new Driver
            {
                Name = dto.Name,
                Location = dto.Location,
                IsAvailable = dto.IsAvailable,
                VehicleCapacity = dto.VehicleCapacity
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDriverById), new { id = driver.DriverId }, driver);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDriver(int id, Driver driver)
        {
            if (id != driver.DriverId)
                return BadRequest();

            _context.Entry(driver).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
                return NotFound();

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
