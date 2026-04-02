---
id: MSG-P001
from: root
to: portal
type: bug-report
priority: P1
status: DONE
created: 2026-03-31T12:00:00
---

## Tárgy

BUG — Frontend API route-ok nem egyeznek a Kernel valós route-jaival

## Probléma

A frontend service fájlok rossz URL-eket hívnak. A Kernel nested resource route-okat használ, a frontend viszont flat route-okat.

## Pontos eltérések

| Frontend (ROSSZ) | Kernel (HELYES) |
|---|---|
| `GET /api/facilities` (getAll) | Nincs ilyen — listázás: `GET /api/tenants/:tenantId/facilities` |
| `GET /api/facilities/by-tenant/:id` | `GET /api/tenants/:tenantId/facilities` |
| `GET /api/workstations` (getAll) | Nincs ilyen — listázás: `GET /api/facilities/:facilityId/work-stations` |
| `GET /api/workstations/by-facility/:id` | `GET /api/facilities/:facilityId/work-stations` |
| `POST /api/workstations` | `POST /api/facilities/:facilityId/work-stations` |
| `GET /api/space-layers/by-facility/:id` | `GET /api/facilities/:facilityId/space-layers` |
| `POST /api/space-layers` | `POST /api/facilities/:facilityId/space-layers` |
| `GET /api/flow-epics/by-facility/:id` | `GET /api/facilities/:facilityId/flow-epics` |
| `POST /api/flow-epics` | `POST /api/facilities/:facilityId/flow-epics` |
| `GET /api/flow-epics/by-tenant/:id` | Nincs ilyen route — Kernel-ben facility-based |

Továbbá a Kernel `PagedList<T>` objektumot ad vissza (pl. `{ items: [...], page, totalCount }`), nem sima tömböt. A service-eknek ki kell nyerniük az `items`-t ahol tömböt várnak.

## Érintett fájlok

- `src/api/tenants.service.ts` — getAll: items kinyerés (MÁR JAVÍTVA)
- `src/api/facilities.service.ts` — route javítás + items kinyerés
- `src/api/workstations.service.ts` — route javítás + items kinyerés
- `src/api/spacelayers.service.ts` — route javítás + items kinyerés
- `src/api/flowepics.service.ts` — route javítás + items kinyerés
- Hook-ok és komponensek szükség szerint

## Kernel API referencia (helyes route-ok)

```
GET  /api/tenants                              → PagedList<TenantDto>
GET  /api/tenants/:id                          → TenantDto
POST /api/tenants                              → TenantDto (201)
GET  /api/tenants/:tenantId/facilities         → PagedList<FacilityDto>
POST /api/tenants/:tenantId/facilities         → FacilityDto (201)

GET  /api/facilities/:id                       → FacilityDto
GET  /api/facilities/:facilityId/work-stations → PagedList<WorkStationDto>
POST /api/facilities/:facilityId/work-stations → WorkStationDto (201)
GET  /api/facilities/:facilityId/space-layers  → PagedList<SpaceLayerDto>
POST /api/facilities/:facilityId/space-layers  → SpaceLayerDto (201)
GET  /api/facilities/:facilityId/flow-epics    → PagedList<FlowEpicDto>
POST /api/facilities/:facilityId/flow-epics    → FlowEpicDto (201)

GET  /api/work-stations/:id                    → WorkStationDto
GET  /api/space-layers/:id                     → SpaceLayerDto
GET  /api/flow-epics/:id                       → FlowEpicDto
GET  /api/audit-events?page=&pageSize=&tenantId=  → PagedList<AuditEventDto>
GET  /api/dashboard/stats                      → DashboardStatsDto
```

## Pipeline

CODE → TEST → REVIEW. Outbox status-update minden phase után.
