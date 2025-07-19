using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging; // Add this using

public class LoginResponse
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
}

public class LoginController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiBaseUrl;
    private readonly ILogger<LoginController> _logger; // Add a logger

    // Inject ILogger
    public LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LoginController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("Driver")) return RedirectToAction("Index", "Driver");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string role, string username, string password)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_apiBaseUrl);
        var loginRequest = new { Role = role, Username = username, Password = password };
        var response = await client.PostAsJsonAsync("api/auth/login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            await SignInUser(loginResponse.Username, loginResponse.Role, loginResponse.UserId);
            if (loginResponse.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (loginResponse.Role == "Driver")
            {
                return RedirectToAction("Index", "Driver");
            }
        }

        ViewBag.LoginError = "Invalid credentials. Please try again.";
        return View("Index");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Login");
    }

    private async Task SignInUser(string username, string role, int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        if (role == "Driver")
        {
            claims.Add(new Claim("DriverId", userId.ToString()));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // --- TEMPORARY DEBUG LOGGING ---
        _logger.LogWarning("--- Attempting to sign in user: {Username} ---", username);
        foreach (var claim in claimsPrincipal.Claims)
        {
            _logger.LogWarning(" > Claim Added: Type = {Type}, Value = {Value}", claim.Type, claim.Value);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        _logger.LogWarning("--- SignInAsync completed. User.Identity.IsAuthenticated: {IsAuth} ---", HttpContext.User.Identity.IsAuthenticated);
    }
}