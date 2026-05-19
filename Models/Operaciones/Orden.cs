using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_flota.Models.Operaciones;

public class Orden
{
    public int Id { get; set; }

    [Required]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    public DateTime FechaEmision { get; set; }

    [Required]
    public string Estado { get; set; } = "Emitida";

    // Datos heredados (solo lectura en vistas)
    public int ClienteId { get; set; }
    public virtual Cliente? Cliente { get; set; }

    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;

    public string NombreConductor { get; set; } = string.Empty;
    public int? ConductorId { get; set; }

    public virtual Conductor? Conductor { get; set; }
    public string PlacaCamion { get; set; } = string.Empty;

    // Relación con la solicitud de servicio original
    public int SolicitudServicioId { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }
}
