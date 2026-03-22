using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class GastosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GastosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Gastos
        public async Task<IActionResult> Index()
        {
            var gastos = await _context.Gastos.OrderByDescending(g => g.Fecha).ToListAsync();
            return View(gastos);
        }

        // GET: Gastos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Gastos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Concepto,Categoria,Monto,Fecha,Referencia")] Gasto gasto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gasto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Gasto operativo registrado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(gasto);
        }

        // POST: Gastos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var gasto = await _context.Gastos.FindAsync(id);
            if (gasto != null)
            {
                _context.Gastos.Remove(gasto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "El registro del gasto ha sido eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
