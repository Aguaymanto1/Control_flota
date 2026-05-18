using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Control_flota.Models.Operaciones;

public class Unidad
{
    public int Id { get; set; }

    [Required]
    [RegularExpression(@"^[A-Z0-9]{3}-[0-9]{3}$", 
        ErrorMessage = "La placa debe tener formato ABC-123 o A1B-234.")]
    public string Placa { get; set; } = string.Empty;

    [Required]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    public string Marca { get; set; } = string.Empty;

    [Required]
    public string Modelo { get; set; } = string.Empty;

    [Range(1990, 2100)]
    public int Anio { get; set; }

    [Required]
    public decimal CapacidadKg { get; set; }

    public string EstadoOperativo { get; set; } = "Activo";

    [DataType(DataType.Date)]
    public DateTime VencimientoSoat { get; set; }

    [DataType(DataType.Date)]
    public DateTime VencimientoRevisionTecnica { get; set; }

    [DataType(DataType.Date)]
    public DateTime VencimientoMtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string Actividad { get; set; } = "Libre";
}