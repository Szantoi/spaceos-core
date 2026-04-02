---
id: MSG-P009
from: root
to: portal
type: task-assign
priority: P2
status: UNREAD
created: 2026-04-02T12:00:00
---

## Tárgy

Audit Log UI — Actor és SourceIp oszlopok megjelenítése

## Előzmény

A Kernel (MSG-K013) hozzáadja az `ActorId` és `SourceIp` mezőket az `AuditEventDto`-hoz. A Portal oldali feladat: megjeleníteni ezeket az Audit táblában.

## Feladat

### 1. Típus frissítés — `src/types/index.ts`

```typescript
export interface AuditEventDto {
  id: string;
  eventType: string;
  aggregateId: string;
  stateHash: string;
  occurredAt: string;
  actorId: string | null;
  sourceIp: string | null;
}
```

### 2. AuditPage tábla — két új oszlop

| Oszlop | Megjelenítés |
|---|---|
| Actor | `actorId ?? '—'` |
| Source IP | `sourceIp ?? '—'` |

Az oszlopok a `State Hash` után kerüljenek.

## Függőség

MSG-K013 (Kernel) elvégzése után tesztelhető. A típus frissítése elvégezhető előre.
