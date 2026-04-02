---
id: MSG-007
from: root
to: kernel
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T13:30:00
---

## Tárgy

BUG — PUT /api/work-stations/:id/status: "TenantId cannot be empty"

## Probléma

Ugyanaz a root cause mint MSG-005: a JWT-ben nincs `tid` claim, az EF Core global query filter `TenantId`-t keres.

A MSG-005 fix csak a nested POST endpointokat javította (`FacilityEndpoints.cs`), de a `WorkStationEndpoints.cs` PUT endpointjai is érintettek.

## Érintett endpointok

Minden endpoint ami tenant-scoped entitást módosít JWT `tid` nélkül:
- `PUT /api/work-stations/:id/status`
- `PUT /api/work-stations/:id/name`
- `PUT /api/work-stations/:id/reassign`
- `PUT /api/space-layers/:id/intent`
- `PUT /api/flow-epics/:id/title`
- `PUT /api/flow-epics/:id/start`
- `PUT /api/flow-epics/:id/delegate`

## Elvárt megoldás

Ha nincs `tid` a JWT-ben (admin user), a global query filter ne szűrjön tenant-ra. Az `AppDbContext`-ben a `CurrentTenantGuid` null → a filter `e.TenantId == null` lesz ahelyett, hogy bypass-olná.

Ellenőrizd az `OnModelCreating` query filter logikáját — null tenant esetén ne szűrjön.

## Várt válasz

Javítás + Kernel újraindítás + outbox status-update.
