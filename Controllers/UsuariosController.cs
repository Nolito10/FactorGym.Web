using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios.OrderByDescending(u => u.FechaRegistro).ToListAsync();
            return View(usuarios);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Nombre,Apellido,Email,PasswordHash,Rol")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate username
                if (await _context.Usuarios.AnyAsync(u => u.Username == usuario.Username))
                {
                    ModelState.AddModelError("Username", "El nombre de usuario ya está en uso.");
                    return View(usuario);
                }

                // Check for duplicate email
                if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                {
                    ModelState.AddModelError("Email", "El correo electrónico ya está en uso.");
                    return View(usuario);
                }

                usuario.FechaRegistro = DateTime.Now;
                usuario.PasswordHash = PasswordHasher.Hash(usuario.PasswordHash);

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Usuario creado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Nombre,Apellido,Email,Rol")] Usuario usuarioInput, string newPassword)
        {
            if (id != usuarioInput.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (existingUser == null) return NotFound();

                    // Check for duplicate username
                    if (await _context.Usuarios.AnyAsync(u => u.Username == usuarioInput.Username && u.Id != id))
                    {
                        ModelState.AddModelError("Username", "El nombre de usuario ya está en uso por otra cuenta.");
                        return View(usuarioInput);
                    }

                    // Check for duplicate email
                    if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioInput.Email && u.Id != id))
                    {
                        ModelState.AddModelError("Email", "El correo electrónico ya está en uso por otra cuenta.");
                        return View(usuarioInput);
                    }

                    // Preserve existing fields not in the form
                    usuarioInput.FechaRegistro = existingUser.FechaRegistro;
                    
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        usuarioInput.PasswordHash = PasswordHasher.Hash(newPassword);
                    }
                    else
                    {
                        usuarioInput.PasswordHash = existingUser.PasswordHash;
                    }

                    _context.Update(usuarioInput);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Usuario actualizado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuarioInput.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(usuarioInput);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                // Prevent deleting the last admin
                if (usuario.Rol == "Administrador" && await _context.Usuarios.CountAsync(u => u.Rol == "Administrador") <= 1)
                {
                    return Json(new { success = false, message = "No puedes eliminar al único administrador del sistema." });
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Usuario eliminado correctamente." });
            }
            return Json(new { success = false, message = "Error al intentar eliminar el usuario." });
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}