using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Control_flota.Models.Login;
using Microsoft.AspNetCore.Authorization;

namespace Control_flota.Controllers  // ← Agrega el namespace
{
    [Authorize(Roles = "Administrador")]
    public class ConductoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public ConductoresController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // LISTAR
        public async Task<IActionResult> Index()
{
    var conductores = await _context.Conductores
        .Where(c => !c.IsDeleted)
        .ToListAsync();

    return View(conductores);
}

        // CREAR (GET)
        public IActionResult Create() => View();

        
        // CREAR (POST)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Conductor conductor, string email, string password, string confirmPassword)
{
    if (password != confirmPassword)
    {
        ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden");
        return View(conductor);
    }

    // VALIDAR LICENCIA VENCIDA
    if (conductor.VencimientoLicencia < DateTime.Today)
    {
        ModelState.AddModelError("VencimientoLicencia",
            "La licencia no puede estar vencida.");

        return View(conductor);
    }

    // 🔴 SOLUCIÓN: Dile a ASP.NET que no valide el UserId porque lo crearemos después
    ModelState.Remove("UserId");

    if (ModelState.IsValid)
    {
        var conductorExistente = await _context.Conductores
    .FirstOrDefaultAsync(c => c.Dni == conductor.Dni && !c.IsDeleted);

if (conductorExistente != null)
{
    ModelState.AddModelError("Dni",
        "Ya existe un conductor registrado con este DNI.");

    return View(conductor);
}
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            ModelState.AddModelError(string.Empty,
                "El correo electrónico ya está registrado");

            return View(conductor);
        }

        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            Estado = true,
            EmailConfirmed = true,
            ConductorId = null
        };

        var result = await _userManager.CreateAsync(usuario, password);

        if (result.Succeeded)
{
    try
    {
        await _userManager.AddToRoleAsync(usuario, "Conductor");

        conductor.IsDeleted = false;

        _context.Add(conductor);

        await _context.SaveChangesAsync();

        usuario.ConductorId = conductor.Id;

        await _userManager.UpdateAsync(usuario);

        conductor.UserId = usuario.Id;

        _context.Update(conductor);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    catch (Exception)
    {
        // Si falla el registro del conductor,
        // eliminar el usuario creado previamente
        await _userManager.DeleteAsync(usuario);

        ModelState.AddModelError(
            string.Empty,
            "Ocurrió un error al registrar el conductor."
        );
    }
}
        foreach (var error in result.Errors)
{
    string mensaje = error.Description;

    if (error.Code == "PasswordRequiresUpper")
        mensaje = "La contraseña debe contener al menos una letra mayúscula.";

    else if (error.Code == "PasswordRequiresLower")
        mensaje = "La contraseña debe contener al menos una letra minúscula.";

    else if (error.Code == "PasswordRequiresDigit")
        mensaje = "La contraseña debe contener al menos un número.";

    else if (error.Code == "PasswordRequiresNonAlphanumeric")
        mensaje = "La contraseña debe contener al menos un carácter especial.";

    else if (error.Code == "PasswordTooShort")
        mensaje = "La contraseña es demasiado corta.";

    else if (error.Code == "DuplicateEmail")
        mensaje = "El correo electrónico ya está registrado.";

    ModelState.AddModelError(string.Empty, mensaje);
}
    }

