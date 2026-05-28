# Historia de Usuario: Registro de combustible (Conductor)

**Persona:** Conductor

**Como**: Conductor,

**Quiero**: registrar los galones y el monto de combustible que abastecí,

**Para**: mantener mi control exacto de los gastos de mi unidad.

---

## Criterios de Aceptación

1. DADO QUE me ubico en mi sección de combustible, CUANDO digito los galones y mi monto total, ENTONCES observo que se activa mi botón de registro.

2. DADO QUE llené mis datos, CUANDO presiono "Registrar", ENTONCES recibo un mensaje de confirmación de mi consumo guardado.

3. DADO QUE mi consumo se guardó, CUANDO el sistema procesa mi información, ENTONCES visualizo mi monto sumado en el historial de mi viaje.

4. Los campos solo deben admitirme valores numéricos positivos.

5. El sistema calculará mi rendimiento (km/galón) cuando cierre mi viaje.

---

## Notas de implementación (sugeridas para este proyecto)

- Archivo propuesto: `UserStories/US-Conductor-RegistroCombustible.md` (este documento).
- Modelo sugerido: `ConsumoCombustible` en `Models/Operaciones/` con campos:
  - `int Id`
  - `int? OrdenId` o `int? UnidadId` (relación con la orden o unidad según el flujo existente)
  - `decimal Galones` (positivo)
  - `decimal Monto` (positivo)
  - `DateTime Fecha` (registro)
  - `int? Kilometros` (opcional, para cálculo de rendimiento)
  - `decimal? Rendimiento` (km/galón, calculado al cerrar viaje)

- Validaciones: usar atributos de datos y validación en el cliente (JS) y servidor:

```csharp
public class ConsumoCombustible
{
    public int Id { get; set; }
    public int? OrdenId { get; set; }
    [Range(0.0001, double.MaxValue, ErrorMessage = "Ingrese galones positivos")]
    public decimal Galones { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Ingrese un monto positivo")]
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? Kilometros { get; set; }
    public decimal? Rendimiento { get; set; }
}
```

- Controlador sugerido: `Controllers/Operaciones/CombustibleController.cs` con acciones `Create` (GET/POST) y listado en el historial de viaje.

- Vista: formulario responsivo en `Views/Operaciones/Combustible/Create.cshtml` que habilite el botón `Registrar` solo cuando los campos tengan valores numéricos positivos.

- Mensajes: usar `TempData` o `ViewBag` para mostrar confirmación tras el `POST` exitoso.

- Migración: agregar la entidad a `ApplicationDbContext` y crear una migración EF Core.

- Suma en historial: al guardar un `ConsumoCombustible`, actualizar el acumulado en la vista de historial del viaje (p. ej. sumar `Monto` por `OrdenId`).

---

## Tareas técnicas (para seguimiento)

1. Añadir modelo `ConsumoCombustible` en `Models/Operaciones/`.
2. Actualizar `Data/ApplicationDbContext.cs` y crear migración.
3. Crear `CombustibleController` y vistas `Create` + `Index` (historial por orden/unidad).
4. Implementar validaciones cliente/servidor y mensajes de confirmación.
5. Calcular y almacenar `Rendimiento` al cerrar el viaje (servicio o método en `Orden`).
6. Integrar la suma de montos en la vista de historial del viaje existente.

---

Si quieres, procedo a crear el modelo y la migración inicial en el proyecto y agregar las vistas básicas.
