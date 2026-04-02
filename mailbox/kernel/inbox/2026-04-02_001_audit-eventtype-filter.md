---
id: MSG-K011
from: root
to: kernel
type: task-assign
priority: P2
status: READ
created: 2026-04-02T09:00:00
---

## Tárgy

Audit szűrő javítás: `eventType` paraméter + dátum kezelés

## Előzmény

A tesztelés során két hibát azonosítottunk az Audit Log szűrőjével (TEST_LOG.md #15):

1. **Entity type szűrő nem működik** — a frontend küld `entityType` query paramétert, de a Kernel nem kezeli (nincs a Specification-ban, Query-ban, Endpoint-ban).
2. **To dátum exclusive** — `to=2026-04-02` → `2026-04-02T00:00:00`-nak értelmeződik, ezért az aznap keletkezett események (`08:32`) kizáródnak (`08:32 <= 00:00` = false).

## Fontos: részleges változtatások már megtörténtek

A root terminál — tévesen, a mailbox protokoll megkerülésével — már módosította az alábbi fájlokat:

| Fájl | Változtatás |
|---|---|
| `Domain/AuditLog/Specifications/AuditEventsByTenantFilterSpec.cs` | `string? eventType` param hozzáadva, `Where` feltétel hozzáadva |
| `Domain/AuditLog/Specifications/AuditEventsByTenantPagedSpec.cs` | ugyanaz |
| `Application/AuditLog/Queries/GetAuditEventsQuery.cs` | `string? EventType` param hozzáadva |
| `Application/AuditLog/Queries/GetAuditEventsQueryHandler.cs` | `EventType` átadva a spec-eknek |
| `Api/Endpoints/AuditEventEndpoints.cs` | `string? eventType` query param hozzáadva, átadva a Query-nek |

## Feladatod

1. Olvasd el a módosított fájlokat és ellenőrizd a helyességüket
2. Futtasd: `dotnet build`
3. Futtasd a teszteket: `dotnet test`
4. Ha minden rendben, adj visszajelzést az outboxban

## Pipeline

CODE → TEST → Outbox status-update.
