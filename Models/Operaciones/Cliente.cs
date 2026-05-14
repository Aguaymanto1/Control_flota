namespace Control_flota.Models.Operaciones;

public class Cliente
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Ruc { get; set; }

    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    public ICollection<SolicitudServicio> SolicitudesServicio { get; set; } = new List<SolicitudServicio>();
}
