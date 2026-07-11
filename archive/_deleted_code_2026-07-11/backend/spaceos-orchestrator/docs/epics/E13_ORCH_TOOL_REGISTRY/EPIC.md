# Epic E13 — Tool Registry & Kernel Action Dispatch

**Priority:** 🔴 P1
**Status:** `CLOSED_DONE`
**Depends on:** E11
**Blocks:** E14

---

## Goal

Every tool in `tool-registry.ts` has a working `case` in `kernel.action.ts`.
All 14 tools map correctly to C# Kernel endpoints.

---

## Scope

**In scope:**
- All 14 tools validated (tool-registry.test.ts passes)
- `kernel.action.ts` dispatch table covers all 14 tool names
- Axios calls match the actual Kernel route paths (`/api/tenants`, `/api/facilities`, etc.)
- Error responses from Kernel (4xx, 5xx) returned as `{ error: true, status, detail }`

**Out of scope:**
- Real Kernel running (mock with nock/vi.mock for unit tests)
- New tools beyond the 14 defined

---

## Acceptance Criteria

- [x] `tool-registry.test.ts` → all 4 tests pass
- [x] Every tool name has a `case` in `kernel.action.ts` — no `Unknown tool` returns
- [x] Axios 404 from Kernel → `{ error: true, status: 404 }` returned to LLM as tool_result
- [x] No Kernel URL hardcoded — always `env.KERNEL_BASE_URL + path`

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | Audit tool-registry ↔ kernel.action parity | `CLOSED_DONE` |
| T2 | Unit test each dispatch case with mocked axios | `CLOSED_DONE` |

---

## Definition of Done

- [x] All AC checked
- [x] `npm run build` → 0 errors
- [x] `npm test` → 0 failed
