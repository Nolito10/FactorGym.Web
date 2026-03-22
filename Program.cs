using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

app.MapGet("/_diag", (IWebHostEnvironment env) => new
{
    env.ContentRootPath,
    env.WebRootPath,
    WebRootExists = Directory.Exists(env.WebRootPath),
    HasLogo = System.IO.File.Exists(System.IO.Path.Combine(env.WebRootPath ?? "", "img", "Logo.png")),
    HasBootstrap = System.IO.File.Exists(System.IO.Path.Combine(env.WebRootPath ?? "", "css", "bootstrap", "css", "bootstrap.min.css"))
});

app.MapGet("/favicon.ico", () => Results.Redirect("/img/Logo.png"));

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();