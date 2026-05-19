using System.Collections.Generic;

namespace Control_flota.Models.Operaciones
{
    public class DespachoViewModel
    {
        public IEnumerable<Orden> Ordenes { get; set; } = new List<Orden>();
        public IEnumerable<SolicitudServicio> SolicitudesPendientes { get; set; } = new List<SolicitudServicio>();
    }
}
