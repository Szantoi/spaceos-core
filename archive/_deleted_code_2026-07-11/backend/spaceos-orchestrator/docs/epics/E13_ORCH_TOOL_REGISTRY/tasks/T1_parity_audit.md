# T1 — Tool Registry ↔ Kernel Action Parity Audit

**Epic:** E13 — Tool Registry & Kernel Action Dispatch
**Status:** `CLOSED_DONE`
**Completed:** 2026-03-28

---

## Objective

Verify that every tool name defined in `tool-registry.ts` has a matching `case` in the
`kernel.action.ts` dispatch table, and that no dispatch case is orphaned (i.e. not backed
by a tool schema).

---

## Audit Result: 14 / 14 — PASS

| # | Tool Name | Dispatch Case | HTTP Method | Kernel Path |
|---|-----------|---------------|-------------|-------------|
| 1 | `get_all_tenants` | `case 'get_all_tenants'` | GET | `/api/tenants` |
| 2 | `create_tenant` | `case 'create_tenant'` | POST | `/api/tenants` |
| 3 | `get_facilities_by_tenant` | `case 'get_facilities_by_tenant'` | GET | `/api/facilities/by-tenant/:tenantId` |
| 4 | `create_facility` | `case 'create_facility'` | POST | `/api/facilities` |
| 5 | `get_workstations_by_facility` | `case 'get_workstations_by_facility'` | GET | `/api/work-stations/by-facility/:facilityId` |
| 6 | `register_workstation` | `case 'register_workstation'` | POST | `/api/work-stations` |
| 7 | `update_workstation_status` | `case 'update_workstation_status'` | PUT | `/api/work-stations/:workStationId/status` |
| 8 | `get_spacelayers_by_facility` | `case 'get_spacelayers_by_facility'` | GET | `/api/space-layers/by-facility/:facilityId` |
| 9 | `register_spacelayer` | `case 'register_spacelayer'` | POST | `/api/space-layers` |
| 10 | `update_spacelayer_intent` | `case 'update_spacelayer_intent'` | PUT | `/api/space-layers/:spaceLayerId/intent` |
| 11 | `get_flowepics_by_facility` | `case 'get_flowepics_by_facility'` | GET | `/api/flow-epics/by-facility/:facilityId` |
| 12 | `create_flowepic` | `case 'create_flowepic'` | POST | `/api/flow-epics` |
| 13 | `start_flowepic_execution` | `case 'start_flowepic_execution'` | PUT | `/api/flow-epics/:epicId/start` |
| 14 | `delegate_flowepic` | `case 'delegate_flowepic'` | PUT | `/api/flow-epics/:epicId/delegate` |

---

## Findings

- **Parity:** 14/14 tools have a matching dispatch case. No orphaned cases.
- **Error handling:** The `default` branch returns `{ error: true, message: 'Unknown tool: <name>' }`.
  Axios errors are caught and returned as `{ error: true, status, detail }` — never rethrown.
- **URL construction:** All Kernel paths are built from `env.KERNEL_BASE_URL` (via `axios.create`)
  + a relative path string. No URL is hardcoded as an absolute value.
- **Input mapping:** Every required tool input field (`i.<field>`) matches the corresponding
  Kernel DTO field documented in `kernel.types.ts`.

---

## Violations

None.
