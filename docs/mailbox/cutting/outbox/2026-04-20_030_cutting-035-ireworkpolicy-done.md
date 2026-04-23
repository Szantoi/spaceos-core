---
id: MSG-CUTTING-030-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-035
created: 2026-04-20
---

## Összefoglaló

CUTTING-035: `IReworkPolicy` interfész + `WarnAndApplyPolicy` implementáció elkészült.

**Új fájlok:**
- `src/SpaceOS.Modules.Cutting.Domain/ValueObjects/ReworkDecision.cs` — sealed record (CanReschedule, TargetSlot?, Reason)
- `src/SpaceOS.Modules.Cutting.Domain/Interfaces/IReworkPolicy.cs` — interfész (PolicyId, Evaluate, Apply)
- `src/SpaceOS.Modules.Cutting.Domain/Services/WarnAndApplyPolicy.cs` — implementáció, `ICapacityModel` injektálva
- `tests/.../Domain/WarnAndApplyPolicyTests.cs` — 10 teszt

**Módosított fájlok:**
- `CuttingJob.cs` — `RescheduleTo(Guid)` + `MarkAsWarning()` metódusok
- `ServiceCollectionExtensions.cs` — `AddSingleton<IReworkPolicy, WarnAndApplyPolicy>()`

**Logika:**
- `Evaluate`: ha slot nem Open vagy nincs kapacitás → `CanReschedule = true`; ha Open és van kapacitás → `CanReschedule = false, TargetSlot = slot`
- `Apply`: megkeresi az első Open + elegendő kapacitású slotot → `job.RescheduleTo()`; ha nincs → `job.MarkAsWarning()`

## Tesztek

**218/218 pass** (207 → 218, +11 új: 10 WarnAndApplyPolicy + 1 RescheduleTo/MarkAsWarning)

Contracts tesztek: 10/10 pass.

## Security review

- Nincs új endpoint → authorization nem érintett
- `IReworkPolicy` pure domain service, nincs külső függőség, nincs user input
- `WarnAndApplyPolicy` nem módosít DB-t közvetlenül, csak domain state-et
- Immutability megőrzött: nincs public setter

## Kockázatok / kérdések

Nincsenek. `WarnAndApplyPolicy` v1-ben nem kerül meghívásra automatikusan (nincs bekötve handler-be) — ez a következő task feladata.
