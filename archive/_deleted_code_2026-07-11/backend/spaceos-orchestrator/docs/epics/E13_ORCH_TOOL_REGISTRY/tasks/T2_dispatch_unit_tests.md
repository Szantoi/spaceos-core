# T2 — Dispatch Unit Tests for kernel.action.ts

**Epic:** E13 — Tool Registry & Kernel Action Dispatch
**Status:** `CLOSED_DONE`
**Completed:** 2026-03-28

---

## Objective

Write unit tests for every case in the `kernel.action.ts` dispatch table using Vitest with
mocked axios. Each test asserts the correct HTTP method, path, and request body, and verifies
the response is passed through as a serialised string.

---

## Test File

`src/interpreter/kernel.action.test.ts`

---

## Mock Strategy

`kernel.action.ts` calls `axios.create()` at module scope. To intercept the instance before
the module is evaluated, the mock must be registered with `vi.hoisted` (which runs before
`vi.mock` factory functions) and injected via a factory override of `axios.create`.

```typescript
const mockKernel = vi.hoisted(() => ({
  get:      vi.fn(),
  post:     vi.fn(),
  put:      vi.fn(),
  defaults: { headers: { common: {} } },
}));

vi.mock('axios', async (importActual) => {
  const actual = await importActual<typeof import('axios')>();
  return {
    ...actual,
    default: {
      ...actual.default,
      create: vi.fn(() => mockKernel),
      isAxiosError: vi.fn((err) => !!err?.isAxiosError),
    },
  };
});
```

`axios.isAxiosError` is also mocked so that the error-handling branch in `executeToolCall`
can be exercised without needing a real Axios error instance.

---

## Test Coverage

| # | Test Description | Method | Assertion |
|---|-----------------|--------|-----------|
| 1 | `setKernelAuthToken` sets Authorization header | — | `mockKernel.defaults.headers.common['Authorization']` |
| 2 | `get_all_tenants` | GET `/api/tenants` | method + path + response |
| 3 | `create_tenant` | POST `/api/tenants` | method + path + body `{ name }` + response |
| 4 | `get_facilities_by_tenant` | GET `/api/facilities/by-tenant/:tenantId` | method + interpolated path |
| 5 | `create_facility` | POST `/api/facilities` | body `{ tenantId, name }` |
| 6 | `get_workstations_by_facility` | GET `/api/work-stations/by-facility/:facilityId` | interpolated path |
| 7 | `register_workstation` | POST `/api/work-stations` | body `{ facilityId, name, type }` |
| 8 | `update_workstation_status` | PUT `/api/work-stations/:id/status` | body `{ status }` |
| 9 | `get_spacelayers_by_facility` | GET `/api/space-layers/by-facility/:facilityId` | interpolated path |
| 10 | `register_spacelayer` | POST `/api/space-layers` | boolean coercion of `isExternalNode` |
| 11 | `update_spacelayer_intent` | PUT `/api/space-layers/:id/intent` | body `{ intentDataJson }` |
| 12 | `get_flowepics_by_facility` | GET `/api/flow-epics/by-facility/:facilityId` | interpolated path |
| 13 | `create_flowepic` | POST `/api/flow-epics` | body `{ facilityId, workStationId, title }` |
| 14 | `start_flowepic_execution` | PUT `/api/flow-epics/:id/start` | no body |
| 15 | `delegate_flowepic` | PUT `/api/flow-epics/:id/delegate` | body `{ guestTenantId }` |
| 16 | Unknown tool name | — | `{ error: true, message: 'Unknown tool: ...' }` |
| 17 | Axios 4xx error | — | `{ error: true, status: 404 }` |
| 18 | Axios 5xx error | — | `{ error: true, status: 500 }` |

---

## Result

```
Tests  18 passed (18)
```

All 18 tests pass. No failures. `npm test` exits 0 across all 36 tests in the suite.
