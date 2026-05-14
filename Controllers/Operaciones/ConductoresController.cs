using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

public class ConductoresController : Controller
{
    private readonly ApplicationDbContext _context;

    public ConductoresController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. Muestra solo los conductores que NO están borrados lógicamente
    public async Task<IActionResult> Index()
    {
        var conductores = await _context.Conductores
            .Where(c => !c.IsDeleted) 
            .ToListAsync();
        return View(conductores);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Conductor conductor)
    {
        if (ModelState.IsValid)
        {
            _context.Add(conductor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(conductor);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var conductor = await _context.Conductores.FindAsync(id);
        if (conductor == null) return NotFound();
        return View(conductor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Conductor conductor)
    {
        if (id != conductor.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(conductor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(conductor);
    }

    // 2. Aquí está el cambio solicitado para Jesús: Borrado Lógico
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var conductor = await _context.Conductores.FindAsync(id);

        if (conductor == null) return NotFound();

        try
        {
            // Criterio: NO eliminar la data física, solo marcarla
            conductor.IsDeleted = true; 
            _context.Update(conductor);
            await _context.SaveChangesAsync(); 
            
            Console.WriteLine($"Conductor con ID {id} marcado como eliminado (lógico).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al procesar: {ex.Message}");
            return View("Error");
        }

        return RedirectToAction(nameof(Index));
    }
}