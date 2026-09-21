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
                    new ConfiguracionPrecio 
                    { 
                        Clave = "PLAN_BASICO", 
                        Nombre = "Plan Básico (Mensualidad)", 
                        Precio = 20.00m,
                        Descripcion = "Acceso a sala de musculación y máquinas cardiovasculares en horario regular."
                    },
                    new ConfiguracionPrecio 
                    { 
                        Clave = "PLAN_PREMIUM", 
                        Nombre = "Plan Premium (Mensualidad)", 
                        Precio = 35.00m,
                        Descripcion = "Acceso completo a pesas, área funcional, cardio y casilleros personales."
                    },
                    new ConfiguracionPrecio 
                    { 
                        Clave = "PLAN_FULL", 
                        Nombre = "Plan Full Access (Mensualidad)", 
                        Precio = 50.00m,
                        Descripcion = "Acceso total ilimitado a instalaciones, clases grupales y beneficios VIP."
                    },
                    new ConfiguracionPrecio 
                    { 
                        Clave = "CANCHA_HORA", 
                        Nombre = "Reservación Cancha (Por Hora)", 
                        Precio = 20.00m,
                        Descripcion = "Tarifa de alquiler por hora para uso y juego exclusivo en la cancha deportiva."
                    }
                };
                
                await _context.ConfiguracionPrecios.AddRangeAsync(initialPrices);
                await _context.SaveChangesAsync();
            }

            var precios = await _context.ConfiguracionPrecios.ToListAsync();

            // Asignar descripciones por defecto si existen registros sin descripción
            bool huboCambios = false;
            foreach (var precio in precios)
            {
                if (string.IsNullOrWhiteSpace(precio.Descripcion))
                {
                    precio.Descripcion = ObtenerDescripcionPorDefecto(precio.Clave);
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                await _context.SaveChangesAsync();
            }

            return View(precios);
        }

        // GET: ConfiguracionPrecios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var precio = await _context.ConfiguracionPrecios.FindAsync(id);
            if (precio == null) return NotFound();

            if (string.IsNullOrWhiteSpace(precio.Descripcion))
            {
                precio.Descripcion = ObtenerDescripcionPorDefecto(precio.Clave);
            }

            return View(precio);
        }

        // POST: ConfiguracionPrecios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Clave,Nombre,Precio,Descripcion")] ConfiguracionPrecio configuracionPrecio)
        {
            if (id != configuracionPrecio.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(configuracionPrecio);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Precio y descripción actualizados correctamente.";
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

        private static string ObtenerDescripcionPorDefecto(string clave)
        {
            return clave switch
            {
                "PLAN_BASICO" => "Acceso a sala de musculación y máquinas cardiovasculares en horario regular.",
                "PLAN_PREMIUM" => "Acceso completo a pesas, área funcional, cardio y casilleros personales.",
                "PLAN_FULL" => "Acceso total ilimitado a instalaciones, clases grupales y beneficios VIP.",
                "CANCHA_HORA" => "Tarifa de alquiler por hora para uso y juego exclusivo en la cancha deportiva.",
                _ => "Tarifa de servicio configurada para el gimnasio."
            };
        }

        private bool ConfiguracionPrecioExists(int id)
        {
            return _context.ConfiguracionPrecios.Any(e => e.Id == id);
        }
    }
}
