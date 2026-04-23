---
id: MSG-CUTTING-034-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-040
created: 2026-04-20
---

## Összefoglaló

CUTTING-038b implementálva: `IInventoryReservationAdapter` / `InventoryReservationHttpAdapter` törölt, `ReservePanelsCommandHandler` átállítva az `SpaceOS.Modules.Contracts v1.3.0` szerinti `IInventoryProvider.ReserveAsync()` / `ReleaseReservationAsync()` hívásokra.

**Törölt fájlok:**
- `src/SpaceOS.Modules.Cutting.Domain/Interfaces/IInventoryReservationAdapter.cs`
- `src/SpaceOS.Modules.Cutting.Infrastructure/Adapters/InventoryReservationHttpAdapter.cs`

**Módosított fájlok:**
- `src/SpaceOS.Modules.Cutting.Application/Commands/ReservePanels/ReservePanelsCommandHandler.cs` — `IInventoryProvider` (Contracts v1.3.0) injektálva, `ReserveAsync(ReserveStockRequest, ct)` + `ReleaseReservationAsync(correlationId, "rollback", ct)` hívások
- `src/SpaceOS.Modules.Cutting.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — `IInventoryReservationAdapter` regisztráció törölve, `ContractsInventoryHttpAdapter` regisztrálva
- `tests/.../Application/ReservePanelsCommandHandlerTests.cs` — mock átállítva `IInventoryReservationAdapter`-ről `SpaceOS.Modules.Contracts.Inventory.IInventoryProvider`-re

**Új fájlok:**
- `src/SpaceOS.Modules.Cutting.Infrastructure/Adapters/ContractsInventoryHttpAdapter.cs` — `SpaceOS.Modules.Contracts.Inventory.IInventoryProvider` HTTP adapter (csak `ReserveAsync` + `ReleaseReservationAsync` aktív, többi `NotSupportedException`)
- `NuGet.Config` — `LocalModuleContracts` source hozzáadva (`/opt/spaceos/spaceos-modules-contracts/artifacts`)

**Megjegyzés:** Az `SpaceOS.Modules.Inventory.Contracts.Providers.IInventoryProvider` (v1.1.0) és az `SpaceOS.Modules.Contracts.Inventory.IInventoryProvider` (v1.3.0) két különböző interfész — a névütközés miatt `using` alias alkalmazva a ServiceCollectionExtensions-ben.

## Tesztek

- 259/259 teszt zöld
- ReservePanelsCommandHandlerTests: 5 teszt, mind zöld, `IInventoryReservationAdapter` mock → `IInventoryProvider` mock

## Security review

- Authorization: minden endpoint `[Authorize(Policy = "ManufacturerOnly")]` — nincs változás
- RLS: nincs sémaváltozás — meglévő RLS érvényes
- Immutability: `PanelReservation` nem módosítható — nincs változás
- `ReserveAsync` / `ReleaseReservationAsync` HTTP hívás exception-safe, rollback garantált

## Kockázatok / kérdések

Nincs. A `ContractsInventoryHttpAdapter` a `SpaceOS.Modules.Inventory.Contracts.Providers.IInventoryProvider` alapú `InventoryProviderHttpAdapter`-rel párhuzamosan él — hosszabb távon érdemes az utóbbit is átmigrálni a Contracts interfészre, de ez külön feladat.
