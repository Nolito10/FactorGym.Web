using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Soporte para proxy HTTPS en Railway / Render
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Soporte para puerto en la nube (Railway / Render)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Configuración regional fija (Nicaragua): C$, punto decimal (.) y coma de miles (,)
var defaultCulture = new System.Globalization.CultureInfo("es-NI");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.NumberGroupSeparator = ",";
defaultCulture.NumberFormat.CurrencyDecimalSeparator = ".";
defaultCulture.NumberFormat.CurrencyGroupSeparator = ",";
defaultCulture.NumberFormat.CurrencySymbol = "C$";

System.Globalization.CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture);
    options.SupportedCultures = new[] { defaultCulture };
    options.SupportedUICultures = new[] { defaultCulture };
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRequestLocalization();

// Aplicar migraciones y crear usuario inicial al arrancar (protegido contra crash)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();

        // Corregir reservaciones que se guardaron infladas (ej. 250000 en vez de 2500) por el formato anterior
        var reservacionesErroneas = db.ReservacionesCanchas.Where(r => r.MontoTotal >= 200000m).ToList();
        if (reservacionesErroneas.Any())
        {
            foreach (var r in reservacionesErroneas)
            {
                r.MontoTotal = Math.Round(r.MontoTotal / 100m, 2);
            }
            db.SaveChanges();
        }

        if (!db.Usuarios.Any())
        {
            db.Usuarios.Add(new Usuario
            {
                Username = "admin",
                Nombre = "Administrador",
                Apellido = "Sistema",
                Email = "admin@factorgym.com",
                PasswordHash = PasswordHasher.Hash("admin123"),
                Rol = "Administrador",
                FechaRegistro = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AVISO MIGRACIÓN]: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"[AVISO MIGRACIÓN DETALLE]: {ex.InnerException.Message}");
        }
    }
}

app.MapGet("/favicon.ico", () => Results.Redirect("/img/Logo.png"));

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();