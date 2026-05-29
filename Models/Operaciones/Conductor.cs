using System.ComponentModel.DataAnnotations;
using Control_flota.Models.Login;

namespace Control_flota.Models.Operaciones;

public class Conductor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener exactamente 8 dígitos.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El celular es obligatorio.")]
    [RegularExpression(@"^9\d{8}$", ErrorMessage = "El celular debe tener 9 dígitos y empezar con 9.")]
    public string Celular { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de licencia es obligatorio.")]
    public string NumeroLicencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría de la licencia es obligatoria.")]
    public string CategoriaLicencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de vencimiento de la licencia es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime VencimientoLicencia { get; set; }

    public string EstadoOperativo { get; set; } = "Activo";

    public bool IsDeleted { get; set; }= false;

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public string Actividad { get; set; } = "Libre";

        // Relación con ApplicationUser(Usuario)
        public string? UserId { get; set; }
   
 

}