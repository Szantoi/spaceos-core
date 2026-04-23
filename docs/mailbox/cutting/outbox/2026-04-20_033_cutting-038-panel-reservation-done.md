---
id: MSG-CUTTING-033-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-038
created: 2026-04-20
---

## Összefoglaló

CUTTING-038: `PanelReservation` aggregate + Inventory reservation pipeline elkészült.

**Új domain fájlok:**
- `Domain/Enums/PanelReservationStatus.cs` — Pending/Confirmed/Released
- `Domain/Aggregates/PanelReservation.cs` — TenantId (saját oszlop, RLS-hez), InventoryReservationId, Confirm()/Release() FSM
- `Domain/Interfaces/IPanelReservationRepository.cs`
- `Domain/Interfaces/IInventoryReservationAdapter.cs` — saját interfész (IInventoryProvider v1.1.0-ban nincs ReserveAsync)

**Megjegyzés:** Az `IInventoryProvider` NuGet csomag (v1.1.0) nem tartalmaz `ReserveAsync()` metódust. Saját `IInventoryReservationAdapter` interfész és HTTP adapter implementálva — ez a Cutting modul belső absztrakciója.

**Új infrastructure fájlok:**
- `Infrastructure/Configurations/PanelReservationConfiguration.cs`
- `Infrastructure/Repositories/PanelReservationRepository.cs`
- `Infrastructure/Adapters/InventoryReservationHttpAdapter.cs` — HTTP POST /api/inventory/reservations + DELETE rollback
- `Migrations/AddPanelReservation` — `PanelReservations` tábla TenantId oszloppal

**Új application fájl:**
- `Commands/ReservePanels/ReservePanelsCommandHandler.cs` — iterálja a DaySlot job-okat, Inventory-t hív, részleges hiba esetén rollback (Release)

**Módosított fájlok:**
- `CuttingDbContext.cs` — `PanelReservations` DbSet + konfiguráció
- `ServiceCollectionExtensions.cs` — `IPanelReservationRepository` + `IInventoryReservationAdapter` DI
- `CuttingPlanningEndpoints.cs` — `POST /api/cutting/planning/{id}/reserve-panels`

## Tesztek

**259/259 pass** (244 → 259, +15 új: 10 PanelReservationTests + 5 ReservePanelsCommandHandlerTests)

Contracts: 10/10 pass.

## Security review

- Endpoint `ManufacturerOnly` policy alatt ✅
- `TenantId` JWT claim-ből, nem body-ból ✅
- `PanelReservations` táblán saját `TenantId` oszlop — RLS policy alkalmazható ✅
- `InventoryReservationId` csak referencia (UUID), nem duplikál Inventory adatot ✅
- Rollback best-effort: hiba naplózandó production-ban, de nem okoz cascade hibát ✅

## Kockázatok / kérdések

1. **IInventoryProvider.ReserveAsync hiánya:** Az arch spec "már létező" metódusra hivatkozott, de a v1.1.0 csomagban nem szerepel. Saját `IInventoryReservationAdapter`-t hoztam létre — ha az Inventory modul kiadja a NuGet update-et, a két interfész összefésülhető.
2. **Inventory endpoint nem létezik még:** A `/api/inventory/reservations` endpoint az Inventory modulban még nem implementált — az adapter hívása éles környezetben 404-et ad vissza. Ez Session C következő task-ja (CUTTING-039 vagy Inventory task).
