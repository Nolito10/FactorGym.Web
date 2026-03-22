using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            var inventario = await _context.Productos.OrderBy(p => p.Nombre).ToListAsync();
            return View(inventario);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Categoria,Precio,Stock,StockMinimo")] Producto producto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Producto '{producto.Nombre}' registrado correctamente en el inventario.";
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Categoria,Precio,Stock,StockMinimo")] Producto producto)
        {
            if (id != producto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Datos del producto '{producto.Nombre}' actualizados.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                // Verify it hasn't been sold before deleting. If it has, usually we soft-delete or throw error.
                // Assuming EF Core will throw a DbUpdateException if there's a foreign key constraint in DetallesVenta.
                try
                {
                    _context.Productos.Remove(producto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "El producto ha sido eliminado del inventario.";
                }
                catch(DbUpdateException)
                {
                    TempData["ErrorMessage"] = "No puedes eliminar este producto porque ya está vinculado a registros de ventas históricas. Intenta actualizar su stock a 0.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}
