using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones;

public class EstadoInicialRuta
{
    public int Id { get; set; }

    public int OrdenId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Ingrese una cantidad válida de combustible")]
    public decimal CombustibleInicial { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Ingrese un kilometraje válido")]
    public int KilometrajeInicial { get; set; }

    public string? Observacion { get; set; }

    public string? RutaImagen { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
