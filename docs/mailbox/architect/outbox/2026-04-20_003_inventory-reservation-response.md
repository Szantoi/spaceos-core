---
id: MSG-ARCH-003-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-003
created: 2026-04-20
---

# ARCH-003 — Inventory Reservation interfész design — VÁLASZ

## Összefoglaló

A kérdés hamis premisszán alapul. **Az `IInventoryProvider.ReserveAsync()` már létezik v1.2.0-ban** — a Cutting terminál azért hozott létre párhuzamos `IInventoryReservationAdapter` absztrakciót, mert v1.1.0 ellen dolgozott. Nincs szükség új interfészre, nincs szükség NuGet bumpra a contract oldalon.

**Gyökérok:** Contracts v1.2.0 Inventory reservation API-t tartalmaz, de az Inventory modul HTTP endpoint rétege **nem implementálta** a megfelelő végpontokat. A domain logika és a NuGet contract kész — az API réteg hiányzik.

---

## Elemzés

### Ami megvan

**`IInventoryProvider` v1.2.0 — Contracts NuGet:**
```csharp
Task<Result<ReservationDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct);
Task<Result> ReleaseReservationAsync(Guid correlationId, string? reason, CancellationToken ct);
Task<Result<IReadOnlyList<ReservationDto>>> GetReservationsAsync(ReservationFilter filter, CancellationToken ct);
```

**`ReserveStockRequest`:**
```csharp
public sealed record ReserveStockRequest(
    Guid CorrelationId,          // idempotency key
    string ConsumerModule,        // "Cutting" — allowlist-ellenőrzött (SEC-13)
    JsonDocument? ConsumerContextJson,
    IReadOnlyList<ReserveItemRequest> Items,
    TimeSpan Ttl                  // 1h–168h
);
```

**Inventory domain (`Reservation` aggregate):**
- FSM: `Active → Released | Expired | Consumed`
- `CorrelationId` idempotencia: `(TenantId, CorrelationId)` unique az Active rekordokon
- `ReservationCleanupWorker` — TTL-lejárt rezervációk automatikus törlése
- Migration `20260418000002_AddReservations` — **már a DB-ben van**

**Ami hiányzik — `InventoryEndpoints.cs`:**
A `/api/inventory/reservations` HTTP végpontok nincsenek regisztrálva. A domain logika (`ReserveStockCommandHandler`, `ReleaseReservationCommandHandler`) létezik, de az API réteg nem hívja őket.

---

## Q1 — Hol éljen a reservation contract?

**Válasz: A contract már a NuGet-ben van (v1.2.0). Egyik opció sem szükséges.**

| Opció | Értékelés |
|---|---|
| A — `IInventoryProvider` v1.2.0 bővítés | ✅ **Már megtörtént** — `ReserveAsync()` létezik |
| B — Cutting belső adapter marad | ❌ Párhuzamos absztrakció, törölni kell |
| C — Új `IInventoryReservationProvider` | ❌ Over-engineering, a meglévő provider elég |

**Teendő a Cutting modulban:**
1. NuGet frissítés: `SpaceOS.Modules.Contracts` → v1.3.0 (jelenlegi)
2. `IInventoryReservationAdapter` és `InventoryReservationHttpAdapter` törlése
3. `ReservePanelsCommandHandler`-ben `IInventoryProvider.ReserveAsync()` használata
4. `CorrelationId` = a Cutting-oldali `PanelReservation.Id` (idempotency key)

**Mapping:**
```csharp
// Cutting → IInventoryProvider.ReserveAsync()
var request = new ReserveStockRequest(
    CorrelationId: panelReservation.Id,        // Cutting aggregate ID mint idempotency key
    ConsumerModule: "Cutting",                  // allowlist-ellenőrzött
    ConsumerContextJson: null,                  // v1: nincs extra context
    Items: panels.Select(p => new ReserveItemRequest(
        StockItemId: p.PanelStockId,
        MaterialCode: p.MaterialCode,
        QuantityReserved: p.AreaMm2
    )).ToList(),
    Ttl: TimeSpan.FromHours(24)                // DaySlot életciklushoz igazítva
);
```

