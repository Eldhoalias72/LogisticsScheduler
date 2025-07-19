using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// --- Register services ---
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// 1. Configure Authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// 2. Add Authorization services
builder.Services.AddAuthorization();


var app = builder.Build();

// --- Configure the HTTP request pipeline (middleware) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Add the Authentication and Authorization middleware (order is critical)
app.UseAuthentication();
app.UseAuthorization();

// 4. Update the default route to point to the Login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();