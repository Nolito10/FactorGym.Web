using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    [Table("Usuarios")]
    [Index(nameof(Nombre), nameof(Apellido), Name = "IX_Usuarios_Nombre_Apellido")]
    [Index(nameof(Username), IsUnique = true, Name = "IX_Usuarios_Username")]
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [Required]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Rol { get; set; }

        [Required]
        public DateTime FechaRegistro { get; set; }

        public ICollection<Membresia> Membresias { get; set; }
        public ICollection<Pago> Pagos { get; set; }
        public ICollection<Asistencia> Asistencias { get; set; }
        public ICollection<ReservacionCancha> ReservacionesCanchas { get; set; }
    }
}