using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FactorGym.Web.Models;

namespace FactorGym.Web.Controllers;

[Authorize(Roles = "Administrador,Empleado")]
public class HomeController : Controller
{
    private readonly FactorFitGym.Web.Models.ApplicationDbContext _context;

    public HomeController(FactorFitGym.Web.Models.ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
        var today = now.Date;

        // Ingresos por Membresías (Pagos del mes)
        var ingresosPagos = await _context.Pagos
            .Where(p => p.FechaPago >= startOfMonth && p.FechaPago <= endOfMonth)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        // Ingresos por Tienda (Ventas del mes)
        var ingresosTienda = await _context.Ventas
            .Where(v => v.Fecha >= startOfMonth && v.Fecha <= endOfMonth)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;

        var ingresosTotales = ingresosPagos + ingresosTienda;

        // Egresos (Gastos del mes)
        var egresos = await _context.Gastos
            .Where(g => g.Fecha >= startOfMonth && g.Fecha <= endOfMonth)
            .SumAsync(g => (decimal?)g.Monto) ?? 0m;

        // Visitas de Hoy
        var visitasHoy = await _context.Asistencias
            .CountAsync(a => a.FechaHoraEntrada.Date == today);

        // Membresías Activas
        var membresiasActivas = await _context.Membresias
            .CountAsync(m => m.Estado == "Activa");

        // Productos bajo stock
        var productosBajoStock = await _context.Productos
            .CountAsync(p => p.Stock <= p.StockMinimo);

        ViewBag.IngresosTotales = ingresosTotales;
        ViewBag.EgresosTotales = egresos;
        ViewBag.UtilidadNeta = ingresosTotales - egresos;
        ViewBag.VisitasHoy = visitasHoy;
        ViewBag.MembresiasActivas = membresiasActivas;
        ViewBag.ProductosBajoStock = productosBajoStock;
        ViewBag.MesActual = now.ToString("MMMM yyyy").ToUpper();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
