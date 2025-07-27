using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // Required for .FirstOrDefault()
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;

// FIX #1: This helper class was missing. It's needed to read the API response.
public class LoginResponse
{
    public string Token { get; set; }
}

public class LoginController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiBaseUrl;

    public LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
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
        await HttpContext.Session.LoadAsync();

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new System.Uri(_apiBaseUrl);
        var loginRequest = new { Role = role, Username = username, Password = password };

        var response = await client.PostAsJsonAsync("api/auth/login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            var token = loginResponse.Token;

            var handler = new JwtSecurityTokenHandler();
            var decodedToken = handler.ReadJwtToken(token);

            HttpContext.Session.SetString("JWToken", token);

            await SignInUser(decodedToken.Claims);

            // FIX #2: Check the role from the token we just received, not from the old HttpContext.User
            var roleClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (roleClaim != null)
            {
                if (roleClaim.Value == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (roleClaim.Value == "Driver")
                {
                    return RedirectToAction("Index", "Driver");
                }
            }
        }

        ViewBag.LoginError = "Invalid credentials or role.";
        return View("Index");
    }

    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove("JWToken");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Login");
    }

    private async Task SignInUser(IEnumerable<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
    }
}