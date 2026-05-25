using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;

namespace Control_flota.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalConductores = await _context.Conductores.CountAsync();
            ViewBag.TotalUnidades = await _context.Unidades.CountAsync();
            ViewBag.SolicitudesPendientes = await _context.SolicitudesServicio.CountAsync(s => s.EstadoSolicitud == "Pendiente de Asignación" || s.EstadoSolicitud == "Pendiente");
            ViewBag.ViajesActivos = await _context.Ordenes.CountAsync(o => o.Estado == "En Tránsito");
            
            return View();
        }
    }
}