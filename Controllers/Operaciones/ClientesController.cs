using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

public class ClientesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ClientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // INDEX con búsqueda
    public async Task<IActionResult> Index(string? buscar)
    {
        var clientes = _context.Clientes.AsQueryable();

        if (!string.IsNullOrEmpty(buscar) && buscar.Length >= 3)
            clientes = clientes.Where(c =>
                c.Nombre.Contains(buscar) ||
                (c.Ruc != null && c.Ruc.Contains(buscar)));

        var lista = await clientes
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        ViewBag.Buscar = buscar;
        return View(lista);
    }

    // REGISTRAR (GET)
    [HttpGet]
    public IActionResult RegistrarCliente() => View();

    // REGISTRAR (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarCliente(Cliente cliente)
    {
        // Validar RUC duplicado
        var existe = await _context.Clientes.AnyAsync(c => c.Ruc == cliente.Ruc);
        if (existe)
            ModelState.AddModelError("Ruc", "Ya existe un cliente con ese RUC.");

        if (!ModelState.IsValid)
            return View(cliente);

        cliente.Estado = "Activo";
        cliente.FechaRegistro = DateTime.Now;

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Cliente registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // EDITAR (GET)
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    // EDITAR (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Cliente cliente)
    {
        if (id != cliente.Id) return NotFound();

        // El RUC no se puede cambiar — lo tomamos de la BD
        var original = await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (original == null) return NotFound();

        cliente.Ruc = original.Ruc;           // RUC intacto
        cliente.FechaRegistro = original.FechaRegistro;
        cliente.Estado = original.Estado;

        if (!ModelState.IsValid)
            return View(cliente);

        _context.Update(cliente);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // INACTIVAR (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inactivar(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound();

        cliente.Estado = "Inactivo";
        _context.Update(cliente);
        await _context.SaveChangesAsync();

        TempData["Exito"] = $"Cliente '{cliente.Nombre}' inactivado. Su historial se conserva.";
        return RedirectToAction(nameof(Index));
    }
}