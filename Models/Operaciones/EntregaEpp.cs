using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Control_flota.Models.Operaciones;

public class EntregaEpp
{
    public int Id { get; set; }

    [Required]
    public int ConductorId { get; set; }

    [ForeignKey("ConductorId")]
    public Conductor? Conductor { get; set; }

    public DateTime FechaEntrega { get; set; } = DateTime.Now;

    public bool EntregoCasco { get; set; }
    public bool EntregoBotas { get; set; }
    public bool EntregoChaleco { get; set; }

    [Required(ErrorMessage = "La firma del conductor es obligatoria para validar la entrega.")]
    public string FirmaDigitalBase64 { get; set; } = string.Empty;
}