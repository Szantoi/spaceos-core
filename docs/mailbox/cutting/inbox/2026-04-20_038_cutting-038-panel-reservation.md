---
id: MSG-CUTTING-038
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-037
created: 2026-04-20
---

# CUTTING-038 — PanelReservation aggregate (Inventory referencia)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (Session C, CUTTING-036)
> **Timeline:** ~1.5 nap
> **Session C első task** — blokkoló: CUTTING-037 ✅ DONE

---

## Kontextus

A `CuttingPlan` Freeze-elése előtt a szükséges paneleket le kell foglalni az Inventory-ban. A `PanelReservation` aggregate nem duplikálja az Inventory adatait — csak az `InventoryReservationId` (UUID referencia) tárolódik a Cutting oldalon.

**OQ-4 döntés (jóváhagyva):** Szinkron HTTP call v1-ben (`IInventoryProvider.ReserveAsync()`).

---

## Feladatok

### 1. PanelReservation aggregate

**Fájl:** `Domain/Aggregates/PanelReservation.cs`

```csharp
public sealed class PanelReservation : AggregateRoot<Guid>
{
    public Guid CuttingPlanId { get; private set; }
    public Guid DaySlotId { get; private set; }
    public Guid InventoryReservationId { get; private set; }  // Inventory SSoT
    public string MaterialCode { get; private set; }
    public decimal WidthMm { get; private set; }
    public decimal HeightMm { get; private set; }
    public PanelReservationStatus Status { get; private set; }  // Pending/Confirmed/Released

    public static PanelReservation Create(Guid planId, Guid slotId,
        Guid inventoryReservationId, string materialCode,
        decimal widthMm, decimal heightMm);

    public void Confirm();   // Pending → Confirmed
    public void Release();   // Confirmed/Pending → Released
}
```

### 2. PanelReservationStatus enum

**Fájl:** `Domain/Enums/PanelReservationStatus.cs`

```csharp
public enum PanelReservationStatus
{
    Pending = 0,
    Confirmed = 1,
    Released = 2
}
```

### 3. ReservePanelsCommandHandler

A CuttingPlan Freeze-elésekor hívódik. Minden DaySlot job-jaihoz szükséges paneleket foglalja le az Inventory-ban HTTP-n keresztül.

```csharp
// IInventoryProvider.ReserveAsync() — már létező interfész
// Létrehoz PanelReservation entitást minden sikeres foglaláshoz
// Ha bármelyik sikertelen → rollback (Release az eddig sikeresekre) + Result.Failure
```

**Endpoint:** `POST /api/cutting/planning/{id}/reserve-panels`
`RequireAuthorization("ManufacturerOnly")`

### 4. EF Core konfiguráció + migration

```bash
dotnet ef migrations add AddPanelReservation \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

Tábla: `PanelReservations` — RLS: `CuttingPlanId` → `CuttingPlans.TenantId` via JOIN (vagy saját `TenantId` oszlop — saját oszlop javasolt, egyszerűbb RLS policy).

### 5. IPanelReservationRepository

```csharp
public interface IPanelReservationRepository
{
    Task<IReadOnlyList<PanelReservation>> GetByPlanAsync(Guid planId, CancellationToken ct);
    Task AddAsync(PanelReservation reservation, CancellationToken ct);
    Task UpdateAsync(PanelReservation reservation, CancellationToken ct);
}
```

---

## Tesztek

- `PanelReservationTests.cs` (ÚJ): Create, Confirm, Release FSM — min. 6 teszt
- `ReservePanelsCommandHandlerTests.cs` (ÚJ): sikeres foglalás, részleges hiba → rollback — min. 5 teszt (IInventoryProvider mock-kal)
- Meglévő 244 teszt mind zölden

---

## Definition of Done

- [ ] `PanelReservation` aggregate + `PanelReservationStatus` enum létrehozva
- [ ] `ReservePanelsCommandHandler` + endpoint
- [ ] `IPanelReservationRepository` + implementáció
- [ ] Migration (`dotnet ef migrations add`)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (244+)
- [ ] Outbox DONE üzenet küldve
