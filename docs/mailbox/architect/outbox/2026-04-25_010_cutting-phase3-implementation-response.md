---
id: MSG-ARCH-010-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-007
created: 2026-04-25
---

## Összefoglalás

Elkészült a `docs/architecture/SpaceOS_Cutting_Phase3_Implementation_v1.md` — Cutting Phase 3 rendelés ingestion + geometry bin-packing spec. Mind a 7 kért pont lefedve.

## Kulcsdöntések

| Kérdés | Döntés |
|--------|--------|
| Ingestion flow | **API-based** (`POST /internal/ingest-order`) — meglévő `X-SpaceOS-Internal` minta, Orchestrator közvetít |
| Geometry nesting | INestingStrategy (NuGet) már DEPLOYED; Phase 3: valós CuttingJob dimenziókat kap + Inventory paneleket |
| Nesting trigger | **Publish transition-kor** (Draft→Published) — a `PublishCuttingPlanCommandHandler` bővül |
| Panel forrás | `PanelSourceService` → `IInventoryProvider.GetStockAsync()` + `GetUsableOffcutsAsync()` |
| Grain direction | `"none"` → `CanRotate=true`, `"vertical"/"horizontal"` → `CanRotate=false` |

## Effort

**Phase 3 MVP: 10.0 nap** (CUTTING 7.0 + ORCH 1.0 + tesztek 1.0 + E2E 1.0)
**Phase 3.5: ~6.0 nap** (re-nest, multi-material, SVG viz, comparison)

## Blokkoló előfeltételek (ELLENŐRIZENDŐ)

| # | Előfeltétel | Hatás |
|---|-------------|-------|
| 1 | **Inventory: GET stock + GET offcuts endpointok** — a `ContractsInventoryHttpAdapter` eddig `NotSupportedException`-t dob ezekre | Ha hiányzik → Inventory modul bővítés szükséges (~1-2 nap) |
| 2 | **Joinery: order items dimenzió mezők** (widthMm, heightMm, material, grainDirection) | Ha hiányzik → Joinery response bővítés szükséges |
| 3 | **CUTTING-031/033 migration deployed** | Phase 3 migration függ a korábbi migrációktól |

## Architektúra összefoglaló

```
Joinery DoorOrder (Calculated)
  → Orchestrator BFF: POST /bff/cutting/planning/{planId}/add-order/{orderId}
    → Orchestrator: GET Joinery order items + transform → Cutting ingest DTO
    → Cutting: POST /internal/ingest-order → CuttingJob-ok valós dimenzióval
  → Publish: INestingStrategy.ComputeAsync(parts + panels from Inventory)
    → PlanNestingSnapshot mentés
    → IInventoryProvider.ReserveAsync()
  → Freeze: offcut regisztráció Inventory-ba (meglévő handler)
```

## Nyitott kérdések (5 db) — Gábor válasza kell

Legfontosabb: az Inventory GET stock/offcuts endpointok léteznek-e, és a Joinery order items response milyen mezőket tartalmaz.
