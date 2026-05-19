using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Login;
using Control_flota.Models.Operaciones;


[Authorize(Roles = "Conductor")]
public class PanelConductorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Usuario> _userManager;

    public PanelConductorController(
        ApplicationDbContext context,
        UserManager<Usuario> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario == null || usuario.ConductorId == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var conductorId = usuario.ConductorId.Value;

        var ordenes = await _context.Ordenes
            .Include(o => o.Cliente)
            .Where(o => _context.SolicitudesServicio
                .Where(s => s.ConductorId == conductorId)
                .Select(s => s.Id)
                .Contains(o.SolicitudServicioId))
            .ToListAsync();

        return View(ordenes);
    }

    [HttpPost]
    public async Task<IActionResult> IniciarRuta(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);

        if (orden == null)
            return NotFound();

        orden.Estado = "En Transito";
        orden.FechaInicio = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> FinalizarRuta(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);

        if (orden == null)
            return NotFound();

        if (orden.Estado != "En Transito")
            return BadRequest();

        orden.Estado = "Completado";
        orden.FechaFin = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}