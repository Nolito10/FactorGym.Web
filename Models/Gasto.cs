using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FactorFitGym.Web.Models
{
    [Table("Gastos")]
    public class Gasto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El concepto del gasto es obligatorio.")]
        [MaxLength(200)]
        public string Concepto { get; set; }

        [Required(ErrorMessage = "Selecciona una categoría.")]
        [MaxLength(50)]
        public string Categoria { get; set; } // Ej: Productos, Mantenimiento, Servicios, Varios

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 1000000, ErrorMessage = "El monto debe ser mayor a 0.")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [MaxLength(100)]
        public string Referencia { get; set; } // Ej: Factura #1234, Folio de Ticket
    }
}
