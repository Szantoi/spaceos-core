# Epic E15 — Kernel Proxy & Auth Middleware

**Priority:** 🟡 P2
**Status:** `CLOSED_DONE`
**Depends on:** E11
**Blocks:** E16, E17

---

## Goal

`/bff/api/*` proxies correctly to the live C# Kernel.
`requireAuth` blocks unauthenticated requests with 401.

---

## Scope

**In scope:**
- `GET /bff/api/tenants` → proxied to `KERNEL_BASE_URL/api/tenants` with JWT forwarded
- `POST /bff/api/tenants` → proxied with body + JWT
- `401` on missing/invalid JWT before proxy fires
- `502` on Kernel unreachable

**Out of scope:**
- Per-tenant rate limiting (future)
- Response caching

---

## Acceptance Criteria

- [x] Valid JWT + `GET /bff/api/tenants` → Kernel response forwarded as-is
- [x] Missing JWT → `401` before proxy
- [x] Invalid JWT → `401` before proxy
- [x] Kernel down → `502 { error: "Kernel unavailable" }`
- [x] Path rewrite: `/bff/api/tenants` → `/api/tenants` (no `/bff` prefix forwarded)

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | Auth middleware unit tests | `CLOSED_DONE` |
| T2 | Proxy integration test against live Kernel | `CLOSED_DONE` |

---

## Definition of Done

- [x] All AC checked
- [x] `npm run build` → 0 errors
- [x] `npm test` → 0 failed
