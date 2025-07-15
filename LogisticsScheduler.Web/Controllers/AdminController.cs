using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogisticsScheduler.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public AdminController(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

   

        public IActionResult AddDriver()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalDrivers = await _context.Drivers.CountAsync();
            var activeJobs = await _context.Jobs.Where(j => j.Status != "Completed").CountAsync();

            ViewBag.TotalDrivers = totalDrivers;
            ViewBag.ActiveJobs = activeJobs;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddDriver(Driver driver, string Password)
        {
            driver.PasswordHash = _authService.HashPassword(Password);
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            return RedirectToAction("AddDriver");
        }
    }
}
