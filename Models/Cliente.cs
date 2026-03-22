using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    [Table("Clientes")]
    [Index(nameof(IsActivo), Name = "IX_Clientes_IsActivo")]
    [Index(nameof(PinAcceso), Name = "IX_Clientes_PinAcceso", IsUnique = true)]
    public class Cliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UsuarioId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [NotMapped]
        public string NombreCompleto => $"{Nombre} {Apellido}";

        [MaxLength(20)]
        public string Telefono { get; set; }

        [Required]
        [MaxLength(15)]
        public string Genero { get; set; } // Masculino o Femenino

        [MaxLength(4)]
        public string PinAcceso { get; set; } // PIN de 4 dígitos para check-in en kiosco

        // Soft Delete flag
        public bool IsActivo { get; set; } = true;

        [ForeignKey("UsuarioId")]
        [ValidateNever]
        public Usuario Usuario { get; set; }

        [ValidateNever]
        public ICollection<Membresia> Membresias { get; set; }
        [ValidateNever]
        public ICollection<Pago> Pagos { get; set; }
        [ValidateNever]
        public ICollection<Asistencia> Asistencias { get; set; }

        [ValidateNever]
        public ICollection<Venta> Ventas { get; set; }
    }
}
