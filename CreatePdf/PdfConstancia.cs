using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Control_flota.CreatePdf;

public static class PdfConstancia
{
    public static async Task<byte[]> Generar(int ordenId, ApplicationDbContext context)
    {
        var orden = await context.Ordenes
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null) return null!;

        var solicitud = await context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        var conductorNombre = solicitud?.Conductor != null
            ? $"{solicitud.Conductor.Nombres} {solicitud.Conductor.Apellidos}"
            : orden.NombreConductor;

        var conductorDni = solicitud?.Conductor?.Dni ?? "-";
        var unidadDesc = solicitud?.Unidad != null
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

        return pdf.GeneratePdf();
    }
}