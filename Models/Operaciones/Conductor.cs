using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones;

public class Conductor
{
    public int Id { get; set; }

    [Required]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener exactamente 8 dígitos.")]
    public string Dni { get; set; } = string.Empty;

    [Required]
    public string Nombres { get; set; } = string.Empty;

    [Required]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^9\d{8}$", ErrorMessage = "El celular debe tener 9 dígitos y empezar con 9.")]
    public string Celular { get; set; } = string.Empty;

    [Required]
    public string NumeroLicencia { get; set; } = string.Empty;

    [Required]
    public string CategoriaLicencia { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime VencimientoLicencia { get; set; }

    public string EstadoOperativo { get; set; } = "Activo";

    public bool IsDeleted { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}