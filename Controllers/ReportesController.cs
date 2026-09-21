using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

        #region Helpers de Estilo para ClosedXML
        private static void AplicarEstiloCabecera(IXLRange range, string colorHex = "#1B2B36")
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.FontSize = 11;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml(colorHex);
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = XLColor.FromArgb(180, 190, 200);
            range.Style.Border.InsideBorderColor = XLColor.FromArgb(180, 190, 200);
        }

        private static void AplicarEstiloFilaTotal(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 245, 248);
            range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            range.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            range.Style.Border.TopBorderColor = XLColor.FromArgb(100, 110, 120);
            range.Style.Border.BottomBorderColor = XLColor.FromArgb(100, 110, 120);
        }

        private static void CrearBanner(IXLWorksheet ws, string titulo, DateTime inicio, DateTime fin, int colFinal)
        {
            var rangeTitulo = ws.Range(1, 1, 1, colFinal);
            rangeTitulo.Merge();
            rangeTitulo.Value = titulo;
            rangeTitulo.Style.Font.Bold = true;
            rangeTitulo.Style.Font.FontSize = 15;
            rangeTitulo.Style.Font.FontColor = XLColor.White;
            rangeTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B2B36");
            rangeTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rangeTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 35;

            var rangeSub = ws.Range(2, 1, 2, colFinal);
            rangeSub.Merge();
            rangeSub.Value = $"Período evaluado: {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}  |  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            rangeSub.Style.Font.Italic = true;
            rangeSub.Style.Font.FontSize = 10;
            rangeSub.Style.Font.FontColor = XLColor.White;
            rangeSub.Style.Fill.BackgroundColor = XLColor.FromHtml("#2A3E4D");
            rangeSub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rangeSub.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 22;
        }
        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarFinanzasExcel(DateTime fechaInicio, DateTime fechaFin)
        {
            if (fechaInicio > fechaFin || fechaInicio == DateTime.MinValue || fechaFin == DateTime.MinValue)
            {
                TempData["ErrorMessage"] = "Rango de fechas inválido.";
                return RedirectToAction(nameof(Index));
            }

            var endOfDay = fechaFin.Date.AddDays(1).AddTicks(-1);

            // Consultas a base de datos
            var pagos = await _context.Pagos.Include(p => p.Cliente)
                .Where(p => p.FechaPago >= fechaInicio.Date && p.FechaPago <= endOfDay)
                .OrderBy(p => p.FechaPago)
                .ToListAsync();

            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.Fecha >= fechaInicio.Date && v.Fecha <= endOfDay)
                .OrderBy(v => v.Fecha)
                .ToListAsync();

            var gastos = await _context.Gastos
                .Where(g => g.Fecha >= fechaInicio.Date && g.Fecha <= endOfDay)
                .OrderBy(g => g.Fecha)
                .ToListAsync();

            var reservaciones = await _context.ReservacionesCanchas
                .Where(r => (r.FechaPago ?? r.FechaHoraInicio) >= fechaInicio.Date && (r.FechaPago ?? r.FechaHoraInicio) <= endOfDay)
                .OrderBy(r => r.FechaHoraInicio)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            // -------------------------------------------------------------
            // HOJA 1: RESUMEN FINANCIERO Y FLUJO DE EFECTIVO
            // -------------------------------------------------------------
            var wsResumen = workbook.Worksheets.Add("Resumen Financiero");
            CrearBanner(wsResumen, "FACTOR FIT GYM - REPORTE DE INGRESOS Y EGRESOS", fechaInicio, fechaFin, 4);

            decimal totalPagos = pagos.Sum(p => p.Monto);
            decimal totalVentas = ventas.Sum(v => v.Total);
            decimal totalCanchas = reservaciones.Sum(r => r.MontoTotal);
            decimal totalIngresos = totalPagos + totalVentas + totalCanchas;
            decimal totalEgresos = gastos.Sum(g => g.Monto);
            decimal utilidad = totalIngresos - totalEgresos;

            // 1. Desglose de Ingresos
            int curRow = 4;
            wsResumen.Cell(curRow, 1).Value = "1. DESGLOSE DE INGRESOS OPERATIVOS";
            wsResumen.Cell(curRow, 1).Style.Font.Bold = true;
            wsResumen.Cell(curRow, 1).Style.Font.FontSize = 12;
            wsResumen.Cell(curRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1B2B36");
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "Fuente de Ingreso";
            wsResumen.Cell(curRow, 2).Value = "Transacciones";
            wsResumen.Cell(curRow, 3).Value = "Monto Total (C$)";
            wsResumen.Cell(curRow, 4).Value = "% Participación";
            AplicarEstiloCabecera(wsResumen.Range(curRow, 1, curRow, 4), "#1B2B36");
            curRow++;

            // Filas de ingresos
            int filaIngresosInicio = curRow;

            wsResumen.Cell(curRow, 1).Value = "Membresías y Cuotas Gym";
            wsResumen.Cell(curRow, 2).Value = pagos.Count;
            wsResumen.Cell(curRow, 3).Value = totalPagos;
            wsResumen.Cell(curRow, 4).Value = totalIngresos > 0 ? (double)(totalPagos / totalIngresos) : 0;
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "Ventas en Tienda (Suplementos / Bebidas)";
            wsResumen.Cell(curRow, 2).Value = ventas.Count;
            wsResumen.Cell(curRow, 3).Value = totalVentas;
            wsResumen.Cell(curRow, 4).Value = totalIngresos > 0 ? (double)(totalVentas / totalIngresos) : 0;
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "Alquiler de Cancha y Eventos Exclusivos";
            wsResumen.Cell(curRow, 2).Value = reservaciones.Count;
            wsResumen.Cell(curRow, 3).Value = totalCanchas;
            wsResumen.Cell(curRow, 4).Value = totalIngresos > 0 ? (double)(totalCanchas / totalIngresos) : 0;
            curRow++;

            // Total Ingresos
            wsResumen.Cell(curRow, 1).Value = "TOTAL INGRESOS";
            wsResumen.Cell(curRow, 2).Value = pagos.Count + ventas.Count + reservaciones.Count;
            wsResumen.Cell(curRow, 3).Value = totalIngresos;
            wsResumen.Cell(curRow, 4).Value = 1.0;
            AplicarEstiloFilaTotal(wsResumen.Range(curRow, 1, curRow, 4));
            wsResumen.Cell(curRow, 3).Style.Font.FontColor = XLColor.FromHtml("#0A58CA");
            curRow += 2;

            // 2. Desglose de Egresos
            wsResumen.Cell(curRow, 1).Value = "2. DESGLOSE DE EGRESOS (GASTOS OPERATIVOS)";
            wsResumen.Cell(curRow, 1).Style.Font.Bold = true;
            wsResumen.Cell(curRow, 1).Style.Font.FontSize = 12;
            wsResumen.Cell(curRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1B2B36");
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "Categoría de Gasto";
            wsResumen.Cell(curRow, 2).Value = "N° Gastos";
            wsResumen.Cell(curRow, 3).Value = "Monto Total (C$)";
            wsResumen.Cell(curRow, 4).Value = "% Participación";
            AplicarEstiloCabecera(wsResumen.Range(curRow, 1, curRow, 4), "#B02A37");
            curRow++;

            var gastosPorCat = gastos
                .GroupBy(g => string.IsNullOrWhiteSpace(g.Categoria) ? "Varios / Otros" : g.Categoria.Trim())
                .Select(grp => new { Categoria = grp.Key, Total = grp.Sum(x => x.Monto), Cantidad = grp.Count() })
                .OrderByDescending(x => x.Total)
                .ToList();

            if (gastosPorCat.Any())
            {
                foreach (var gCat in gastosPorCat)
                {
                    wsResumen.Cell(curRow, 1).Value = gCat.Categoria;
                    wsResumen.Cell(curRow, 2).Value = gCat.Cantidad;
                    wsResumen.Cell(curRow, 3).Value = gCat.Total;
                    wsResumen.Cell(curRow, 4).Value = totalEgresos > 0 ? (double)(gCat.Total / totalEgresos) : 0;
                    curRow++;
                }
            }
            else
            {
                wsResumen.Cell(curRow, 1).Value = "Sin egresos registrados en este período";
                wsResumen.Cell(curRow, 2).Value = 0;
                wsResumen.Cell(curRow, 3).Value = 0;
                wsResumen.Cell(curRow, 4).Value = 0;
                curRow++;
            }

            // Total Egresos
            wsResumen.Cell(curRow, 1).Value = "TOTAL EGRESOS";
            wsResumen.Cell(curRow, 2).Value = gastos.Count;
            wsResumen.Cell(curRow, 3).Value = totalEgresos;
            wsResumen.Cell(curRow, 4).Value = 1.0;
            AplicarEstiloFilaTotal(wsResumen.Range(curRow, 1, curRow, 4));
            wsResumen.Cell(curRow, 3).Style.Font.FontColor = XLColor.FromHtml("#B02A37");
            curRow += 2;

            // 3. Balance General / Utilidad Neta
            wsResumen.Cell(curRow, 1).Value = "3. BALANCE Y RESULTADO OPERATIVO";
            wsResumen.Cell(curRow, 1).Style.Font.Bold = true;
            wsResumen.Cell(curRow, 1).Style.Font.FontSize = 12;
            wsResumen.Cell(curRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1B2B36");
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "Concepto";
            wsResumen.Cell(curRow, 2).Value = "Valor Financiero";
            wsResumen.Range(curRow, 2, curRow, 4).Merge();
            AplicarEstiloCabecera(wsResumen.Range(curRow, 1, curRow, 4), "#1B2B36");
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "(+) Total Ingresos Recibidos";
            wsResumen.Cell(curRow, 2).Value = totalIngresos;
            wsResumen.Cell(curRow, 2).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsResumen.Range(curRow, 2, curRow, 4).Merge();
            curRow++;

            wsResumen.Cell(curRow, 1).Value = "(-) Total Egresos Realizados";
            wsResumen.Cell(curRow, 2).Value = totalEgresos;
            wsResumen.Cell(curRow, 2).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsResumen.Cell(curRow, 2).Style.Font.FontColor = XLColor.FromHtml("#B02A37");
            wsResumen.Range(curRow, 2, curRow, 4).Merge();
            curRow++;

            // Fila Destacada: Utilidad Neta
            wsResumen.Cell(curRow, 1).Value = "(=) UTILIDAD NETA DEL PERÍODO";
            wsResumen.Cell(curRow, 2).Value = utilidad;
            wsResumen.Cell(curRow, 2).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsResumen.Range(curRow, 2, curRow, 4).Merge();
            var utilidadRange = wsResumen.Range(curRow, 1, curRow, 4);
            utilidadRange.Style.Font.Bold = true;
            utilidadRange.Style.Font.FontSize = 12;
            utilidadRange.Style.Font.FontColor = XLColor.White;
            utilidadRange.Style.Fill.BackgroundColor = utilidad >= 0 ? XLColor.FromHtml("#198754") : XLColor.FromHtml("#DC3545");
            curRow++;

            // Margen Operativo
            wsResumen.Cell(curRow, 1).Value = "Margen de Utilidad Operativa";
            wsResumen.Cell(curRow, 2).Value = totalIngresos > 0 ? (double)(utilidad / totalIngresos) : 0;
            wsResumen.Cell(curRow, 2).Style.NumberFormat.Format = "0.0%";
            wsResumen.Cell(curRow, 2).Style.Font.Bold = true;
            wsResumen.Range(curRow, 2, curRow, 4).Merge();
            wsResumen.Range(curRow, 1, curRow, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(245, 247, 250);

            // Formatear montos y porcentajes del resumen
            wsResumen.Column(3).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsResumen.Column(4).Style.NumberFormat.Format = "0.0%";
            wsResumen.Columns().AdjustToContents();

            // -------------------------------------------------------------
            // HOJA 2: DETALLE INGRESOS - MEMBRESÍAS
            // -------------------------------------------------------------
            var wsMembresias = workbook.Worksheets.Add("Ingresos - Membresías");
            CrearBanner(wsMembresias, "DETALLE DE INGRESOS: MEMBRESÍAS Y PLANES", fechaInicio, fechaFin, 6);

            int rMem = 4;
            wsMembresias.Cell(rMem, 1).Value = "Fecha y Hora";
            wsMembresias.Cell(rMem, 2).Value = "Cliente";
            wsMembresias.Cell(rMem, 3).Value = "Servicio / Plan";
            wsMembresias.Cell(rMem, 4).Value = "Método de Pago";
            wsMembresias.Cell(rMem, 5).Value = "Referencia / Recibo";
            wsMembresias.Cell(rMem, 6).Value = "Monto (C$)";
            AplicarEstiloCabecera(wsMembresias.Range(rMem, 1, rMem, 6));
            rMem++;

            int rMemInicio = rMem;
            foreach (var p in pagos)
            {
                wsMembresias.Cell(rMem, 1).Value = p.FechaPago.ToString("dd/MM/yyyy HH:mm");
                wsMembresias.Cell(rMem, 2).Value = $"{p.Cliente?.Nombre} {p.Cliente?.Apellido}".Trim();
                wsMembresias.Cell(rMem, 3).Value = p.TipoServicio ?? "Membresía";
                wsMembresias.Cell(rMem, 4).Value = p.MetodoPago ?? "Efectivo";
                wsMembresias.Cell(rMem, 5).Value = !string.IsNullOrEmpty(p.Referencia) ? p.Referencia : (!string.IsNullOrEmpty(p.NumeroRecibo) ? p.NumeroRecibo : "-");
                wsMembresias.Cell(rMem, 6).Value = p.Monto;
                rMem++;
            }

            // Fila Total Membresías
            wsMembresias.Cell(rMem, 1).Value = "TOTAL MEMBRESÍAS";
            wsMembresias.Range(rMem, 1, rMem, 5).Merge();
            wsMembresias.Cell(rMem, 6).Value = totalPagos;
            AplicarEstiloFilaTotal(wsMembresias.Range(rMem, 1, rMem, 6));
            wsMembresias.Column(6).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsMembresias.Columns().AdjustToContents();

            // -------------------------------------------------------------
            // HOJA 3: DETALLE INGRESOS - TIENDA (VENTAS)
            // -------------------------------------------------------------
            var wsTienda = workbook.Worksheets.Add("Ingresos - Tienda");
            CrearBanner(wsTienda, "DETALLE DE INGRESOS: VENTAS EN TIENDA", fechaInicio, fechaFin, 6);

            int rVta = 4;
            wsTienda.Cell(rVta, 1).Value = "Fecha y Hora";
            wsTienda.Cell(rVta, 2).Value = "N° Ticket";
            wsTienda.Cell(rVta, 3).Value = "Cliente";
            wsTienda.Cell(rVta, 4).Value = "Método de Pago";
            wsTienda.Cell(rVta, 5).Value = "Artículos Vendidos";
            wsTienda.Cell(rVta, 6).Value = "Total (C$)";
            AplicarEstiloCabecera(wsTienda.Range(rVta, 1, rVta, 6));
            rVta++;

            foreach (var v in ventas)
            {
                var resumenItems = v.DetallesVenta != null && v.DetallesVenta.Any()
                    ? string.Join(", ", v.DetallesVenta.Select(d => $"{d.Cantidad}x {d.Producto?.Nombre ?? "Prod"}"))
                    : "Productos varios";

                wsTienda.Cell(rVta, 1).Value = v.Fecha.ToString("dd/MM/yyyy HH:mm");
                wsTienda.Cell(rVta, 2).Value = v.NumeroRecibo ?? $"-{v.Id}";
                wsTienda.Cell(rVta, 3).Value = v.Cliente != null ? $"{v.Cliente.Nombre} {v.Cliente.Apellido}".Trim() : "Cliente Ocasional";
                wsTienda.Cell(rVta, 4).Value = v.MetodoPago ?? "Efectivo";
                wsTienda.Cell(rVta, 5).Value = resumenItems;
                wsTienda.Cell(rVta, 6).Value = v.Total;
                rVta++;
            }

            // Fila Total Ventas
            wsTienda.Cell(rVta, 1).Value = "TOTAL VENTAS TIENDA";
            wsTienda.Range(rVta, 1, rVta, 5).Merge();
            wsTienda.Cell(rVta, 6).Value = totalVentas;
            AplicarEstiloFilaTotal(wsTienda.Range(rVta, 1, rVta, 6));
            wsTienda.Column(6).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsTienda.Columns().AdjustToContents();

            // -------------------------------------------------------------
            // HOJA 4: DETALLE INGRESOS - CANCHAS
            // -------------------------------------------------------------
            var wsCanchas = workbook.Worksheets.Add("Ingresos - Canchas");
            CrearBanner(wsCanchas, "DETALLE DE INGRESOS: CANCHAS Y EVENTOS", fechaInicio, fechaFin, 7);

            int rCan = 4;
            wsCanchas.Cell(rCan, 1).Value = "Fecha y Hora Inicio";
            wsCanchas.Cell(rCan, 2).Value = "Fecha y Hora Fin";
            wsCanchas.Cell(rCan, 3).Value = "Cliente Responsable";
            wsCanchas.Cell(rCan, 4).Value = "Uso / Deporte";
            wsCanchas.Cell(rCan, 5).Value = "Método de Pago";
            wsCanchas.Cell(rCan, 6).Value = "N° Recibo";
            wsCanchas.Cell(rCan, 7).Value = "Monto Total (C$)";
            AplicarEstiloCabecera(wsCanchas.Range(rCan, 1, rCan, 7));
            rCan++;

            foreach (var r in reservaciones)
            {
                wsCanchas.Cell(rCan, 1).Value = r.FechaHoraInicio.ToString("dd/MM/yyyy HH:mm");
                wsCanchas.Cell(rCan, 2).Value = r.FechaHoraFin.HasValue ? r.FechaHoraFin.Value.ToString("dd/MM/yyyy HH:mm") : "-";
                wsCanchas.Cell(rCan, 3).Value = r.NombreCliente;
                wsCanchas.Cell(rCan, 4).Value = r.TipoUso;
                wsCanchas.Cell(rCan, 5).Value = r.MetodoPago ?? "Efectivo";
                wsCanchas.Cell(rCan, 6).Value = r.NumeroRecibo ?? "-";
                wsCanchas.Cell(rCan, 7).Value = r.MontoTotal;
                rCan++;
            }

            // Fila Total Canchas
            wsCanchas.Cell(rCan, 1).Value = "TOTAL RESERVACIONES CANCHAS";
            wsCanchas.Range(rCan, 1, rCan, 6).Merge();
            wsCanchas.Cell(rCan, 7).Value = totalCanchas;
            AplicarEstiloFilaTotal(wsCanchas.Range(rCan, 1, rCan, 7));
            wsCanchas.Column(7).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsCanchas.Columns().AdjustToContents();

            // -------------------------------------------------------------
            // HOJA 5: DETALLE EGRESOS - GASTOS
            // -------------------------------------------------------------
            var wsGastos = workbook.Worksheets.Add("Detalle Egresos");
            CrearBanner(wsGastos, "DETALLE DE EGRESOS: GASTOS OPERATIVOS", fechaInicio, fechaFin, 5);

            int rGas = 4;
            wsGastos.Cell(rGas, 1).Value = "Fecha";
            wsGastos.Cell(rGas, 2).Value = "Categoría";
            wsGastos.Cell(rGas, 3).Value = "Concepto";
            wsGastos.Cell(rGas, 4).Value = "Referencia / Factura";
            wsGastos.Cell(rGas, 5).Value = "Monto (C$)";
            AplicarEstiloCabecera(wsGastos.Range(rGas, 1, rGas, 5), "#B02A37");
            rGas++;

            foreach (var g in gastos)
            {
                wsGastos.Cell(rGas, 1).Value = g.Fecha.ToString("dd/MM/yyyy");
                wsGastos.Cell(rGas, 2).Value = g.Categoria;
                wsGastos.Cell(rGas, 3).Value = g.Concepto;
                wsGastos.Cell(rGas, 4).Value = !string.IsNullOrEmpty(g.Referencia) ? g.Referencia : "-";
                wsGastos.Cell(rGas, 5).Value = g.Monto;
                rGas++;
            }

            // Fila Total Gastos
            wsGastos.Cell(rGas, 1).Value = "TOTAL EGRESOS OPERATIVOS";
            wsGastos.Range(rGas, 1, rGas, 4).Merge();
            wsGastos.Cell(rGas, 5).Value = totalEgresos;
            AplicarEstiloFilaTotal(wsGastos.Range(rGas, 1, rGas, 5));
            wsGastos.Cell(rGas, 5).Style.Font.FontColor = XLColor.FromHtml("#B02A37");
            wsGastos.Column(5).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsGastos.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string excelName = $"ReporteFinanciero_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
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
                .OrderBy(a => a.FechaHoraEntrada)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            // Sheet 1: Resumen de Afluencia
            var wsResumen = workbook.Worksheets.Add("Resumen de Afluencia");
            CrearBanner(wsResumen, "FACTOR FIT GYM - REPORTE DE AFLUENCIA", fechaInicio, fechaFin, 3);

            int diasPeriodo = Math.Max(1, (int)(fechaFin.Date - fechaInicio.Date).TotalDays + 1);
            double promedioDiario = asistencias.Count > 0 ? (double)asistencias.Count / diasPeriodo : 0;

            wsResumen.Cell(4, 1).Value = "Indicador de Afluencia";
            wsResumen.Cell(4, 2).Value = "Valor";
            wsResumen.Range(4, 2, 4, 3).Merge();
            AplicarEstiloCabecera(wsResumen.Range(4, 1, 4, 3));

            wsResumen.Cell(5, 1).Value = "Total Check-ins Registrados";
            wsResumen.Cell(5, 2).Value = asistencias.Count;
            wsResumen.Range(5, 2, 5, 3).Merge();

            wsResumen.Cell(6, 1).Value = "Días del Período";
            wsResumen.Cell(6, 2).Value = diasPeriodo;
            wsResumen.Range(6, 2, 6, 3).Merge();

            wsResumen.Cell(7, 1).Value = "Promedio de Visitas Diarias";
            wsResumen.Cell(7, 2).Value = Math.Round(promedioDiario, 1);
            wsResumen.Range(7, 2, 7, 3).Merge();

            // Hora Pico
            var horaPico = asistencias.GroupBy(a => a.FechaHoraEntrada.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            wsResumen.Cell(8, 1).Value = "Hora Pico con Mayor Ocupación";
            wsResumen.Cell(8, 2).Value = horaPico != null ? $"{horaPico.Key}:00 - {horaPico.Key}:59 ({horaPico.Count()} visitas)" : "N/D";
            wsResumen.Range(8, 2, 8, 3).Merge();

            // Día más concurrido
            var diaPico = asistencias.GroupBy(a => a.FechaHoraEntrada.ToString("dddd"))
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            wsResumen.Cell(9, 1).Value = "Día Más Concurrido";
            wsResumen.Cell(9, 2).Value = diaPico != null ? $"{diaPico.Key} ({diaPico.Count()} visitas)" : "N/D";
            wsResumen.Range(9, 2, 9, 3).Merge();

            wsResumen.Range(5, 1, 9, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Range(5, 1, 9, 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsResumen.Columns().AdjustToContents();

            // Sheet 2: Registro Detallado
            var wsDetalle = workbook.Worksheets.Add("Registro Check-ins");
            CrearBanner(wsDetalle, "REGISTRO DETALLADO DE ASISTENCIAS", fechaInicio, fechaFin, 6);

            int rDet = 4;
            wsDetalle.Cell(rDet, 1).Value = "Fecha";
            wsDetalle.Cell(rDet, 2).Value = "Día Sem.";
            wsDetalle.Cell(rDet, 3).Value = "Hora Entrada";
            wsDetalle.Cell(rDet, 4).Value = "Hora Salida";
            wsDetalle.Cell(rDet, 5).Value = "Cliente";
            wsDetalle.Cell(rDet, 6).Value = "Método de Registro";
            AplicarEstiloCabecera(wsDetalle.Range(rDet, 1, rDet, 6));
            rDet++;

            foreach (var a in asistencias)
            {
                wsDetalle.Cell(rDet, 1).Value = a.FechaHoraEntrada.ToString("dd/MM/yyyy");
                wsDetalle.Cell(rDet, 2).Value = a.FechaHoraEntrada.ToString("dddd");
                wsDetalle.Cell(rDet, 3).Value = a.FechaHoraEntrada.ToString("HH:mm");
                wsDetalle.Cell(rDet, 4).Value = a.FechaHoraSalida.HasValue ? a.FechaHoraSalida.Value.ToString("HH:mm") : "-";
                wsDetalle.Cell(rDet, 5).Value = $"{a.Cliente?.Nombre} {a.Cliente?.Apellido}".Trim();
                wsDetalle.Cell(rDet, 6).Value = a.MetodoRegistro ?? "Kiosco PIN";
                rDet++;
            }

            wsDetalle.Cell(rDet, 1).Value = "TOTAL ASISTENCIAS";
            wsDetalle.Range(rDet, 1, rDet, 5).Merge();
            wsDetalle.Cell(rDet, 6).Value = asistencias.Count;
            AplicarEstiloFilaTotal(wsDetalle.Range(rDet, 1, rDet, 6));
            wsDetalle.Columns().AdjustToContents();

            // Sheet 3: Análisis por Horas
            var wsHoras = workbook.Worksheets.Add("Análisis Horas Pico");
            CrearBanner(wsHoras, "DISTRIBUCIÓN DE AFLUENCIA POR HORA", fechaInicio, fechaFin, 3);

            int rH = 4;
            wsHoras.Cell(rH, 1).Value = "Franja Horaria";
            wsHoras.Cell(rH, 2).Value = "Cantidad de Visitas";
            wsHoras.Cell(rH, 3).Value = "% del Total";
            AplicarEstiloCabecera(wsHoras.Range(rH, 1, rH, 3));
            rH++;

            var agrupadoHora = asistencias.GroupBy(a => a.FechaHoraEntrada.Hour)
                                          .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                                          .OrderBy(g => g.Hora).ToList();

            foreach (var h in agrupadoHora)
            {
                wsHoras.Cell(rH, 1).Value = $"{h.Hora:00}:00 - {h.Hora:00}:59";
                wsHoras.Cell(rH, 2).Value = h.Cantidad;
                wsHoras.Cell(rH, 3).Value = asistencias.Count > 0 ? (double)h.Cantidad / asistencias.Count : 0;
                rH++;
            }

            wsHoras.Cell(rH, 1).Value = "TOTAL";
            wsHoras.Cell(rH, 2).Value = asistencias.Count;
            wsHoras.Cell(rH, 3).Value = 1.0;
            AplicarEstiloFilaTotal(wsHoras.Range(rH, 1, rH, 3));
            wsHoras.Column(3).Style.NumberFormat.Format = "0.0%";
            wsHoras.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string excelName = $"ReporteAfluencia_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
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

            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.Producto)
                .Where(v => v.Fecha >= fechaInicio.Date && v.Fecha <= endOfDay)
                .OrderBy(v => v.Fecha)
                .ToListAsync();

            var detalles = ventas.SelectMany(v => v.DetallesVenta).ToList();

            using var workbook = new XLWorkbook();

            // Sheet 1: Ranking Productos
            var wsRanking = workbook.Worksheets.Add("Rendimiento de Productos");
            CrearBanner(wsRanking, "FACTOR FIT GYM - VENTAS DE PRODUCTOS (BEST SELLERS)", fechaInicio, fechaFin, 6);

            decimal totalVentasTienda = ventas.Sum(v => v.Total);
            int totalUnidades = detalles.Sum(d => d.Cantidad);

            int rRank = 4;
            wsRanking.Cell(rRank, 1).Value = "Ranking";
            wsRanking.Cell(rRank, 2).Value = "Producto";
            wsRanking.Cell(rRank, 3).Value = "Categoría";
            wsRanking.Cell(rRank, 4).Value = "Unidades Vendidas";
            wsRanking.Cell(rRank, 5).Value = "Total Generado (C$)";
            wsRanking.Cell(rRank, 6).Value = "% Participación";
            AplicarEstiloCabecera(wsRanking.Range(rRank, 1, rRank, 6));
            rRank++;

            var agrupado = detalles.GroupBy(d => d.Producto)
                .Select(g => new {
                    Producto = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad),
                    TotalGenerado = g.Sum(x => x.Cantidad * x.PrecioUnitario)
                })
                .OrderByDescending(x => x.TotalGenerado).ToList();

            int pos = 1;
            foreach (var item in agrupado)
            {
                wsRanking.Cell(rRank, 1).Value = $"#{pos}";
                wsRanking.Cell(rRank, 2).Value = item.Producto?.Nombre ?? "Desc.";
                wsRanking.Cell(rRank, 3).Value = item.Producto?.Categoria ?? "-";
                wsRanking.Cell(rRank, 4).Value = item.Cantidad;
                wsRanking.Cell(rRank, 5).Value = item.TotalGenerado;
                wsRanking.Cell(rRank, 6).Value = totalVentasTienda > 0 ? (double)(item.TotalGenerado / totalVentasTienda) : 0;
                pos++;
                rRank++;
            }

            wsRanking.Cell(rRank, 1).Value = "TOTAL GENERAL";
            wsRanking.Range(rRank, 1, rRank, 3).Merge();
            wsRanking.Cell(rRank, 4).Value = totalUnidades;
            wsRanking.Cell(rRank, 5).Value = totalVentasTienda;
            wsRanking.Cell(rRank, 6).Value = 1.0;
            AplicarEstiloFilaTotal(wsRanking.Range(rRank, 1, rRank, 6));
            wsRanking.Column(5).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsRanking.Column(6).Style.NumberFormat.Format = "0.0%";
            wsRanking.Columns().AdjustToContents();

            // Sheet 2: Detalle de Tickets
            var wsTickets = workbook.Worksheets.Add("Detalle de Tickets");
            CrearBanner(wsTickets, "REGISTRO DE TICKETS DE VENTA", fechaInicio, fechaFin, 6);

            int rTick = 4;
            wsTickets.Cell(rTick, 1).Value = "Fecha y Hora";
            wsTickets.Cell(rTick, 2).Value = "N° Ticket";
            wsTickets.Cell(rTick, 3).Value = "Cliente";
            wsTickets.Cell(rTick, 4).Value = "Método de Pago";
            wsTickets.Cell(rTick, 5).Value = "Artículos";
            wsTickets.Cell(rTick, 6).Value = "Total (C$)";
            AplicarEstiloCabecera(wsTickets.Range(rTick, 1, rTick, 6));
            rTick++;

            foreach (var v in ventas)
            {
                var resumenItems = v.DetallesVenta != null && v.DetallesVenta.Any()
                    ? string.Join(", ", v.DetallesVenta.Select(d => $"{d.Cantidad}x {d.Producto?.Nombre ?? "Prod"}"))
                    : "Sin detalle";

                wsTickets.Cell(rTick, 1).Value = v.Fecha.ToString("dd/MM/yyyy HH:mm");
                wsTickets.Cell(rTick, 2).Value = v.NumeroRecibo ?? $"-{v.Id}";
                wsTickets.Cell(rTick, 3).Value = v.Cliente != null ? $"{v.Cliente.Nombre} {v.Cliente.Apellido}".Trim() : "Cliente Ocasional";
                wsTickets.Cell(rTick, 4).Value = v.MetodoPago ?? "Efectivo";
                wsTickets.Cell(rTick, 5).Value = resumenItems;
                wsTickets.Cell(rTick, 6).Value = v.Total;
                rTick++;
            }

            wsTickets.Cell(rTick, 1).Value = "TOTAL INGRESOS TIENDA";
            wsTickets.Range(rTick, 1, rTick, 5).Merge();
            wsTickets.Cell(rTick, 6).Value = totalVentasTienda;
            AplicarEstiloFilaTotal(wsTickets.Range(rTick, 1, rTick, 6));
            wsTickets.Column(6).Style.NumberFormat.Format = "\"C$\" #,##0.00";
            wsTickets.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            string excelName = $"ReporteTienda_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }
    }
}
