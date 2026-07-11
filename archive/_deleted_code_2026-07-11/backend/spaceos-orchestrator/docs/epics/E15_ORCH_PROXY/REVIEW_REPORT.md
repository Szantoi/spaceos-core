# Review Report — E15 Kernel Proxy & Auth Middleware

**Epic:** E15 — Kernel Proxy & Auth Middleware
**Status:** `CLOSED_DONE`
**Reviewed:** 2026-03-28
**Reviewer:** Claude Code (automated — E15 PLAN + CODE + TEST agent)

---

## Summary

E15 delivers unit tests for `requireAuth` (T1) and a documented manual integration procedure
for the proxy (T2). All automated tests pass. No violations found.

---

## Test Results

```
npm test → vitest run

Test Files  8 passed (8)
     Tests  49 passed (49)
  Duration  ~1.86s
```

`auth.middleware.test.ts` contributes **9 tests**, all green:

| # | Test | Result |
|---|------|--------|
| 1 | No Authorization header → 401 | PASS |
| 2 | Header without "Bearer " prefix → 401 | PASS |
| 3 | Header with only "Bearer" (no space) → 401 | PASS |
| 4 | JWT signed with wrong key → 401 | PASS |
| 5 | Expired JWT → 401 | PASS |
| 6 | Malformed token string → 401 | PASS |
| 7 | Valid JWT → 200 | PASS |
| 8 | Valid JWT → req.jwtToken set to raw token | PASS |
| 9 | Valid JWT → req.jwtPayload contains claims | PASS |

---

## Build Check

```
npm run build → 0 TypeScript errors, 0 warnings
```

---

## Code Review — auth.middleware.ts

| Check | Result | Notes |
|-------|--------|-------|
| Uses `jwt.verify()` not `jwt.decode()` | PASS | Signature is checked (OWASP A02) |
| Reads key from `env.JWT_SIGNING_KEY` only | PASS | Never touches `process.env` directly |
| Returns no stack trace on 401 | PASS | Only fixed error string exposed |
| `AuthenticatedRequest` interface exported | PASS | Route files can import it |
| `try/catch` around `jwt.verify()` | PASS | Both invalid-signature and expired cases caught |

---

## Code Review — auth.middleware.test.ts

| Check | Result | Notes |
|-------|--------|-------|
| No `vi.mock('jsonwebtoken')` | PASS | Real JWT verification exercised |
| Test key matches `vitest.config.ts` env | PASS | `'test-secret-key-minimum-32-characters-long'` |
| Supertest + minimal Express fixture | PASS | No full `index.ts` bootstrap required |
| All 5 specified test scenarios covered | PASS | Plus 4 additional edge-case tests |
| No TODO/FIXME | PASS | Clean committed code |
| File naming: `*.test.ts` next to source | PASS | `src/middleware/auth.middleware.test.ts` |

---

## Code Review — kernel.proxy.ts

| Check | Result | Notes |
|-------|--------|-------|
| Path rewrite `/bff/api` → `/api` configured | PASS | `pathRewrite` present |
| `changeOrigin: true` set | PASS | Required for virtual-hosted targets |
| Error handler returns `502` with JSON | PASS | No raw error bubbles to client |
| Reads target from `env.KERNEL_BASE_URL` | PASS | Never touches `process.env` directly |
| Automated test | N/A | Live Kernel required — manual procedure in T2 |

---

## Acceptance Criteria Verification

| AC | Method | Result |
|----|--------|--------|
| Valid JWT + proxy → Kernel forwarded as-is | Manual (T2) | Documented in T2 |
| Missing JWT → 401 before proxy | Unit test #1 | PASS |
| Invalid JWT → 401 before proxy | Unit test #4 | PASS |
| Kernel down → 502 | Manual (T2) | Documented in T2 |
| Path rewrite verified | Manual (T2) | Documented in T2 |

---

## Open Violations

None.

---

## Definition of Done

- [x] All Acceptance Criteria checked
- [x] `npm run build` → 0 TypeScript errors
- [x] `npm test` → 0 failed (49/49 passing)
- [x] REVIEW_REPORT.md generated — no open violations
- [x] Layer dependency rules respected
- [x] No TODO/FIXME in committed code
- [x] `.env.example` unchanged (no new env vars introduced)
