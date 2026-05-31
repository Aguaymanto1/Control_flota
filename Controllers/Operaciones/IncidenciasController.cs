using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Control_flota.Data;
using Control_flota.Models.ViewModels;

namespace Control_flota.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class IncidenciasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IncidenciasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
{
    var incidencias = await _context.IncidenciasRuta
        .Include(i => i.Conductor)
        .Include(i => i.Orden)
        .ToListAsync();

    var resumen = incidencias
        .GroupBy(i => new { i.ConductorId, i.OrdenId })
        .Select(g => new IncidenciaResumenViewModel
        {
            ConductorId = g.Key.ConductorId,
            OrdenId = g.Key.OrdenId,
            NombreConductor = g.First().Conductor.Nombres + " " + g.First().Conductor.Apellidos,
            CodigoOrden = g.First().Orden.Codigo,
            CantidadIncidencias = g.Count(),
            FechaUltimaIncidencia = g.Max(x => x.FechaReporte),
            Estado = g.Any(x => x.Estado == "Pendiente")
                ? "Pendiente"
                : "Atendido"
        })
        .OrderByDescending(x => x.FechaUltimaIncidencia)
        .ToList();

    return View(resumen);
}
public async Task<IActionResult> Detalles(int conductorId, int ordenId)
{
    var incidencias = await _context.IncidenciasRuta
        .Include(i => i.Conductor)
        .Include(i => i.Orden)
        .Where(i => i.ConductorId == conductorId
                 && i.OrdenId == ordenId)
        .OrderByDescending(i => i.FechaReporte)
        .ToListAsync();

    if (!incidencias.Any())
        return NotFound();

    return View(incidencias);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Atender(int conductorId, int ordenId)
{
    var incidencias = await _context.IncidenciasRuta
        .Where(i => i.ConductorId == conductorId
                 && i.OrdenId == ordenId)
        .ToListAsync();

    foreach (var incidencia in incidencias)
    {
        incidencia.Estado = "Atendido";
    }

    await _context.SaveChangesAsync();

    TempData["Exito"] = "Incidencias atendidas correctamente.";

    return RedirectToAction(nameof(Index));
}
    }
}