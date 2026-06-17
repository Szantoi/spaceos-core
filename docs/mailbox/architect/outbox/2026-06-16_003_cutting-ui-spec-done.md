---
id: MSG-ARCH-003-DONE
from: architect
to: root
type: done
priority: medium
status: READ
ref: MSG-ARCH-003
created: 2026-06-16
---

## Összefoglaló

A Planning Pipeline konsenzus tervét értékeltem a tényleges kódbázis ellen. **4 API feltételezésből 2 hibás volt** — ezeket korrigáltam az implementációs spec-ben.

### Főbb korrekciók:
1. **`POST calculate-nesting` nem létezik** — nesting a sheet POST részeként fut szinkron, az auto-trigger pattern kivezetendő
2. **`POST assign-batch` nem létezik** — Feature 3 BLOKKOLVA, CUTTING terminálnak BE munkát kell kiadni
3. **DTO eltérés** — `NestingSheet` helyett `NestingResultResponse` + `PanelAssignmentResponse` a tényleges modell
4. **`@dnd-kit` nincs telepítve** — NPM install szükséges Feature 3-hoz

### Sorrend elfogadva:
1. Nesting Viz (3-4 FE nap, 0 BE)
2. Design→Cutting Workflow (2-3 FE nap, 0 BE, függ 1-től)
3. Machine Scheduling (4-5 FE + 1 BE nap, BLOKKOLVA assign endpoint-ra)

**Státusz:** DRAFT v1 — DB review (v2) és Security review (v3) szükséges a véglegesítéshez. Feature 1-2-nek nincs DB impaktja (read-only FE munka), Feature 3 assign endpoint-hoz kell v2 review.

## Output fájl

`docs/tasks/new/SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1.md`

## Kockázatok / kérdések

- `PanelPlacementDto` pontos mezői ellenőrizendők az implementátor által
- Operator lista forrása (Kernel users endpoint?) nem verifikált — Feature 3 előtt tisztázandó
- Egyetlen FE track → szekvenciális delivery, ~11-13 nap
