using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Control_flota.Models.Login;

namespace Control_flota.Controllers  // ← Agrega el namespace
{
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
            var conductores = await _context.Conductores.ToListAsync();
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

            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "El correo electrónico ya está registrado");
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

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(conductor);
        }

        // EDITAR (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor == null) return NotFound();
            return View(conductor);
        }

        // EDITAR (POST)
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

        // ELIMINAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var conductor = await _context.Conductores.FindAsync(id);
            if (conductor == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(conductor.UserId))
                {
                    var usuario = await _userManager.FindByIdAsync(conductor.UserId);
                    if (usuario != null)
                    {
                        await _userManager.DeleteAsync(usuario);
                    }
                }

                _context.Conductores.Remove(conductor);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return View("Error");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}