namespace Control_flota.Models.Operaciones;

public class EvidenciaRuta
{
    public int Id { get; set; }
    public int OrdenId { get; set; }
    public string RutaImagen { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public Orden? Orden { get; set; }
}