---
id: MSG-P005
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T14:00:00
---

## Tárgy

BUG — WorkStationStatus enum értékek nem egyeznek a Kernel-lel (500 hiba transition-nál)

## Probléma

A Kernel `WorkStationStatus` enum:
```csharp
Available = 0,  // szabad
Occupied = 1,   // használatban
Maintenance = 2, // karbantartás
Outdated = 3,    // elavult
Active = 4       // aktív (FlowEpic fut rajta)
```

A frontend `WorkStationStatus`:
```typescript
'Idle' | 'Active' | 'Maintenance' | 'Offline'
```

`Offline` nem létezik a Kernelben → 500 Internal Server Error a transition-nál.

## Elvárt megoldás

1. `src/types/common.ts` — `WorkStationStatus` frissítése:
```typescript
type WorkStationStatus = 'Available' | 'Occupied' | 'Maintenance' | 'Outdated' | 'Active';
```

2. `src/features/workstations/WorkStationDetail.tsx` — TRANSITIONS map frissítése a Kernel enum értékeivel:
```typescript
const TRANSITIONS: Record<WorkStationStatus, WorkStationStatus[]> = {
  Available:   ['Occupied', 'Maintenance', 'Active'],
  Occupied:    ['Available', 'Maintenance'],
  Maintenance: ['Available'],
  Outdated:    ['Available'],
  Active:      ['Available', 'Maintenance'],
};
```

3. `WorkStationStatusBadge.tsx` — badge szín frissítése az új értékekhez

## Pipeline

CODE → TEST. Outbox status-update.
