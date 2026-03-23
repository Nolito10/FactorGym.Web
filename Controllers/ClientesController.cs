using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FactorFitGym.Web.Models;

namespace FactorFitGym.Web.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Clientes.Where(c => c.IsActivo).OrderBy(c => c.Apellido);
            int total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalRecords = total;
            return View(items);
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .Include(c => c.Asistencias)
                .Include(c => c.Membresias)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // GET: Clientes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Apellido,Telefono,Genero")] Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                cliente.IsActivo = true; // Set active by default
                
                // Generar PIN único (4 dígitos)
                string nuevoPin;
                bool exists;
                var rnd = new Random();
                do
                {
                    nuevoPin = rnd.Next(1000, 10000).ToString();
                    exists = await _context.Clientes.AnyAsync(c => c.PinAcceso == nuevoPin);
                } while (exists);
                cliente.PinAcceso = nuevoPin;

                _context.Add(cliente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "¡Cliente agregado con éxito!";
                return RedirectToAction(nameof(Create));
            }
            return View(cliente);
        }

        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }
            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Apellido,Telefono,Genero")] Cliente cliente)
        {
            if (id != cliente.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Mantener el estado activo y el PIN anterior al actualizar
                    var existingCliente = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    if(existingCliente != null) {
                        cliente.IsActivo = existingCliente.IsActivo;
                        cliente.PinAcceso = existingCliente.PinAcceso;
                    }
                    else {
                        cliente.IsActivo = true;
                    }
                    _context.Update(cliente);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "¡Datos del cliente actualizados correctamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClienteExists(cliente.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                cliente.IsActivo = false; // Soft delete
                _context.Clientes.Update(cliente);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "¡Cliente inactivado con éxito!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.Id == id);
        }
    }
}
