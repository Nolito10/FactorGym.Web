using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VentasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Ventas (Historial de Tickets)
        public async Task<IActionResult> Index()
        {
            var historial = await _context.Ventas
                .Include(v => v.Cliente)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();
            return View(historial);
        }

        // GET: Ventas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (venta == null) return NotFound();

            return View(venta);
        }

        // GET: Ventas/Create (Punto de Venta)
        public async Task<IActionResult> Create()
        {
            // Solo mandamos productos que tengan stock disponible
            ViewBag.Productos = await _context.Productos
                .Where(p => p.Stock > 0)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View();
        }

        // Modelo auxiliar para recibir los datos por AJAX/JSON desde el frontend
        public class NuevaVentaDto
        {
            public int? ClienteId { get; set; }
            public string MetodoPago { get; set; }
            public List<DetalleItemDto> Articulos { get; set; }
        }

        public class DetalleItemDto
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
        }

        // POST: Ventas/ProcesarVenta (API Endpoint para guardar el carrito)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarVenta([FromBody] NuevaVentaDto dto)
        {
            if (dto.Articulos == null || !dto.Articulos.Any())
            {
                return Json(new { success = false, message = "El carrito está vacío." });
            }

            if (string.IsNullOrEmpty(dto.MetodoPago))
            {
                return Json(new { success = false, message = "Debes seleccionar un método de pago." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Crear la Venta base
                var venta = new Venta
                {
                    ClienteId = dto.ClienteId,
                    Fecha = DateTime.Now,
                    MetodoPago = dto.MetodoPago,
                    NumeroRecibo = GenerarNumeroRecibo(),
                    Total = 0 // Se calculará sumando los detalles
                };
                
                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync(); // Para obtener el Venta.Id

                decimal totalVenta = 0;

                foreach (var item in dto.Articulos)
                {
                    var productoDb = await _context.Productos.FindAsync(item.ProductoId);
                    if (productoDb == null) continue;

                    if (productoDb.Stock < item.Cantidad)
                    {
                        return Json(new { success = false, message = $"Stock insuficiente para el producto {productoDb.Nombre}." });
                    }

                    // Calcular total y crear el detalle
                    decimal subtotal = productoDb.Precio * item.Cantidad;
                    totalVenta += subtotal;

                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = productoDb.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = productoDb.Precio
                    };
                    _context.DetallesVenta.Add(detalle);

                    // Descontar del inventario
                    productoDb.Stock -= item.Cantidad;
                    _context.Update(productoDb);
                }

                // Finalizar el total de la venta
                venta.Total = totalVenta;
                _context.Update(venta);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, ventaId = venta.Id, message = "¡Venta completada con éxito!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Ocurrió un error al procesar la venta: " + ex.Message });
            }
        }

        private string GenerarNumeroRecibo()
        {
            return "TK-" + DateTime.Now.ToString("yyMMdd") + new Random().Next(1000, 9999).ToString();
        }
    }
}
