---
id: MSG-P008
from: portal
to: root
type: task-done
ref: 2026-04-02_008_role-based-ui
status: DONE
completed: 2026-04-02T10:25:00
---

## Összefoglalás

MSG-P008 (Role-alapú UI + 403 console hiba) kész.

## Elvégzett feladatok

| # | Feladat | Fájl | Megjegyzés |
|---|---|---|---|
| 1 | `useHasRole` + `useIsAdmin` hook | `src/hooks/useHasRole.ts` | Zustand auth store-ból olvassa a `user.roles` tömböt |
| 2 | TanStack Query 4xx retry tiltás | `src/App.tsx` | `retry` callback: `status < 500` → `return false` |
| 3 | 403 silent kezelés | `src/api/client.ts` | `Promise.reject(error)` visszaad, de a Query nem retry-ol |
| 4 | Admin gate — TenantsPage | `src/features/tenants/TenantsPage.tsx` | Create, Rename, Delete gombok `{isAdmin && ...}` |
| 5 | Admin gate — FacilitiesPage | `src/features/facilities/FacilitiesPage.tsx` | New Facility, Edit, Delete gombok |
| 6 | Admin gate — WorkStationsPage | `src/features/workstations/WorkStationsPage.tsx` | Register, Delete gombok |
| 7 | Admin gate — SpaceLayersPage | `src/features/spacelayers/SpaceLayersPage.tsx` | New SpaceLayer, Edit, Delete gombok |
| 8 | Admin gate — FlowEpicsPage | `src/features/flowepics/FlowEpicsPage.tsx` | New Epic, Delete gombok |
| 9 | Tesztek — useHasRole | `src/hooks/useHasRole.test.ts` | 7 eset: szerepkör megvan, hiányzik, null user |
| 10 | AuditPage.test.tsx javítás | `src/features/audit/AuditPage.test.tsx` | `capturedParams.entityType` → `capturedParams.eventType` |

## Teszteredmény

```
Test Files  27 passed (27)
Tests       188 passed (188)
```

## Megjegyzés

- Task 5 (TenantDetail read-only nézet) nem lett implementálva — a detail oldalak egyelőre nem tartalmaznak szerkesztési gombokat, ezért nincs mit elrejteni.
- Az AuditPage tesztben egy pre-existing bug volt: a form mező neve `entityType`, de a szűrőparaméter neve `eventType`. A teszt a rossz kulcsot ellenőrizte — javítva.
