using LogisticsScheduler.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class LoginController : Controller
{
    private readonly AuthService _authService;

    public LoginController(AuthService authService)
    {
        _authService = authService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string role, string username, string password)
    {
        if (role == "Admin")
        {
            var admin = await _authService.ValidateAdminAsync(username, password);
            if (admin != null)
            {
                await SignInUser(username, "Admin");
                return RedirectToAction("Dashboard", "Admin");
            }
        }
        else if (role == "Driver")
        {
            var driver = await _authService.ValidateDriverAsync(username, password);
            if (driver != null)
            {
                await SignInUser(username, "Driver", driver.DriverId);
                return RedirectToAction("Index", "Driver", new { driverId = driver.DriverId });
            }
        }

        ViewBag.LoginError = "Invalid credentials.";
        return View("Index");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Index", "Login");
    }

    private async Task SignInUser(string username, string role, int driverId = 0)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        if (role == "Driver")
        {
            claims.Add(new Claim("DriverId", driverId.ToString()));
        }

        var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);
    }
}
