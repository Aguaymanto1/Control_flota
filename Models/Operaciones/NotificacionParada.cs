namespace Control_flota.Models.Operaciones;

public class NotificacionParada
{
    public int Id { get; set; }
    public int OrdenId { get; set; }
    public int ConductorId { get; set; }
    public DateTime FechaNotificacion { get; set; }
    public bool Leida { get; set; } = false;

    public Orden? Orden { get; set; }
    public Conductor? Conductor { get; set; }
}