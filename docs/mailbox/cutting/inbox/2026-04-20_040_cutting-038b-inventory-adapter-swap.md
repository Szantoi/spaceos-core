---
id: MSG-CUTTING-040
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-INVENTORY-058
created: 2026-04-20
---

# CUTTING-038b — IInventoryReservationAdapter csere IInventoryProvider-re

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Timeline:** ~0.5 nap
> **Blokkoló feloldva:** INVENTORY-014 ✅ DONE — `/api/inventory/reservations` endpoint él (commit 6abc087)

---

## Kontextus

A CUTTING-038-ban létrehozott `IInventoryReservationAdapter` / `InventoryReservationHttpAdapter` párhuzamos absztrakció volt, mert a terminál v1.1.0 Contracts NuGet ellen dolgozott. Az `IInventoryProvider.ReserveAsync()` valójában v1.2.0-tól létezik. Most, hogy az Inventory HTTP endpoint is él, a belső adapter törlendő és a shared contract-ot kell használni.

---

## Feladatok

### 1. Törlendő fájlok

```
Domain/Interfaces/IInventoryReservationAdapter.cs     ← törlés
Infrastructure/Adapters/InventoryReservationHttpAdapter.cs  ← törlés
```

### 2. ReservePanelsCommandHandler frissítése

**Fájl:** `Commands/ReservePanels/ReservePanelsCommandHandler.cs`

- Inject: `IInventoryProvider` (Contracts NuGet) helyett `IInventoryReservationAdapter`
- `ReserveAsync()` hívás → `IInventoryProvider.ReserveAsync(ReserveStockRequest)` szerint
- `ReleaseAsync()` hívás → `IInventoryProvider.ReleaseReservationAsync(correlationId, reason)`

**Mapping:**
```csharp
var request = new ReserveStockRequest(
    CorrelationId: panelReservation.Id,    // Cutting PanelReservation.Id = idempotency key
    ConsumerModule: "Cutting",
    ConsumerContextJson: null,
    Items: panels.Select(p => new ReserveItemRequest(
        StockItemId: p.PanelStockId,
        MaterialCode: p.MaterialCode,
        QuantityReserved: p.AreaMm2
    )).ToList(),
    Ttl: TimeSpan.FromHours(24)
);
```

Ha a `ReserveItemRequest` konstruktor paraméterei eltérnek a tényleges v1.3.0 NuGet-től, igazíts a valódi interfészhez — ne feltételezz, olvasd el a NuGet package-t.

### 3. DI regisztráció frissítése

**Fájl:** `ServiceCollectionExtensions.cs`

- `IInventoryReservationAdapter` regisztráció törlése
- `IInventoryProvider` már regisztrálva van (ellenőrizd, ha nincs → add hozzá az Inventory HTTP adapter-rel)

### 4. NuGet verzió ellenőrzés

```xml
<!-- SpaceOS.Modules.Cutting.csproj -->
<PackageReference Include="SpaceOS.Modules.Contracts" Version="1.3.0" />
```

Ha v1.1.0 vagy v1.2.0 → frissítsd v1.3.0-ra (`dotnet add package`).

---

## Tesztek

- `ReservePanelsCommandHandlerTests.cs` frissítése: `IInventoryReservationAdapter` mock → `IInventoryProvider` mock
- Meglévő 259 teszt mind zölden (ha a tesztek `IInventoryReservationAdapter`-t mockoltak, azok frissítendők)

---

## Definition of Done

- [ ] `IInventoryReservationAdapter` + `InventoryReservationHttpAdapter` törölve
- [ ] `ReservePanelsCommandHandler` → `IInventoryProvider.ReserveAsync()` + `ReleaseReservationAsync()`
- [ ] DI regisztráció tiszta
- [ ] NuGet v1.3.0
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (259+)
- [ ] Outbox DONE üzenet küldve
