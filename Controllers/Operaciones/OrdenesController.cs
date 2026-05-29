using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Control_flota.Data;
using Control_flota.Models.Operaciones;
using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO; // Necesario para guardar las imágenes de los gastos
using System.Text.RegularExpressions;

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
// Detalle de orden (Vista del Administrador)
    public async Task<IActionResult> Details(int id)
    {
        var orden = await _context.Ordenes
            .Include(o => o.Cliente)
            .Include(o => o.Gastos) // <-- AGREGA ESTA LÍNEA AQUÍ
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

        // Cargar datos iniciales de ruta si existen (último registro)
        var estadoInicial = await _context.EstadosInicialesRuta
            .Where(e => e.OrdenId == orden.Id)
            .OrderByDescending(e => e.FechaRegistro)
            .FirstOrDefaultAsync();
        ViewBag.EstadoInicial = estadoInicial;

        return View(orden);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var orden = await _context.Ordenes.FindAsync(id);
        if (orden == null)
            return NotFound();

        // 1. Rescatamos la solicitud original ANTES de borrar la orden
        var solicitud = await _context.SolicitudesServicio
            .Include(s => s.Conductor)
            .Include(s => s.Unidad)
            .FirstOrDefaultAsync(s => s.Id == orden.SolicitudServicioId);

        if (solicitud != null)
        {
            // 2. Devolvemos la solicitud a estado pendiente para que puedas asignarle otra flota
            solicitud.EstadoSolicitud = "Pendiente de Asignación";

            // 3. Liberamos al conductor
            if (solicitud.Conductor != null)
            {
                solicitud.Conductor.Actividad = "Libre";
                _context.Update(solicitud.Conductor);
            }

            // 4. Liberamos a la unidad
            if (solicitud.Unidad != null)
            {
                solicitud.Unidad.Actividad = "Libre";
                _context.Update(solicitud.Unidad);
            }
        }

        // 5. Ahora sí, borramos la orden
        _context.Ordenes.Remove(orden);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Orden eliminada. El conductor y la unidad han sido liberados.";
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
            .Include(o => o.Gastos)
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

        // Validación server-side: no aceptar valores negativos
        if (combustibleInicial < 0 || kilometrajeInicial < 0)
        {
            TempData["Error"] = "Los valores no pueden ser negativos.";
            return RedirectToAction(nameof(PanelConductor));
        }

        // Validación de ModelState para capturar posibles problemas de binding (ej. formatos inválidos)
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
            // Intentar asegurar la base de datos y reintentar (útil en desarrollo cuando faltan tablas)
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
                // Intentar mostrar inner exception si existe para diagnóstico
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
    public async Task<IActionResult> FinalizarRuta(int ordenId, string recepcionistaNombre, string recepcionistaDni)
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
            _context.Update(solicitud.Unidad);
        }

        await _context.SaveChangesAsync();

        TempData["Exito"] = "Ruta finalizada. Conductor y unidad liberados.";
        return RedirectToAction(nameof(PanelConductor));
    }

// --- NUEVAS ACCIONES DE GASTOS (HU-12) ---
    [HttpGet]
    [Authorize(Roles = "Conductor")]
    public async Task<IActionResult> ReportarGasto(int id)
    {
        // Cambiamos el FindAsync por un Include para traer los gastos que ya existen
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
        // Si se reporta combustible, crear también el registro de ConsumoCombustible
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
            // Borrar el archivo físico para no saturar el servidor
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
    // -----------------------------------------
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
}