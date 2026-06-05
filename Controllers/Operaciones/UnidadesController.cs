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
    public async Task<IActionResult> Index(string? capacidad)
{
    var query = _context.Unidades.AsQueryable();

    if (!string.IsNullOrEmpty(capacidad))
    {
        switch (capacidad)
        {
            case "2":
                query = query.Where(u => u.CapacidadKg <= 2000);
                break;

            case "5":
                query = query.Where(u => u.CapacidadKg > 2000 &&
                                         u.CapacidadKg <= 5000);
                break;

            case "10":
                query = query.Where(u => u.CapacidadKg > 5000);
                break;
        }
    }

    ViewBag.Capacidad = capacidad;

    var unidades = await query.ToListAsync();

    return View(unidades);
}

// Crear unidad (GET)
[HttpGet]
public IActionResult Create()
{
    return View();
}

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Unidad unidad)
{
    var hoy = DateTime.Today;

if (unidad.VencimientoSoat < hoy)
{
    ModelState.AddModelError("VencimientoSoat",
        "La fecha de vencimiento del SOAT no puede ser anterior a hoy.");
}

if (unidad.VencimientoRevisionTecnica < hoy)
{
    ModelState.AddModelError("VencimientoRevisionTecnica",
        "La fecha de vencimiento de la revisión técnica no puede ser anterior a hoy.");
}

if (unidad.VencimientoMtc < hoy)
{
    ModelState.AddModelError("VencimientoMtc",
        "La fecha de vencimiento del MTC no puede ser anterior a hoy.");
}

if (unidad.CapacidadKg != 2000 &&
    unidad.CapacidadKg != 5000 &&
    unidad.CapacidadKg != 10000)
{
    ModelState.AddModelError(
        "CapacidadKg",
        "Seleccione una capacidad válida."
    );
}
    if (ModelState.IsValid)
    {
        // Verificar si la placa ya existe
        var placaExistente = await _context.Unidades
            .AnyAsync(u => u.Placa == unidad.Placa);

        if (placaExistente)
        {
            ModelState.AddModelError("Placa", "La placa ya está registrada.");
            return View(unidad);
        }

        try
        {
            _context.Add(unidad);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al registrar la unidad: {ex.Message}");

            ModelState.AddModelError("", "Ocurrió un error al registrar la unidad.");

            return View(unidad);
        }
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
    if (id != unidad.Id)
        return NotFound();

    var unidadDb = await _context.Unidades.FindAsync(id);

    if (unidadDb == null)
        return NotFound();

    if (unidad.CapacidadKg != 2000 &&
    unidad.CapacidadKg != 5000 &&
    unidad.CapacidadKg != 10000)
{
    ModelState.AddModelError(
        "CapacidadKg",
        "Seleccione una capacidad válida."
    );
}

    if (ModelState.IsValid)
    {
        var placaNormalizada = unidad.Placa.Trim().ToUpper();

        var placaExistente = await _context.Unidades
            .AnyAsync(u => u.Id != unidad.Id &&
                           u.Placa.ToUpper() == placaNormalizada);

        if (placaExistente)
        {
            ModelState.AddModelError("Placa", "La placa ya está registrada.");
            return View(unidad);
        }

        try
        {
            unidadDb.Placa = placaNormalizada;
            unidadDb.Tipo = unidad.Tipo;
            unidadDb.Marca = unidad.Marca;
            unidadDb.Modelo = unidad.Modelo;
            unidadDb.Anio = unidad.Anio;
            unidadDb.CapacidadKg = unidad.CapacidadKg;
            unidadDb.EstadoOperativo = unidad.EstadoOperativo;
            unidadDb.VencimientoSoat = unidad.VencimientoSoat;
            unidadDb.VencimientoRevisionTecnica = unidad.VencimientoRevisionTecnica;
            unidadDb.VencimientoMtc = unidad.VencimientoMtc;
            unidadDb.Actividad = unidad.Actividad;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al actualizar la unidad: {ex.Message}");

            ModelState.AddModelError("", "Ocurrió un error al actualizar la unidad.");

            return View(unidad);
        }
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