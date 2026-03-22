using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    [Table("Asistencias")]
    [Index(nameof(FechaHoraEntrada), Name = "IX_Asistencias_FechaHoraEntrada")]
    public class Asistencia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; } // Modificado a ClienteId

        [Required]
        public DateTime FechaHoraEntrada { get; set; }

        public DateTime? FechaHoraSalida { get; set; }

        [MaxLength(50)]
        public string MetodoRegistro { get; set; }

        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }
    }
}