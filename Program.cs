using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Soporte para puerto dinámico en plataformas cloud (Railway, Render, Fly.io, etc.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 1. Soporte si Railway inyectó variables individuales (MYSQLHOST, MYSQLUSER, etc.)
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");
if (!string.IsNullOrEmpty(mysqlHost))
{
    var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
    var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER") ?? "root";
    var mysqlPass = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? "";
    var mysqlDb = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "railway";
    connectionString = $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDb};User={mysqlUser};Password={mysqlPass};AllowPublicKeyRetrieval=True;SslMode=Preferred;";
}
else
{
    // 2. Soporte si Railway inyectó una URL completa (MYSQL_PRIVATE_URL, MYSQL_URL, DATABASE_URL)
    var envDbUrl = Environment.GetEnvironmentVariable("MYSQL_PRIVATE_URL")
                ?? Environment.GetEnvironmentVariable("MYSQL_URL")
                ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(envDbUrl) && (string.IsNullOrEmpty(connectionString) || connectionString.Contains("127.0.0.1") || connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase)))
    {
        connectionString = envDbUrl;
    }
}

// 3. Convertir automáticamente de formato URL (mysql://user:pass@host:port/db) al formato que espera MySQL Pomelo
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var dbPort = uri.Port > 0 ? uri.Port : 3306;
        var database = uri.AbsolutePath.TrimStart('/');

        connectionString = $"Server={host};Port={dbPort};Database={database};User={user};Password={password};AllowPublicKeyRetrieval=True;SslMode=Preferred;";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parseando URL de base de datos: {ex.Message}");
    }
}

ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch
{
    serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

var app = builder.Build();

// Endpoint de diagnóstico para aplicar migraciones desde el navegador si es necesario
app.MapGet("/_migrate", async (ApplicationDbContext db) =>
{
    try
    {
        var conn = db.Database.GetDbConnection();
        await db.Database.MigrateAsync();

        if (!await db.Usuarios.AnyAsync())
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
            await db.SaveChangesAsync();
        }

        return Results.Ok(new 
        { 
            status = "success", 
            message = "¡Migraciones aplicadas con éxito! Tablas y usuario inicial (admin / admin123) creados en MySQL.",
            hostConectado = conn.DataSource,
            baseDeDatos = conn.Database
        });
    }
    catch (Exception ex)
    {
        var conn = db.Database.GetDbConnection();
        return Results.Problem(detail: $"{ex.Message} -> {ex.InnerException?.Message} | Host intentado: {conn.DataSource}, DB: {conn.Database}", title: "Error al migrar");
    }
});

// Aplicar migraciones de base de datos automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Aplicando migraciones de base de datos en inicio...");
        context.Database.Migrate();
        logger.LogInformation("Migraciones aplicadas exitosamente.");

        if (!context.Usuarios.Any())
        {
            context.Usuarios.Add(new Usuario
            {
                Username = "admin",
                Nombre = "Administrador",
                Apellido = "Sistema",
                Email = "admin@factorgym.com",
                PasswordHash = PasswordHasher.Hash("admin123"),
                Rol = "Administrador",
                FechaRegistro = DateTime.UtcNow
            });
            context.SaveChanges();
            logger.LogInformation("Usuario inicial 'admin' creado exitosamente.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Aviso: No se pudieron aplicar migraciones automáticas al inicio: {Message}", ex.Message);
    }
}

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