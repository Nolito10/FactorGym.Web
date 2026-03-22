using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FactorFitGym.Web.Models
{
    [Table("Ventas")]
    public class Venta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ClienteId { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }

        [MaxLength(50)]
        public string MetodoPago { get; set; }

        [MaxLength(50)]
        public string NumeroRecibo { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        public ICollection<DetalleVenta> DetallesVenta { get; set; }
    }
}
