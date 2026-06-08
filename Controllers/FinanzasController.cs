using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.CreatePdf;
using Control_flota.Data;
using Control_flota.Services;
using System.IO.Compression;
using System.IO;
using System.Collections.Generic;

namespace Control_flota.Controllers
{
    [Authorize(Roles = "AdministradorFinanzas")]
    public class FinanzasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public FinanzasController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("ServiciosPorCobrar");
        }

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

        [HttpGet]
        public async Task<IActionResult> GenerarFactura(int id)
        {
            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Gastos)
                .Include(o => o.SolicitudServicio)
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

            decimal? montoSolicitado = orden.SolicitudServicio?.Monto;
            var montoAPagar = montoSolicitado ?? totalGastos;

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
                    MontoSolicitado = montoSolicitado,
                    TotalAPagar = montoAPagar
                }
            });
        }

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

        [HttpPost]
        public async Task<IActionResult> EnviarFacturaAlCorreo(int id)
        {
            try
            {
                var orden = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .Include(o => o.Gastos)
                    .Include(o => o.SolicitudServicio)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orden == null)
                    return Json(new { success = false, mensaje = "Orden no encontrada" });

                if (orden.Cliente == null || string.IsNullOrWhiteSpace(orden.Cliente.Correo))
                    return Json(new { success = false, mensaje = "El cliente no tiene un correo registrado" });

                var pdfBytes = await PdfConstancia.GenerarFactura(id, _context);
                if (pdfBytes == null || pdfBytes.Length == 0)
                    return Json(new { success = false, mensaje = "Error al generar el PDF" });

                var gastos = orden.Gastos?.ToList() ?? new List<Control_flota.Models.Operaciones.GastoRuta>();
                var peajeTotal = gastos
                    .Where(g => g.Concepto.Contains("peaje", StringComparison.OrdinalIgnoreCase))
                    .Sum(g => g.Monto);
                var montoSolicitud = orden.SolicitudServicio?.Monto;
                var fleteBase = montoSolicitud ?? gastos.Sum(g => g.Monto) - peajeTotal;
                if (fleteBase < 0) fleteBase = 0;

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
                                <p><strong>Flete base:</strong> S/ {fleteBase:F2}</p>
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

        public async Task<IActionResult> ServiciosPorCobrar()
        {
            var viajesCompletados = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Gastos)
                .Include(o => o.SolicitudServicio)
                .Where(o => o.Estado == "Completado")
                .OrderByDescending(o => o.FechaEmision)
                .ToListAsync();

            ViewBag.TotalViajesCompletados = viajesCompletados.Count;
            ViewBag.TotalGastosRegistrados = await _context.GastosRuta.CountAsync();

            return View(viajesCompletados);
        }

        // ==================== MÉTODOS PARA FACTURACIÓN ====================

        public class FacturaRequest
        {
            public int OrdenId { get; set; }
            public string Cliente { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;
            public decimal MontoBase { get; set; }
            public List<SobrecostoItem> Sobrecostos { get; set; } = new List<SobrecostoItem>();
            public decimal Total { get; set; }
            public string FechaVencimiento { get; set; } = string.Empty;
            public string FechaEmision { get; set; } = string.Empty;
        }

        public class SobrecostoItem
        {
            public string Concepto { get; set; } = string.Empty;
            public decimal Monto { get; set; }
        }

        public class PagoRequest
        {
            public int FacturaId { get; set; }
            public string FechaPago { get; set; } = string.Empty;
        }

        public class NotificacionMoraRequest
        {
            public int OrdenId { get; set; }
            public string? Correo { get; set; }
            public string Cliente { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public string FechaVencimiento { get; set; } = string.Empty;
        }

        private string GenerarNumeroFactura()
        {
            var año = DateTime.Now.Year;
            var mes = DateTime.Now.Month;

            var ultimaFactura = _context.Facturas
                .Where(f => f.FechaEmision.Year == año && f.FechaEmision.Month == mes)
                .OrderByDescending(f => f.Id)
                .FirstOrDefault();

            int correlativo = 1;
            if (ultimaFactura != null && ultimaFactura.NumeroFactura.Contains("-"))
            {
                var partes = ultimaFactura.NumeroFactura.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out int num))
                {
                    correlativo = num + 1;
                }
            }

            return $"F{año}{mes:D2}-{correlativo:D6}";
        }

        [HttpPost]
        public async Task<IActionResult> GenerarFacturaPdf([FromBody] FacturaRequest factura)
        {
            try
            {
                if (factura == null || factura.OrdenId == 0)
                    return Json(new { success = false, message = "Datos inválidos" });

                var orden = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == factura.OrdenId);

                if (orden == null)
                    return Json(new { success = false, message = "Orden no encontrada" });

                var sobrecostosParaPdf = factura.Sobrecostos?.Select(s => new PdfConstancia.SobrecostoItem
                {
                    Concepto = s.Concepto,
                    Monto = s.Monto
                }).ToList() ?? new List<PdfConstancia.SobrecostoItem>();

                var pdfBytes = await PdfConstancia.GenerarFacturaConSobrecostos(
                    factura.OrdenId,
                    sobrecostosParaPdf,
                    factura.FechaVencimiento,
                    _context
                );

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return Json(new { success = false, message = "Error al generar PDF" });

                return File(pdfBytes, "application/pdf", $"Factura_{factura.Codigo}.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EmitirYEnviarFactura([FromBody] FacturaRequest factura)
        {
            try
            {
                if (factura == null || factura.OrdenId == 0)
                    return Json(new { success = false, message = "Datos inválidos" });

                var orden = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == factura.OrdenId);

                if (orden == null)
                    return Json(new { success = false, message = "Orden no encontrada" });

                var facturaExistente = await _context.Facturas
                    .FirstOrDefaultAsync(f => f.OrdenId == factura.OrdenId && f.Estado == "Emitida");

                if (facturaExistente != null)
                    return Json(new { success = false, message = "Esta orden ya tiene una factura emitida" });

                var sobrecostosJson = System.Text.Json.JsonSerializer.Serialize(factura.Sobrecostos);
                var numeroFactura = GenerarNumeroFactura();

                var nuevaFactura = new Control_flota.Models.Finanzas.Factura
                {
                    OrdenId = factura.OrdenId,
                    NumeroFactura = numeroFactura,
                    Cliente = factura.Cliente,
                    CodigoOrden = factura.Codigo,
                    MontoBase = factura.MontoBase,
                    SobrecostosJson = sobrecostosJson,
                    Total = factura.Total,
                    FechaEmision = DateTime.Parse(factura.FechaEmision),
                    FechaVencimiento = DateTime.Parse(factura.FechaVencimiento),
                    Estado = "Emitida",
                    FechaCreacion = DateTime.Now
                };

                _context.Facturas.Add(nuevaFactura);
                await _context.SaveChangesAsync();

                var sobrecostosParaPdf = factura.Sobrecostos?.Select(s => new PdfConstancia.SobrecostoItem
                {
                    Concepto = s.Concepto,
                    Monto = s.Monto
                }).ToList() ?? new List<PdfConstancia.SobrecostoItem>();

                var pdfBytes = await PdfConstancia.GenerarFacturaConSobrecostos(
                    factura.OrdenId,
                    sobrecostosParaPdf,
                    factura.FechaVencimiento,
                    _context
                );

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return Json(new { success = false, message = "Error al generar PDF" });

                var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "facturas");
                if (!Directory.Exists(pdfPath))
                    Directory.CreateDirectory(pdfPath);

                var pdfFileName = $"Factura_{numeroFactura}.pdf";
                var pdfFullPath = Path.Combine(pdfPath, pdfFileName);
                await System.IO.File.WriteAllBytesAsync(pdfFullPath, pdfBytes);

                nuevaFactura.RutaPdf = $"/facturas/{pdfFileName}";
                nuevaFactura.FechaEnvio = DateTime.Now;
                await _context.SaveChangesAsync();

                var sobrecostosHtml = string.Empty;
                if (factura.Sobrecostos != null && factura.Sobrecostos.Any())
                {
                    sobrecostosHtml = "<h3>Sobrecostos:</h3><ul>";
                    foreach (var sc in factura.Sobrecostos)
                    {
                        sobrecostosHtml += $"<li>{sc.Concepto}: S/ {sc.Monto:F2}</li>";
                    }
                    sobrecostosHtml += "</ul>";
                }

                var mensajeHtml = $@"
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body>
                        <h2>🚛 De La Sota S.A.C.</h2>
                        <p>Estimado(a) <strong>{factura.Cliente}</strong>,</p>
                        <p>Adjunto encontrarás la factura correspondiente al servicio.</p>
                        <p><strong>N° Factura:</strong> {numeroFactura}</p>
                        <p><strong>Orden:</strong> {factura.Codigo}</p>
                        <p><strong>Monto Base:</strong> S/ {factura.MontoBase:F2}</p>
                        {sobrecostosHtml}
                        <p><strong>Total a Pagar:</strong> S/ {factura.Total:F2}</p>
                        <p><strong>Fecha Vencimiento:</strong> {factura.FechaVencimiento}</p>
                        <p>Gracias por confiar en nosotros.</p>
                    </body>
                    </html>";

                await _emailService.EnviarCorreoConConstanciaAsync(
                    orden.Cliente?.Correo ?? factura.Cliente,
                    $"Factura de Venta - {numeroFactura}",
                    mensajeHtml,
                    pdfBytes,
                    pdfFileName
                );

                return Json(new { success = true, message = $"Factura {numeroFactura} emitida y enviada" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFactura(int ordenId)
        {
            var factura = await _context.Facturas
                .FirstOrDefaultAsync(f => f.OrdenId == ordenId);

            if (factura == null)
                return Json(new { success = false, message = "No hay factura" });

            return Json(new
            {
                success = true,
                factura = new
                {
                    factura.Id,
                    factura.OrdenId,
                    factura.NumeroFactura,
                    factura.Cliente,
                    factura.CodigoOrden,
                    factura.MontoBase,
                    factura.Total,
                    FechaEmision = factura.FechaEmision.ToString("yyyy-MM-dd"),
                    FechaVencimiento = factura.FechaVencimiento.ToString("yyyy-MM-dd"),
                    factura.Estado,
                    factura.RutaPdf,
                    factura.FechaPago
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodasFacturas()
        {
            var facturas = await _context.Facturas
                .Select(f => new { f.OrdenId, f.Estado,  f.FechaPago, FechaVencimiento = f.FechaVencimiento.ToString("yyyy-MM-dd")})
                .ToListAsync();

            return Json(new { success = true, facturas });
        }

        [HttpPost]
        public async Task<IActionResult> EnviarFacturaPorCorreo([FromBody] FacturaRequest factura)
        {
            try
            {
                if (factura == null || factura.OrdenId == 0)
                    return Json(new { success = false, message = "Datos inválidos" });

                var orden = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == factura.OrdenId);

                if (orden == null)
                    return Json(new { success = false, message = "Orden no encontrada" });

                if (orden.Cliente == null || string.IsNullOrWhiteSpace(orden.Cliente.Correo))
                    return Json(new { success = false, message = "El cliente no tiene correo registrado" });

                var sobrecostosParaPdf = factura.Sobrecostos?.Select(s => new PdfConstancia.SobrecostoItem
                {
                    Concepto = s.Concepto,
                    Monto = s.Monto
                }).ToList() ?? new List<PdfConstancia.SobrecostoItem>();

                var pdfBytes = await PdfConstancia.GenerarFacturaConSobrecostos(
                    factura.OrdenId,
                    sobrecostosParaPdf,
                    factura.FechaVencimiento,
                    _context
                );

                if (pdfBytes == null || pdfBytes.Length == 0)
                    return Json(new { success = false, message = "Error al generar PDF" });

                var sobrecostosHtml = string.Empty;
                if (factura.Sobrecostos != null && factura.Sobrecostos.Any())
                {
                    sobrecostosHtml = "<h3>Sobrecostos:</h3><ul>";
                    foreach (var sc in factura.Sobrecostos)
                    {
                        sobrecostosHtml += $"<li>{sc.Concepto}: S/ {sc.Monto:F2}</li>";
                    }
                    sobrecostosHtml += "</ul>";
                }

                var mensajeHtml = $@"
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body>
                        <h2>🚛 De La Sota S.A.C.</h2>
                        <p>Estimado(a) <strong>{orden.Cliente.Nombre}</strong>,</p>
                        <p>Adjunto encontrarás la factura correspondiente al servicio.</p>
                        <p><strong>Orden:</strong> {factura.Codigo}</p>
                        <p><strong>Monto Base:</strong> S/ {factura.MontoBase:F2}</p>
                        {sobrecostosHtml}
                        <p><strong>Total a Pagar:</strong> S/ {factura.Total:F2}</p>
                        <p><strong>Fecha Vencimiento:</strong> {factura.FechaVencimiento}</p>
                        <p>Gracias por confiar en nosotros.</p>
                    </body>
                    </html>";

                var exito = await _emailService.EnviarCorreoConConstanciaAsync(
                    orden.Cliente.Correo,
                    $"Factura de Venta - Orden {factura.Codigo}",
                    mensajeHtml,
                    pdfBytes,
                    $"Factura_{factura.Codigo}.pdf"
                );

                if (exito)
                    return Json(new { success = true, message = $"Factura enviada a: {orden.Cliente.Correo}" });

                return Json(new { success = false, message = "Error al enviar el correo" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ==================== REGISTRAR PAGO ====================

        [HttpPost]
        public async Task<IActionResult> RegistrarPago([FromBody] PagoRequest request)
        {
            try
            {
                var factura = await _context.Facturas
                    .FirstOrDefaultAsync(f => f.OrdenId == request.FacturaId);

                if (factura == null)
                    return Json(new { success = false, message = "Factura no encontrada" });

                if (factura.Estado == "Cancelada")
                    return Json(new { success = false, message = "Esta factura ya está cancelada" });

                var fechaPago = DateTime.Parse(request.FechaPago);
                var hoy = DateTime.Today;
        
                if (fechaPago < hoy)
                    return Json(new { success = false, message = "❌ La fecha de pago no puede ser menor a la fecha actual" });

                factura.FechaPago = fechaPago;
                factura.Estado = "Cancelada";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "✅ Pago registrado. Factura cancelada" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ==================== NOTIFICAR MORA ====================

        [HttpPost]
        public async Task<IActionResult> NotificarMora([FromBody] NotificacionMoraRequest request)
        {
            try
            {
                var orden = await _context.Ordenes
                    .Include(o => o.Cliente)
                    .FirstOrDefaultAsync(o => o.Id == request.OrdenId);

                if (orden == null)
                    return Json(new { success = false, message = "Orden no encontrada" });
                
                var correoDestino = orden?.Cliente?.Correo;
                
                if (string.IsNullOrWhiteSpace(correoDestino))
                    return Json(new { success = false, message = "No hay correo registrado para notificar" });

                var mensajeHtml = $@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
                            .container {{ max-width: 600px; margin: auto; padding: 20px; }}
                            .header {{ background: #dc2626; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .alerta {{ background: #fee2e2; padding: 15px; border-left: 4px solid #dc2626; margin: 15px 0; }}
                            .footer {{ margin-top: 20px; font-size: 12px; color: #64748b; text-align: center; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>⚠️ ALERTA DE MORA</h2>
                                <p>De La Sota S.A.C.</p>
                            </div>
                            <div class='content'>
                                <p>Estimado(a) <strong>{request.Cliente}</strong>,</p>
                                <div class='alerta'>
                                    <p><strong>📢 Su factura se encuentra VENCIDA</strong></p>
                                    <p><strong>Orden:</strong> {request.Codigo}</p>
                                    <p><strong>Monto Total:</strong> S/ {request.Total:F2}</p>
                                    <p><strong>Fecha de Vencimiento:</strong> {request.FechaVencimiento}</p>
                                </div>
                                <p>Por favor, regularice su situación a la brevedad para evitar cargos adicionales.</p>
                                <p>Si ya realizó el pago, ignore este mensaje.</p>
                                <p>Saludos cordiales,<br><strong>Departamento de Finanzas</strong></p>
                            </div>
                            <div class='footer'>
                                <p>© {DateTime.Now.Year} De La Sota S.A.C.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                var exito = await _emailService.EnviarCorreoAsync(
                    correoDestino,
                    $"⚠️ ALERTA DE MORA - Factura Vencida {request.Codigo}",
                    mensajeHtml
                );

                if (exito)
                    return Json(new { success = true, message = "Correo de recordatorio enviado exitosamente" });

                return Json(new { success = false, message = "Error al enviar el correo" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}