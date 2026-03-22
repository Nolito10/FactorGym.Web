using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    [Table("Pagos")]
    [Index(nameof(FechaPago), Name = "IX_Pagos_FechaPago")]
    public class Pago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; } // Cambiado a ClienteId

        [Required]
        public int MembresiaId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaPago { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetodoPago { get; set; }

        [MaxLength(100)]
        public string Referencia { get; set; }

        [MaxLength(50)]
        public string TipoServicio { get; set; }

        [MaxLength(50)]
        public string NumeroRecibo { get; set; }

        [ValidateNever]
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [ValidateNever]
        [ForeignKey("MembresiaId")]
        public Membresia Membresia { get; set; }
    }
}