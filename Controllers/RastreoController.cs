using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;

public class RastreoController : Controller
{
    private readonly ApplicationDbContext _context;

    public RastreoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Vista pública de consulta
    [HttpGet]
    public IActionResult Consultar(string? codigo)
    {
        if (string.IsNullOrEmpty(codigo))
            return View();

        // Buscar por código de orden O código de solicitud
        var orden = _context.Ordenes
            .Include(o => o.Cliente)
            .FirstOrDefault(o => o.Codigo == codigo.Trim().ToUpper());

        if (orden == null)
        {
            // Intentar buscar por código de solicitud
            var solicitud = _context.SolicitudesServicio
                .Include(s => s.Cliente)
                .FirstOrDefault(s => s.Codigo == codigo.Trim().ToUpper());

            if (solicitud == null)
            {
                ViewBag.Error = "No se encontró ningún servicio con ese código.";
                ViewBag.Codigo = codigo;
                return View();
            }

            ViewBag.EsSolicitud = true;
            ViewBag.Codigo = codigo;
            return View(solicitud);
        }

        ViewBag.EsOrden = true;
        ViewBag.Codigo = codigo;
        return View(orden);
    }
}