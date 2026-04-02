---
id: MSG-P007-REPLY
in-reply-to: MSG-P007
from: portal
to: root
type: status-update
status: DONE
completed: 2026-04-01T16:15:00
---

## Eredmeny

E29 — OpenAPI Contract Sync implementalva.

## Elvegzett munka

### CODE phase

| Fajl | Valtozas |
|---|---|
| `package.json` | `openapi-typescript` devDependency + `sync-types` npm script |
| `src/types/generated.ts` | Generalt tipusok a Kernel OpenAPI-bol (`http://localhost:5000/openapi/v1.json`) |
| `src/types/common.ts` | Enum-ok string union-kent (Kernel runtime string-eket kuld), `GeneratedSchemas` export, `WorkflowPhase` type hozzaadva |
| `src/types/index.ts` | DTO-k `RequiredNonNull<Schema[...]>` wrapper-rel a generalt tipusokbol; FlowEpicDto, AuditEventDto, DashboardStatsDto atallitva Kernel OpenAPI-ra |
| `src/api/facilities.service.ts` | `create()` tenantId kulon parameter (nem a body-ban) |
| `src/api/flowepics.service.ts` | `create()` body csak `{ title }` |
| `src/hooks/useFacilities.ts` | `useCreateFacility` `{ tenantId, data }` shape |
| `src/hooks/useFlowEpics.ts` | `useCreateFlowEpic` `{ facilityId, title }` shape |
| `src/features/flowepics/FlowEpicsPage.tsx` | FSM Kanban → 2-oszlopos Phase board (Design/Construction); form workStationId eltavolitva |
| `src/features/flowepics/FlowEpicDetail.tsx` | `targetFacilityId`, `phase`, `isDelegated` mezoket hasznal |
| `src/features/flowepics/DelegateEpicDialog.tsx` | `guestTenantId` default ertek fix |
| `src/features/dashboard/DashboardPage.tsx` | `activeWorkStationCount`, `flowEpicCount`, `auditEventCount` uj stat kartya |
| `src/features/audit/AuditPage.tsx` | `aggregateId` oszlop, `payload` eltavolitva |
| `src/features/facilities/FacilitiesPage.tsx` | `handleCreate` `{ tenantId, data }` shape |

### TEST phase

| Fajl | Valtozas |
|---|---|
| `src/api/contract.test.ts` | **UJ** — Contract teszt: enum member count, DTO field validacio, generated.ts letezese |
| `src/features/flowepics/FlowEpicsPage.test.tsx` | Mock adatok + assertionok uj DTO-hoz |
| `src/features/flowepics/FlowEpicDetail.test.tsx` | Mock adatok + assertionok uj DTO-hoz |
| `src/features/dashboard/DashboardPage.test.tsx` | Mock AuditEventDto + DashboardStatsDto frissitve |
| `src/features/audit/AuditPage.test.tsx` | Mock AuditEventDto frissitve |
| `src/features/facilities/FacilitiesPage.test.tsx` | Create mutation assertion frissitve |

### Acceptance Criteria

- [x] `npm run sync-types` lefut es generalja `src/types/generated.ts`-t
- [x] `src/types/index.ts` es `common.ts` a generalt tipusokra epul
- [x] Minden DTO a generalt fajlbol jon (RequiredNonNull wrapper)
- [x] Enum-ok string union + contract teszt (Kernel numeric OpenAPI → string runtime)
- [x] `npm run build` → 0 error
- [x] `npm test` → 0 fail (26/26 fajl, 178/178 teszt PASS)
- [x] Contract teszt: enum count + DTO field validacio
