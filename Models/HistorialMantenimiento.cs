using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FactorFitGym.Web.Models
{
    [Table("HistorialMantenimientos")]
    public class HistorialMantenimiento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EquipoId { get; set; }

        [ForeignKey("EquipoId")]
        public virtual Equipo Equipo { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Seleccione el tipo de intervención.")]
        [MaxLength(50)]
        public string Tipo { get; set; } 
        // "Mantenimiento Preventivo", "Reparación / Correctivo", "Reporte de Desgaste", "Inspección Rutinaria", "Limpieza y Ajuste"

        [Required(ErrorMessage = "Ingrese la descripción o detalle de la intervención.")]
        [MaxLength(500)]
        public string Descripcion { get; set; }

        [MaxLength(100)]
        public string Responsable { get; set; } // Nombre del técnico o personal

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Costo { get; set; }

        public bool RegistrarComoGasto { get; set; } = false;

        [MaxLength(50)]
        public string EstadoPosterior { get; set; } = "Operativo"; 
        // "Operativo", "Desgastado", "En Mantenimiento", "Fuera de Servicio", "De Baja"

        public DateTime? ProximaFechaSugerida { get; set; }
    }
}
