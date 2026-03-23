using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using FactorFitGym.Web.Models;

namespace FactorGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reportes
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarFinanzasExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaInicio > fechaFin || fechaInicio == DateTime.MinValue || fechaFin == DateTime.MinValue)
            {
                TempData["ErrorMessage"] = "Rango de fechas inválido.";
                return RedirectToAction(nameof(Index));
            }

            // Normalize end date to end of day
            var endOfDay = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Fetch data
            var pagos = await _context.Pagos.Include(p => p.Cliente)
                .Where(p => p.FechaPago >= fechaInicio.Date && p.FechaPago <= endOfDay).ToListAsync();
            var ventas = await _context.Ventas
                .Where(v => v.Fecha >= fechaInicio.Date && v.Fecha <= endOfDay).ToListAsync();
            var gastos = await _context.Gastos
                .Where(g => g.Fecha >= fechaInicio.Date && g.Fecha <= endOfDay).ToListAsync();

            using var workbook = new XLWorkbook();
            
            // Sheet 1: Resumen
            var wsResumen = workbook.Worksheets.Add("Resumen Financiero");
            wsResumen.Cell(1, 1).Value = "REPORTE FINANCIERO";
            wsResumen.Cell(1, 1).Style.Font.Bold = true;
            wsResumen.Cell(1, 1).Style.Font.FontSize = 14;
            wsResumen.Cell(2, 1).Value = $"Período: {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}";
            
            decimal totalPagos = pagos.Sum(p => p.Monto);
            decimal totalVentas = ventas.Sum(v => v.Total);
            decimal totalEgresos = gastos.Sum(g => g.Monto);
            decimal utilidad = (totalPagos + totalVentas) - totalEgresos;

            wsResumen.Cell(4, 1).Value = "Categoría";
            wsResumen.Cell(4, 2).Value = "Monto (CRC)";
            wsResumen.Range("A4:B4").Style.Font.Bold = true;
            wsResumen.Range("A4:B4").Style.Fill.BackgroundColor = XLColor.AirForceBlue;
            wsResumen.Range("A4:B4").Style.Font.FontColor = XLColor.White;

            wsResumen.Cell(5, 1).Value = "Ingresos por Membresías";
            wsResumen.Cell(5, 2).Value = totalPagos;
            wsResumen.Cell(6, 1).Value = "Ingresos por Tienda (Ventas)";
            wsResumen.Cell(6, 2).Value = totalVentas;
            wsResumen.Cell(7, 1).Value = "Total Ingresos";
            wsResumen.Cell(7, 2).Value = totalPagos + totalVentas;
            wsResumen.Cell(7, 1).Style.Font.Bold = true;
            wsResumen.Cell(7, 2).Style.Font.Bold = true;

            wsResumen.Cell(9, 1).Value = "Total Egresos (Gastos)";
            wsResumen.Cell(9, 2).Value = totalEgresos;
            wsResumen.Cell(9, 1).Style.Font.FontColor = XLColor.Red;
            wsResumen.Cell(9, 2).Style.Font.FontColor = XLColor.Red;

            wsResumen.Cell(11, 1).Value = "UTILIDAD NETA";
            wsResumen.Cell(11, 2).Value = utilidad;
            wsResumen.Range("A11:B11").Style.Font.Bold = true;
            wsResumen.Range("A11:B11").Style.Fill.BackgroundColor = utilidad >= 0 ? XLColor.AppleGreen : XLColor.CandyAppleRed;
            wsResumen.Range("A11:B11").Style.Font.FontColor = XLColor.White;
            wsResumen.Columns().AdjustToContents();

            // Sheet 2: Detalle Ingresos
            var wsIngresos = workbook.Worksheets.Add("Detalle Membresías");
            wsIngresos.Cell(1, 1).Value = "Fecha";
            wsIngresos.Cell(1, 2).Value = "Cliente";
            wsIngresos.Cell(1, 3).Value = "Servicio";
            wsIngresos.Cell(1, 4).Value = "Método Pago";
            wsIngresos.Cell(1, 5).Value = "Monto";
            wsIngresos.Range("A1:E1").Style.Font.Bold = true;
            wsIngresos.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var p in pagos.OrderBy(p => p.FechaPago))
            {
                wsIngresos.Cell(row, 1).Value = p.FechaPago.ToString("dd/MM/yyyy HH:mm");
                wsIngresos.Cell(row, 2).Value = $"{p.Cliente?.Nombre} {p.Cliente?.Apellido}";
                wsIngresos.Cell(row, 3).Value = p.TipoServicio;
                wsIngresos.Cell(row, 4).Value = p.MetodoPago;
                wsIngresos.Cell(row, 5).Value = p.Monto;
                row++;
            }
            wsIngresos.Columns().AdjustToContents();

            // Sheet 3: Detalle Gastos
            var wsGastos = workbook.Worksheets.Add("Detalle Egresos");
            wsGastos.Cell(1, 1).Value = "Fecha";
            wsGastos.Cell(1, 2).Value = "Concepto";
            wsGastos.Cell(1, 3).Value = "Categoría";
            wsGastos.Cell(1, 4).Value = "Monto";
            wsGastos.Range("A1:D1").Style.Font.Bold = true;
            wsGastos.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.LightGray;
            row = 2;
            foreach (var g in gastos.OrderBy(g => g.Fecha))
            {
                wsGastos.Cell(row, 1).Value = g.Fecha.ToString("dd/MM/yyyy");
                wsGastos.Cell(row, 2).Value = g.Concepto;
                wsGastos.Cell(row, 3).Value = g.Categoria;
                wsGastos.Cell(row, 4).Value = g.Monto;
                row++;
            }
            wsGastos.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string excelName = $"ReporteFinanciero_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarAfluenciaExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaInicio > fechaFin || fechaInicio == DateTime.MinValue || fechaFin == DateTime.MinValue)
            {
                TempData["ErrorMessage"] = "Rango de fechas inválido.";
                return RedirectToAction(nameof(Index));
            }

            var endOfDay = fechaFin.Date.AddDays(1).AddTicks(-1);
            var asistencias = await _context.Asistencias.Include(a => a.Cliente)
                .Where(a => a.FechaHoraEntrada >= fechaInicio.Date && a.FechaHoraEntrada <= endOfDay)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Afluencia y Asistencias");
            
            ws.Cell(1, 1).Value = "REPORTE DE AFLUENCIA (CHECK-INS)";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Período: {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}";

            // Tabla de Registros
            ws.Cell(4, 1).Value = "Fecha";
            ws.Cell(4, 2).Value = "Día Sem.";
            ws.Cell(4, 3).Value = "Hora Entr.";
            ws.Cell(4, 4).Value = "Cliente";
            ws.Range("A4:D4").Style.Font.Bold = true;
            ws.Range("A4:D4").Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 5;
            foreach (var a in asistencias.OrderBy(a => a.FechaHoraEntrada))
            {
                ws.Cell(row, 1).Value = a.FechaHoraEntrada.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = a.FechaHoraEntrada.ToString("dddd");
                ws.Cell(row, 3).Value = a.FechaHoraEntrada.ToString("HH:mm");
                ws.Cell(row, 4).Value = $"{a.Cliente?.Nombre} {a.Cliente?.Apellido}";
                row++;
            }
            ws.Columns().AdjustToContents();

            // Análisis simple por hora
            var analisisWs = workbook.Worksheets.Add("Análisis Horas Pico");
            analisisWs.Cell(1, 1).Value = "Agrupación por Hora del Día";
            analisisWs.Cell(1, 1).Style.Font.Bold = true;
            analisisWs.Cell(3, 1).Value = "Hora (Formato 24h)";
            analisisWs.Cell(3, 2).Value = "Cantidad de Visitas";
            analisisWs.Range("A3:B3").Style.Font.Bold = true;

            var agrupadoHora = asistencias.GroupBy(a => a.FechaHoraEntrada.Hour)
                                          .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                                          .OrderByDescending(g => g.Cantidad).ToList();
            int aRow = 4;
            foreach (var h in agrupadoHora)
            {
                analisisWs.Cell(aRow, 1).Value = $"{h.Hora}:00 - {h.Hora}:59";
                analisisWs.Cell(aRow, 2).Value = h.Cantidad;
                aRow++;
            }
            analisisWs.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string excelName = $"ReporteAfluencia_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarTiendaExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaInicio > fechaFin || fechaInicio == DateTime.MinValue || fechaFin == DateTime.MinValue)
            {
                TempData["ErrorMessage"] = "Rango de fechas inválido.";
                return RedirectToAction(nameof(Index));
            }

            var endOfDay = fechaFin.Date.AddDays(1).AddTicks(-1);
            
            // Cargar DetallesVenta conectados a una Venta que cae en la fecha
            var detalles = await _context.DetallesVenta
                .Include(d => d.Venta)
                .Include(d => d.Producto)
                .Where(d => d.Venta.Fecha >= fechaInicio.Date && d.Venta.Fecha <= endOfDay)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Rendimiento de Tienda");
            
            ws.Cell(1, 1).Value = "REPORTE DE VENTAS (PRODUCTOS)";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Value = $"Período: {fechaInicio:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}";

            ws.Cell(4, 1).Value = "Producto";
            ws.Cell(4, 2).Value = "Categoría";
            ws.Cell(4, 3).Value = "Cant. Vendida";
            ws.Cell(4, 4).Value = "Ingreso Bruto Generado";
            ws.Range("A4:D4").Style.Font.Bold = true;
            ws.Range("A4:D4").Style.Fill.BackgroundColor = XLColor.LightGray;

            var agrupado = detalles.GroupBy(d => d.Producto)
                .Select(g => new {
                    Producto = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad),
                    TotalGenerado = g.Sum(x => x.Cantidad * x.PrecioUnitario)
                })
                .OrderByDescending(x => x.TotalGenerado).ToList();

            int row = 5;
            foreach (var item in agrupado)
            {
                ws.Cell(row, 1).Value = item.Producto?.Nombre ?? "Desc.";
                ws.Cell(row, 2).Value = item.Producto?.Categoria ?? "-";
                ws.Cell(row, 3).Value = item.Cantidad;
                ws.Cell(row, 4).Value = item.TotalGenerado;
                row++;
            }
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string excelName = $"ReporteTienda_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }
    }
}
