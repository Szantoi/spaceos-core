---
id: MSG-JOINERY-056
from: root
to: joinery
type: task
priority: medium
status: READ
model: sonnet
created: 2026-06-16
---

# Joinery — DESIGN_MEMORY + cross-modul szabályok

## Tájékoztató

A VPS-en megjelent a DESIGN_MEMORY dokumentum:

```
docs/knowledge/architecture/DESIGN_MEMORY.md
```

## ADR-039 — Joinery nem hív más modult közvetlenül

Zárolt döntés: a Joinery Driver **csak a saját DB-jébe ír/olvas**.

- Joinery nem hív Cutting, Inventory, Kernel DB-t közvetlenül
- Ha Joinery orderből Cutting plan kell → az Orchestrator koordinál
- Ha Inventory stock kell az order validáláshoz → Orchestrator hívja mindkettőt

### Mi következik ebből

- Joinery service publikus API-ja a határvonal (amit az Orchestrator hív)
- Belső Joinery logika (price calculation, BOM generation) maradhat a Driver-ben
- Cross-modul esemény (pl. „order státusz változott") → Orchestrator event fanout

## ADR-024 — IParametricProduct implementor

A Joinery Driver implementálja az `IParametricProduct` interfészt.
- A Kernel csak az interfészen keresztül lát Joinery adatot
- Joinery-specifikus mezők (pl. `DoorProfile`, `HingeType`) nem szivárognak Kernelbe

## Nincs azonnali teendő

Ez tájékoztató — nem kell DONE outbox. A következő feladatoknál tartsd szem előtt.
