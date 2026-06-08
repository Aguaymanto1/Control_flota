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
            .Include(o => o.SolicitudServicio)
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null) return null!;

        var gastos = orden.Gastos.ToList();
        var peajes = gastos
            .Where(g => g.Concepto.Contains("peaje", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var peajeTotal = peajes.Sum(g => g.Monto);
        var totalGastos = gastos.Sum(g => g.Monto);
        var montoSolicitud = orden.SolicitudServicio?.Monto;
        var baseFlete = montoSolicitud ?? totalGastos - peajeTotal;
        if (baseFlete < 0) baseFlete = 0;

        var totalFactura = baseFlete + peajeTotal;

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
                            c.Item().Text("Factura de Venta - factura")
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
                        if (montoSolicitud.HasValue)
                        {
                            c.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text("Monto solicitado").FontColor("#334155");
                                r.ConstantItem(120).AlignRight().Text($"S/ {montoSolicitud.Value:F2}").Bold();
                            });
                        }
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Sobrecostos (peajes)").FontColor("#334155");
                            r.ConstantItem(120).AlignRight().Text($"S/ {peajeTotal:F2}").Bold();
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Total a facturar").FontColor("#0f172a").Bold();
                            r.ConstantItem(120).AlignRight().Text($"S/ {totalFactura:F2}").Bold();
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

                    col.Item().PaddingTop(24).Text("Observación: Esta es una factura con desglose de flete base y peajes.")
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

        // ==================== NUEVO MÉTODO PARA FACTURA CON SOBRECOSTOS ====================

    public class SobrecostoItem
    {
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public static async Task<byte[]> GenerarFacturaConSobrecostos(
        int ordenId,
        List<SobrecostoItem> sobrecostos,
        string fechaVencimiento,
        ApplicationDbContext context)
    {
        var orden = await context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Gastos)
            .Include(o => o.SolicitudServicio)
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null) return null!;

        var montoBase = orden.SolicitudServicio?.Monto ?? orden.Gastos?.Sum(g => g.Monto) ?? 0m;
        var totalSobrecostos = sobrecostos?.Sum(s => s.Monto) ?? 0m;
        var totalFactura = montoBase + totalSobrecostos;

        DateTime fechaVenc = DateTime.TryParse(fechaVencimiento, out var fv) ? fv : DateTime.Now.AddDays(30);
        DateTime fechaEmision = DateTime.Now;

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
                                .FontSize(20).Bold().FontColor("#1e3a5f");
                            c.Item().Text("RUC: 20512345678")
                                .FontSize(9).FontColor("#64748b");
                            c.Item().Text("Factura Electrónica")
                                .FontSize(9).FontColor("#64748b");
                        });
                        row.ConstantItem(80).AlignRight().Column(c =>
                        {
                            c.Item().Text("🚛").FontSize(36);
                            c.Item().Text($"N°: {DateTime.Now.Year}{DateTime.Now.Month:D2}-0001")
                                .FontSize(8).FontColor("#64748b");
                        });
                    });
                    col.Item().PaddingTop(8).BorderBottom(1).BorderColor("#cbd5e1");
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Señor(es):").FontSize(9).FontColor("#64748b");
                            c.Item().Text(orden.Cliente?.Nombre ?? "-").FontSize(12).Bold();
                            c.Item().Text($"RUC: {orden.Cliente?.Ruc ?? "-"}").FontSize(9);
                            c.Item().Text($"Dirección: {orden.Cliente?.Direccion ?? "-"}").FontSize(9);
                        });
                        r.ConstantItem(180).Column(c =>
                        {
                            c.Item().Text("N° de Orden:").FontSize(9).FontColor("#64748b").AlignRight();
                            c.Item().Text(orden.Codigo).FontSize(10).Bold().AlignRight();
                            c.Item().Text($"Fecha Emisión: {fechaEmision:dd/MM/yyyy}").FontSize(9).AlignRight();
                            c.Item().Text($"Fecha Vencimiento: {fechaVenc:dd/MM/yyyy}").FontSize(9).Bold().AlignRight().FontColor("#dc2626");
                        });
                    });

                    col.Item().PaddingTop(12).Background("#f8fafc").Padding(10).Column(c =>
                    {
                        c.Item().Text("DETALLE DEL SERVICIO").FontSize(9).Bold().FontColor("#1e3a5f");
                        c.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text("Origen:").FontSize(9).FontColor("#64748b");
                            r.RelativeItem().Text(orden.Origen).FontSize(9);
                        });
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Destino:").FontSize(9).FontColor("#64748b");
                            r.RelativeItem().Text(orden.Destino).FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(12).Text("DESGLOSE DE LA FACTURA").FontSize(9).Bold().FontColor("#1e3a5f");

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(100);
                        });

                        table.Cell().BorderBottom(1).BorderColor("#e2e8f0").Padding(6).Text("Flete Base - Servicio de transporte").FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor("#e2e8f0").Padding(6).Text($"S/ {montoBase:F2}").AlignRight().FontSize(9).Bold();

                        if (sobrecostos != null && sobrecostos.Any())
                        {
                            foreach (var sc in sobrecostos)
                            {
                                table.Cell().Padding(6).Text($"Sobrecosto: {sc.Concepto}").FontSize(9).FontColor("#475569");
                                table.Cell().Padding(6).Text($"S/ {sc.Monto:F2}").AlignRight().FontSize(9);
                            }
                        }

                        table.Cell().Padding(8).BorderTop(1).BorderColor("#cbd5e1").Text("TOTAL").FontSize(11).Bold();
                        table.Cell().Padding(8).BorderTop(1).BorderColor("#cbd5e1").Text($"S/ {totalFactura:F2}").AlignRight().FontSize(14).Bold().FontColor("#059669");
                    });

                    col.Item().PaddingTop(16).Background("#fef3c7").Padding(10).Column(c =>
                    {
                        c.Item().Text("📌 CONDICIONES DE PAGO").FontSize(9).Bold().FontColor("#92400e");
                        c.Item().Text($"Fecha límite de pago: {fechaVenc:dd/MM/yyyy}").FontSize(8).FontColor("#78350f");
                        c.Item().Text("Forma de pago: Transferencia bancaria / Depósito en cuenta").FontSize(8).FontColor("#78350f");
                        c.Item().Text("Banco: BBVA Continental - Cuenta Corriente: 0011-0456-7890123456").FontSize(8).FontColor("#78350f");
                    });

                    col.Item().PaddingTop(16).AlignCenter().Text("¡Gracias por su preferencia!").FontSize(9).FontColor("#64748b");
                });

                page.Footer().Column(col =>
                {
                    col.Item().BorderTop(1).BorderColor("#e2e8f0").PaddingTop(6);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("De La Sota S.A.C. - Todos los derechos reservados").FontSize(7).FontColor("#94a3b8");
                        row.ConstantItem(130).Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#94a3b8").AlignRight();
                    });
                });
            });
        });

        return pdf.GeneratePdf();
    }
}