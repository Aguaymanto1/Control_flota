using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

public class OrdenesController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrdenesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lista general de órdenes
    public async Task<IActionResult> Index()
    {

        ViewBag.Clientes = _context.Clientes.ToList();

        ViewBag.TipoCargaOptions = new List<string>
        {
            "Carga General",
            "Carga Refrigerada",
            "Carga Peligrosa"
        };

        var ordenes = await _context.Ordenes
            .Include(o => o.Cliente)
            .OrderByDescending(o => o.FechaEmision)
            .ToListAsync();

        return View(ordenes);
    }

    // Detalle de orden
    public async Task<IActionResult> Details(int id)
    {
        var orden = await _context.Ordenes
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orden == null)
            return NotFound();

        // Cargar la solicitud relacionada y sus datos de conductor/unidad si existen
        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        ViewBag.Solicitud = solicitud;
        ViewBag.Conductor = solicitud?.Conductor;
        ViewBag.Unidad = solicitud?.Unidad;

        return View(orden);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);
        if (orden == null)
            return NotFound();

        _context.Ordenes.Remove(orden);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
