using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FactorFitGym.Web.Models
{
    [Table("ReservacionesCanchas")]
    [Index(nameof(FechaHoraInicio), nameof(Estado), Name = "IX_Reservaciones_FechaInicio_Estado")]
    public class ReservacionCancha
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NombreCliente { get; set; }

        /// <summary>Básquetbol | Voleibol | Fútbol | Evento</summary>
        [Required]
        [MaxLength(50)]
        public string TipoUso { get; set; }

        [Required]
        public DateTime FechaHoraInicio { get; set; }

        public DateTime? FechaHoraFin { get; set; }

        /// <summary>Confirmada | Cancelada | Completada</summary>
        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = "Confirmada";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal MontoTotal { get; set; }

        [MaxLength(50)]
        public string MetodoPago { get; set; } = "Efectivo";

        [MaxLength(50)]
        public string NumeroRecibo { get; set; }

        public DateTime? FechaPago { get; set; }

        [MaxLength(20)]
        public string EstadoPago { get; set; } = "Pagado";
    }
}