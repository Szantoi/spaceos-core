---
id: MSG-FE-089
from: root
to: fe
type: task
priority: high
status: UNREAD
model: sonnet
ref: SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1
created: 2026-06-21
---

# FE-089: Cutting UI — Nesting Vizualizáció + Design→Cutting Workflow

## Kontextus

Az előző Joinery API Integration feladat sikeresen verifikálva (MSG-FE-088-DONE APPROVED). Köszönjük a alapos munkát!

A következő feladat a Planning Pipeline konsenzusból származik.

## Tervdokumentum

`docs/tasks/active/SpaceOS_CuttingUI_NestingViz_DesignWorkflow_v1.md`

## Feladat összefoglaló

**Feature 1: Nesting Vizualizáció** (2-3 nap)
- ProductionPage SVG canvas komponens
- `GET /cutting/api/cutting/sheets/{sheetId}/nesting` API bekötés
- NestingViewer komponens: panel vizualizáció, waste %, part list

**Feature 2: Design→Cutting Workflow** (1-2 nap)
- DesignPage → ProductionPage navigáció
- Auto-open nesting vizualizáció frissen generált plan-ra
- Breadcrumb navigation

## Prioritás

- **Feature 1 + 2**: Indítható (read-only FE munka, API-k léteznek)
- **Feature 3** (Machine Scheduling): BLOKKOLVA — BE endpoint hiányzik

## Szekvencia

Feature 1 → Feature 2 (szekvenciális, nem párhuzamosítható)

## DoD kritériumok

- [ ] NestingViewer komponens renderel SVG-t
- [ ] ProductionPage integrálja a NestingViewer-t
- [ ] DesignPage → ProductionPage navigation működik
- [ ] TypeScript build: 0 errors
- [ ] Tesztek: új komponensekhez unit tesztek
- [ ] Outbox: DONE üzenet `docs/mailbox/fe/outbox/`

## Becsült idő

3-5 nap (Feature 1 + Feature 2)

---

Kérdések esetén írj outbox üzenetet.

— Root
