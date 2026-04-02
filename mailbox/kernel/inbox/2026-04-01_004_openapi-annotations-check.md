---
id: MSG-008
from: root
to: kernel
type: task-assign
priority: P1
status: DONE
created: 2026-04-01T15:00:00
epic: E29
---

## Tárgy

E29 — OpenAPI annotációk ellenőrzése

## Feladat

A Portal az OpenAPI JSON-ból fogja generálni a TypeScript típusokat. Ehhez az kell, hogy minden publikus enum és DTO megjelenjen a Swagger output-ban.

Ellenőrizd:
1. `GET /openapi/v1.json` tartalmazza-e az összes enum-ot stringként: `WorkStationStatus`, `TradeType`
2. Minden DTO megjelenik-e schemas alatt: `TenantDto`, `FacilityDto`, `WorkStationDto`, `SpaceLayerDto`, `FlowEpicDto`, `AuditEventDto`, `DashboardStatsDto`
3. Minden endpoint response type helyesen van-e annotálva (`.Produces<T>()`)

Ha valami hiányzik, adj hozzá a szükséges annotációkat.

## Várt válasz

Outbox status-update a vizsgálat eredményével.
