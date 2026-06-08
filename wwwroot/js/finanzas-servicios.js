let facturaActual = {
    ordenId: null,
    cliente: '',
    codigo: '',
    montoBase: 0,
    sobrecostos: [],
    fechaVencimiento: ''
};

function showFlash(message, success = true) {
    const flash = document.getElementById('flashMensaje');
    if (!flash) {
        alert(message);
        return;
    }
    flash.style.display = 'block';
    flash.style.background = success ? '#ecfdf5' : '#fff1f2';
    flash.style.color = success ? '#065f46' : '#991b1b';
    flash.style.border = success ? '1px solid #bbf7d0' : '1px solid #fca5a5';
    flash.textContent = message;
    setTimeout(() => { flash.style.display = 'none'; }, 6000);
}

function abrirFormularioFactura(ordenId, cliente, codigo, montoBase) {
    facturaActual = {
        ordenId: ordenId,
        cliente: cliente,
        codigo: codigo,
        montoBase: parseFloat(montoBase) || 0,
        sobrecostos: [],
        fechaVencimiento: ''
    };
    
    document.getElementById('facturaOrdenId').value = ordenId;
    document.getElementById('facturaCliente').value = cliente;
    document.getElementById('facturaCodigo').value = codigo;
    document.getElementById('facturaMontoBase').value = 'S/ ' + (parseFloat(montoBase) || 0).toFixed(2);
    
    const fecha = new Date();
    fecha.setDate(fecha.getDate() + 30);
    document.getElementById('facturaFechaVencimiento').value = fecha.toISOString().split('T')[0];
    
    const lista = document.getElementById('listaSobrecostos');
    lista.innerHTML = `
        <div class="sobrecosto-item">
            <input type="text" placeholder="Concepto" class="sobrecosto-concepto" style="width: 60%;" />
            <input type="number" placeholder="Monto" class="sobrecosto-monto" style="width: 35%;" step="0.01" oninput="actualizarTotal()" />
            <button type="button" class="btn-eliminar-sobrecosto" onclick="eliminarSobrecosto(this)" style="display: none;">✕</button>
        </div>
    `;
    
    actualizarTotal();
    document.getElementById('modalFormularioFactura').classList.add('activo');
}

function cerrarFormularioFactura() {
    document.getElementById('modalFormularioFactura').classList.remove('activo');
    facturaActual = {
        ordenId: null,
        cliente: '',
        codigo: '',
        montoBase: 0,
        sobrecostos: [],
        fechaVencimiento: ''
    };
}

function agregarSobrecosto() {
    const lista = document.getElementById('listaSobrecostos');
    const nuevoItem = document.createElement('div');
    nuevoItem.className = 'sobrecosto-item';
    nuevoItem.innerHTML = `
        <input type="text" placeholder="Concepto" class="sobrecosto-concepto" style="width: 60%;" />
        <input type="number" placeholder="Monto" class="sobrecosto-monto" style="width: 35%;" step="0.01" oninput="actualizarTotal()" />
        <button type="button" class="btn-eliminar-sobrecosto" onclick="eliminarSobrecosto(this)">✕</button>
    `;
    lista.appendChild(nuevoItem);
    
    const items = lista.querySelectorAll('.sobrecosto-item');
    items.forEach(item => {
        const btn = item.querySelector('.btn-eliminar-sobrecosto');
        if (btn) btn.style.display = 'flex';
    });
}

function eliminarSobrecosto(btn) {
    const item = btn.parentElement;
    item.remove();
    actualizarTotal();
}

function actualizarTotal() {
    const montos = document.querySelectorAll('.sobrecosto-monto');
    let totalSobrecostos = 0;
    montos.forEach(input => {
        const valor = parseFloat(input.value) || 0;
        totalSobrecostos += valor;
    });
    
    const total = facturaActual.montoBase + totalSobrecostos;
    document.getElementById('totalFactura').textContent = 'S/ ' + total.toFixed(2);
    
    const conceptos = document.querySelectorAll('.sobrecosto-concepto');
    facturaActual.sobrecostos = [];
    for (let i = 0; i < conceptos.length; i++) {
        const concepto = conceptos[i].value.trim();
        const monto = parseFloat(montos[i]?.value) || 0;
        if (concepto && monto > 0) {
            facturaActual.sobrecostos.push({ concepto, monto });
        }
    }
}

