---
id: MSG-CUTTING-044
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-043-DONE
created: 2026-04-25
---

# CUTTING-044 — Phase 3 Day 3-4: Nesting integráció + Publish bővítés

> **Tervdok:** `docs/architecture/SpaceOS_Cutting_Phase3_Implementation_v1.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CUTTING-043 ✅ (Order Ingestion, 293 teszt)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Nap 3 — Nesting integráció a Publish flow-ba

A `PublishCuttingPlanCommandHandler` jelenleg stub nesting-et használ. Bővítsd valós `INestingStrategy`-vel:

### PanelSourceService

```csharp
// Application/Services/PanelSourceService.cs
// Összegyűjti az elérhető paneleket a nesting-hez:
// 1. IInventoryProvider.GetStockAsync(tenantId, material) → stock panels
// 2. IInventoryProvider.GetUsableOffcutsAsync(tenantId, material) → offcut panels
// Mindkét forrás → NestingInput.AvailablePanel lista
```

Ha `IInventoryProvider` nem tartalmazza ezeket a metódusokat, használj stub-ot és jelezd a DONE-ban.

### PublishCuttingPlanCommandHandler bővítés

```
1. DaySlot CuttingJob-jainak összegyűjtése → NestingInput parts (widthMm, heightMm, grainDirection)
2. PanelSourceService → elérhető panelek
3. INestingStrategy.ComputeAsync(parts, panels) → NestingResult
4. PlanNestingSnapshot mentés (meglévő entity — frissítsd a mezőit)
5. IInventoryProvider.ReserveAsync() — panel foglalás (ha elérhető)
```

### GrainDirection → CanRotate mapping

```csharp
// None → CanRotate = true
// Vertical/Horizontal → CanRotate = false
```

---

## Nap 4 — Tesztek + PlanNestingSnapshot frissítés

### PlanNestingSnapshot bővítés

Ha a `PlanNestingSnapshot` entity-ben hiányzik:
- `Placements` (JSONB — placement koordináták)
- `YieldPercent` (decimal)
- `WasteAreaMm2` (long)
- `Algorithm` (string)

EF migration ha szükséges.

### Tesztek (+10)

1. Publish happy path: CuttingJob-ok → nesting → PlanNestingSnapshot mentve
2. Nesting result: placements.Count == total parts
3. Yield > 0%
4. GrainDirection mapping: None → CanRotate=true
5. GrainDirection mapping: Vertical → CanRotate=false
6. PanelSourceService: stock + offcut kombináció (vagy stub teszt)
7. ReserveAsync hívás publish-kor
8. PlanNestingSnapshot tartalmazza az algorithm nevet
9. Üres DaySlot (0 job) → skip nesting
10. Publish idempotencia: már Published plan → error

## Definition of Done

- [ ] PanelSourceService (vagy stub jelzéssel)
- [ ] PublishCuttingPlanCommandHandler valós nesting-gel
- [ ] GrainDirection → CanRotate mapping
- [ ] PlanNestingSnapshot frissítve placement adatokkal
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 303 pass (293 + min 10 új)
- [ ] Outbox DONE