    return View(conductor);
}
        // EDITAR (GET)
        public async Task<IActionResult> Edit(int id)
{
    var conductor = await _context.Conductores.FindAsync(id);

    if (conductor == null)
        return NotFound();

    if (!string.IsNullOrEmpty(conductor.UserId))
    {
        var usuario = await _userManager.FindByIdAsync(conductor.UserId);

        ViewBag.Email = usuario?.Email;
    }

    return View(conductor);
}

        // EDITAR (POST)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(
    int id,
    Conductor conductor,
    string email,
    string? nuevaPassword,
    string? confirmarPassword)
{
    if (id != conductor.Id)
        return NotFound();

    var conductorDb = await _context.Conductores.FindAsync(id);

    if (conductorDb == null)
        return NotFound();

    if (conductor.VencimientoLicencia < DateTime.Today)
    {
        ModelState.AddModelError("VencimientoLicencia",
            "La licencia no puede estar vencida.");

        return View(conductor);
    }

    // Validar contraseñas
    if (!string.IsNullOrWhiteSpace(nuevaPassword))
    {
        if (nuevaPassword != confirmarPassword)
        {
            ModelState.AddModelError(string.Empty,
                "Las contraseñas no coinciden.");

            return View(conductor);
        }
    }

    if (ModelState.IsValid)
    {
        // Actualizar conductor
        conductorDb.Nombres = conductor.Nombres;
        conductorDb.Apellidos = conductor.Apellidos;
        conductorDb.Dni = conductor.Dni;
        conductorDb.Celular = conductor.Celular;
        conductorDb.NumeroLicencia = conductor.NumeroLicencia;
        conductorDb.CategoriaLicencia = conductor.CategoriaLicencia;
        conductorDb.VencimientoLicencia = conductor.VencimientoLicencia;

        // Actualizar usuario de Identity
        if (!string.IsNullOrEmpty(conductorDb.UserId))
        {
            var usuario = await _userManager.FindByIdAsync(conductorDb.UserId);

            if (usuario != null)
            {
                // Actualizar correo
                usuario.Email = email;
                usuario.UserName = email;

                await _userManager.UpdateAsync(usuario);

                // Actualizar contraseña si ingresó una nueva
                if (!string.IsNullOrWhiteSpace(nuevaPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

                    var resultadoPassword =
                        await _userManager.ResetPasswordAsync(
                            usuario,
                            token,
                            nuevaPassword);

                    if (!resultadoPassword.Succeeded)
                    {
                        foreach (var error in resultadoPassword.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return View(conductor);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    return View(conductor);
}
        // ELIMINAR
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(int id)
{
    var conductor = await _context.Conductores.FindAsync(id);

    if (conductor == null)
        return NotFound();

    try
    {
        // Buscar solicitudes donde esté asignado el conductor
        var solicitudes = await _context.SolicitudesServicio
            .Where(s => s.ConductorId == id)
            .ToListAsync();

        foreach (var solicitud in solicitudes)
        {
            // Liberar unidad si existe
            if (solicitud.UnidadId != null)
            {
                var unidad = await _context.Unidades
                    .FindAsync(solicitud.UnidadId);

                if (unidad != null)
                {
                    unidad.Actividad = "Libre";
                }
            }

            // Quitar asignaciones
            solicitud.ConductorId = null;
            solicitud.UnidadId = null;

            // Cambiar estado de la solicitud
            solicitud.EstadoSolicitud = "Pendiente de Asignación";
        }

        // Eliminación lógica del conductor
        conductor.IsDeleted = true;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");

        return View("Error");
    }
}
        // GET: Vista de alertas de licencias próximas a vencer
        [HttpGet]
        public IActionResult AlertasLicencias()
        {
            var hoy = DateTime.Today;
            var limite = hoy.AddDays(30);

            var conductores = _context.Conductores
                .Where(c => !c.IsDeleted && c.VencimientoLicencia <= limite)
                .OrderBy(c => c.VencimientoLicencia)
                .ToList();

            return View(conductores);
        }

        // POST: Actualizar fecha de vencimiento de licencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarVigencia(int id, DateTime nuevaFecha)
        {
            if (nuevaFecha < DateTime.Today)
            {
                TempData["Error"] = "No se puede registrar una fecha pasada.";
                return RedirectToAction(nameof(AlertasLicencias));
            }

            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor == null) return NotFound();

            conductor.VencimientoLicencia = nuevaFecha;
            _context.Update(conductor);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Vigencia de {conductor.Nombres} {conductor.Apellidos} actualizada correctamente.";
            return RedirectToAction(nameof(AlertasLicencias));
        }
    }
}