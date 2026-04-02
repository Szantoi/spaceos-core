---
id: MSG-005
from: root
to: kernel
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T11:30:00
---

## Tárgy

BUG — WorkStation/SpaceLayer/FlowEpic POST: TenantId cannot be empty

## Probléma

A `POST /api/facilities/:facilityId/work-stations` endpoint `TenantId`-t vár, amit a `HttpTenantResolver` a JWT `tid` claimből olvas. De az Orchestrator dev token nem tartalmaz `tid`-t (mert dev módban a user nincs tenant-hoz kötve).

Hiba: `"TenantId cannot be empty."` → 400

Ez minden nested POST-ot érint:
- `POST /api/facilities/:facilityId/work-stations`
- `POST /api/facilities/:facilityId/space-layers`
- `POST /api/facilities/:facilityId/flow-epics`

## Elvárt megoldás

A Kernel az URL-ből kapott `facilityId`-ből tudja kinyerni a tenant-ot (a facility tartozik egy tenanthoz). Ha a JWT-ben nincs `tid`, a handler keresse ki a facility tenant-ját a DB-ből.

Pseudocode:
```csharp
var tenantId = tenantResolver.TryResolve()?.Value
    ?? (await facilityRepo.GetByIdAsync(facilityId, ct))?.TenantId.Value
    ?? Guid.Empty;
```

## Várt válasz

Javítás + outbox status-update.
