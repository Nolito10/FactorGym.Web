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
        public async Task<IActionResult> Create([Bind("NombreCliente,TipoUso,FechaHoraInicio,FechaHoraFin,MontoTotal")] ReservacionCancha reservacion)
        {
            if (reservacion.TipoUso != "Evento" && !reservacion.FechaHoraFin.HasValue)
            {
                TempData["ErrorMessage"] = "Los deportes de cancha requieren una fecha de fin.";
                return RedirectToAction(nameof(Create));
            }

            if (reservacion.FechaHoraFin.HasValue && reservacion.FechaHoraInicio >= reservacion.FechaHoraFin.Value)
            {
                TempData["ErrorMessage"] = "La fecha/hora de fin debe ser posterior a la de inicio.";
                return RedirectToAction(nameof(Create));
            }

            if (reservacion.FechaHoraInicio < DateTime.Now)
            {
                TempData["ErrorMessage"] = "No puedes crear reservaciones en el pasado.";
                return RedirectToAction(nameof(Create));
            }

            // Validar solapamiento cruzado (colisión)
            // Eventos sin fin bloquean el día; deportes verifican horas.
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
                TempData["ErrorMessage"] = "El horario seleccionado choca con otra reservación existente. Por favor, elige otra hora.";
                return RedirectToAction(nameof(Create));
            }

            reservacion.Estado = "Confirmada";
            _context.Add(reservacion);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Reservación creada y confirmada exitosamente.";
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
                TempData["SuccessMessage"] = "Reservación cancelada correctamente.";
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
