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
                    page.ContinuousSize(226.7f); // 80mm width
                    page.Margin(10); // Small margin for POS
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                    page.Content().Element(x => ComposeTicket(x, pago));
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Recibo_{pago.NumeroRecibo}.pdf");
        }

        void ComposeTicket(IContainer container, Pago pago)
        {
            container.Column(col =>
            {
                // HEADER
                col.Item().AlignCenter().Text("FACTOR FIT GYM")
                    .FontSize(14).Bold().FontColor("000000");
                col.Item().AlignCenter().Text("Comprobante de Pago")
                    .FontSize(10).FontColor(Colors.Grey.Darken2);
                
                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);
                
                // INFO TXT
                col.Item().Text($"Recibo: {pago.NumeroRecibo}");
                col.Item().Text($"Fecha: {pago.FechaPago:dd/MM/yyyy HH:mm}");
                col.Item().Text($"Cliente: {pago.Cliente.Nombre} {pago.Cliente.Apellido}");
                if (!string.IsNullOrEmpty(pago.Cliente.PinAcceso))
                {
                    col.Item().Text($"PIN de Ingreso: {pago.Cliente.PinAcceso}").Bold();
                }

                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                // ITEMS
                col.Item().Text("DETALLE DE MEMBRESÍA").Bold();
                col.Item().PaddingTop(2).Text($"{pago.Membresia.Tipo} - {pago.TipoServicio}");
                col.Item().Text($"Modalidad: {pago.Membresia.Frecuencia ?? "Mensual"}");
                col.Item().Text($"Vigencia: {pago.Membresia.FechaInicio:dd/MM/yyyy} a {pago.Membresia.FechaFin:dd/MM/yyyy}");
                
                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                // TOTAL
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Método de Pago:").Bold();
                    row.RelativeItem().AlignRight().Text(pago.MetodoPago);
                });

                if (!string.IsNullOrEmpty(pago.Referencia))
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Referencia:").Bold();
                        row.RelativeItem().AlignRight().Text(pago.Referencia);
                    });
                }

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL PAGADO:").Bold().FontSize(10);
                    row.RelativeItem().AlignRight().Text($"C${pago.Monto:N2}").Bold().FontSize(12);
                });

                col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Black);

                // FOOTER
                col.Item().PaddingTop(10).AlignCenter()
                    .Text("¡Gracias por su preferencia!")
                    .Italic().FontSize(8);
                col.Item().AlignCenter().Text("Este documento es su comprobante.").FontSize(8);
            });
        }
    }
}
