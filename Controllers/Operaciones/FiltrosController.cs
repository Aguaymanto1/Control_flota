using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

public class FiltrosController : Controller
{
    private readonly ApplicationDbContext _context;

    public FiltrosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(decimal? capacidadKg)
    {
        var query = _context.Unidades.AsQueryable();

        if (capacidadKg.HasValue)
        {
            query = query.Where(u => u.CapacidadKg == capacidadKg.Value);
            ViewBag.Mensaje = $"Unidades filtradas por {capacidadKg.Value}Kg.";
        }

        var unidades = await query.ToListAsync();
        return View(unidades);
    }
}