---
id: MSG-K012
from: root
to: kernel
type: task-assign
priority: P2
status: READ
created: 2026-04-02T10:00:00
---

## Tárgy

DELETE végpontok hiányoznak minden entitásból

## Probléma

A tesztelés során kiderült, hogy egyik entitásnak sincs DELETE endpoint-ja. Minden `DELETE /api/{entity}/{id}` kérésre `405 Method Not Allowed` jön vissza.

| Endpoint | Status |
|---|---|
| DELETE /api/tenants/{id} | 405 |
| DELETE /api/facilities/{id} | 405 |
| DELETE /api/work-stations/{id} | 405 |
| DELETE /api/space-layers/{id} | 405 |
| DELETE /api/flow-epics/{id} | 405 |

A frontend már tartalmazza a Delete gombokat és a `delete` service hívásokat — csak a Kernel oldali implementáció hiányzik.

## Feladat

**Soft delete (archiválás)** — ne hard delete legyen, hanem `IsArchived` flag beállítása.

### Domain változtatások
Minden érintett aggregátumhoz (`Tenant`, `Facility`, `WorkStation`, `SpaceLayer`, `FlowEpic`):
- `bool IsArchived` property hozzáadása (default: `false`)
- `Archive()` metódus, ami `IsArchived = true`-t állít és domain event-et dob (pl. `TenantArchivedEvent`)
- EF Core global query filter: `Where(e => !e.IsArchived)` — archivált rekordok ne jelenjenek meg a listákban

### API endpoint-ok
Minden entitásra: `DELETE /api/{entity}/{id}` → `204 No Content`
(A HTTP DELETE szemantikája teljesül, de fizikailag nem törlünk)

- 404 ha nem létezik
- 409 ha már archivált
- RequireAuthorization() megmarad

### DTO
Az archivált entitások ne szerepeljenek a GET list válaszokban (a global query filter kezeli).

## Pipeline

CODE → TEST → Outbox status-update.
