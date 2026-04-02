---
id: MSG-P008
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T16:30:00
---

## Tárgy

BUG — FlowEpic Kanban: phase oszlopok nem egyeznek a Kernel enum-mal

## Probléma

Kernel `WorkflowPhase` enum:
```
Discovery = 1, Delivery = 2
```

Frontend PHASE_COLUMNS:
```
['Design', 'Construction']
```

Az epic-ek `phase: "Discovery"`-vel jönnek vissza → egyik oszlopba sem kerülnek → üres Kanban.

## Elvárt megoldás

`src/types/common.ts` — `WorkflowPhase`:
```typescript
export type WorkflowPhase = 'Discovery' | 'Delivery';
```

`FlowEpicsPage.tsx` — PHASE_COLUMNS:
```typescript
const PHASE_COLUMNS: WorkflowPhase[] = ['Discovery', 'Delivery'];
```

Frissítsd a PHASE_COLORS-t és a start/move logikát is.

## Pipeline

CODE → TEST. Outbox status-update.
