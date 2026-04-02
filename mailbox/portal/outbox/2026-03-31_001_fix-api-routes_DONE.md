---
id: MSG-P001-REPLY
in-reply-to: MSG-P001
from: portal
to: root
type: status-update
status: DONE
completed: 2026-03-31T12:00:00
---

## Eredmeny

P1 BUG javitva — Frontend API route-ok osszehangolva a Kernel route-jaival.

## Elvegzett munka

### CODE phase

| Fajl | Valtozas |
|---|---|
| `src/api/facilities.service.ts` | `getAll` eltavolitva, `getByTenant` route: `/api/tenants/:tenantId/facilities`, `create` nested route, PagedList `.items` kinyeres |
| `src/api/workstations.service.ts` | `getAll` eltavolitva, route-ok `/api/facilities/:facilityId/work-stations`-ra javitva (kotojeles), PagedList `.items` kinyeres |
| `src/api/spacelayers.service.ts` | `getByFacility` route: `/api/facilities/:facilityId/space-layers`, `register` nested route, PagedList `.items` kinyeres |
| `src/api/flowepics.service.ts` | `getByFacility` route: `/api/facilities/:facilityId/flow-epics`, `create` nested route, PagedList `.items` kinyeres |
| `src/hooks/useFacilities.ts` | `useFacilities()` most `tenantId`-t var |
| `src/hooks/useWorkStations.ts` | `useWorkStations()` eltavolitva, `useCreateWorkStation` `{ facilityId, data }` shape |
| `src/hooks/useFlowEpics.ts` | `useCreateFlowEpic` facilityId atadas javitva |
| `src/features/facilities/FacilitiesPage.tsx` | Tenant-scoped facility lista |
| `src/features/workstations/WorkStationsPage.tsx` | `useWorkStationsByFacility` hasznalata |
| `src/features/spacelayers/SpaceLayersPage.tsx` | Tenant dropdown hozzaadva a facility szuro ele |

### TEST phase

| Fajl | Valtozas |
|---|---|
| `src/features/facilities/FacilitiesPage.test.tsx` | Mock-ok frissitve tenant-scoped flow-hoz |
| `src/features/workstations/WorkStationsPage.test.tsx` | `useWorkStationsByFacility` mock, `useSearchParams` mock |
| `src/features/spacelayers/SpaceLayersPage.test.tsx` | `useTenants` mock hozzaadva, tenant kivalasztas a tesztekben |

### Eredmeny

- **26/26 teszt fajl PASS**
- **174/174 teszt PASS**
