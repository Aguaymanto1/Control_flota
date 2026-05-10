namespace Control_flota.Models.Operaciones;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
public class SolicitudServicio
{
    public int Id { get; set; }

    [Required]
    public string Origen { get; set; }

    [Required]
    public string Destino { get; set; }

    [Required]
    [Range(0.1, double.MaxValue)]
    public double PesoKg { get; set; }

    public string EstadoSolicitud { get; set; } = "Pendiente"; // Estado predeterminado

    public DateTime FechaDespacho { get; set; } = DateTime.Now; // Fecha de solicitud autocompletada

    public string? Codigo { get; set; } // Código único

    public string TipoCarga { get; set; } // Código único

    [Range(1, int.MaxValue, ErrorMessage = "El cliente es obligatorio.")]

    public int ClienteId { get; set; } // Relación con Cliente
        
    [ValidateNever]  // ← CLAVE: evita que EF exija el objeto completo en el POST

    public virtual Cliente Cliente { get; set; } // Relación con Cliente

    public int? UnidadId { get; set; } // Relación con Unidad (opcional)
    public int? ConductorId { get; set; } // Relación con Conductor (opcional)
}