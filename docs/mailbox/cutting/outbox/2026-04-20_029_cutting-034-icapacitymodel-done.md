---
id: MSG-CUTTING-029-DONE
from: cutting
to: root
type: done
status: READ
priority: high
ref: CUTTING-034
---

## Összefoglaló

CUTTING-034: `ICapacityModel` interfész + `AreaCapacityModel` implementáció elkészült.

**Új fájlok:**
- `src/SpaceOS.Modules.Cutting.Domain/Interfaces/ICapacityModel.cs` — interfész (ModelId, ComputeCapacityHours, ComputeJobCost, HasCapacity)
- `src/SpaceOS.Modules.Cutting.Domain/Services/AreaCapacityModel.cs` — area-v1 implementáció (area / throughput formula, EstimatedTimeHours fallback ha nincs geometria)
- `src/SpaceOS.Modules.Cutting.Infrastructure/Migrations/20260420_AddCuttingJobDimensions.cs` — WidthMm + HeightMm mezők migrációja
- `tests/.../Domain/AreaCapacityModelTests.cs` — 9 teszt (ModelId, ComputeJobCost fallback, area formula, HasCapacity változatok)

**Módosított fájlok:**
- `CuttingJob.cs` — `WidthMm`, `HeightMm` mezők (decimal, default 0), `Create()` opcionális paraméterekkel
- `DaySlot.AddJob()` — aláírás: `AddJob(CuttingJob job, ICapacityModel capacityModel)`, Status=Open ellenőrzés első helyen
- `CreateCuttingPlanCommandHandler.cs` — `ICapacityModel` injection, `slot.AddJob(job, _capacityModel)` hívás
- `ServiceCollectionExtensions.cs` — `AddSingleton<ICapacityModel, AreaCapacityModel>()`
- `CuttingJobConfiguration.cs` — WidthMm + HeightMm EF konfiguráció (precision 10,2)
- Összes stratégia teszt (FIFO, Priority, Custom, MaxCut, DaySlot) — `AddJob` + `AreaCapacityModel` paraméterrel frissítve
- `DaySlotTests.cs` — 2 új teszt: `AddJob_WhenLocked_ShouldReturnInvalid`, `AddJob_WhenClosed_ShouldReturnInvalid`

## Tesztek

**207/207 pass** (korábban 196 → most 207, +11 új teszt: 9 AreaCapacityModel + 2 DaySlot status)

Contracts tesztek: 10/10 pass.

## Security review

- Nincs új endpoint → authorization nem érintett
- `ICapacityModel` pure domain service, nincs külső függőség
- `WidthMm`/`HeightMm` nullable-helyett 0-default: RLS szempontból semleges (tenant-szintű adatok nem szivárognak)
- Immutability megőrzött: nincs public setter az új mezőkön

## Kockázatok / kérdések

Nincs blokkoló kockázat. A `AreaCapacityModel` Phase 3-ra készül elő — addig `EstimatedTimeHours` fallback aktív, ami a jelenlegi v1 működést nem változtatja meg.
