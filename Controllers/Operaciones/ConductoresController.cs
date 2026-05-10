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

    public async Task<IActionResult> Index()
    {
        var conductores = await _context.Conductores.ToListAsync();
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

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var conductor = await _context.Conductores.FindAsync(id);

    if (conductor == null)
    {
        return NotFound();  // Si no se encuentra el conductor, retorna 404
    }

    try
    {
        _context.Conductores.Remove(conductor);  // Elimina el conductor
        await _context.SaveChangesAsync();  // Guarda los cambios

        // Agregar un log para asegurarse de que la eliminación fue exitosa
        Console.WriteLine($"Conductor con ID {id} eliminado exitosamente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al eliminar el conductor: {ex.Message}");
        return View("Error");  // O redirigir a una página de error personalizada
    }

    // Redirige al listado de conductores
    return RedirectToAction(nameof(Index));
}
}