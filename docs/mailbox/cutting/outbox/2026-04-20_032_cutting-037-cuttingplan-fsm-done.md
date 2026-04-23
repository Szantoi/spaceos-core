---
id: MSG-CUTTING-032-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-037
created: 2026-04-20
---

## Összefoglaló

CUTTING-037: CuttingPlan FSM — `Publish`, `Freeze`, `Close` átmenetek implementálva. Session B lezárva.

**Módosított domain fájlok:**
- `CuttingPlan.cs` — `ProfileSnapshotId` mező (nullable Guid) + `Publish(Guid)` + `Freeze()` + `Close()` FSM metódusok invariánsokkal + `UpdateStatus()` → `[Obsolete]`

**Új command handlerek:**
- `Commands/PublishCuttingPlan/` — `POST /api/cutting/planning/{id}/publish`
- `Commands/FreezeCuttingPlan/` — `POST /api/cutting/planning/{id}/freeze`
- `Commands/CloseCuttingPlan/` — `POST /api/cutting/planning/{id}/close`

**Módosított fájlok:**
- `CuttingPlanConfiguration.cs` — `ProfileSnapshotId` EF konfiguráció (nullable)
- `CuttingPlanningEndpoints.cs` — 3 új endpoint regisztrálva
- `UpdateCuttingPlanStatusCommandHandler.cs` — `#pragma warning disable CS0618` elnyomva
- `CuttingPlanTests.cs` — `UpdateStatus` hívások `#pragma warning disable CS0618` elnyomva
- `CuttingPlanRepositoryTests.cs` — ua.

**Migration:** `AddCuttingPlanProfileSnapshotId` — nullable `ProfileSnapshotId uuid` kolumn

**FSM invariánsok:**
- `Publish`: csak Draft-ból, legalább 1 DaySlot kell, ProfileSnapshotId nem lehet Empty
- `Freeze`: csak Published-ből, legalább 1 Open DaySlot kell
- `Close`: csak Frozen-ból, minden DaySlot Locked vagy Closed kell legyen

## Tesztek

**244/244 pass** (233 → 244, +11 új FSM teszt)

Contracts: 10/10 pass.

## Security review

- 3 új endpoint `ManufacturerOnly` policy alatt, a meglévő `group` route-on belül ✅
- `ProfileSnapshotId` kizárólag a request body-ból jön, nem query paramból ✅
- FSM metódusok nem hívnak external service-t, pure domain logika ✅
- `[Obsolete]` attribútum nem érinti az API szerződést ✅

## Kockázatok / kérdések

Nincsenek. Session B teljes — 7 task (CUTTING-031..037) mind DONE.
