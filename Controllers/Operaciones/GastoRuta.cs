using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_flota.Models.Operaciones;

public class GastoRuta
{
    public int Id { get; set; }

    public int OrdenId { get; set; }
    public virtual Orden? Orden { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio.")]
    public string Concepto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Monto { get; set; }

    public string? RutaComprobante { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}