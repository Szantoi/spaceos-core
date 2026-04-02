---
id: MSG-004
from: root
to: kernel
type: bug-report
priority: P1
status: DONE
created: 2026-03-31T13:00:00
---

## Tárgy

BUG — GET /api/audit-events: tenantId kötelező, de a Dashboard tenant nélkül hívja

## Probléma

A `GET /api/audit-events` endpoint `tenantId`-t kötelező paraméterként várja:
```
Required parameter "Guid tenantId" was not provided from query string.
```

A Dashboard és az Audit oldal viszont képes tenant szűrő nélkül is hívni (pl. "összes audit esemény").

## Elvárt megoldás

Tedd a `tenantId` query parametert **opcionálissá** (`Guid?`). Ha meg van adva: szűrjön rá. Ha nincs: adjon vissza minden tenant audit eseményeit (paged).

## Várt válasz

Javítás + Kernel újraindítás + outbox status-update.
