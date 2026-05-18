using System.ComponentModel.DataAnnotations;

namespace Control_flota.Models.Operaciones
{
    public class Inspeccion
    {
        public int Id { get; set; }

        public string Placa { get; set; } = string.Empty;

        public string Luces { get; set; } = string.Empty;

        public string Llantas { get; set; } = string.Empty;

        public string Frenos { get; set; } = string.Empty;

        public string Fluidos { get; set; } = string.Empty;

        public string EstadoVehiculo { get; set; } = string.Empty;

        public DateTime FechaInspeccion { get; set; }
    }
}