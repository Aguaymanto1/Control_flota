using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

public class UnidadesController : Controller
{
    private readonly ApplicationDbContext _context;

    public UnidadesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Index para mostrar todas las unidades
    public async Task<IActionResult> Index()
    {
        var unidades = await _context.Unidades.ToListAsync();
        return View(unidades); 
    }

    // Crear unidad
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Unidad unidad)
    {
        if (ModelState.IsValid)
        {
            _context.Add(unidad);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(unidad);
    }

    // Editar unidad
    public async Task<IActionResult> Edit(int id)
    {
        var unidad = await _context.Unidades.FindAsync(id);
        if (unidad == null) return NotFound();
        return View(unidad);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Unidad unidad)
    {
        if (id != unidad.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(unidad);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(unidad);
    }

    // Eliminar unidad
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    // Buscar la unidad con el ID proporcionado
    var unidad = await _context.Unidades.FindAsync(id);

    if (unidad == null)
    {
        return NotFound();  // Si no se encuentra la unidad, retorna 404
    }

    try
    {
        _context.Unidades.Remove(unidad); // Elimina la unidad
        await _context.SaveChangesAsync(); // Guarda los cambios

        // Agregar un log para asegurarse de que la eliminación fue exitosa
        Console.WriteLine($"Unidad con ID {id} eliminada exitosamente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al eliminar la unidad: {ex.Message}");
        // Mostrar un mensaje de error o redirigir a una página de error
        return View("Error"); // O redirigir a una página de error personalizada
    }

    // Redirige al listado de unidades
    return RedirectToAction(nameof(Index)); 
}
}