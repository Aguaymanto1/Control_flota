using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using System.Linq;

public class SolicitudesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SolicitudesController(ApplicationDbContext context)
    {
        _context = context;
    }


    // Método para registrar un nuevo servicio (GET)
    [HttpGet]
    public IActionResult RegistrarServicio()
    {
        // Obtener la lista de clientes desde la base de datos
        var clientes = _context.Clientes.ToList();
        
        // Pasar la lista de clientes a la vista
        ViewBag.Clientes = clientes;

        // Opciones para el campo "TipoCarga"
        ViewBag.TipoCargaOptions = new List<string> { "Carga General", "Carga Refrigerada", "Carga Peligrosa" };

        return View();
    }

    // Método para registrar un nuevo servicio (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarServicio(SolicitudServicio solicitud, string? returnUrl)
    {
        if (ModelState.IsValid)
        {
            // Validación del peso
            if (solicitud.PesoKg <= 0)
            {
                ModelState.AddModelError("PesoKg", "El peso debe ser un valor positivo.");
                return View(solicitud);
            }

            // Validar que el ClienteId sea válido
            if (solicitud.ClienteId == 0)
            {
                ModelState.AddModelError("ClienteId", "El cliente es obligatorio.");
                return View(solicitud);
            }

            // Si el campo "Codigo" está vacío, generarlo automáticamente
            if (string.IsNullOrEmpty(solicitud.Codigo))
            {
                solicitud.Codigo = $"SRV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            }

            // Asegurarse de que el TipoCarga esté seleccionado
            if (string.IsNullOrEmpty(solicitud.TipoCarga))
            {
                ModelState.AddModelError("TipoCarga", "El tipo de carga es obligatorio.");
                return View(solicitud);
            }

            // Establecer el estado y la fecha de despacho por defecto
            solicitud.EstadoSolicitud = "Pendiente de Asignación";
            solicitud.FechaDespacho = DateTime.Now;

            // Guardar la nueva solicitud en la base de datos
            _context.Add(solicitud);
            await _context.SaveChangesAsync();

            // Redirigir a la lista de solicitudes o al origen si viene de Despacho
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // Si el formulario tiene errores, volver a pasar la lista de clientes a la vista
        ViewBag.Clientes = _context.Clientes.ToList();
        ViewBag.TipoCargaOptions = new List<string> { "Carga General", "Carga Refrigerada", "Carga Peligrosa" };
        
        return View(solicitud);
    }

    // Método para mostrar la lista de solicitudes
public async Task<IActionResult> Index()
{
    var solicitudes = await _context.Set<SolicitudServicio>()
    .Include(s => s.Cliente)
    .Include(s => s.Conductor)
    .Include(s => s.Unidad)
    .OrderByDescending(s => s.Id)
    .ToListAsync();

    // Obtener los IDs de solicitudes que ya tienen órdenes emitidas
    var ordenesEmitidas = await _context.Ordenes
        .Where(o => o.Estado == "Emitida")
        .Select(o => o.SolicitudServicioId)
        .ToListAsync();

    ViewBag.OrdenesEmitidasIds = new HashSet<int>(ordenesEmitidas);

    // CLIENTES PARA EL MODAL
    ViewBag.Clientes = _context.Clientes.ToList();

    // TIPOS DE CARGA PARA EL MODAL
    ViewBag.TipoCargaOptions = new List<string>
    {
        "Carga General",
        "Carga Refrigerada",
        "Carga Peligrosa"
    };

    return View(solicitudes);
}
 // Acción para ver los detalles de una solicitud
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Cliente)  // Para cargar los datos del cliente relacionado
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
        {
            return NotFound();  // Si no se encuentra la solicitud, retorna 404
        }

        // Buscar si ya existe una orden vinculada a esta solicitud
        var orden = await _context.Ordenes
            .FirstOrDefaultAsync(o => o.SolicitudServicioId == id);

        ViewBag.OrdenId = orden?.Id;

        return View(solicitud);  // Pasa la solicitud a la vista
    }

    // Acción para generar una Orden a partir de una Solicitud asignada
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerarOrden(int id)
    {
        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Cliente)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
            return NotFound();

        if (solicitud.EstadoSolicitud == "Completado")
        {
            TempData["Error"] = "No puedes modificar una solicitud que ya ha sido completada de forma definitiva.";
            return RedirectToAction(nameof(Index));
        }
        // Solo permitir generación si la solicitud está asignada
        if (solicitud.EstadoSolicitud != "Asignado")
        {
            TempData["Error"] = "La orden sólo puede generarse desde una solicitud asignada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Obtener datos de conductor y unidad si existen
        string nombreConductor = string.Empty;
        string placaCamion = string.Empty;

        if (solicitud.ConductorId.HasValue)
        {
            var conductor = await _context.Conductores.FindAsync(solicitud.ConductorId.Value);
            if (conductor != null)
                nombreConductor = conductor.Nombres + " " + conductor.Apellidos;
        }
        if (solicitud.UnidadId.HasValue)
        {
            var unidad = await _context.Unidades.FindAsync(solicitud.UnidadId.Value);
            if (unidad != null)
                placaCamion = unidad.Placa;
        }

        // Fecha de emisión estrictamente del reloj del servidor
        var now = DateTime.Now;

        // Calcular correlativo para el mes actual
        var year = now.Year;
        var month = now.Month;
        var count = await _context.Ordenes
            .Where(o => o.FechaEmision.Year == year && o.FechaEmision.Month == month)
            .CountAsync();
        var correlativo = count + 1;
        var codigo = $"ORD-{year:0000}-{month:00}-{correlativo:0000}";

        var orden = new Orden
        {
            Codigo = codigo,
            FechaEmision = now,
            Estado = "Emitida",
            ClienteId = solicitud.ClienteId,
            Origen = solicitud.Origen,
            Destino = solicitud.Destino,
            NombreConductor = nombreConductor,
            PlacaCamion = placaCamion,
            SolicitudServicioId = solicitud.Id,
            ConductorId = solicitud.ConductorId
        };

        _context.Ordenes.Add(orden);
        await _context.SaveChangesAsync();

        // Redirigir a la vista detalle de la orden recién creada
        return RedirectToAction("Details", "Ordenes", new { id = orden.Id });
    }


   [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Eliminar(int id)
{
    // Buscar la solicitud
    var solicitud = await _context.SolicitudesServicio.FindAsync(id);

    if (solicitud == null)
    {
        return NotFound();
    }

    // Liberar conductor
    if (solicitud.ConductorId != null)
    {
        var conductor = await _context.Conductores
            .FindAsync(solicitud.ConductorId);

        if (conductor != null)
        {
            conductor.Actividad = "Libre";
        }
    }

    // Liberar unidad
    if (solicitud.UnidadId != null)
    {
        var unidad = await _context.Unidades
            .FindAsync(solicitud.UnidadId);

        if (unidad != null)
        {
            unidad.Actividad = "Libre";
        }
    }

    // Eliminar solicitud
    _context.SolicitudesServicio.Remove(solicitud);

    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}



[HttpGet]
public IActionResult AsignarFlota(int id)
{
    // OBTENER LA SOLICITUD
    var solicitud = _context.SolicitudesServicio.Find(id);

    if (solicitud == null)
    {
        return NotFound();
    }

    // CONDUCTORES DISPONIBLES
    var conductores = _context.Conductores
        .Where(c => c.Actividad == "Libre")
        .ToList();

    // SOLO UNIDADES APTAS Y LIBRES
   var unidades = _context.Unidades
    .Where(u =>
        u.Actividad == "Libre"
        && u.EstadoOperativo == "Inspeccionado - Apto"
        && u.CapacidadKg.HasValue
        && (double)u.CapacidadKg.Value >= solicitud.PesoKg
    )
    .OrderBy(u => u.Placa)
    .ToList();
    
   
    // SI NO HAY UNIDADES APTAS
    if (!unidades.Any())
    {
        ViewBag.UnidadesMensaje =
            "No hay unidades inspeccionadas y libres disponibles.";
    }

    ViewBag.Conductores = conductores;
    ViewBag.Unidades = unidades;
    ViewBag.SolicitudId = id;

    return View();
}
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult AsignarFlota(int SolicitudId, int ConductorId, int UnidadId)
{
    // VALIDACIONES
    if (ConductorId == 0 && UnidadId == 0)
    {
        TempData["Error"] =
            "Seleccione conductor y unidad.";

        return RedirectToAction(
            "AsignarFlota",
            new { id = SolicitudId });
    }

    if (ConductorId == 0)
    {
        TempData["Error"] =
            "Seleccione un conductor.";

        return RedirectToAction(
            "AsignarFlota",
            new { id = SolicitudId });
    }

    if (UnidadId == 0)
    {
        TempData["Error"] =
            "Seleccione una unidad.";

        return RedirectToAction(
            "AsignarFlota",
            new { id = SolicitudId });
    }

    var solicitud =
        _context.SolicitudesServicio.Find(SolicitudId);

    var conductor =
        _context.Conductores.Find(ConductorId);

    var unidad =
        _context.Unidades.Find(UnidadId);

    if (solicitud == null ||
        conductor == null ||
        unidad == null)
    {
        return NotFound();
    }

    if (solicitud.EstadoSolicitud == "Completado")
    {
        TempData["Error"] = "No puedes modificar una solicitud que ya ha sido completada de forma definitiva.";
        return RedirectToAction(nameof(Index));
    }

    // CAMBIAR ESTADO DE LA SOLICITUD
    solicitud.EstadoSolicitud = "Asignado";

    // GUARDAR RELACIÓN
    solicitud.ConductorId = ConductorId;
    solicitud.UnidadId = UnidadId;

    // CAMBIAR ACTIVIDAD DEL CONDUCTOR
    conductor.Actividad = "Ocupado";

    // CAMBIAR ACTIVIDAD DE LA UNIDAD
    unidad.Actividad = "Ocupado";

    // GUARDAR FECHA Y HORA
    solicitud.FechaDespacho = DateTime.Now;

    _context.SaveChanges();

    TempData["Success"] =
        "Flota asignada correctamente.";

    return RedirectToAction("Index");
}
}