using Microsoft.AspNetCore.Mvc;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

public class ClientesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ClientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Vista para registrar un nuevo cliente (GET)
    [HttpGet]
    public IActionResult RegistrarCliente()
    {
        return View();
    }

    // Registrar un nuevo cliente (POST)
    [HttpPost]
    public async Task<IActionResult> RegistrarCliente(Cliente cliente)
    {
        if (ModelState.IsValid)
        {
            // Validación del RUC (Debe ser de 11 dígitos)
            if (cliente.Ruc?.Length != 11)
            {
                ModelState.AddModelError("Ruc", "El RUC debe tener exactamente 11 dígitos.");
                return View(cliente);
            }

            // Agregar el cliente a la base de datos
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // Redirigir al listado de clientes o a la página de inicio
            return RedirectToAction("Index", "Home");
        }

        return View(cliente);
    }
}