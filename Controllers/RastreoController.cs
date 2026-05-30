using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;

public class RastreoController : Controller
{
    private readonly ApplicationDbContext _context;

    public RastreoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Vista pública de consulta
    [HttpGet]
    public IActionResult Consultar(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo))
            return View();

        // Buscar por código de orden O código de solicitud
        var orden = _context.Ordenes
            .Include(o => o.Cliente)
            .FirstOrDefault(o => o.Codigo == codigo.Trim().ToUpper());

        if (orden == null)
        {
            // Intentar buscar por código de solicitud
            var solicitud = _context.SolicitudesServicio
                .Include(s => s.Cliente)
                .FirstOrDefault(s => s.Codigo == codigo.Trim().ToUpper());

            if (solicitud == null)
            {
                ViewBag.Error = "No se encontró ningún servicio con ese código.";
                ViewBag.Codigo = codigo;
                return View();
            }

            ViewBag.EsSolicitud = true;
            ViewBag.Codigo = codigo;
            return View(solicitud);
        }

        ViewBag.EsOrden = true;
        ViewBag.Codigo = codigo;
        return View(orden);
    }

    // Catálogo de coordenadas predefinidas para las principales ciudades del Perú
    private static readonly Dictionary<string, (double Lat, double Lng)> CoordenadasCiudades = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Lima", (-12.046374, -77.042793) },
        { "Callao", (-12.050849, -77.125916) },
        { "Arequipa", (-16.409047, -71.537451) },
        { "Trujillo", (-8.115989, -79.029976) },
        { "Chiclayo", (-6.77137, -79.84411) },
        { "Piura", (-5.19449, -80.63282) },
        { "Iquitos", (-3.74912, -73.25383) },
        { "Cusco", (-13.53195, -71.96734) },
        { "Chimbote", (-9.08528, -78.57833) },
        { "Huancayo", (-12.06513, -75.20486) },
        { "Tacna", (-18.00656, -70.24622) },
        { "Ica", (-14.06777, -75.72861) },
        { "Pucallpa", (-8.37915, -74.55387) },
        { "Sullana", (-4.90389, -80.68528) },
        { "Cajamarca", (-7.16378, -78.50027) },
        { "Chincha", (-13.40985, -76.13028) },
        { "Ayacucho", (-13.15878, -74.22321) },
        { "Huánuco", (-9.93062, -76.24056) },
        { "Puno", (-15.8422, -70.0199) },
        { "Tarapoto", (-6.48694, -76.36528) },
        { "Huaraz", (-9.52778, -77.52778) },
        { "Tumbes", (-3.56694, -80.45139) },
        { "Talara", (-4.57722, -81.27194) },
        { "Moyobamba", (-6.03417, -76.97167) },
        { "Cerro de Pasco", (-10.6675, -76.25611) },
        { "Huancavelica", (-12.7875, -74.9725) },
        { "Abancay", (-13.63389, -72.88139) },
        { "Puerto Maldonado", (-12.59333, -69.18333) },
        { "Moquegua", (-17.19583, -70.93556) }
    };

    private static (double Lat, double Lng) ObtenerCoordenadas(string? ciudad, double defaultLat = -12.046374, double defaultLng = -77.042793)
    {
        if (string.IsNullOrWhiteSpace(ciudad))
            return (defaultLat, defaultLng);

        var limpia = ciudad.Trim();
        if (CoordenadasCiudades.TryGetValue(limpia, out var coords))
            return coords;

        // Fallback: si no se encuentra en el diccionario, genera una coordenada determinista
        // cerca de Lima para que se diferencie de otras ubicaciones.
        int hash = limpia.GetHashCode();
        double offsetLat = (hash % 100) * 0.005;
        double offsetLng = ((hash / 100) % 100) * 0.005;
        return (defaultLat + offsetLat, defaultLng + offsetLng);
    }

    // Endpoint JSON que expone los datos de los camiones activos para el mapa
    [HttpGet]
    public async Task<IActionResult> ObtenerFlotaActiva()
    {
        var ordenesTransito = await _context.Ordenes
            .Include(o => o.Cliente)
            .Where(o => o.Estado == "En Tránsito")
            .ToListAsync();

        var flota = new List<object>();

        // Simulación de progreso por tiempo: ciclo de 120 segundos
        double segundosCiclo = 120;
        double ticksSegundos = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) % segundosCiclo;

        foreach (var o in ordenesTransito)
        {
            var coordOrigen = ObtenerCoordenadas(o.Origen);
            var coordDestino = ObtenerCoordenadas(o.Destino);
            double progress = ticksSegundos / segundosCiclo;

            // Si hay una última ciudad intermedia reportada por el conductor
            if (!string.IsNullOrWhiteSpace(o.UltimaCiudad))
            {
                var coordUltima = ObtenerCoordenadas(o.UltimaCiudad);
                coordOrigen = coordUltima;
                
                // La animación avanza en un sub-ciclo
                progress = (ticksSegundos % (segundosCiclo / 2.0)) / (segundosCiclo / 2.0);
            }

            double lat = coordOrigen.Lat + (coordDestino.Lat - coordOrigen.Lat) * progress;
            double lng = coordOrigen.Lng + (coordDestino.Lng - coordOrigen.Lng) * progress;

            // De momento, todas las rutas iniciadas se muestran sin incidencia (pin verde)
            // hasta que se implemente la funcionalidad de registrar incidencias desde la interfaz gráfica.
            bool tieneIncidencia = false;

            flota.Add(new
            {
                id = o.Id,
                codigo = o.Codigo,
                placa = o.PlacaCamion,
                conductor = o.NombreConductor,
                origen = o.Origen,
                destino = o.Destino,
                ultimaCiudad = o.UltimaCiudad ?? o.Origen,
                latitud = lat,
                longitud = lng,
                tieneIncidencia = tieneIncidencia,
                estado = o.Estado
            });
        }

        return Json(flota);
    }
}