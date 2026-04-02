---
id: MSG-P004
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T12:45:00
---

## Tárgy

BUG — WorkStation status enum eltérés: Kernel "Available", frontend "Idle"

## Probléma

A Kernel `WorkStationStatus` enum értékei:
```
Available, Active, Maintenance, Offline
```

A frontend `WorkStationDetail.tsx` TRANSITIONS map-je:
```
Idle, Active, Maintenance, Offline
```

`Idle` nem létezik a Kernelben → a `TRANSITIONS["Available"]` undefined → nincs transition gomb.

## Elvárt megoldás

1. `src/types/common.ts` — `WorkStationStatus` frissítése: `Idle` → `Available`
2. `src/features/workstations/WorkStationDetail.tsx` — TRANSITIONS és TRANSITION_BUTTON_STYLES map frissítése
3. `src/features/workstations/WorkStationStatusBadge.tsx` — badge szín frissítése

## Pipeline

CODE → TEST. Outbox status-update.
