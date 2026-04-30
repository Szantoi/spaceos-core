---
id: MSG-ARCH-007
from: root
to: architect
type: task
priority: high
status: READ
created: 2026-04-24
---

# ARCH-007 — Cutting Planning Phase 3 Implementation Spec

> Cutting Planning Phase 1+2 DEPLOYED (284 teszt). Phase 3: valós rendelés ingestion + geometry bin-packing.
> **Meglévő tervdok:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`
> **Output:** `docs/architecture/SpaceOS_Cutting_Phase3_Implementation_v1.md`

## Kontextus

**Phase 1 (DONE):** Data Model + API (DaySlot, CuttingJob, CuttingPlan) — 136 teszt
**Phase 2 (DONE):** Strategy Pattern + yield calculation — 181 teszt (284 összesen)
**Phase 3 (PLANNED):** Valós rendelés ingestion + geometry bin-packing

A Growth Strategy v1 szerint a Cutting Phase 3 a Doorstar production feature-teljesség kulcsa.

## Amit a spec-nek tartalmaznia KELL

1. **Rendelés ingestion flow** — hogyan jön a DoorOrder a Joinery-ből a Cutting modulba?
   - Event-based (CuttingPlanFrozen → DoorOrder items) vagy API-based?
   - Milyen adatok kellenek: item dimensions, quantity, material, grain direction
   
2. **Geometry bin-packing** — a Nesting.Algorithms NuGet (v1.1.0, FFDH + Guillotine) hogyan integrálódik?
   - Input mapping: CuttingJob items → NestingInput
   - Output mapping: NestingResult → CuttingPlan placements
   
3. **DaySlot → CuttingPlan** lifecycle — hogyan lesz a DaySlot-ból végleges CuttingPlan?

4. **Inventory integráció** — a PanelReservation (Session C) hogyan kapcsolódik?

5. **Fázisolás** — mi Phase 3 MVP, mi Phase 3.5?

6. **Effort becslés** napokban

7. **Végrehajtási sorrend** (Cutting → INFRA deploy → TESTER)

## Referenciák

- Cutting Planning v4: `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md`
- Cutting context: `docs/knowledge/context/CUTTING_CONTEXT.md`
- Nesting Algorithms: `spaceos-nesting-algorithms/` (v1.1.0, FFDH + Guillotine)
- Growth Strategy: `docs/tasks/new/SpaceOS_Growth_Strategy_v1.md` §3

## Definition of Done

- [ ] `docs/architecture/SpaceOS_Cutting_Phase3_Implementation_v1.md` létrehozva
- [ ] Fenti 7 pont lefedve
- [ ] Outbox response küldve
