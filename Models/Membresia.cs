using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    [Table("Membresias")]
    [Index(nameof(FechaFin), nameof(Estado), Name = "IX_Membresias_FechaFin_Estado")]
    public class Membresia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; } // Modificado a ClienteId

        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; }

        [MaxLength(50)]
        public string TipoPlan { get; set; } // Zumba, Cancha, General

        [MaxLength(50)]
        public string Frecuencia { get; set; } // Semanal, Mensual, Hora

        public int? Capacidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Costo { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; }

        [ValidateNever]
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [ValidateNever]
        public ICollection<Pago> Pagos { get; set; }
    }
}