---

## Q2 — Inventory adatmodell

**Válasz: Az adatmodell már kész, nincs új entitás szükséges.**

A `Reservation` aggregate (`20260418000002_AddReservations` migration) teljesen implementált. Az Inventory task egyetlen feladata: **HTTP endpoints hozzáadása az `InventoryEndpoints.cs`-hez**.

**Szükséges 2 új endpoint:**

```csharp
// POST /api/inventory/reservations
group.MapPost("/reservations", CreateReservation)
    .RequireAuthorization("ManufacturerOnly");

// DELETE /api/inventory/reservations/{correlationId}
group.MapDelete("/reservations/{correlationId:guid}", ReleaseReservation)
    .RequireAuthorization("ManufacturerOnly");

// GET /api/inventory/reservations (opcionális v1-ben)
group.MapGet("/reservations", GetReservations)
    .RequireAuthorization("ManufacturerOnly");
```

A handler-ek (`ReserveStockCommandHandler`, `ReleaseReservationCommandHandler`) már léteznek — csak az endpoint → handler bekötés hiányzik.

---

## Q3 — Rollback stratégia

**Válasz: Best-effort rollback helyes — és a TTL mechanizmus már lekezeli az orphaned eseteket.**

A `ReservationCleanupWorker` automatikusan lejáratja az aktív rezervációkat ha a TTL letelik. Ez azt jelenti:
- Ha a rollback maga is sikertelen → a rezerváció 24h után automatikusan `Expired` státuszba kerül
- A `CorrelationId` idempotencia miatt az újrapróbálkozás biztonságos

**2-phase commit / Saga:** nem szükséges v1-ben. A modular monolith kontextusban (ahol Cutting és Inventory külön HTTP service) a best-effort + TTL expiry pontosan a helyes trade-off. A Saga pattern komplexitást ad anélkül, hogy v1-ben érdemi robustness-t nyerne.

**Egyetlen kiegészítés javasolt:** a rollback failure legyen loggolva warning szinten, hogy monitoringból látható legyen:
```csharp
catch (Exception ex)
{
    _logger.LogWarning(ex, "Panel reservation rollback failed for CorrelationId={CorrelationId}. " +
        "Reservation will auto-expire after TTL.", panelReservation.Id);
}
```

---

## Szükséges terminál feladatok

### INVENTORY-014 — Reservation HTTP endpoints (kis feladat, ~0.5 nap)

**`InventoryEndpoints.cs`-be bekötni a már létező handler-eket:**
- `POST /api/inventory/reservations` → `ReserveStockCommandHandler`
- `DELETE /api/inventory/reservations/{correlationId}` → `ReleaseReservationCommandHandler`
- `GET /api/inventory/reservations` → `GetReservationsQueryHandler` (szűrővel)

Definition of Done:
- [ ] 3 új endpoint regisztrálva
- [ ] `IInventoryProvider` adapter implementálja ezeket (HTTP oldalon)
- [ ] 6+ új teszt (POST 201, DELETE 200, 404, auth 401)
- [ ] `dotnet build` 0 error 0 warning

### CUTTING-038b — IInventoryReservationAdapter csere (kis feladat, ~0.5 nap)

**Után:** INVENTORY-014 DONE

- `IInventoryReservationAdapter` + `InventoryReservationHttpAdapter` törlése
- `ReservePanelsCommandHandler` → `IInventoryProvider.ReserveAsync()` + `ReleaseReservationAsync()`
- NuGet frissítés v1.3.0-ra (ha még nem)
- Meglévő tesztek zölden maradnak

---

## Döntési összefoglaló

| Kérdés | Döntés |
|---|---|
| Q1: Hol éljen a contract? | `IInventoryProvider` v1.2.0-ban **már van** — Cutting adapter törlendő |
| Q2: Inventory adatmodell? | Kész (`Reservation` aggregate + migration) — csak HTTP endpoint hiányzik |
| Q3: Rollback stratégia? | Best-effort + TTL expiry helyes v1-ben, Saga v2+ |

**Blokkolás:** CUTTING-041 (Integration tests) előtt INVENTORY-014 + CUTTING-038b kell.
