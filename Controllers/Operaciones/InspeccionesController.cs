using Microsoft.AspNetCore.Mvc;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using System.Linq;

namespace Control_flota.Controllers.Operaciones
{
    public class InspeccionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InspeccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var unidades = _context.Unidades.ToList();

            return View(unidades);
        }

        public IActionResult Create(string placa)
        {
            var inspeccion = new Inspeccion
            {
                Placa = placa
            };

            return View(inspeccion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Inspeccion inspeccion)
        {
            if (ModelState.IsValid)
            {
                _context.Inspecciones.Add(inspeccion);

                _context.SaveChanges();

                return RedirectToAction("Details", new
                {
                    placa = inspeccion.Placa
                });
            }

            return View(inspeccion);
        }

        public IActionResult Details(string placa)
        {
            var historial = _context.Inspecciones
                .Where(i => i.Placa == placa)
                .OrderByDescending(i => i.Fecha)
                .ToList();

            ViewBag.Placa = placa;

            return View(historial);
        }
    }
}