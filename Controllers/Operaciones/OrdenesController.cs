using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text.RegularExpressions;
using Control_flota.CreatePdf;

namespace Control_flota.Services;

public class OrdenesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public OrdenesController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // Lista general de órdenes y solicitudes pendientes en despacho
    public async Task<IActionResult> Index(string? buscar, bool busquedaRealizada = false)
    {
        ViewBag.Clientes = _context.Clientes.ToList();

        ViewBag.TipoCargaOptions = new List<string>
        {
            "Carga General",
            "Carga Refrigerada",
            "Carga Peligrosa"
        };

        var queryOrdenes = _context.Ordenes
            .Include(o => o.Cliente)
            .AsQueryable();

        // Solo filtrar cuando realmente se presionó Buscar
        if (busquedaRealizada)
        {
            if (string.IsNullOrWhiteSpace(buscar))
            {
                queryOrdenes = queryOrdenes.Where(o => false);
            }
            else
            {
                queryOrdenes = queryOrdenes.Where(o =>
                    (o.Cliente != null && o.Cliente.Nombre.Contains(buscar)) ||
                    o.Origen.Contains(buscar) ||
                    o.Destino.Contains(buscar));
            }
        }

        var ordenes = await queryOrdenes
            .OrderByDescending(o => o.FechaEmision)
            .ToListAsync();

        var solicitudesPendientes = await _context.SolicitudesServicio
            .Include(s => s.Cliente)
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .Where(s => s.EstadoSolicitud == "Pendiente de Asignación"
                     || s.EstadoSolicitud == "Pendiente")
            .OrderByDescending(s => s.FechaDespacho)
            .ToListAsync();

        var model = new DespachoViewModel
        {
            Ordenes = ordenes,
            SolicitudesPendientes = solicitudesPendientes
        };

        ViewBag.BusquedaRealizada = busquedaRealizada;
        ViewBag.Buscar = buscar;

        return View(model);
    }

    // Detalle de orden (Vista del Administrador)
    public async Task<IActionResult> Details(int id)
    {
       var orden = await _context.Ordenes
    .Include(o => o.Cliente)
    .Include(o => o.Gastos)
    .Include(o => o.Evidencias)
    .FirstOrDefaultAsync(o => o.Id == id);
        if (orden == null)
            return NotFound();

        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        ViewBag.Solicitud = solicitud;
        ViewBag.Conductor = solicitud?.Conductor;
        ViewBag.Unidad = solicitud?.Unidad;

        var estadoInicial = await _context.EstadosInicialesRuta
            .Where(e => e.OrdenId == orden.Id)
            .OrderByDescending(e => e.FechaRegistro)
            .FirstOrDefaultAsync();
        ViewBag.EstadoInicial = estadoInicial;
        ViewBag.RutaFotoFinal = GetRutaFotoFinal(orden.Id);

        return View(orden);
    }

    private string? GetRutaFotoFinal(int ordenId)
    {
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "finales");
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var files = Directory.GetFiles(folder, $"orden-{ordenId}-*.*");
        if (files.Length == 0)
        {
            return null;
        }

        var latestFile = files
            .OrderByDescending(f => System.IO.File.GetCreationTimeUtc(f))
            .FirstOrDefault();

        return latestFile != null ? "/finales/" + Path.GetFileName(latestFile) : null;
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
        .Include(o => o.Gastos)
        .Where(o => o.ConductorId == user.Conductor.Id)
        .OrderByDescending(o => o.FechaEmision)
        .ToListAsync();

    // Verificar si hay notificación de parada pendiente para este conductor
    var tieneNotificacion = await _context.NotificacionesParada
        .AnyAsync(n => n.ConductorId == user.Conductor.Id && n.Leida == false);

    if (tieneNotificacion)
    {
        // Marcar como leída para que no aparezca de nuevo
        var notificaciones = await _context.NotificacionesParada
            .Where(n => n.ConductorId == user.Conductor.Id && n.Leida == false)
            .ToListAsync();

        foreach (var n in notificaciones)
            n.Leida = true;

        await _context.SaveChangesAsync();
        ViewBag.NotificacionParada = true;
    }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> IniciarRutaConDatos(int ordenId, decimal combustibleInicial, int kilometrajeInicial, string? observacion, IFormFile? fotoInicio)
    {
        var orden = await _context.Ordenes.FindAsync(ordenId);
        if (orden == null) return NotFound();

        if (orden.Estado != "Emitida")
        {
            TempData["Error"] = "Solo puedes iniciar una orden en estado 'Emitida'.";
            return RedirectToAction(nameof(PanelConductor));
        }

        if (combustibleInicial < 0 || kilometrajeInicial < 0)
        {
            TempData["Error"] = "Los valores no pueden ser negativos.";
            return RedirectToAction(nameof(PanelConductor));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            var rawComb = Request.Form["combustibleInicial"].ToString();
            var rawKm = Request.Form["kilometrajeInicial"].ToString();
            var msg = "Entrada inválida: ";
            if (errors.Any()) msg += string.Join("; ", errors);
            msg += $" (combustible='{rawComb}', km='{rawKm}')";
            TempData["Error"] = msg;
            return RedirectToAction(nameof(PanelConductor));
        }

        string? rutaImagen = null;
        if (fotoInicio != null)
        {
            if (fotoInicio.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "La imagen supera el peso máximo de 5MB permitido";
                return RedirectToAction(nameof(PanelConductor));
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/inicios");
            Directory.CreateDirectory(folder);
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fotoInicio.FileName);
            string path = Path.Combine(folder, fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await fotoInicio.CopyToAsync(stream);
            }
            rutaImagen = "/inicios/" + fileName;
        }

        var estado = new EstadoInicialRuta
        {
            OrdenId = ordenId,
            CombustibleInicial = combustibleInicial,
            KilometrajeInicial = kilometrajeInicial,
            Observacion = observacion,
            RutaImagen = rutaImagen,
            FechaRegistro = DateTime.UtcNow
        };

        _context.EstadosInicialesRuta.Add(estado);
        orden.Estado = "En Tránsito";
        _context.Update(orden);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Exito"] = "Ruta iniciada correctamente y datos iniciales guardados.";
            return RedirectToAction(nameof(PanelConductor));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error guardando EstadoInicialRuta (intento 1): " + ex.Message);
            try
            {
                _context.Database.EnsureCreated();
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Ruta iniciada correctamente y datos iniciales guardados.";
                return RedirectToAction(nameof(PanelConductor));
            }
            catch (Exception ex2)
            {
                Console.WriteLine("Error guardando EstadoInicialRuta (intento 2): " + ex2.Message);
                var inner = ex2.InnerException?.Message;
                var detalle = inner ?? ex2.Message;
                if (detalle.Length > 300) detalle = detalle.Substring(0, 300) + "...";
                TempData["Error"] = "No se pudo guardar los datos iniciales. Detalle: " + detalle;
                return RedirectToAction(nameof(PanelConductor));
            }
        }
    }

    // Finalizar Ruta
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> FinalizarRuta(int ordenId, string recepcionistaNombre, string recepcionistaDni, string? fotoFinal, int kilometrajeFinal)
    {
        var orden = await _context.Ordenes.FindAsync(ordenId);
        if (orden == null) return NotFound();

        if (orden.Estado != "En Tránsito")
        {
            TempData["Error"] = "Solo puedes finalizar una orden que esté 'En Tránsito'.";
            return RedirectToAction(nameof(PanelConductor));
        }

        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        if (solicitud == null)
        {
            TempData["Error"] = "No se encontró la solicitud de servicio asociada.";
            return RedirectToAction(nameof(PanelConductor));
        }

        if (string.IsNullOrWhiteSpace(solicitud.NombreReceptor) || string.IsNullOrWhiteSpace(solicitud.DniReceptor))
        {
            TempData["Error"] = "La solicitud no contiene datos de receptor para validar.";
            return RedirectToAction(nameof(PanelConductor));
        }

        if (!Regex.IsMatch(recepcionistaNombre?.Trim() ?? string.Empty, @"^[A-Za-zÀ-ÿÑñ ]+$"))
        {
            TempData["Error"] = "El nombre del recepcionista no puede contener números ni símbolos.";
            return RedirectToAction(nameof(PanelConductor));
        }

        if (!string.Equals(solicitud.NombreReceptor?.Trim(), recepcionistaNombre?.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(solicitud.DniReceptor?.Trim(), recepcionistaDni?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Los datos del recepcionista no coinciden con la solicitud de servicio.";
            return RedirectToAction(nameof(PanelConductor));
        }

        // ==========================================
        // === INICIO DE LÓGICA HU-014 ===
        // ==========================================
        var estadoInicial = await _context.EstadosInicialesRuta
            .Where(e => e.OrdenId == ordenId)
            .OrderByDescending(e => e.FechaRegistro)
            .FirstOrDefaultAsync();

        if (estadoInicial != null && kilometrajeFinal < estadoInicial.KilometrajeInicial)
        {
            TempData["Error"] = $"El kilometraje final ({kilometrajeFinal}) no puede ser menor al inicial ({estadoInicial.KilometrajeInicial}).";
            return RedirectToAction(nameof(PanelConductor));
        }
        // ==========================================
        // === FIN DE LÓGICA HU-014 ===
        // ==========================================

        string? rutaFotoFinal = null;
        if (!string.IsNullOrWhiteSpace(fotoFinal) && fotoFinal.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = fotoFinal.IndexOf(',');
            if (commaIndex > 0)
            {
                var header = fotoFinal.Substring(11, commaIndex - 11); // e.g. jpeg;base64
                var base64Data = fotoFinal.Substring(commaIndex + 1);

                byte[] imageBytes;
                try
                {
                    imageBytes = Convert.FromBase64String(base64Data);
                }
                catch
                {
                    imageBytes = Array.Empty<byte>();
                }

                if (imageBytes.Length > 0)
                {
                    if (imageBytes.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "La imagen supera el peso máximo de 5MB permitido.";
                        return RedirectToAction(nameof(PanelConductor));
                    }

                    var extension = ".jpg";
                    if (header.Contains("png", StringComparison.OrdinalIgnoreCase)) extension = ".png";
                    else if (header.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || header.Contains("jpg", StringComparison.OrdinalIgnoreCase)) extension = ".jpg";
                    else if (header.Contains("gif", StringComparison.OrdinalIgnoreCase)) extension = ".gif";

                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "finales");
                    Directory.CreateDirectory(folder);
                    var fileName = $"orden-{ordenId}-{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(folder, fileName);
                    await System.IO.File.WriteAllBytesAsync(path, imageBytes);
                    rutaFotoFinal = "/finales/" + fileName;
                }
            }
        }

        //sdsd
        orden.Estado = "Completado";
        _context.Update(orden);

        solicitud.EstadoSolicitud = "Completado";

        if (solicitud.Conductor != null)
        {
            solicitud.Conductor.Actividad = "Libre";
            _context.Update(solicitud.Conductor);
        }

        if (solicitud.Unidad != null)
        {
            solicitud.Unidad.Actividad = "Libre";
            // === LÓGICA HU-014: Asignamos el nuevo kilometraje a la unidad ===
            solicitud.Unidad.KilometrajeActual = kilometrajeFinal; 
            _context.Update(solicitud.Unidad);
        }

        await _context.SaveChangesAsync();

        // === LÓGICA HU-014: Modificamos el mensaje para cumplir el criterio de aceptación ===
        TempData["Exito"] = rutaFotoFinal != null 
            ? "Ruta finalizada, evidencia guardada y registro de kilometraje almacenado." 
            : "Ruta finalizada y registro de kilometraje almacenado. No se guardó evidencia final.";
            
        return RedirectToAction(nameof(PanelConductor));
    }

    // --- NUEVAS ACCIONES DE GASTOS (HU-12) ---
    [HttpGet]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> ReportarGasto(int id)
    {
        var orden = await _context.Ordenes
            .Include(o => o.Gastos)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orden == null) return NotFound();
        
        return View(orden);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> ReportarGasto(int ordenId, decimal monto, string concepto, IFormFile comprobante, bool esCombustible = false, decimal? galones = null)
    {
        if (monto <= 0)
        {
            TempData["Error"] = "El monto debe ser numérico y mayor a cero";
            return RedirectToAction("ReportarGasto", new { id = ordenId });
        }

        if (esCombustible && (!galones.HasValue || galones.Value <= 0))
        {
            TempData["Error"] = "Los galones deben ser un número mayor a cero";
            return RedirectToAction("ReportarGasto", new { id = ordenId });
        }

        string? rutaImagen = null;

        if (comprobante != null)
        {
            if (comprobante.Length > 5 * 1024 * 1024) 
            {
                TempData["Error"] = "La imagen supera el peso máximo de 5MB permitido";
                return RedirectToAction("ReportarGasto", new { id = ordenId });
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/gastos");
            Directory.CreateDirectory(folder);
            
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(comprobante.FileName);
            string path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await comprobante.CopyToAsync(stream);
            }
            rutaImagen = "/gastos/" + fileName;
        }

        var gasto = new GastoRuta
        {
            OrdenId = ordenId,
            Monto = monto,
            Concepto = concepto,
            RutaComprobante = rutaImagen
        };

        _context.GastosRuta.Add(gasto);
        
        if (esCombustible)
        {
            var consumo = new ConsumoCombustible
            {
                OrdenId = ordenId,
                Galones = galones.GetValueOrDefault(),
                Monto = monto,
                Fecha = DateTime.UtcNow
            };
            _context.ConsumosCombustible.Add(consumo);
        }
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Gasto registrado correctamente.";
        return RedirectToAction(nameof(PanelConductor));
    }

    // NUEVO MÉTODO PARA ELIMINAR GASTOS
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> EliminarGasto(int id, int ordenId)
    {
        var gasto = await _context.GastosRuta.FindAsync(id);
        if (gasto != null)
        {
            if (!string.IsNullOrEmpty(gasto.RutaComprobante))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", gasto.RutaComprobante.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.GastosRuta.Remove(gasto);
            await _context.SaveChangesAsync();
            TempData["Exito"] = "Gasto eliminado correctamente.";
        }

        return RedirectToAction("ReportarGasto", new { id = ordenId });
    }

    // ==================== MÉTODOS DE CONSTANCIA ====================
    
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> DescargarConstancia(int id)
    {
        var pdfBytes = await PdfConstancia.Generar(id, _context);
        if (pdfBytes == null || pdfBytes.Length == 0)
            return NotFound();
        var orden = await _context.Ordenes.FindAsync(id);
        return File(pdfBytes, "application/pdf", $"Constancia_{orden?.Codigo}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarConstanciaPorCorreo(int id)
    {
        try
        {
            var orden = await _context.Ordenes
                .Include(o => o.Cliente)
                .Include(o => o.Gastos)
                .Include(o => o.SolicitudServicio)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
                return Json(new { success = false, message = "Orden no encontrada" });

            if (orden.Cliente == null || string.IsNullOrEmpty(orden.Cliente.Correo))
                return Json(new { success = false, message = "El cliente no tiene un correo registrado" });

            var pdfBytes = await PdfConstancia.Generar(id, _context);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return Json(new { success = false, message = "Error al generar el PDF" });

            var gastos = orden.Gastos?.ToList() ?? new List<Control_flota.Models.Operaciones.GastoRuta>();
            var peajeTotal = gastos
                .Where(g => g.Concepto.Contains("peaje", StringComparison.OrdinalIgnoreCase))
                .Sum(g => g.Monto);
            var montoSolicitud = orden.SolicitudServicio?.Monto;
            var fleteBase = montoSolicitud ?? gastos.Sum(g => g.Monto) - peajeTotal;
            if (fleteBase < 0) fleteBase = 0;

            // HTML del correo
            var mensajeHtml = $@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: auto; }}
                        .header {{ background: #002d62; color: white; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .footer {{ background: #f1f1f1; padding: 10px; text-align: center; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>🚛 De La Sota S.A.C.</h2>
                            <p>Constancia de Viaje</p>
                        </div>
                        <div class='content'>
                            <h3>Estimado(a) {orden.Cliente.Nombre},</h3>
                            <p>Adjunto encontrará la constancia de su servicio de transporte.</p>
                            <p><strong>📄 Orden:</strong> {orden.Codigo}</p>
                            <p><strong>📍 Ruta:</strong> {orden.Origen} → {orden.Destino}</p>
                            <p><strong>💰 Flete base:</strong> S/ {fleteBase:F2}</p>
                            <p><strong>📅 Fecha:</strong> {orden.FechaEmision:dd/MM/yyyy HH:mm}</p>
                            <p><strong>📊 Estado:</strong> {orden.Estado}</p>
                            <p>Saludos cordiales,<br><strong>Departamento de Logística</strong></p>
                        </div>
                        <div class='footer'>
                            <p>© {DateTime.Now.Year} De La Sota S.A.C.</p>
                        </div>
                    </div>
                </body>
                </html>";

            // Enviar correo
             var exito = await _emailService.EnviarCorreoConConstanciaAsync(
                orden.Cliente.Correo,
                $"Constancia de Viaje - Orden {orden.Codigo}",
                mensajeHtml,
                pdfBytes,
                $"Constancia_{orden.Codigo}.pdf"
            );

            if (exito)
                return Json(new { success = true, message = $"Constancia enviada a: {orden.Cliente.Correo}" });
            else
                return Json(new { success = false, message = $" Error al enviar el correo" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"❌ Error: {ex.Message}" });
        }
    }

    // ==================== OTROS MÉTODOS ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> ReportarUbicacion(int id, string ciudad)
    {
        var orden = await _context.Ordenes.FindAsync(id);
        if (orden == null) return NotFound();

        orden.UltimaCiudad = ciudad;
        _context.Update(orden);
        await _context.SaveChangesAsync();

        TempData["Exito"] = $"Ubicación actualizada: {ciudad}";
        return RedirectToAction(nameof(PanelConductor));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> ReportarIncidencia(
        int OrdenId,
        string TipoIncidencia,
        string Descripcion,
        double? Latitud,
        double? Longitud)
    {
        var user = await _context.Users
            .Include(u => u.Conductor)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user?.Conductor == null)
        {
            TempData["Error"] = "No se encontró el conductor.";
            return RedirectToAction(nameof(PanelConductor));
        }

        var orden = await _context.Ordenes.FindAsync(OrdenId);

        if (orden == null)
        {
            TempData["Error"] = "La orden no existe.";
            return RedirectToAction(nameof(PanelConductor));
        }

        var incidencia = new IncidenciaRuta
        {
            OrdenId = OrdenId,
            ConductorId = user.Conductor.Id,
            TipoIncidencia = TipoIncidencia,
            Descripcion = Descripcion,
            Latitud = Latitud,
            Longitud = Longitud,
            FechaReporte = DateTime.Now,
            Estado = "Pendiente"
        };

        _context.IncidenciasRuta.Add(incidencia);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Alerta registrada correctamente.";
        return RedirectToAction(nameof(PanelConductor));
    }


    // HU-029: Subir evidencias durante la ruta (máximo 3 por orden)
    
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Conductor")]
public async Task<IActionResult> SubirEvidencia(int ordenId, IFormFile foto)
{
    var orden = await _context.Ordenes
        .Include(o => o.Evidencias)
        .FirstOrDefaultAsync(o => o.Id == ordenId);

    if (orden == null) return NotFound();

    if (orden.Estado != "En Tránsito")
    {
        TempData["Error"] = "Solo puedes subir evidencias en una orden En Tránsito.";
        return RedirectToAction(nameof(PanelConductor));
    }

    var totalEvidencias = orden.Evidencias?.Count ?? 0;
    if (totalEvidencias >= 3)
    {
        TempData["Error"] = "Ya alcanzaste el máximo de 3 evidencias para esta orden.";
        return RedirectToAction(nameof(PanelConductor));
    }

    if (foto == null || foto.Length == 0)
    {
        TempData["Error"] = "Debes seleccionar una foto.";
        return RedirectToAction(nameof(PanelConductor));
    }

    if (foto.Length > 5 * 1024 * 1024)
    {
        TempData["Error"] = "La imagen supera el peso máximo de 5MB permitido.";
        return RedirectToAction(nameof(PanelConductor));
    }

    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "evidencias");
    Directory.CreateDirectory(folder);
    var fileName = $"orden-{ordenId}-ev-{Guid.NewGuid()}{Path.GetExtension(foto.FileName)}";
    var path = Path.Combine(folder, fileName);

    using (var stream = new FileStream(path, FileMode.Create))
    {
        await foto.CopyToAsync(stream);
    }

    var evidencia = new EvidenciaRuta
    {
        OrdenId = ordenId,
        RutaImagen = "/evidencias/" + fileName,
        FechaRegistro = DateTime.UtcNow
    };

    _context.EvidenciasRuta.Add(evidencia);
    await _context.SaveChangesAsync();

    TempData["Exito"] = "Evidencia subida correctamente.";
    return RedirectToAction(nameof(PanelConductor));
}
   //eliminar orden//
    [HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Administrador")]
public async Task<IActionResult> EliminarOrden(int id)
{
    var orden = await _context.Ordenes.FindAsync(id);
    if (orden == null) return NotFound();

    if (orden.Estado == "En Tránsito")
    {
        TempData["Error"] = "No puedes eliminar una orden que está En Tránsito.";
        return RedirectToAction(nameof(Index));
    }

    // Revertir la solicitud a su estado anterior
    var solicitud = await _context.SolicitudesServicio
        .Include(s => s.Conductor)
        .Include(s => s.Unidad)
        .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

    if (solicitud != null)
    {
        solicitud.EstadoSolicitud = "Pendiente de Asignación";

        if (solicitud.Conductor != null)
            solicitud.Conductor.Actividad = "Libre";

        if (solicitud.Unidad != null)
            solicitud.Unidad.Actividad = "Libre";
    }

    _context.Ordenes.Remove(orden);
    await _context.SaveChangesAsync();

    TempData["Exito"] = "Orden eliminada correctamente. La solicitud volvió a estado Pendiente.";
    return RedirectToAction(nameof(Index));
}






}