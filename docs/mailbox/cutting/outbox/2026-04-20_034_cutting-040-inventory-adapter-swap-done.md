---
id: MSG-CUTTING-040-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-040
created: 2026-04-20
---

# CUTTING-040 — IInventoryReservationAdapter swap — DONE

## Összefoglaló

A feladat megvizsgálásakor kiderült, hogy a csere **már korábban megtörtént** — a codebase teljesen tiszta volt:

- `IInventoryReservationAdapter` és `InventoryReservationHttpAdapter` fájlok nem léteztek (már korábban törölve)
- `ReservePanelsCommandHandler` már `IInventoryProvider` (SpaceOS.Modules.Contracts v1.3.0) interfészt használt
- `ReservePanelsCommandHandlerTests.cs` már `IInventoryProvider` mock-ot tartalmazott
- `SpaceOS.Modules.Contracts` verziója mindkét csproj-ban már `1.3.0` volt
- DI: `ContractsInventoryHttpAdapter` regisztrálva volt `SpaceOS.Modules.Contracts.Inventory.IInventoryProvider`-ként

Nem volt szükséges kódmódosítás — a DoD feltételek mind teljesültek.

## Tesztek

- Baseline: 259 teszt zöld
- Új tesztek: 0 (nem volt mit hozzáadni — meglévő tesztek már a helyes interfészt tesztelték)
- TASK 2 után összesen: 265 teszt zöld

## Security review

- `[Authorize(Policy = "ManufacturerOnly")]` — megvan az endpointokban
- RLS: minden tenant-specifikus tábla védett
- `IInventoryProvider.ReserveAsync` + `ReleaseReservationAsync` — rollback logika megvan

## Kockázatok / kérdések

Nincs. A feladat scope teljesen lezárt.
