using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class MembresiasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MembresiasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Membresias
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Membresias.Include(m => m.Cliente).Include(m => m.Pagos)
                .OrderByDescending(m => m.FechaInicio);

            // Auto-inactivate expired
            var allActive = await _context.Membresias.Where(m => m.Estado == "Activa" && m.FechaFin < DateTime.Now).ToListAsync();
            bool updated = false;
            foreach (var m in allActive) { m.Estado = "Vencida"; updated = true; }
            if(updated) await _context.SaveChangesAsync();

            int total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalRecords = total;

            ViewBag.ClientesSinMembresia = _context.Clientes
                .Count(c => c.IsActivo && !c.Membresias.Any(m => m.Estado == "Activa"));

            return View(items);
        }


        // GET: Membresias/Create
        public IActionResult Create(int? clienteId)
        {
            var clientes = _context.Clientes
                .Where(c => c.IsActivo && (!c.Membresias.Any(m => m.Estado == "Activa") || c.Id == clienteId))
                .Select(c => new { c.Id, NombreCompleto = c.Nombre + " " + c.Apellido })
                .ToList();

            ViewBag.ClienteId = new SelectList(clientes, "Id", "NombreCompleto", clienteId);
            
            ViewBag.PrecioBasico = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "PLAN_BASICO")?.Precio ?? 20.00m;
            ViewBag.PrecioPremium = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "PLAN_PREMIUM")?.Precio ?? 35.00m;
            ViewBag.PrecioFull = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "PLAN_FULL")?.Precio ?? 50.00m;

            return View();
        }

        // POST: Membresias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClienteId,Tipo,TipoPlan,Frecuencia,Costo,FechaInicio,FechaFin")] Membresia membresia)
        {
            ModelState.Remove("Estado");
            
            // Valores por defecto para campos automáticos
            if (string.IsNullOrWhiteSpace(membresia.Tipo))
                membresia.Tipo = "Membresía";
            if (string.IsNullOrWhiteSpace(membresia.TipoPlan))
                membresia.TipoPlan = "Básico";
            if (string.IsNullOrWhiteSpace(membresia.Frecuencia))
                membresia.Frecuencia = "Mensual";

            // Recalcular costo con base en el Gestor de Precios
            string clavePrecio = membresia.TipoPlan switch
            {
                "Premium" => "PLAN_PREMIUM",
                "Full Access" => "PLAN_FULL",
                _ => "PLAN_BASICO"
            };

            decimal precioBase = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == clavePrecio)?.Precio ?? 20.00m;
            decimal costoCalculado = membresia.Frecuencia switch
            {
                "Semanal" => Math.Round(precioBase / 4m, 2),
                "Quincenal" => Math.Round(precioBase / 2m, 2),
                _ => precioBase // Mensual
            };
            membresia.Costo = costoCalculado;

            // Ajustar FechaFin si no fue definida o es menor
            if (membresia.FechaFin <= membresia.FechaInicio)
            {
                membresia.FechaFin = membresia.Frecuencia switch
                {
                    "Semanal" => membresia.FechaInicio.AddDays(7),
                    "Quincenal" => membresia.FechaInicio.AddDays(15),
                    _ => membresia.FechaInicio.AddMonths(1)
                };
            }

            if (ModelState.IsValid)
            {
                membresia.Estado = "Activa";

                _context.Add(membresia);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"¡Membresía ({membresia.TipoPlan} - {membresia.Frecuencia}) asignada exitosamente al cliente!";
                return RedirectToAction(nameof(Index));
            }

            var clientes = _context.Clientes.Where(c => c.IsActivo)
                .Select(c => new { c.Id, NombreCompleto = c.Nombre + " " + c.Apellido })
                .ToList();
            ViewBag.ClienteId = new SelectList(clientes, "Id", "NombreCompleto", membresia.ClienteId);
            ViewBag.PrecioBasico = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "PLAN_BASICO")?.Precio ?? 20.00m;
            ViewBag.PrecioPremium = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "PLAN_PREMIUM")?.Precio ?? 35.00m;
            ViewBag.PrecioFull = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "PLAN_FULL")?.Precio ?? 50.00m;
            
            return View(membresia);
        }

        // GET: Membresias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var membresia = await _context.Membresias.FindAsync(id);
            if (membresia == null) return NotFound();

            var clientes = _context.Clientes.Where(c => c.IsActivo)
                .Select(c => new { c.Id, NombreCompleto = c.Nombre + " " + c.Apellido })
                .ToList();
            ViewBag.ClienteId = new SelectList(clientes, "Id", "NombreCompleto", membresia.ClienteId);
            
            return View(membresia);
        }

        // POST: Membresias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClienteId,Tipo,TipoPlan,Frecuencia,Costo,FechaInicio,FechaFin,Estado")] Membresia membresia)
        {
            if (id != membresia.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(membresia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MembresiaExists(membresia.Id)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Membresía actualizada / renovada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            var clientes = _context.Clientes.Where(c => c.IsActivo)
                .Select(c => new { c.Id, NombreCompleto = c.Nombre + " " + c.Apellido })
                .ToList();
            ViewBag.ClienteId = new SelectList(clientes, "Id", "NombreCompleto", membresia.ClienteId);
            return View(membresia);
        }

        private bool MembresiaExists(int id)
        {
            return _context.Membresias.Any(e => e.Id == id);
        }

        // POST: Membresias/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var membresia = await _context.Membresias.FindAsync(id);
            if (membresia != null)
            {
                _context.Membresias.Remove(membresia);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Membresía rectificada/eliminada correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
