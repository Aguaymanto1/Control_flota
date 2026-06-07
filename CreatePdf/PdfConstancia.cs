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

    public static async Task<byte[]> GenerarFactura(int ordenId, ApplicationDbContext context)
    {
        var orden = await context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Gastos)
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null) return null!;

        var gastos = orden.Gastos.ToList();
        var peajes = gastos
            .Where(g => g.Concepto.Contains("peaje", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var peajeTotal = peajes.Sum(g => g.Monto);
        var totalGastos = gastos.Sum(g => g.Monto);
        var baseFlete = totalGastos - peajeTotal;
        if (baseFlete < 0) baseFlete = 0;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("DE LA SOTA S.A.C.")
                                .FontSize(20).Bold().FontColor("#002d62");
                            c.Item().Text("Factura de Venta - Pre-factura")
                                .FontSize(10).FontColor("#64748b");
                        });
                        row.ConstantItem(80).AlignRight().Column(c =>
                        {
                            c.Item().Text("🚛").FontSize(36);
                        });
                    });

                    col.Item().PaddingTop(8).BorderBottom(2).BorderColor("#002d62");
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Cliente:").FontColor("#64748b");
                            c.Item().Text(orden.Cliente?.Nombre ?? "-").Bold();
                            c.Item().PaddingTop(8).Text("Ruta:").FontColor("#64748b");
                            c.Item().Text($"{orden.Origen} → {orden.Destino}").Bold();
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Orden N°:").FontColor("#64748b");
                            c.Item().Text(orden.Codigo).Bold();
                            c.Item().PaddingTop(8).Text("Fecha:").FontColor("#64748b");
                            c.Item().Text(orden.FechaEmision.ToString("dd/MM/yyyy HH:mm")).Bold();
                        });
                    });

                    col.Item().PaddingTop(20).Text("DESGLOSE DE COSTOS")
                        .FontSize(9).Bold().FontColor("#64748b");

                    col.Item().PaddingTop(8).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Flete base").FontColor("#334155");
                            r.ConstantItem(120).AlignRight().Text($"S/ {baseFlete:F2}").Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Sobrecostos (peajes)").FontColor("#334155");
                            r.ConstantItem(120).AlignRight().Text($"S/ {peajeTotal:F2}").Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Total a facturar").FontColor("#0f172a").Bold();
                            r.ConstantItem(120).AlignRight().Text($"S/ {totalGastos:F2}").Bold();
                        });
                    });

                    if (gastos.Any())
                    {
                        col.Item().PaddingTop(20).Text("DETALLE DE GASTOS")
                            .FontSize(9).Bold().FontColor("#64748b");

                        gastos.ForEach(gasto =>
                        {
                            col.Item().PaddingTop(8).Row(r =>
                            {
                                r.RelativeItem().Text(gasto.Concepto).FontSize(9).FontColor("#334155");
                                r.ConstantItem(120).AlignRight().Text($"S/ {gasto.Monto:F2}").FontSize(9).Bold();
                            });
                        });
                    }

                    col.Item().PaddingTop(24).Text("Observación: Esta es una pre-factura con desglose de flete base y peajes.")
                        .FontSize(9).FontColor("#475569");
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} — De La Sota S.A.C.")
                        .FontSize(9).FontColor("#94a3b8");
                });
            });
        });

        return pdf.GeneratePdf();
    }
}