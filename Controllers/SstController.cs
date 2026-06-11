using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Control_flota.Controllers;

[Authorize(Roles = "SST")]
public class SstController : Controller
{
    private readonly ApplicationDbContext _context;

    public SstController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var ordenesEnTransito = await _context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Conductor)
            .Include(o => o.NotificacionesParada)
            .Where(o => o.Estado == "En Tránsito" && o.ConductorId != null)
            .ToListAsync();

        var ordenesCompletadas = await _context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Conductor)
            .Where(o => o.Estado == "Completado" && o.ConductorId != null)
            .ToListAsync();

        var estadosIniciales = await _context.EstadosInicialesRuta.ToListAsync();
        var incidencias = await _context.IncidenciasRuta.ToListAsync();

        ViewBag.EstadosIniciales = estadosIniciales;
        ViewBag.LimiteHoras = 5;
        ViewBag.OrdenesCompletadas = ordenesCompletadas;
        ViewBag.Incidencias = incidencias;

        return View(ordenesEnTransito);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotificarParada(int ordenId, int conductorId)
    {
        var yaNotificado = await _context.NotificacionesParada
            .AnyAsync(n => n.OrdenId == ordenId && n.Leida == false);

        if (yaNotificado)
        {
            TempData["Error"] = "Ya existe una notificación pendiente para este conductor.";
            return RedirectToAction(nameof(Index));
        }

        var notificacion = new NotificacionParada
        {
            OrdenId = ordenId,
            ConductorId = conductorId,
            FechaNotificacion = DateTime.UtcNow,
            Leida = false
        };

        _context.NotificacionesParada.Add(notificacion);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Notificación de parada enviada al conductor.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GenerarCertificadoSST(int ordenId)
    {
        var orden = await _context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Conductor)
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null) return NotFound();

        var incidencias = await _context.IncidenciasRuta
            .Where(i => i.OrdenId == ordenId)
            .ToListAsync();

        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        var firmaPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sst", "firma.png");
        byte[]? firmaBytes = null;
        if (System.IO.File.Exists(firmaPath))
            firmaBytes = await System.IO.File.ReadAllBytesAsync(firmaPath);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("DE LA SOTA S.A.C.")
                                .FontSize(20).Bold().FontColor("#002060");
                            c.Item().Text("Certificado de Cumplimiento SST")
                                .FontSize(13).FontColor("#444444");
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text($"N° CERT-{orden.Id:D5}")
                                .FontSize(10).Bold().FontColor("#002060");
                            c.Item().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                                .FontSize(10).FontColor("#666666");
                        });
                    });

                    col.Item().PaddingTop(8).LineHorizontal(2).LineColor("#002060");
                    col.Item().PaddingBottom(8);
                });

                page.Content().Column(col =>
                {
                    // Introducción
                    col.Item().PaddingBottom(12).Text(text =>
                    {
                        text.Span("El Responsable de Seguridad y Salud en el Trabajo (SST) de ")
                            .FontSize(11);
                        text.Span("De La Sota S.A.C.").Bold();
                        text.Span(" certifica que el siguiente servicio de transporte fue ejecutado ")
                            .FontSize(11);
                        text.Span("cumpliendo las normas de salud ocupacional vigentes").Bold();
                        text.Span(" y sin incidencias de seguridad registradas.").FontSize(11);
                    });

                    // Datos del viaje
                    col.Item().PaddingBottom(6).Text("DATOS DEL VIAJE")
                        .FontSize(12).Bold().FontColor("#002060");

                    col.Item().PaddingBottom(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void Fila(string label, string valor)
                        {
                            table.Cell().Background("#F2F5FA").Padding(6)
                                .Text(label).Bold().FontSize(10);
                            table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(6)
                                .Text(valor).FontSize(10);
                        }

                        Fila("Código de Orden:", orden.Codigo);
                        Fila("Cliente:", orden.Cliente?.Nombre ?? "-");
                        Fila("Origen:", orden.Origen);
                        Fila("Destino:", orden.Destino);
                        Fila("Fecha de Emisión:", orden.FechaEmision.ToString("dd/MM/yyyy HH:mm"));
                        Fila("Estado:", orden.Estado);
                        Fila("Conductor:", $"{orden.Conductor?.Nombres} {orden.Conductor?.Apellidos}");
                        Fila("DNI Conductor:", orden.Conductor?.Dni ?? "-");
                        Fila("Unidad (Placa):", solicitud?.Unidad?.Placa ?? orden.PlacaCamion);
                    });

                    // Resultado SST
                    col.Item().PaddingBottom(6).Text("RESULTADO DE EVALUACIÓN SST")
                        .FontSize(12).Bold().FontColor("#002060");

                    var sinIncidencias = !incidencias.Any();
                    var colorEstado = sinIncidencias ? "#16a34a" : "#dc2626";
                    var textoEstado = sinIncidencias
                        ? "APROBADO — Sin incidencias registradas durante la ruta"
                        : $"OBSERVADO — Se registraron {incidencias.Count} incidencia(s) durante la ruta";

                    col.Item().PaddingBottom(12)
                        .Background(sinIncidencias ? "#f0fdf4" : "#fff5f5")
                        .Border(1).BorderColor(colorEstado)
                        .Padding(10)
                        .Text(textoEstado).Bold().FontColor(colorEstado).FontSize(11);

                    if (!sinIncidencias)
                    {
                        col.Item().PaddingBottom(6).Text("Detalle de Incidencias:")
                            .FontSize(10).Bold().FontColor("#dc2626");
                        foreach (var inc in incidencias)
                        {
                            col.Item().PaddingBottom(4).PaddingLeft(10).Text(
                                $"• [{inc.FechaReporte:dd/MM/yyyy HH:mm}] {inc.TipoIncidencia}: {inc.Descripcion}"
                            ).FontSize(10).FontColor("#7f1d1d");
                        }
                    }

                    col.Item().PaddingBottom(16);

                    // Declaración
                    col.Item().PaddingBottom(16).Background("#eff6ff").Border(1)
                        .BorderColor("#2563eb").Padding(12).Text(text =>
                    {
                        text.Span("Declaración: ").Bold().FontColor("#1e3a8a");
                        text.Span("El presente certificado avala que el servicio descrito fue monitoreado por el área de SST, verificándose el cumplimiento del protocolo de seguridad vial, el respeto de los límites de horas de conducción continua (máx. 5 horas) y la ausencia de riesgos reportados durante la operación.").FontSize(10);
                    });

                    // Firma
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(220).Column(c =>
                        {
                            if (firmaBytes != null)
                            {
                                c.Item().AlignCenter().Height(60)
                                    .Image(firmaBytes).FitArea();
                            }
                            else
                            {
                                c.Item().AlignCenter().Height(60)
                                    .Text("[ Firma Digital ]").FontColor("#94a3b8").Italic();
                            }
                            c.Item().LineHorizontal(1).LineColor("#002060");
                            c.Item().PaddingTop(4).AlignCenter()
                                .Text("Responsable de SST").Bold().FontSize(10);
                            c.Item().AlignCenter()
                                .Text("De La Sota S.A.C.").FontSize(9).FontColor("#666666");
                        });
                        row.RelativeItem();
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Documento generado el ").FontSize(9).FontColor("#94a3b8");
                    text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).FontColor("#94a3b8");
                    text.Span($" — Certificado N° CERT-{orden.Id:D5}").FontSize(9).FontColor("#94a3b8");
                });
            });
        });

        var pdfBytes = pdf.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"CertificadoSST_{orden.Codigo}.pdf");
    }
}