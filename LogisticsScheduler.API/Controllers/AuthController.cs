using BCrypt.Net; // This is the correct using statement
using LogisticsScheduler.API.DTOs;
using LogisticsScheduler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LogisticsScheduler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto loginRequest)
        {
            if (loginRequest.Role == "Admin")
            {
                var admin = await _context.Admin.FirstOrDefaultAsync(a => a.Username == loginRequest.Username);
            
                if (admin != null && BCrypt.Net.BCrypt.Verify(loginRequest.Password, admin.PasswordHash))
                {
                    var response = new LoginResponseDto
                    {
                        UserId = admin.AdminId,
                        Username = admin.Username,
                        Role = "Admin"
                    };
                    return Ok(response);
                }
            }
            else if (loginRequest.Role == "Driver")
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Username == loginRequest.Username);
        
                if (driver != null && BCrypt.Net.BCrypt.Verify(loginRequest.Password, driver.PasswordHash))
                {
                    var response = new LoginResponseDto
                    {
                        UserId = driver.DriverId,
                        Username = driver.Username,
                        Role = "Driver"
                    };
                    return Ok(response);
                }
            }

            return Unauthorized(new { message = "Invalid credentials." });
        }
    }
}