function mostrarVistaPreviaFactura(factura) {
    const sobrecostosHtml = factura.sobrecostos.map(s => `
        <div class="factura-info-row">
            <span>Sobrecosto: ${s.concepto}</span>
            <span>S/ ${s.monto.toFixed(2)}</span>
        </div>
    `).join('');
    
    const html = `
        <div class="factura-section">
            <h3>Cliente</h3>
            <div class="factura-info">${factura.cliente}</div>
        </div>
        <div class="factura-section">
            <h3>Detalles del Servicio</h3>
            <div class="factura-info-row"><span class="factura-info-label">Código:</span><span>${factura.codigo}</span></div>
            <div class="factura-info-row"><span class="factura-info-label">Fecha Emisión:</span><span>${factura.fechaEmision}</span></div>
            <div class="factura-info-row"><span class="factura-info-label">Fecha Vencimiento:</span><span>${factura.fechaVencimiento}</span></div>
        </div>
        <div class="factura-section">
            <h3>Desglose</h3>
            <div class="factura-info-row"><span>Monto Base</span><span>S/ ${factura.montoBase.toFixed(2)}</span></div>
            ${sobrecostosHtml}
            <div class="factura-info-row" style="border-top: 2px solid #e2e8f0; margin-top: 8px; padding-top: 8px; font-weight: bold;">
                <span>TOTAL A PAGAR</span>
                <span style="color: #059669; font-size: 1.2rem;">S/ ${factura.total.toFixed(2)}</span>
            </div>
        </div>
    `;
    
    document.getElementById('contenidoFactura').innerHTML = html;
    document.getElementById('modalVistaFactura').classList.add('activo');
}

function cerrarVistaFactura() {
    document.getElementById('modalVistaFactura').classList.remove('activo');
}

function descargarFacturaPdf() {
    const facturaStr = localStorage.getItem('facturaPendiente');
    if (!facturaStr) {
        showFlash('No hay factura para descargar', false);
        return;
    }
    
    const factura = JSON.parse(facturaStr);
    
    fetch('/Finanzas/GenerarFacturaPdf', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(factura)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Error en la respuesta del servidor');
        }
        return response.blob();
    })
    .then(blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Factura_${factura.codigo}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        showFlash('Factura descargada exitosamente', true);
    })
    .catch(error => {
        console.error('Error:', error);
        showFlash('Error al generar la factura: ' + error.message, false);
    });
}

// ==================== FUNCIONES NUEVAS ====================

function cargarEstadosFacturas() {
    fetch('/Finanzas/ObtenerTodasFacturas')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                data.facturas.forEach(factura => {
                    const boton = document.querySelector(`.btn-generar-factura[data-orden-id="${factura.ordenId}"]`);
                    let badge = document.getElementById(`estado-${factura.ordenId}`);
                    
                    let estadoTexto = factura.estado;
                    let estadoClase = factura.estado.toLowerCase();
                    
                    if (factura.fechaPago) {
                        estadoTexto = 'CANCELADA';
                        estadoClase = 'cancelada';
                    } else if (factura.estado === 'Emitida') {
                        const fechaVenc = new Date(factura.fechaVencimiento);
                        const hoy = new Date();
                        hoy.setHours(0, 0, 0, 0);
                        if (fechaVenc < hoy) {
                            estadoTexto = 'VENCIDA';
                            estadoClase = 'vencida';
                        } else {
                            estadoTexto = 'PENDIENTE';
                            estadoClase = 'pendiente';
                        }
                    }
                    
                    if (boton) {
                        boton.textContent = '📄 Ver Factura';
                        boton.classList.add('btn-ver-factura');
                        boton.classList.remove('btn-generar-factura');
                        boton.setAttribute('onclick', `verFacturaGuardada(${factura.ordenId})`);
                    }
                    
                    if (badge) {
                        badge.textContent = estadoTexto;
                        badge.className = `estado-badge estado-${estadoClase}`;
                    } else if (boton) {
                        const nuevoBadge = document.createElement('span');
                        nuevoBadge.id = `estado-${factura.ordenId}`;
                        nuevoBadge.className = `estado-badge estado-${estadoClase}`;
                        nuevoBadge.textContent = estadoTexto;
                        boton.parentElement?.appendChild(nuevoBadge);
                    }
                });
            }
        })
        .catch(error => console.error('Error cargando facturas:', error));
}

// ==================== FUNCIÓN MODIFICADA CON BOTÓN DE NOTIFICAR CLIENTE ====================

