using LogisticsScheduler.Data;
using LogisticsScheduler.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

public class AuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Admin?> ValidateAdminAsync(string username, string password)
    {
        var admin = await _context.Admin.FirstOrDefaultAsync(a => a.Username == username);
        if (admin == null) return null;

        return VerifyPassword(password, admin.PasswordHash) ? admin : null;
    }

    public async Task<Driver?> ValidateDriverAsync(string username, string password)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Username == username);
        if (driver == null) return null;

        return VerifyPassword(password, driver.PasswordHash) ? driver : null;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private bool VerifyPassword(string input, string storedHash)
    {
        return HashPassword(input) == storedHash;
    }
}
