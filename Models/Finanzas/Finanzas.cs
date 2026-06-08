using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_flota.Models.Finanzas
{
    public class Factura
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrdenId { get; set; }

        [Required]
        [MaxLength(50)]
        public string NumeroFactura { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Cliente { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CodigoOrden { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoBase { get; set; }

        [Column(TypeName = "text")]
        public string SobrecostosJson { get; set; } = "[]";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Required]
        public DateTime FechaEmision { get; set; }

        [Required]
        public DateTime FechaVencimiento { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = "Borrador";

        [MaxLength(500)]
        public string? RutaPdf { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaEnvio { get; set; }

        public DateTime? FechaPago { get; set; }
    }
}