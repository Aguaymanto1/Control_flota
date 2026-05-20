using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La Razón Social es obligatoria.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El RUC es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe tener exactamente 11 dígitos numéricos.")]
    public string? Ruc { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    public string? Direccion { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string? Correo { get; set; }

    public string? PersonaContacto { get; set; } // opcional

    public string Estado { get; set; } = "Activo"; // autogenerado

    public DateTime FechaRegistro { get; set; } = DateTime.Now; // autogenerado

    public ICollection<SolicitudServicio> SolicitudesServicio { get; set; } = new List<SolicitudServicio>();
}