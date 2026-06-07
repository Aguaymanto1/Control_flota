using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.CreatePdf;
using Control_flota.Data;
using Control_flota.Services; // Asegúrate de tener el namespace correcto
using System.IO.Compression;

namespace Control_flota.Controllers
{
    [Authorize(Roles = "AdministradorFinanzas")]
    public class FinanzasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        // Modifica el constructor para inyectar IEmailService
        public FinanzasController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }


        // Panel de finanzas - redirige a ServiciosPorCobrar
        public IActionResult Index()
        {
            return RedirectToAction("ServiciosPorCobrar");
        }

        // Detalle del viaje completado - para generar factura
        public async Task<IActionResult> Detalles(int id)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Gastos)
                .FirstOrDefaultAsync(o => o.Id == id && o.Estado == "Completado");

            if (orden == null)
                return NotFound();

            return View(orden);
        }

        // Acción para generar la pre-factura con desglose matemático
        [HttpGet]
        public async Task<IActionResult> GenerarFactura(int id)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Gastos)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
                return NotFound();

            var gastos = orden.Gastos.ToList();
            var peajes = gastos
                .Where(g => g.Concepto.Contains("peaje", StringComparison.OrdinalIgnoreCase))
                .Select(g => new { g.Concepto, Monto = g.Monto })
                .ToList();

            var peajeTotal = peajes.Sum(g => g.Monto);
            var totalGastos = gastos.Sum(g => g.Monto);
            var fleteBase = totalGastos - peajeTotal;
            if (fleteBase < 0) fleteBase = 0;

            return Json(new
            {
                success = true,
                orden = new
                {
                    orden.Id,
                    orden.Codigo,
                    FechaEmision = orden.FechaEmision,
                    orden.Origen,
                    orden.Destino,
                    Cliente = new
                    {
                        Nombre = orden.Cliente?.Nombre ?? "-",
                        Correo = orden.Cliente?.Correo ?? "-"
                    },
                    Gastos = gastos.Select(g => new { g.Concepto, Monto = g.Monto }).ToList(),
                    Peajes = peajes,
                    TotalGastos = totalGastos,
                    FleteBase = fleteBase,
                    TotalAPagar = totalGastos
                }
            });
        }

        // Descargar factura en PDF
        [HttpGet]
        public async Task<IActionResult> DescargarFacturaPdf(int id)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
                return NotFound();

            var pdfBytes = await PdfConstancia.GenerarFactura(id, _context);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return BadRequest("Error generando la factura PDF");

            return File(pdfBytes, "application/pdf", $"Factura_{orden.Codigo}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarFacturasZip(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest("No se proporcionaron IDs de factura.");

            var ordenIds = ids
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(idString => int.TryParse(idString, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!ordenIds.Any())
                return BadRequest("No se proporcionaron IDs válidos.");

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var ordenId in ordenIds)
                {
                    var orden = await _context.Ordenes
                        .Include(o => o.Cliente)
                        .FirstOrDefaultAsync(o => o.Id == ordenId);

                    if (orden == null)
                        continue;

                    var pdfBytes = await PdfConstancia.GenerarFactura(ordenId, _context);
                    if (pdfBytes == null || pdfBytes.Length == 0)
                        continue;

                    var entry = archive.CreateEntry($"Factura_{orden.Codigo}.pdf", CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pdfBytes);
                }
            }

            zipStream.Position = 0;
            return File(zipStream.ToArray(), "application/zip", $"Facturas_{DateTime.Now:yyyyMMddHHmmss}.zip");
        }

        // Enviar factura por correo
        [HttpPost]
        public async Task<IActionResult> EnviarFacturaAlCorreo(int id)
        {
            try
            {
                var orden = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orden == null)
                    return Json(new { success = false, mensaje = "Orden no encontrada" });

                if (orden.Cliente == null || string.IsNullOrWhiteSpace(orden.Cliente.Correo))
                    return Json(new { success = false, mensaje = "El cliente no tiene un correo registrado" });

                var pdfBytes = await PdfConstancia.GenerarFactura(id, _context);
                if (pdfBytes == null || pdfBytes.Length == 0)
                    return Json(new { success = false, mensaje = "Error al generar el PDF" });

                var mensajeHtml = $@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
                            .container {{ max-width: 600px; margin: auto; padding: 20px; }}
                            .header {{ background: #002d62; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; color: #0f172a; }}
                            .footer {{ margin-top: 20px; font-size: 12px; color: #64748b; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>🚛 De La Sota S.A.C.</h2>
                                <p>Factura de Venta</p>
                            </div>
                            <div class='content'>
                                <p>Estimado(a) {orden.Cliente.Nombre},</p>
                                <p>Adjunto encontrarás la factura correspondiente al servicio.</p>
                                <p><strong>Orden:</strong> {orden.Codigo}</p>
                                <p><strong>Ruta:</strong> {orden.Origen} → {orden.Destino}</p>
                                <p><strong>Fecha:</strong> {orden.FechaEmision:dd/MM/yyyy HH:mm}</p>
                                <p>Gracias por confiar en nosotros.</p>
                                <p>Saludos cordiales,<br><strong>Departamento de Finanzas</strong></p>
                            </div>
                            <div class='footer'>
                                <p>© {DateTime.Now.Year} De La Sota S.A.C.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                var exito = await _emailService.EnviarCorreoConConstanciaAsync(
                    orden.Cliente.Correo,
                    $"Factura de Venta - Orden {orden.Codigo}",
                    mensajeHtml,
                    pdfBytes,
                    $"Factura_{orden.Codigo}.pdf"
                );

                if (exito)
                    return Json(new { success = true, mensaje = $"Factura enviada a: {orden.Cliente.Correo}" });

                return Json(new { success = false, mensaje = "Error al enviar el correo" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = $"Error: {ex.Message}" });
            }
        }

        // Apartado de Servicios por Cobrar
        public async Task<IActionResult> ServiciosPorCobrar()
        {
            // Obtener solo las órdenes completadas (estado = "Completado" o similar)
            var viajesCompletados = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Gastos)
                .Where(o => o.Estado == "Completado") // Ajusta el estado según tu aplicación
                .OrderByDescending(o => o.FechaEmision)
                .ToListAsync();

            ViewBag.TotalViajesCompletados = viajesCompletados.Count;
            ViewBag.TotalGastosRegistrados = await _context.GastosRuta.CountAsync();

            return View(viajesCompletados);
        }
        }
}
