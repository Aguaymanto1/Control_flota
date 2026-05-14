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
    public async Task<IActionResult> RegistrarServicio(SolicitudServicio solicitud)
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

            // Redirigir a la lista de solicitudes
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
        .Include(s => s.Cliente) // ← Cargar el objeto Cliente
        .ToListAsync();
    
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

        return View(solicitud);  // Pasa la solicitud a la vista
    }
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Eliminar(int id)
{
    // Buscar la solicitud con el ID proporcionado
    var solicitud = await _context.SolicitudesServicio.FindAsync(id);

    if (solicitud == null)
    {
        return NotFound();  // Si no se encuentra la solicitud, retorna 404
    }

    // Eliminar la solicitud de la base de datos
    _context.SolicitudesServicio.Remove(solicitud);
    await _context.SaveChangesAsync();

    // Redirigir de vuelta a la lista de solicitudes
    return RedirectToAction(nameof(Index));
}
[HttpGet]
public IActionResult AsignarFlota(int id)
{
    // Obtener la solicitud actual
    var solicitud = _context.SolicitudesServicio.Find(id);

    if (solicitud == null)
    {
        return NotFound();
    }

    // CONDUCTORES DISPONIBLES
    var conductores = _context.Conductores
        .Where(c => c.Actividad == "Libre")
        .ToList();

    // UNIDADES DISPONIBLES
    var unidades = _context.Unidades
        .Where(u =>
            u.Actividad == "Libre"      // Solo libres
            && u.EstadoOperativo == "Activo"    // No mantenimiento
            && u.CapacidadKg >= (decimal)solicitud.PesoKg// Soporta peso
        )
        .ToList();

    ViewBag.Conductores = conductores;
    ViewBag.Unidades = unidades;
    ViewBag.SolicitudId = id;

    return View();
}
[HttpPost]
public IActionResult AsignarFlota(int SolicitudId, int ConductorId, int UnidadId)
{
    var solicitud = _context.SolicitudesServicio.Find(SolicitudId);

    var conductor = _context.Conductores.Find(ConductorId);

    var unidad = _context.Unidades.Find(UnidadId);

    if (solicitud == null || conductor == null || unidad == null)
    {
        return NotFound();
    }

    // CAMBIAR ESTADO DE LA SOLICITUD
    solicitud.EstadoSolicitud = "Asignado";

    // CAMBIAR ACTIVIDAD DEL CONDUCTOR
    conductor.Actividad = "Ocupado";

    // CAMBIAR ACTIVIDAD DE LA UNIDAD
    unidad.Actividad = "Ocupado";

    // GUARDAR FECHA Y HORA
    solicitud.FechaDespacho = DateTime.Now;

    _context.SaveChanges();

    return RedirectToAction("Index");
}
}