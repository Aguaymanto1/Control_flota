using Microsoft.AspNetCore.Mvc;
using Control_flota.Data;
using Control_flota.Models.Operaciones;

namespace Control_flota.Controllers.Operaciones
{
    public class InspeccionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InspeccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create(string placa)
        {
            ViewBag.Placa = placa;
            return View();
        }

       [HttpPost]
public IActionResult Create(Inspeccion inspeccion)
{
    inspeccion.FechaInspeccion = DateTime.Now;

    // VALIDAR INSPECCIÓN
    if (
        inspeccion.Luces == "Cumple" &&
        inspeccion.Llantas == "Cumple" &&
        inspeccion.Frenos == "Cumple" &&
        inspeccion.Fluidos == "Cumple"
       )
    {
        inspeccion.EstadoVehiculo = "Inspeccionado - Apto";
    }
    else
    {
        inspeccion.EstadoVehiculo = "Mantenimiento Preventivo";
    }

    // GUARDAR INSPECCIÓN
    _context.Inspecciones.Add(inspeccion);

    // BUSCAR UNIDAD POR PLACA
    var unidad = _context.Unidades
        .FirstOrDefault(u => u.Placa == inspeccion.Placa);

    // ACTUALIZAR ESTADO DE LA UNIDAD
    if (unidad != null)
    {
        unidad.EstadoOperativo = inspeccion.EstadoVehiculo;
    }

    // GUARDAR CAMBIOS
    _context.SaveChanges();

    // MENSAJE OPCIONAL
    TempData["Mensaje"] = "Inspección registrada correctamente";

    // VOLVER A UNIDADES
    return RedirectToAction("Index", "Unidades");
}
    }
}