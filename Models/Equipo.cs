using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FactorFitGym.Web.Models
{
    [Table("Equipos")]
    public class Equipo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [MaxLength(150)]
        public string Nombre { get; set; } // Ej: "Mancuernas 25 Lbs (Par)", "Caminadora Matrix #1", "Discos 45 Lbs"

        [MaxLength(50)]
        public string Codigo { get; set; } // Ej: "MC-25", "CAM-01", "PO-03"

        [Required(ErrorMessage = "Seleccione una categoría.")]
        [MaxLength(50)]
        public string Categoria { get; set; } // "Cardio", "Fuerza y Máquinas", "Peso Libre", "Accesorios y Funcional"

        [MaxLength(100)]
        public string Ubicacion { get; set; } // "Zona de Mancuernas", "Área de Cardio", "Planta Alta", "Funcional"

        [Range(1, 1000, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Cantidad { get; set; } = 1;

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Operativo"; 
        // "Operativo", "Desgastado", "En Mantenimiento", "Fuera de Servicio", "Completado", "De Baja"

        [MaxLength(50)]
        public string NivelDesgaste { get; set; } = "Ninguno"; 
        // "Ninguno", "Leve", "Moderado", "Crítico"

        [Display(Name = "Frecuencia de Mantenimiento (Días)")]
        [Range(1, 3650, ErrorMessage = "La frecuencia debe ser de al menos 1 día.")]
        public int? FrecuenciaMantenimientoDias { get; set; } // Ej: 30, 60, 90 días

        public DateTime? UltimoMantenimiento { get; set; }

        public DateTime? ProximoMantenimiento { get; set; }

        public DateTime? FechaAdquisicion { get; set; }

        [MaxLength(500)]
        public string Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Historial de mantenimientos
        public virtual ICollection<HistorialMantenimiento> Mantenimientos { get; set; } = new List<HistorialMantenimiento>();

        // Propiedades calculadas (NotMapped) para lógica de alertas
        [NotMapped]
        public bool MantenimientoVencido => ProximoMantenimiento.HasValue && ProximoMantenimiento.Value.Date < DateTime.Today;

        [NotMapped]
        public bool MantenimientoProximo => ProximoMantenimiento.HasValue && 
            ProximoMantenimiento.Value.Date >= DateTime.Today && 
            ProximoMantenimiento.Value.Date <= DateTime.Today.AddDays(7);

        [NotMapped]
        public bool RequiereAtencion => Estado != "Completado" && Estado != "De Baja" && 
            (Estado == "Desgastado" || Estado == "Fuera de Servicio" || MantenimientoVencido || MantenimientoProximo);

        [NotMapped]
        public int? DiasParaProximoMantenimiento => ProximoMantenimiento.HasValue 
            ? (int)(ProximoMantenimiento.Value.Date - DateTime.Today).TotalDays 
            : null;
    }
}
