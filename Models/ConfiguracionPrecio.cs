using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FactorFitGym.Web.Models
{
    [Table("ConfiguracionPrecios")]
    public class ConfiguracionPrecio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Clave { get; set; } // Identificador único ej. PLAN_BASICO

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } // Ej. Plan Básico Mensual

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        [MaxLength(255)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }
    }
}
