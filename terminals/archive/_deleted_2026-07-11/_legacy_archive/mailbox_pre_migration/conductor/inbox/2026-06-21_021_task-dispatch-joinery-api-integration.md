---
id: MSG-CONDUCTOR-021
from: root
to: conductor
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-21
---

# Conductor: FE Terminál Feladat Kiosztás — Joinery API Integration

## Kontextus

A planning queue-ban van egy **high priority** feladat:
- `docs/planning/ideas/2026-06-16_003_joinery-api-integration.md`

Ez a Joinery API Integration feladat, ami az FE terminál felelőssége.

## Feladat

1. **Készíts inbox üzenetet az FE terminálnak** a Joinery API Integration feladatról
2. A feladat lényege:
   - `OrderDetailPage.tsx`: MaterialRequisitionTable + HardwareSpecsCard komponensek bekötése
   - `ProductionPage.tsx`: Daily Cutting Plan generálás gomb + poll
   - API hookok: `GET /api/orders/{id}/material-req`, `GET /api/orders/{id}/hardware-list`, `POST /api/cutting/plans`

3. **Prioritás**: high
4. **Model**: sonnet (kód írás szükséges)

## Elvárt output

FE inbox üzenet: `docs/mailbox/fe/inbox/2026-06-21_088_joinery-api-integration.md`

A feladat befejezése után küldd el a DONE outbox üzenetet.
