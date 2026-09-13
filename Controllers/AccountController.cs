using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FactorFitGym.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var hashedPassword = PasswordHasher.Hash(model.Password);
                    var usuario = await _context.Usuarios
                        .FirstOrDefaultAsync(u => u.Username == model.Username && (u.PasswordHash == model.Password || u.PasswordHash == hashedPassword));

                    if (usuario != null)
                    {
                        var rol = usuario.Rol?.Trim() ?? "Administrador";
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, usuario.Username),
                            new Claim("NombreCompleto", $"{usuario.Nombre} {usuario.Apellido}"),
                            new Claim(ClaimTypes.Role, rol),
                            new Claim("role", rol)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        if (usuario.Rol == "Cliente")
                        {
                            return RedirectToAction("Index", "Asistencias");
                        }

                        return RedirectToAction("Index", "Home");
                    }

                    ModelState.AddModelError(string.Empty, "Intento de inicio de sesión no válido. Verifica usuario y contraseña.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error de base de datos: {ex.Message}");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}