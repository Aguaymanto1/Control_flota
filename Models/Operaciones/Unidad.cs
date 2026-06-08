using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones;

public class Unidad
{
    public int Id { get; set; }

    [Required (ErrorMessage = "La placa es obligatoria.")]
    [RegularExpression(@"^[A-Z0-9]{3}-[0-9]{3}$", 
        ErrorMessage = "La placa debe tener formato ABC-123 o A1B-234.")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de unidad es obligatorio.")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    public string Marca { get; set; } = string.Empty;


    [Required(ErrorMessage = "El modelo es obligatorio.")]
    public string Modelo { get; set; } = string.Empty;

    [Range(1990, 2100, ErrorMessage = "El año debe estar entre 1990 y 2100.")]
    [Required(ErrorMessage = "El año es obligatorio.")]
    public int? Anio { get; set; }


    [Required(ErrorMessage = "La capacidad es obligatoria.")]
    public decimal? CapacidadKg { get; set; }

    public string EstadoOperativo { get; set; } = "SIN INSPECCION";

    [Required(ErrorMessage = "La fecha de vencimiento del SOAT es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime? VencimientoSoat { get; set; }

    [Required(ErrorMessage = "La fecha de vencimiento de la revisión técnica es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime? VencimientoRevisionTecnica { get; set; }

    [Required(ErrorMessage = "La fecha de vencimiento del MTC es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime? VencimientoMtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string Actividad { get; set; } = "Libre";

    [Required(ErrorMessage = "El kilometraje es obligatorio.")]
    public int KilometrajeActual { get; set; } = 0;
}