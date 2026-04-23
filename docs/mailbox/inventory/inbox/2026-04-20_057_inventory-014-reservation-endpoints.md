---
id: MSG-INVENTORY-057
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-ARCH-003-RESPONSE
created: 2026-04-20
---

# INVENTORY-014 — Reservation HTTP Endpoints

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Timeline:** ~0.5 nap
> **Kontextus:** Az Architect (ARCH-003) megerősítette — a domain logika és migration már kész, csak az API réteg bekötése hiányzik.

---

## Kontextus

A `Reservation` aggregate, a `ReserveStockCommandHandler`, `ReleaseReservationCommandHandler`, és a `20260418000002_AddReservations` migration **már implementálva és a DB-ben van**. A Cutting modul (`ReservePanelsCommandHandler`) szükséges ezekre a végpontokra a panel foglaláshoz.

Az `IInventoryProvider` v1.2.0 (Contracts NuGet) már tartalmazza:
```csharp
Task<Result<ReservationDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct);
Task<Result> ReleaseReservationAsync(Guid correlationId, string? reason, CancellationToken ct);
Task<Result<IReadOnlyList<ReservationDto>>> GetReservationsAsync(ReservationFilter filter, CancellationToken ct);
```

---

## Feladat

**Fájl:** `Api/Endpoints/InventoryEndpoints.cs` (vagy ahol a végpontok regisztrálva vannak)

Kösd be a már létező handler-eket:

```csharp
// POST /api/inventory/reservations
group.MapPost("/reservations", async (
    ReserveStockRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new ReserveStockCommand(request), ct);
    return result.IsSuccess ? Results.Created($"/api/inventory/reservations/{result.Value.Id}", result.Value)
                            : Results.BadRequest(result.Error);
}).RequireAuthorization("ManufacturerOnly");

// DELETE /api/inventory/reservations/{correlationId}
group.MapDelete("/reservations/{correlationId:guid}", async (
    Guid correlationId,
    string? reason,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new ReleaseReservationCommand(correlationId, reason), ct);
    return result.IsSuccess ? Results.Ok() : Results.NotFound(result.Error);
}).RequireAuthorization("ManufacturerOnly");

// GET /api/inventory/reservations
group.MapGet("/reservations", async (
    [AsParameters] ReservationFilter filter,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new GetReservationsQuery(filter), ct);
    return Results.Ok(result.Value);
}).RequireAuthorization("ManufacturerOnly");
```

Ha a command/query nevek vagy paraméterezés eltér a tényleges implementációtól, igazíts — a lényeg: a 3 endpoint legyen bekötve a meglévő handler-ekhez.

---

## Tesztek

Min. 6 új teszt az `InventoryReservationEndpointTests.cs` (ÚJ) fájlban:
- `POST /api/inventory/reservations` → 201 Created
- `POST /api/inventory/reservations` duplikált CorrelationId → idempotens (200 vagy 201)
- `DELETE /api/inventory/reservations/{id}` → 200 OK
- `DELETE /api/inventory/reservations/{not-found}` → 404
- `GET /api/inventory/reservations` → 200 listával
- `POST /api/inventory/reservations` auth nélkül → 401

Meglévő tesztek mind zölden.

---

## Definition of Done

- [ ] `POST /api/inventory/reservations` endpoint bekötve → `ReserveStockCommandHandler`
- [ ] `DELETE /api/inventory/reservations/{correlationId}` → `ReleaseReservationCommandHandler`
- [ ] `GET /api/inventory/reservations` → `GetReservationsQueryHandler`
- [ ] Min. 6 új teszt
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld
- [ ] Outbox DONE üzenet küldve

---

## Megjegyzés

Ez a task blokkolja a **CUTTING-038b**-t (Cutting modul adapter csere). Amint ez DONE, a Cutting terminál lecseréli a belső `IInventoryReservationAdapter`-t az `IInventoryProvider.ReserveAsync()`-ra.
