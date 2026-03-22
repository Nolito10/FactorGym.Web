using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class PagosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PagosController(ApplicationDbContext context)
        {
            _context = context;
            // Configure QuestPDF license for open source use
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // GET: Pagos
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Pagos.Include(p => p.Cliente).Include(p => p.Membresia)
                .OrderByDescending(p => p.FechaPago);

            int total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalRecords = total;

            ViewBag.MembresiasConPendiente = _context.Membresias
                .Count(m => m.Estado == "Activa" && !m.Pagos.Any());

            return View(items);
        }


        // GET: Pagos/Create
        public async Task<IActionResult> Create(int? membresiaId)
        {
            if (membresiaId.HasValue)
            {
                var membresia = await _context.Membresias.Include(m => m.Cliente).FirstOrDefaultAsync(m => m.Id == membresiaId.Value);
                if (membresia != null)
                {
                    ViewBag.ClienteNombre = membresia.Cliente.Nombre + " " + membresia.Cliente.Apellido;
                    ViewBag.MontoSugerido = membresia.Costo;
                    ViewBag.TipoPlan = membresia.Tipo + " - " + membresia.TipoPlan;
                    
                    var model = new Pago
                    {
                        MembresiaId = membresia.Id,
                        ClienteId = membresia.ClienteId,
                        Monto = membresia.Costo,
                        FechaPago = DateTime.Now,
                        TipoServicio = membresia.TipoPlan
                    };
                    return View(model);
                }
            }
            
            // Si entra por menú libre
            ViewBag.MembresiasActivas = _context.Membresias.Include(m => m.Cliente).Where(m => m.Estado == "Activa").ToList();
            return View();
        }

        // POST: Pagos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClienteId,MembresiaId,Monto,FechaPago,MetodoPago,Referencia,TipoServicio")] Pago pago)
        {
            ModelState.Remove("NumeroRecibo");

            var mem = await _context.Membresias.FindAsync(pago.MembresiaId);
            if(mem != null)
            {
                pago.Monto = mem.Costo;
                pago.ClienteId = mem.ClienteId;
                pago.TipoServicio = mem.TipoPlan;
                
                ModelState.Remove("Monto");
                ModelState.Remove("ClienteId");
                ModelState.Remove("TipoServicio");
            }

            // Inyectar la hora exacta del reloj sobre la Fecha seleccionada por el usuario
            pago.FechaPago = pago.FechaPago.Date.Add(DateTime.Now.TimeOfDay);

            if (ModelState.IsValid)
            {
                // Regla de Negocio: solo 1 pago por membresía
                bool yaExistePago = await _context.Pagos.AnyAsync(p => p.MembresiaId == pago.MembresiaId);
                if (yaExistePago)
                {
                    TempData["ErrorMessage"] = "Esta membresía ya tiene un pago registrado. Para renovar, usa el botón Renovar en el directorio.";
                    return RedirectToAction(nameof(Index));
                }

                pago.NumeroRecibo = "REC-" + DateTime.Now.Ticks.ToString().Substring(8);
                _context.Add(pago);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Pago registrado exitosamente. Puedes generar el recibo ahora.";
                return RedirectToAction(nameof(Index));
            }
            return View(pago);
        }

        // ACCIÓN PARA GENERAR EL PDF CON QUESTPDF
        public async Task<IActionResult> GenerarReciboPDF(int id)
        {
            var pago = await _context.Pagos
                .Include(p => p.Cliente)
                .Include(p => p.Membresia)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pago == null)
            {
                return NotFound();
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, pago));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Recibo_{pago.NumeroRecibo}.pdf");
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().BorderBottom(3).BorderColor("0A58CA").PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("FACTOR FIT GYM")
                            .FontSize(22).Bold().FontColor("0A58CA");
                        column.Item().Text("Comprobante de Pago")
                            .FontSize(11).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(120).AlignRight().Column(column =>
                    {
                        column.Item().AlignRight().Text("Nro. Recibo").FontSize(9).FontColor(Colors.Grey.Medium);
                        column.Item().AlignRight().Text(text =>
                        {
                            text.Span("").FontSize(9);
                        });
                    });
                });
            });
        }

        void ComposeContent(IContainer container, Pago pago)
        {
            container.PaddingVertical(8).Column(column =>
            {
                column.Spacing(6);

                // === ENCABEZADO DE RECIBO ===
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(txt =>
                    {
                        txt.Span("No. Recibo: ").Bold();
                        txt.Span(pago.NumeroRecibo).FontColor("0A58CA");
                    });
                    row.RelativeItem().AlignRight().Text(txt =>
                    {
                        txt.Span("Fecha: ").Bold();
                        txt.Span(pago.FechaPago.ToString("dd/MM/yyyy  HH:mm"));
                    });
                });

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // === DATOS DEL CLIENTE ===
                column.Item().PaddingTop(6).Background("F8FAFF").Padding(10).Column(sec =>
                {
                    sec.Item().Text("DATOS DEL CLIENTE").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                    sec.Spacing(3);
                    sec.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(txt => { txt.Span("Nombre: ").Bold(); txt.Span(pago.Cliente.Nombre + " " + pago.Cliente.Apellido); });
                        row.RelativeItem().Text(txt => { txt.Span("Teléfono: ").Bold(); txt.Span(pago.Cliente.Telefono ?? "—"); });
                    });
                    sec.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(txt =>
                        {
                            txt.Span("PIN de Acceso (Check-In): ").Bold();
                            txt.Span(pago.Cliente.PinAcceso ?? "Sin asignar").FontSize(13).Bold().FontColor("0A58CA");
                        });
                    });
                });

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // === DETALLE DE MEMBRESÍA ===
                column.Item().PaddingTop(6).Column(sec =>
                {
                    sec.Item().Text("DETALLE DE MEMBRESÍA").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                    sec.Spacing(4);

                    sec.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn();
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background("0A58CA").Padding(4).Text("Plan / Servicio").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background("0A58CA").Padding(4).Text("Frecuencia").FontColor(Colors.White).Bold().FontSize(9);
                            header.Cell().Background("0A58CA").Padding(4).AlignRight().Text("Costo").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        // Data row
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{pago.Membresia.Tipo} — {pago.TipoServicio}").FontSize(10);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(pago.Membresia.Frecuencia ?? "Mensual").FontSize(10);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₡{pago.Monto:N2}").Bold().FontSize(10).FontColor("0A58CA");
                    });
                });

                // === VIGENCIA ===
                column.Item().PaddingTop(6).Background("EFF6FF").Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("VIGENCIA DEL PLAN").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text(txt => { txt.Span("Inicio: ").Bold(); txt.Span(pago.Membresia.FechaInicio.ToString("dd/MM/yyyy")); });
                            r.RelativeItem().Text(txt => { txt.Span("Vence: ").Bold().FontColor("B91C1C"); txt.Span(pago.Membresia.FechaFin.ToString("dd/MM/yyyy")).FontColor("B91C1C").Bold(); });
                        });
                    });
                });

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // === TOTAL Y MÉTODO DE PAGO ===
                column.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text(txt =>
                    {
                        txt.Span("Método de Pago: ").Bold();
                        txt.Span(pago.MetodoPago);
                        if (!string.IsNullOrEmpty(pago.Referencia))
                        {
                            txt.Span("  Ref: ").Bold();
                            txt.Span(pago.Referencia);
                        }
                    });
                    row.ConstantItem(130).AlignRight().Text(txt =>
                    {
                        txt.Span("TOTAL PAGADO: ").Bold().FontSize(12);
                        txt.Span($"₡{pago.Monto:N2}").Bold().FontSize(14).FontColor("0A58CA");
                    });
                });

                // === PIE ===
                column.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).AlignCenter()
                    .Text("¡Gracias por ser parte de Factor Fit Gym! Este documento es su comprobante oficial de pago.")
                    .Italic().FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }
    }
}
