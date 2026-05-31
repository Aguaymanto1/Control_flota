using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_flota.Models.Operaciones
{
    public class IncidenciaRuta
    {
        public int Id { get; set; }

        [Required]
        public int OrdenId { get; set; }

        [ForeignKey("OrdenId")]
        public Orden? Orden { get; set; }

        [Required]
        public int ConductorId { get; set; }

        [ForeignKey("ConductorId")]
        public Conductor? Conductor { get; set; }

        [Required]
        public string TipoIncidencia { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        public double? Latitud { get; set; }

        public double? Longitud { get; set; }

        public DateTime FechaReporte { get; set; } = DateTime.Now;

        public string Estado { get; set; } = "Pendiente";
    }
}
