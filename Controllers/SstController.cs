using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Microsoft.AspNetCore.Authorization;

namespace Control_flota.Controllers;

[Authorize(Roles = "SST")]
public class SstController : Controller
{
    private readonly ApplicationDbContext _context;

    public SstController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var ordenesEnTransito = await _context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Conductor)
            .Include(o => o.NotificacionesParada)
            .Where(o => o.Estado == "En Tránsito" && o.ConductorId != null)
            .ToListAsync();

        var estadosIniciales = await _context.EstadosInicialesRuta
            .ToListAsync();

        ViewBag.EstadosIniciales = estadosIniciales;
        ViewBag.LimiteHoras = 5;

        return View(ordenesEnTransito);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotificarParada(int ordenId, int conductorId)
    {
        var yaNotificado = await _context.NotificacionesParada
            .AnyAsync(n => n.OrdenId == ordenId && n.Leida == false);

        if (yaNotificado)
        {
            TempData["Error"] = "Ya existe una notificación pendiente para este conductor.";
            return RedirectToAction(nameof(Index));
        }

        var notificacion = new NotificacionParada
        {
            OrdenId = ordenId,
            ConductorId = conductorId,
            FechaNotificacion = DateTime.UtcNow,
            Leida = false
        };

        _context.NotificacionesParada.Add(notificacion);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Notificación de parada enviada al conductor.";
        return RedirectToAction(nameof(Index));
    }
}