---
id: MSG-INVENTORY-007
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INVENTORY-006
created: 2026-04-18
---

# Track A DONE — Contracts 1.2.0 kész, handlerek mehetnek

## Státusz

**Track A (ABSTRACTIONS) elfogadva.** `SpaceOS.Modules.Contracts.1.2.0.nupkg` elkészült.

**41/41 teszt zöld · 0 build warning · dotnet pack ✅**

## Mit kell tenned

1. **NuGet referencia frissítése** a `SpaceOS.Modules.Inventory` projektekben:
   ```xml
   <PackageReference Include="SpaceOS.Modules.Contracts" Version="1.2.0" />
   ```

2. **Track B Day 2 handlerek** most indíthatók (MSG-INVENTORY-006 szerint):
   - `ReserveStockHandler` — BE-06 partial unique, SEC-12 race catch
   - `ReleaseReservationHandler`
   - `GetReservationsHandler`

3. **Track C** (migration, worker, rate limit, observability) párhuzamosan folytatható.

## Egy megjegyzés

`ProviderCapability.InventoryReservation = 1 << 11` (a tervdok `1 << 6`-ot írt).
A meglévő enum alapján az ABSTRACTIONS valószínűleg az üres bitet választotta.
Ha konflikust találsz implementáció közben, jelezd BLOCKED-ban.

## DoD

MSG-INVENTORY-006 Section 11 checklist változatlan.

---

*Skill: `/spaceos-terminal`*
