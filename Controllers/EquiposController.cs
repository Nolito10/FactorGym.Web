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
    public class EquiposController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EquiposController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Equipos
        public async Task<IActionResult> Index(string categoria = null, string estado = null, string filtro = null, string search = null)
        {
            // Métricas y KPIs para las tarjetas superiores
            var hoy = DateTime.Today;
            var limiteAlerta = hoy.AddDays(7);

            var todosLosEquipos = await _context.Equipos.ToListAsync();

            ViewBag.TotalEquipos = todosLosEquipos.Count;
            ViewBag.TotalOperativos = todosLosEquipos.Count(e => e.Estado == "Operativo" || e.Estado == "Completado");
            ViewBag.TotalDesgastados = todosLosEquipos.Count(e => e.Estado == "Desgastado" || e.NivelDesgaste == "Moderado" || e.NivelDesgaste == "Crítico");
            ViewBag.TotalFueraServicio = todosLosEquipos.Count(e => e.Estado == "Fuera de Servicio");
            ViewBag.TotalMantenimientosAlertas = todosLosEquipos.Count(e => 
                e.Estado != "Completado" && e.Estado != "De Baja" &&
                (e.Estado == "Desgastado" || 
                e.Estado == "Fuera de Servicio" || 
                (e.ProximoMantenimiento.HasValue && e.ProximoMantenimiento.Value.Date <= limiteAlerta)));

            // Filtrado
            var query = _context.Equipos
                .Include(e => e.Mantenimientos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                if (filtro.Equals("alertas", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(e => 
                        e.Estado != "Completado" && e.Estado != "De Baja" &&
                        (e.Estado == "Desgastado" || 
                        e.Estado == "Fuera de Servicio" || 
                        (e.ProximoMantenimiento.HasValue && e.ProximoMantenimiento.Value.Date <= limiteAlerta)));
                }
                else if (filtro.Equals("vencidos", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(e => e.Estado != "Completado" && e.ProximoMantenimiento.HasValue && e.ProximoMantenimiento.Value.Date < hoy);
                }
                else if (filtro.Equals("desgastados", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(e => e.Estado == "Desgastado" || e.NivelDesgaste == "Moderado" || e.NivelDesgaste == "Crítico");
                }
            }

            if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
            {
                query = query.Where(e => e.Categoria == categoria);
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                query = query.Where(e => e.Estado == estado);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e => 
                    e.Nombre.ToLower().Contains(s) || 
                    (e.Codigo != null && e.Codigo.ToLower().Contains(s)) ||
                    (e.Ubicacion != null && e.Ubicacion.ToLower().Contains(s)));
            }

            // Ordenamiento inteligente: primero los que requieren atención urgente, luego alfabéticamente
            var equipos = await query
                .OrderByDescending(e => e.Estado == "Fuera de Servicio")
                .ThenByDescending(e => e.Estado == "Desgastado")
                .ThenByDescending(e => e.ProximoMantenimiento.HasValue && e.ProximoMantenimiento.Value.Date < hoy)
                .ThenBy(e => e.Nombre)
                .ToListAsync();

            ViewBag.CategoriaSeleccionada = categoria ?? "Todas";
            ViewBag.EstadoSeleccionado = estado ?? "Todos";
            ViewBag.FiltroActivo = filtro;
            ViewBag.SearchTerm = search;

            return View(equipos);
        }

        // GET: Equipos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var equipo = await _context.Equipos
                .Include(e => e.Mantenimientos.OrderByDescending(m => m.Fecha))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipo == null) return NotFound();

            return View(equipo);
        }

        // GET: Equipos/Create
        public IActionResult Create()
        {
            var equipo = new Equipo
            {
                Cantidad = 1,
                Estado = "Operativo",
                NivelDesgaste = "Ninguno",
                FrecuenciaMantenimientoDias = null
            };
            return View(equipo);
        }

        // POST: Equipos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Codigo,Categoria,Ubicacion,Cantidad,Estado,NivelDesgaste,FrecuenciaMantenimientoDias,UltimoMantenimiento,ProximoMantenimiento,FechaAdquisicion,Observaciones")] Equipo equipo)
        {
            if (ModelState.IsValid)
            {
                equipo.FechaRegistro = DateTime.UtcNow;
                if (string.IsNullOrWhiteSpace(equipo.Estado)) equipo.Estado = "Operativo";
                if (string.IsNullOrWhiteSpace(equipo.NivelDesgaste)) equipo.NivelDesgaste = "Ninguno";

                // Generar código aleatorio según categoría si no vino asignado
                if (string.IsNullOrWhiteSpace(equipo.Codigo))
                {
                    string prefijo = equipo.Categoria switch
                    {
                        "Peso Libre" => "PL",
                        "Fuerza y Máquinas" => "FM",
                        "Cardio" => "CR",
                        "Accesorios y Funcional" => "AF",
                        _ => "EQ"
                    };
                    var rnd = new Random();
                    string codigoGen;
                    do
                    {
                        codigoGen = $"{prefijo}-{rnd.Next(1000, 9999)}";
                    } while (await _context.Equipos.AnyAsync(e => e.Codigo == codigoGen));
                    equipo.Codigo = codigoGen;
                }

                // Determinar fecha de próximo mantenimiento según frecuencia (semanal, quincenal, mensual o no hacerlo)
                if (equipo.FrecuenciaMantenimientoDias.HasValue && equipo.FrecuenciaMantenimientoDias.Value > 0)
                {
                    equipo.ProximoMantenimiento = DateTime.Today.AddDays(equipo.FrecuenciaMantenimientoDias.Value);
                }
                else
                {
                    equipo.ProximoMantenimiento = null;
                    equipo.FrecuenciaMantenimientoDias = null;
                }

                _context.Add(equipo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Equipo '{equipo.Nombre}' ({equipo.Codigo}) registrado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(equipo);
        }

        // GET: Equipos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null) return NotFound();

            return View(equipo);
        }

        // POST: Equipos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Codigo,Categoria,Ubicacion,Cantidad,Estado,NivelDesgaste,FrecuenciaMantenimientoDias,UltimoMantenimiento,ProximoMantenimiento,FechaAdquisicion,Observaciones,FechaRegistro")] Equipo equipo)
        {
            if (id != equipo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Determinar fecha de próximo mantenimiento según frecuencia
                    if (equipo.FrecuenciaMantenimientoDias.HasValue && equipo.FrecuenciaMantenimientoDias.Value > 0)
                    {
                        if (!equipo.ProximoMantenimiento.HasValue)
                        {
                            equipo.ProximoMantenimiento = DateTime.Today.AddDays(equipo.FrecuenciaMantenimientoDias.Value);
                        }
                    }
                    else
                    {
                        equipo.ProximoMantenimiento = null;
                        equipo.FrecuenciaMantenimientoDias = null;
                    }

                    _context.Update(equipo);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Equipo '{equipo.Nombre}' actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EquipoExists(equipo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(equipo);
        }

        // POST: Equipos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var equipo = await _context.Equipos
                .Include(e => e.Mantenimientos)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (equipo != null)
            {
                _context.HistorialMantenimientos.RemoveRange(equipo.Mantenimientos);
                _context.Equipos.Remove(equipo);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "El equipo y su historial han sido eliminados.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Equipos/RegistrarMantenimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarMantenimiento([Bind("EquipoId,Fecha,Tipo,Descripcion,Responsable,Costo,RegistrarComoGasto,EstadoPosterior,ProximaFechaSugerida")] HistorialMantenimiento mantenimiento)
        {
            var equipo = await _context.Equipos.FindAsync(mantenimiento.EquipoId);
            if (equipo == null)
            {
                TempData["ErrorMessage"] = "No se encontró el equipo seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                _context.HistorialMantenimientos.Add(mantenimiento);

                // Actualizar estado del equipo
                equipo.UltimoMantenimiento = mantenimiento.Fecha;
                equipo.Estado = mantenimiento.EstadoPosterior;

                if (mantenimiento.EstadoPosterior == "Operativo")
                {
                    equipo.NivelDesgaste = "Ninguno";
                }

                // Calcular próxima fecha de mantenimiento
                if (mantenimiento.ProximaFechaSugerida.HasValue)
                {
                    equipo.ProximoMantenimiento = mantenimiento.ProximaFechaSugerida.Value;
                }
                else if (equipo.FrecuenciaMantenimientoDias.HasValue && equipo.FrecuenciaMantenimientoDias.Value > 0)
                {
                    equipo.ProximoMantenimiento = mantenimiento.Fecha.AddDays(equipo.FrecuenciaMantenimientoDias.Value);
                }

                // Si hubo un costo o inversión en el mantenimiento, registrarlo directamente en los egresos del gimnasio
                if (mantenimiento.Costo.HasValue && mantenimiento.Costo.Value > 0)
                {
                    mantenimiento.RegistrarComoGasto = true;
                    var nuevoGasto = new Gasto
                    {
                        Concepto = $"Mantenimiento {mantenimiento.Tipo}: {equipo.Nombre}",
                        Categoria = "Mantenimiento",
                        Monto = mantenimiento.Costo.Value,
                        Fecha = mantenimiento.Fecha,
                        Referencia = $"Equipo #{equipo.Codigo ?? equipo.Id.ToString()}"
                    };
                    _context.Gastos.Add(nuevoGasto);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Se registró el servicio para '{equipo.Nombre}' con éxito.";
                return RedirectToAction(nameof(Details), new { id = equipo.Id });
            }

            TempData["ErrorMessage"] = "Hubo un error al registrar el mantenimiento. Verifique los datos.";
            return RedirectToAction(nameof(Details), new { id = equipo.Id });
        }

        // POST: Equipos/ReportarDesgasteRapido (Para reportar desde el index sin entrar a formularios largos)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportarDesgasteRapido(int equipoId, string nivelDesgaste, string estado, string observacion, string reportadoPor)
        {
            var equipo = await _context.Equipos.FindAsync(equipoId);
            if (equipo == null)
            {
                TempData["ErrorMessage"] = "Equipo no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            equipo.Estado = !string.IsNullOrEmpty(estado) ? estado : "Desgastado";
            equipo.NivelDesgaste = !string.IsNullOrEmpty(nivelDesgaste) ? nivelDesgaste : "Moderado";
            
            if (!string.IsNullOrWhiteSpace(observacion))
            {
                equipo.Observaciones = observacion;

                // Creamos un registro en el historial de tipo "Reporte de Desgaste"
                var registroHistorial = new HistorialMantenimiento
                {
                    EquipoId = equipo.Id,
                    Fecha = DateTime.Now,
                    Tipo = "Reporte de Desgaste",
                    Descripcion = observacion,
                    Responsable = !string.IsNullOrWhiteSpace(reportadoPor) ? reportadoPor : User.Identity.Name ?? "Staff",
                    EstadoPosterior = equipo.Estado,
                    RegistrarComoGasto = false
                };
                _context.HistorialMantenimientos.Add(registroHistorial);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Reporte de desgaste registrado para '{equipo.Nombre}'.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Equipos/CompletarAlerta/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarAlerta(int id)
        {
            var equipo = await _context.Equipos.FindAsync(id);
            if (equipo == null)
            {
                TempData["ErrorMessage"] = "No se encontró el equipo seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            equipo.Estado = "Completado";
            equipo.NivelDesgaste = "Ninguno";
            equipo.UltimoMantenimiento = DateTime.Today;

            if (equipo.FrecuenciaMantenimientoDias.HasValue && equipo.FrecuenciaMantenimientoDias.Value > 0)
            {
                equipo.ProximoMantenimiento = DateTime.Today.AddDays(equipo.FrecuenciaMantenimientoDias.Value);
            }
            else
            {
                equipo.ProximoMantenimiento = null;
            }

            var registroHistorial = new HistorialMantenimiento
            {
                EquipoId = equipo.Id,
                Fecha = DateTime.Now,
                Tipo = "Revisión / Alerta Atendida",
                Descripcion = "Alerta marcada como completada y resuelta.",
                Responsable = User.Identity?.Name ?? "Administrador",
                EstadoPosterior = "Completado",
                Costo = 0,
                RegistrarComoGasto = false
            };
            _context.HistorialMantenimientos.Add(registroHistorial);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Alerta completada y resuelta para '{equipo.Nombre}'.";
            return RedirectToAction(nameof(Index), new { filtro = "alertas" });
        }

        // GET: Equipos/HistorialGeneral
        public async Task<IActionResult> HistorialGeneral()
        {
            var historiales = await _context.HistorialMantenimientos
                .Include(h => h.Equipo)
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            return View(historiales);
        }

        private bool EquipoExists(int id)
        {
            return _context.Equipos.Any(e => e.Id == id);
        }
    }
}
