using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones;

public class ConsumoCombustible
{
    public int Id { get; set; }

    public int? OrdenId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Ingrese galones positivos")]
    public decimal Galones { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Ingrese un monto positivo")]
    public decimal Monto { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    // Kilometros totales registrados en el cierre del viaje (opcional)
    public int? Kilometros { get; set; }

    // Rendimiento calculado al cerrar la orden: km / galón
    public decimal? Rendimiento { get; set; }

    // Relaciones de navegación (opcionales)
    // public Orden? Orden { get; set; }
}
