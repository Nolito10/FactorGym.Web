using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado,Cliente")]
    public class AsistenciasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AsistenciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Asistencias (Pantalla Kiosco)
        public IActionResult Index()
        {
            return View();
        }

        // GET: Asistencias/Historial
        public async Task<IActionResult> Historial()
        {
            var hoy = DateTime.Today;
            var asistencias = await _context.Asistencias
                .Include(a => a.Cliente)
                .Where(a => a.FechaHoraEntrada.Date == hoy)
                .OrderByDescending(a => a.FechaHoraEntrada)
                .ToListAsync();

            return View(asistencias);
        }

        // POST: Asistencias/CheckIn (API JSON)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        {
            if (string.IsNullOrEmpty(dto?.Pin) || dto.Pin.Length != 4)
            {
                return Json(new { success = false, status = "error", message = "El PIN debe tener 4 dígitos." });
            }

            // 1. Buscar al cliente por PIN
            var cliente = await _context.Clientes
                .Include(c => c.Membresias)
                .FirstOrDefaultAsync(c => c.PinAcceso == dto.Pin && c.IsActivo);

            if (cliente == null)
            {
                return Json(new { success = false, status = "not_found", message = "PIN no reconocido. Verifica tu código de acceso." });
            }

            // 2. Verificar membresía vigente
            var membresiaActiva = cliente.Membresias?
                .Where(m => m.Estado == "Activa" && m.FechaFin >= DateTime.Today)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefault();

            var membresiaVencida = cliente.Membresias?
                .Where(m => m.Estado == "Vencida" || m.FechaFin < DateTime.Today)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefault();

            var membresiaCongelada = cliente.Membresias?
                .FirstOrDefault(m => m.Estado == "Congelada");

            string estadoMembresia;
            string mensajeEstado;
            string colorEstado;

            if (membresiaActiva != null)
            {
                estadoMembresia = "activa";
                mensajeEstado = $"Membresía vigente hasta el {membresiaActiva.FechaFin:dd/MM/yyyy}";
                colorEstado = "success";
            }
            else if (membresiaCongelada != null)
            {
                estadoMembresia = "congelada";
                mensajeEstado = "Tu membresía está congelada. Acude a recepción.";
                colorEstado = "warning";
            }
            else if (membresiaVencida != null)
            {
                estadoMembresia = "vencida";
                mensajeEstado = $"Tu membresía venció el {membresiaVencida.FechaFin:dd/MM/yyyy}. Renueva en recepción.";
                colorEstado = "danger";
            }
            else
            {
                estadoMembresia = "sin_membresia";
                mensajeEstado = "No tienes membresía registrada. Acude a recepción.";
                colorEstado = "danger";
            }

            // 3. Verificar si ya se registró hoy
            var hoy = DateTime.Today;
            var yaRegistrado = await _context.Asistencias
                .AnyAsync(a => a.ClienteId == cliente.Id && a.FechaHoraEntrada.Date == hoy);

            if (yaRegistrado)
            {
                return Json(new
                {
                    success = true,
                    status = "already_checked",
                    clienteNombre = cliente.NombreCompleto,
                    estadoMembresia,
                    mensajeEstado,
                    colorEstado,
                    message = "Ya registraste tu asistencia el día de hoy."
                });
            }

            // 4. Registrar asistencia
            var asistencia = new Asistencia
            {
                ClienteId = cliente.Id,
                FechaHoraEntrada = DateTime.Now,
                MetodoRegistro = "PIN"
            };

            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                status = estadoMembresia,
                clienteNombre = cliente.NombreCompleto,
                estadoMembresia,
                mensajeEstado,
                colorEstado,
                horaEntrada = asistencia.FechaHoraEntrada.ToString("hh:mm tt"),
                message = "Asistencia registrada correctamente."
            });
        }

        public class CheckInDto
        {
            public string Pin { get; set; }
        }

        // POST: Asistencias/RecuperarPin (Recovery)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarPin([FromBody] RecuperarPinDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Telefono))
            {
                return Json(new { success = false, message = "Ingresa un número de teléfono." });
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Telefono == dto.Telefono.Trim() && c.IsActivo);

            if (cliente == null)
            {
                return Json(new { success = false, message = "No se encontró ningún cliente con ese teléfono. Acude a recepción." });
            }

            if (string.IsNullOrEmpty(cliente.PinAcceso))
            {
                return Json(new { success = false, message = $"El cliente {cliente.NombreCompleto} no tiene PIN asignado. Solicita uno en recepción." });
            }

            return Json(new
            {
                success = true,
                nombre = cliente.NombreCompleto,
                pin = cliente.PinAcceso,
                message = "PIN recuperado exitosamente."
            });
        }

        public class RecuperarPinDto
        {
            public string Telefono { get; set; }
        }
    }
}
