namespace Control_flota.Models.ViewModels
{
    public class IncidenciaResumenViewModel
    {
        public int ConductorId { get; set; }

        public int OrdenId { get; set; }

        public string NombreConductor { get; set; } = string.Empty;

        public string CodigoOrden { get; set; } = string.Empty;

        public int CantidadIncidencias { get; set; }

        public DateTime FechaUltimaIncidencia { get; set; }

        public string Estado { get; set; } = string.Empty;
    }
}