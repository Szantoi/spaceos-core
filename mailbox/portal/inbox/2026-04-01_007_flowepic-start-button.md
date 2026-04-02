---
id: MSG-P009
from: root
to: portal
type: task-assign
priority: P2
status: DONE
created: 2026-04-01T17:00:00
---

## Tárgy

FlowEpic detail — "Start Delivery" gomb hozzáadása

## Feladat

A `FlowEpicDetail.tsx`-be adj hozzá egy "Start Delivery" gombot, ami Discovery fázisban látszik és a `PUT /api/flow-epics/:id/start` endpointot hívja.

### Részletek

- A gomb csak `phase === 'Discovery'` esetén jelenjen meg
- Kattintás → `useStartFlowEpic` mutation (ha nincs ilyen hook, hozd létre)
- Sikeres válasz → `invalidateQueries` → phase frissül `Delivery`-re
- `Delivery` fázisban a gomb eltűnik (nincs visszaút)

### Kernel API

```
PUT /api/flow-epics/:id/start
Body: (üres)
→ 200 OK
```

A `flowepics.service.ts`-ben már van `start` metódus.

## Pipeline

CODE → TEST. Outbox status-update.
