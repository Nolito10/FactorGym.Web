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
    public class ReservacionesCanchasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservacionesCanchasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ReservacionesCanchas
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var query = _context.ReservacionesCanchas
                .OrderByDescending(r => r.FechaHoraInicio);

            // Auto-completar reservaciones pasadas
            var pasadas = await _context.ReservacionesCanchas
                .Where(r => r.Estado == "Confirmada" && r.FechaHoraFin.HasValue && r.FechaHoraFin.Value < DateTime.Now)
                .ToListAsync();

            if (pasadas.Any())
            {
                foreach(var r in pasadas) r.Estado = "Completada";
                await _context.SaveChangesAsync();
            }

            int total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalRecords = total;

            return View(items);
        }

        // GET: ReservacionesCanchas/Create
        public IActionResult Create()
        {
            ViewBag.PrecioCancha = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "CANCHA_HORA")?.Precio ?? 20.00m;
            return View();
        }

        // POST: ReservacionesCanchas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreCliente,TipoUso,FechaHoraInicio,MetodoPago,MontoTotal")] ReservacionCancha reservacion, decimal duracionHoras = 1)
        {
            if (reservacion.TipoUso == "Evento")
            {
                // Evento exclusivo se alquila el día completo
                reservacion.FechaHoraInicio = reservacion.FechaHoraInicio.Date.AddHours(6);
                reservacion.FechaHoraFin = reservacion.FechaHoraInicio.Date.AddHours(23).AddMinutes(59);

                // Parsear robustamente el monto total acordado tolerando punto o coma decimal
                if (Request.Form.TryGetValue("MontoTotal", out var rawMontoVal))
                {
                    string rawMonto = rawMontoVal.ToString().Trim().Replace(" ", "").Replace("C$", "");
                    if (rawMonto.Contains(",") && !rawMonto.Contains("."))
                    {
                        rawMonto = rawMonto.Replace(",", ".");
                    }
                    if (decimal.TryParse(rawMonto, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal montoParsed))
                    {
                        reservacion.MontoTotal = montoParsed;
                    }
                }

                if (reservacion.MontoTotal <= 0)
                {
                    TempData["ErrorMessage"] = "Para un evento exclusivo, debes ingresar el monto total acordado con el cliente.";
                    return RedirectToAction(nameof(Create));
                }
            }
            else
            {
                // Deporte regular
                if (duracionHoras <= 0) duracionHoras = 1;
                reservacion.FechaHoraFin = reservacion.FechaHoraInicio.AddMinutes((double)(duracionHoras * 60));

                // El precio se toma automáticamente del Gestor de Precios (CANCHA_HORA)
                decimal precioPorHora = _context.ConfiguracionPrecios.FirstOrDefault(p => p.Clave == "CANCHA_HORA")?.Precio ?? 20.00m;
                reservacion.MontoTotal = Math.Round(duracionHoras * precioPorHora, 2);
            }

            if (reservacion.FechaHoraInicio < DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "No puedes crear reservaciones en fechas pasadas.";
                return RedirectToAction(nameof(Create));
            }

            // Validar solapamiento cruzado (colisión)
            bool isOverlapping = await _context.ReservacionesCanchas.AnyAsync(r => 
                (r.Estado == "Confirmada" || r.Estado == "Completada") && 
                (
                    (r.TipoUso == "Evento" && r.FechaHoraInicio.Date == reservacion.FechaHoraInicio.Date)
                    ||
                    (reservacion.TipoUso == "Evento" && r.FechaHoraInicio.Date == reservacion.FechaHoraInicio.Date)
                    ||
                    (r.FechaHoraFin != null && reservacion.FechaHoraFin != null && 
                     reservacion.FechaHoraInicio < r.FechaHoraFin && reservacion.FechaHoraFin > r.FechaHoraInicio)
                )
            );

            if (isOverlapping)
            {
                TempData["ErrorMessage"] = reservacion.TipoUso == "Evento"
                    ? "No se puede reservar el día completo para evento porque ya existen reservaciones agendadas en esa fecha."
                    : "El horario seleccionado choca con otra reservación existente. Por favor, elige otro horario.";
                return RedirectToAction(nameof(Create));
            }

            // Registro del cobro inmediato (Política: Reservación hecha = Reservación pagada)
            reservacion.Estado = "Confirmada";
            reservacion.EstadoPago = "Pagado";
            reservacion.FechaPago = DateTime.Now;
            reservacion.MetodoPago = string.IsNullOrWhiteSpace(reservacion.MetodoPago) ? "Efectivo" : reservacion.MetodoPago;
            reservacion.NumeroRecibo = $"REC-CAN-{DateTime.Now:yyyyMMddHHmmss}";

            _context.Add(reservacion);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"¡Reservación pagada y confirmada con éxito! Total: C${reservacion.MontoTotal:N2} (Recibo N° {reservacion.NumeroRecibo}).";
            return RedirectToAction(nameof(Index));
        }

        // POST: ReservacionesCanchas/Reagendar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reagendar(int id, DateTime nuevaFechaHoraInicio, decimal duracionHoras = 1)
        {
            var reservacion = await _context.ReservacionesCanchas.FindAsync(id);
            if (reservacion == null) return NotFound();

            // Validación: si ya fue completada, NO se puede reprogramar
            if (reservacion.Estado == "Completada")
            {
                TempData["ErrorMessage"] = "No se puede reprogramar una reservación que ya fue completada.";
                return RedirectToAction(nameof(Index));
            }

            DateTime nuevaFechaHoraFin;
            if (reservacion.TipoUso == "Evento")
            {
                nuevaFechaHoraInicio = nuevaFechaHoraInicio.Date.AddHours(6);
                nuevaFechaHoraFin = nuevaFechaHoraInicio.Date.AddHours(23).AddMinutes(59);
            }
            else
            {
                if (duracionHoras <= 0)
                {
                    duracionHoras = reservacion.FechaHoraFin.HasValue 
                        ? (decimal)(reservacion.FechaHoraFin.Value - reservacion.FechaHoraInicio).TotalHours 
                        : 1m;
                }
                if (duracionHoras <= 0) duracionHoras = 1m;

                nuevaFechaHoraFin = nuevaFechaHoraInicio.AddMinutes((double)(duracionHoras * 60));
            }

            if (nuevaFechaHoraInicio < DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "No puedes reagendar para una fecha pasada.";
                return RedirectToAction(nameof(Index));
            }

            // Validar choque de horario excluyendo la reservación actual
            bool isOverlapping = await _context.ReservacionesCanchas.AnyAsync(r => 
                r.Id != id &&
                (r.Estado == "Confirmada" || r.Estado == "Completada") && 
                (
                    (r.TipoUso == "Evento" && r.FechaHoraInicio.Date == nuevaFechaHoraInicio.Date)
                    ||
                    (reservacion.TipoUso == "Evento" && r.FechaHoraInicio.Date == nuevaFechaHoraInicio.Date)
                    ||
                    (r.FechaHoraFin != null && 
                     nuevaFechaHoraInicio < r.FechaHoraFin && nuevaFechaHoraFin > r.FechaHoraInicio)
                )
            );

            if (isOverlapping)
            {
                TempData["ErrorMessage"] = "El nuevo horario seleccionado choca con otra reservación. Por favor elige otro horario disponible.";
                return RedirectToAction(nameof(Index));
            }

            reservacion.FechaHoraInicio = nuevaFechaHoraInicio;
            reservacion.FechaHoraFin = nuevaFechaHoraFin;
            reservacion.Estado = "Confirmada"; // Se reactiva si estaba cancelada
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservación reprogramada exitosamente sin costo adicional.";
            return RedirectToAction(nameof(Index));
        }

        // POST: ReservacionesCanchas/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var r = await _context.ReservacionesCanchas.FindAsync(id);
            if (r != null)
            {
                r.Estado = "Cancelada";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reservación cancelada (cancha liberada). Conforme a la política del gimnasio, el pago no es reembolsable, pero el cliente puede reprogramar.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: ReservacionesCanchas/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.ReservacionesCanchas.FindAsync(id);
            if (r != null)
            {
                _context.ReservacionesCanchas.Remove(r);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reservación eliminada definitivamente del historial.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
