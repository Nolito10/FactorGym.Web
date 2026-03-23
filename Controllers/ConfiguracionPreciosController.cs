using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;

namespace FactorGym.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ConfiguracionPreciosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracionPreciosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ConfiguracionPrecios
        public async Task<IActionResult> Index()
        {
            // Seed initial data if empty
            if (!await _context.ConfiguracionPrecios.AnyAsync())
            {
                var initialPrices = new[]
                {
                    new ConfiguracionPrecio { Clave = "PLAN_BASICO", Nombre = "Plan Básico (Mensualidad)", Precio = 20.00m },
                    new ConfiguracionPrecio { Clave = "PLAN_PREMIUM", Nombre = "Plan Premium (Mensualidad)", Precio = 35.00m },
                    new ConfiguracionPrecio { Clave = "PLAN_FULL", Nombre = "Plan Full Access (Mensualidad)", Precio = 50.00m },
                    new ConfiguracionPrecio { Clave = "CANCHA_HORA", Nombre = "Reservación Cancha (Por Hora)", Precio = 20.00m }
                };
                
                await _context.ConfiguracionPrecios.AddRangeAsync(initialPrices);
                await _context.SaveChangesAsync();
            }

            return View(await _context.ConfiguracionPrecios.ToListAsync());
        }

        // GET: ConfiguracionPrecios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var precio = await _context.ConfiguracionPrecios.FindAsync(id);
            if (precio == null) return NotFound();

            return View(precio);
        }

        // POST: ConfiguracionPrecios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Clave,Nombre,Precio")] ConfiguracionPrecio configuracionPrecio)
        {
            if (id != configuracionPrecio.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(configuracionPrecio);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Precio actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfiguracionPrecioExists(configuracionPrecio.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(configuracionPrecio);
        }

        private bool ConfiguracionPrecioExists(int id)
        {
            return _context.ConfiguracionPrecios.Any(e => e.Id == id);
        }
    }
}
