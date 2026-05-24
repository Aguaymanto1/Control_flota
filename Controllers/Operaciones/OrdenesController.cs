using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class OrdenesController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrdenesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lista general de órdenes y solicitudes pendientes en despacho
    public async Task<IActionResult> Index()
    {
        ViewBag.Clientes = _context.Clientes.ToList();

        ViewBag.TipoCargaOptions = new List<string>
        {
            "Carga General",
            "Carga Refrigerada",
            "Carga Peligrosa"
        };

        var ordenes = await _context.Ordenes
            .Include(o => o.Cliente)
            .OrderByDescending(o => o.FechaEmision)
            .ToListAsync();

        var solicitudesPendientes = await _context.SolicitudesServicio
            .Include(s => s.Cliente)
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .Where(s => s.EstadoSolicitud == "Pendiente de Asignación" || s.EstadoSolicitud == "Pendiente")
            .OrderByDescending(s => s.FechaDespacho)
            .ToListAsync();

        var model = new DespachoViewModel
        {
            Ordenes = ordenes,
            SolicitudesPendientes = solicitudesPendientes
        };

        return View(model);
    }

    // Detalle de orden
    public async Task<IActionResult> Details(int id)
    {
        var orden = await _context.Ordenes
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orden == null)
            return NotFound();

        // Cargar la solicitud relacionada y sus datos de conductor/unidad si existen
        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        ViewBag.Solicitud = solicitud;
        ViewBag.Conductor = solicitud?.Conductor;
        ViewBag.Unidad = solicitud?.Unidad;

        return View(orden);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);
        if (orden == null)
            return NotFound();

        _context.Ordenes.Remove(orden);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    // Panel del conductor - solo sus órdenes
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> PanelConductor()
    {
        var user = await _context.Users
            .Include(u => u.Conductor)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user?.Conductor == null)
            return RedirectToAction("Index", "Home");

        var ordenes = await _context.Ordenes
            .Include(o => o.Cliente)
            .Where(o => o.ConductorId == user.Conductor.Id)
            .OrderByDescending(o => o.FechaEmision)
            .ToListAsync();

        return View(ordenes);
    }

    // Iniciar Ruta
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> IniciarRuta(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);
        if (orden == null) return NotFound();

        if (orden.Estado != "Emitida")
        {
            TempData["Error"] = "Solo puedes iniciar una orden en estado 'Emitida'.";
            return RedirectToAction(nameof(PanelConductor));
        }

        orden.Estado = "En Tránsito";
        _context.Update(orden);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Ruta iniciada correctamente.";
        return RedirectToAction(nameof(PanelConductor));
    }

    // Finalizar Ruta
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> FinalizarRuta(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);
        if (orden == null) return NotFound();

        if (orden.Estado != "En Tránsito")
        {
            TempData["Error"] = "Solo puedes finalizar una orden que esté 'En Tránsito'.";
            return RedirectToAction(nameof(PanelConductor));
        }

        orden.Estado = "Completado";
        _context.Update(orden);
        await _context.SaveChangesAsync();

        // Liberar conductor y unidad
        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        if (solicitud != null)
        {
            if (solicitud.Conductor != null)
            {
                solicitud.Conductor.Actividad = "Libre";
                _context.Update(solicitud.Conductor);
            }
            if (solicitud.Unidad != null)
            {
                solicitud.Unidad.Actividad = "Libre";
                _context.Update(solicitud.Unidad);
            }
            await _context.SaveChangesAsync();
        }

        TempData["Exito"] = "Ruta finalizada. Conductor y unidad liberados.";
        return RedirectToAction(nameof(PanelConductor));
    }
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> DescargarConstancia(int id)
    {
        var orden = await _context.Ordenes
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orden == null) return NotFound();

        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        var conductorNombre = solicitud?.Conductor != null
            ? $"{solicitud.Conductor.Nombres} {solicitud.Conductor.Apellidos}"
            : orden.NombreConductor;

        var conductorDni = solicitud?.Conductor?.Dni ?? "-";
        var unidadDesc  = solicitud?.Unidad != null
            ? $"{solicitud.Unidad.Marca} {solicitud.Unidad.Modelo}"
            : "-";

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                // HEADER
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("De La Sota S.A.C.")
                                .FontSize(20).Bold().FontColor("#002d62");
                            c.Item().Text("Sistema Integrado de Operaciones Logísticas")
                                .FontSize(10).FontColor("#64748b");
                        });
                        row.ConstantItem(80).AlignRight().Column(c =>
                        {
                            c.Item().Text("🚛").FontSize(36);
                        });
                    });

                    col.Item().PaddingTop(8).BorderBottom(2).BorderColor("#002d62");

                    col.Item().PaddingTop(12).AlignCenter()
                        .Text("CONSTANCIA DE VIAJE")
                        .FontSize(16).Bold().FontColor("#002d62");
                });

                // CONTENT
                page.Content().PaddingTop(20).Column(col =>
                {
                    // Sección: Datos de la Orden
                    col.Item().Background("#f8fafc").Padding(12).Column(c =>
                    {
                        c.Item().Text("DATOS DE LA ORDEN")
                            .FontSize(9).Bold().FontColor("#64748b");
                        c.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Text("Código de Orden:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.Codigo).Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Fecha de Emisión:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.FechaEmision.ToString("dd/MM/yyyy HH:mm")).Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Estado:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.Estado).Bold().FontColor("#16a34a");
                        });
                    });

                    col.Item().PaddingTop(16).Column(c =>
                    {
                        c.Item().Text("CLIENTE Y RUTA")
                            .FontSize(9).Bold().FontColor("#64748b");
                        c.Item().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(4);
                        c.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Text("Cliente:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.Cliente?.Nombre ?? "-").Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Origen:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.Origen).Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Destino:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.Destino).Bold();
                        });
                    });

                    col.Item().PaddingTop(16).Column(c =>
                    {
                        c.Item().Text("CONDUCTOR Y UNIDAD")
                            .FontSize(9).Bold().FontColor("#64748b");
                        c.Item().BorderBottom(1).BorderColor("#e2e8f0").PaddingBottom(4);
                        c.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Text("Conductor:").FontColor("#64748b");
                            r.RelativeItem().Text(conductorNombre).Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("DNI:").FontColor("#64748b");
                            r.RelativeItem().Text(conductorDni).Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Placa:").FontColor("#64748b");
                            r.RelativeItem().Text(orden.PlacaCamion).Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Unidad:").FontColor("#64748b");
                            r.RelativeItem().Text(unidadDesc).Bold();
                        });
                    });

                    // Firma
                    col.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor("#1a1a1a").Width(150);
                            c.Item().PaddingTop(4).Text("Firma del Conductor").FontSize(10).FontColor("#64748b");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().BorderBottom(1).BorderColor("#1a1a1a").Width(150);
                            c.Item().PaddingTop(4).Text("Sello de la Empresa").FontSize(10).FontColor("#64748b");
                        });
                    });
                });

                // FOOTER
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} — FleetOps Pro © {DateTime.Now.Year}")
                        .FontSize(9).FontColor("#94a3b8");
                });
            });
        });

        var pdfBytes = pdf.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Constancia_{orden.Codigo}.pdf");
    }
}