function verFacturaGuardada(ordenId) {
    fetch(`/Finanzas/ObtenerFactura?ordenId=${ordenId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success && data.factura) {
                const f = data.factura;
                
                let estadoColor = '#059669';
                let estadoTexto = f.estado;
                let puedePagar = false;
                let puedeNotificar = false;
                
                const fechaVenc = new Date(f.fechaVencimiento);
                const hoy = new Date();
                hoy.setHours(0, 0, 0, 0);
                
                if (f.fechaPago) {
                    estadoTexto = 'CANCELADA';
                    estadoColor = '#059669';
                } else if (f.estado === 'Emitida' && fechaVenc < hoy) {
                    estadoTexto = 'VENCIDA';
                    estadoColor = '#dc2626';
                    puedePagar = true;
                    puedeNotificar = true;
                } else if (f.estado === 'Emitida') {
                    estadoTexto = 'PENDIENTE';
                    estadoColor = '#f59e0b';
                    puedePagar = true;
                } else if (f.estado === 'Anulada') {
                    estadoColor = '#6b7280';
                }
                
                const html = `
                    <div class="factura-section">
                        <h3>Cliente</h3>
                        <div class="factura-info">${f.cliente}</div>
                    </div>
                    <div class="factura-section">
                        <h3>Detalles</h3>
                        <div class="factura-info-row"><span>N° Factura:</span><span>${f.numeroFactura}</span></div>
                        <div class="factura-info-row"><span>Código:</span><span>${f.codigoOrden}</span></div>
                        <div class="factura-info-row"><span>Fecha Emisión:</span><span>${f.fechaEmision}</span></div>
                        <div class="factura-info-row"><span>Fecha Vencimiento:</span><span>${f.fechaVencimiento}</span></div>
                        <div class="factura-info-row"><span>Monto Base:</span><span>S/ ${parseFloat(f.montoBase).toFixed(2)}</span></div>
                        <div class="factura-info-row"><span>Total:</span><span>S/ ${parseFloat(f.total).toFixed(2)}</span></div>
                        <div class="factura-info-row"><span>Estado:</span><span style="color: ${estadoColor}; font-weight: bold;">${estadoTexto}</span></div>
                        ${f.fechaPago ? `<div class="factura-info-row"><span>Fecha de Pago:</span><span>${new Date(f.fechaPago).toLocaleDateString()}</span></div>` : ''}
                    </div>
                    ${puedePagar ? `
                    <div class="factura-section" id="seccionPago" style="border-top: 2px solid #e2e8f0; margin-top: 16px; padding-top: 16px;">
                        <h3>💰 Registrar Pago</h3>
                        <div style="display: flex; gap: 12px; align-items: flex-end; flex-wrap: wrap;">
                            <div style="flex: 1;">
                                <label>Fecha de Pago</label>
                                <input type="date" id="fechaPago" class="form-input" value="${new Date().toISOString().split('T')[0]}" />
                            </div>
                            <button class="btn-registrar-pago" onclick="registrarPago()" style="background: #059669; color: white; padding: 10px 24px; border: none; border-radius: 8px; cursor: pointer; font-weight: 600;">
                                💰 Registrar Pago
                            </button>
                        </div>
                    </div>
                    ` : ''}
                    ${puedeNotificar ? `
                    <div class="factura-section" id="seccionNotificacion" style="border-top: 2px solid #e2e8f0; margin-top: 16px; padding-top: 16px;">
                        <h3>⚠️ Alerta de Mora</h3>
                        <button class="btn-notificar-cliente" onclick="notificarClienteMora(${f.ordenId})" style="background: #dc2626; color: white; padding: 10px 24px; border: none; border-radius: 8px; cursor: pointer; font-weight: 600;">
                            📧 Notificar Cliente
                        </button>
                    </div>
                    ` : ''}
                `;
                
                document.getElementById('contenidoFactura').innerHTML = html;
                document.getElementById('modalVistaFactura').classList.add('activo');
                
                localStorage.setItem('facturaPendiente', JSON.stringify({
                    ordenId: f.ordenId,
                    codigo: f.codigoOrden,
                    cliente: f.cliente,
                    montoBase: f.montoBase,
                    total: f.total,
                    fechaVencimiento: f.fechaVencimiento,
                    fechaEmision: f.fechaEmision
                }));
            } else {
                showFlash('No se encontró la factura', false);
            }
        });
}

// ==================== FUNCIÓN PARA NOTIFICAR CLIENTE EN MORA (CORREGIDA) ====================

function notificarClienteMora(ordenId) {
    const facturaStr = localStorage.getItem('facturaPendiente');
    if (!facturaStr) {
        showFlash('No hay factura seleccionada', false);
        return;
    }
    
    const factura = JSON.parse(facturaStr);
    
    const btn = event.target;
    const originalText = btn.textContent;
    btn.textContent = '⏳ Enviando...';
    btn.disabled = true;
    
    // ✅ CORREGIDO: Ya no enviamos "correo", el backend lo obtiene solo
    fetch('/Finanzas/NotificarMora', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            ordenId: ordenId,
            cliente: factura.cliente,
            codigo: factura.codigo,
            total: factura.total,
            fechaVencimiento: factura.fechaVencimiento
        })
    })
    .then(response => response.json())
    .then(data => {
        btn.textContent = originalText;
        btn.disabled = false;
        
        if (data.success) {
            showFlash('✅ Correo de recordatorio enviado al cliente', true);
        } else {
            showFlash(data.message || 'Error al enviar el correo', false);
        }
    })
    .catch(error => {
        btn.textContent = originalText;
        btn.disabled = false;
        console.error('Error:', error);
        showFlash('Error al enviar el correo', false);
    });
}

function registrarPago() {
    const facturaStr = localStorage.getItem('facturaPendiente');
    if (!facturaStr) {
        showFlash('No hay factura seleccionada', false);
        return;
    }
    
    const factura = JSON.parse(facturaStr);
    const fechaPago = document.getElementById('fechaPago')?.value;
    
    if (!fechaPago) {
        showFlash('Seleccione una fecha de pago', false);
        return;
    }
    
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);
    const fechaPagoDate = new Date(fechaPago);
    
    if (fechaPagoDate < hoy) {
        showFlash('❌ La fecha de pago no puede ser menor a la fecha actual', false);
        return;
    }
    
    fetch('/Finanzas/RegistrarPago', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ facturaId: factura.ordenId, fechaPago: fechaPago })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showFlash(data.message, true);
            verFacturaGuardada(factura.ordenId);
        } else {
            showFlash(data.message, false);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showFlash('Error al registrar pago', false);
    });
}

// ==================== FUNCIÓN MODIFICADA CON VALIDACIÓN ACTIVADA ====================

function guardarYEmitirFactura() {
    const fechaVencimiento = document.getElementById('facturaFechaVencimiento').value;
    if (!fechaVencimiento) {
        showFlash('Por favor selecciona una fecha de vencimiento', false);
        return;
    }
    
    // ========== VALIDACIÓN ACTIVADA ==========
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);
    const fechaVenc = new Date(fechaVencimiento);
    
    if (fechaVenc < hoy) {
        showFlash('❌ La fecha de vencimiento no puede ser menor a la fecha actual', false);
        return;
    }
    // ========================================
    
    const conceptos = document.querySelectorAll('.sobrecosto-concepto');
    const montos = document.querySelectorAll('.sobrecosto-monto');
    const sobrecostos = [];
    
    for (let i = 0; i < conceptos.length; i++) {
        const concepto = conceptos[i].value.trim();
        const monto = parseFloat(montos[i].value) || 0;
        if (concepto && monto > 0) {
            sobrecostos.push({ concepto, monto });
        }
    }
    
    const totalSobrecostos = sobrecostos.reduce((sum, s) => sum + s.monto, 0);
    const totalFactura = facturaActual.montoBase + totalSobrecostos;
    
    const factura = {
        ordenId: facturaActual.ordenId,
        cliente: facturaActual.cliente,
        codigo: facturaActual.codigo,
        montoBase: facturaActual.montoBase,
        sobrecostos: sobrecostos,
        total: totalFactura,
        fechaVencimiento: fechaVencimiento,
        fechaEmision: new Date().toISOString().split('T')[0]
    };
    
    const btn = event.target;
    const originalText = btn.textContent;
    btn.textContent = '⏳ Emitiendo...';
    btn.disabled = true;
    
    fetch('/Finanzas/EmitirYEnviarFactura', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(factura)
    })
    .then(response => response.json())
    .then(data => {
        btn.textContent = originalText;
        btn.disabled = false;
        
        if (data.success) {
            showFlash(data.message, true);
            cerrarFormularioFactura();
            
            const boton = document.querySelector(`.btn-generar-factura[data-orden-id="${factura.ordenId}"]`);
            if (boton) {
                boton.textContent = '📄 Ver Factura';
                boton.classList.add('btn-ver-factura');
                boton.classList.remove('btn-generar-factura');
                boton.setAttribute('onclick', `verFacturaGuardada(${factura.ordenId})`);
            }
        } else {
            showFlash(data.message, false);
        }
    })
    .catch(error => {
        btn.textContent = originalText;
        btn.disabled = false;
        console.error('Error:', error);
        showFlash('Error al emitir la factura', false);
    });
}

function enviarFacturaCorreo() {
    const facturaStr = localStorage.getItem('facturaPendiente');
    if (!facturaStr) {
        showFlash('No hay factura para enviar', false);
        return;
    }
    
    const factura = JSON.parse(facturaStr);
    
    fetch('/Finanzas/EnviarFacturaPorCorreo', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(factura)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showFlash('Factura enviada al correo del cliente exitosamente', true);
            cerrarVistaFactura();
        } else {
            showFlash(data.message || 'Error al enviar la factura', false);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showFlash('Error al enviar la factura', false);
    });
}

document.addEventListener('DOMContentLoaded', cargarEstadosFacturas);