using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Membresia> Membresias { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<ReservacionCancha> ReservacionesCanchas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<ConfiguracionPrecio> ConfiguracionPrecios { get; set; }
        public DbSet<Gasto> Gastos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>().HasData(new Usuario
            {
                Id = 1,
                Username = "admin",
                Nombre = "Administrador",
                Apellido = "Sistema",
                Email = "admin@factorgym.com",
                PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=",
                Rol = "Administrador",
                FechaRegistro = new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
            });
        }
    }
}