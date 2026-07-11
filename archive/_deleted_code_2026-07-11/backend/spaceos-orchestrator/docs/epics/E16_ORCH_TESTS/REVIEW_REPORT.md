# Review Report — E16_ORCH_TESTS

**Date:** 2026-03-28
**Status:** `CLOSED_DONE`
**Reviewer:** Claude Code (automated)

---

## Test Run Summary

```
Test Files  8 passed (8)
     Tests  50 passed (50)
  Duration  ~1.65s
```

Previous baseline: 49 tests. E16 adds 1 new test (429 rate limit) for a total of 50.

---

## Acceptance Criteria — Final State

| AC | Criterion | Result |
|----|-----------|--------|
| AC1 | `interpreter.service.test.ts` → 5 tests pass | PASS (5/5) |
| AC2 | `tool-registry.test.ts` → 4 tests pass | PASS (4/4) |
| AC3 | `mock.provider.test.ts` → 2 tests pass | PASS (2/2) |
| AC4 | `chat.route.test.ts` → 401, 422, 200, 429 cases | PASS (5/5) |
| AC5 | `health.route.test.ts` → 200, 207 cases | PASS (4/4) |
| AC6 | `auth.middleware.test.ts` → valid/invalid/missing JWT | PASS (9/9) |
| AC7 | `kernel.action.test.ts` → full dispatch coverage | PASS (18/18) |
| AC8 | `llm.provider.test.ts` → provider selection | PASS (3/3) |
| AC9 | `npm test` → 0 failed | PASS |

---

## Security Checklist — S1–S10

| ID | Control | Verified By | Result |
|----|---------|-------------|--------|
| S1 | Missing `Authorization` header → 401 | `auth.middleware.test.ts` #1 | PASS |
| S2 | Malformed header (no `Bearer` prefix) → 401 | `auth.middleware.test.ts` #2, #3 | PASS |
| S3 | JWT signed with wrong key → 401 | `auth.middleware.test.ts` #4 | PASS |
| S4 | Expired JWT → 401 | `auth.middleware.test.ts` #5 | PASS |
| S5 | Malformed token string → 401 | `auth.middleware.test.ts` #6 | PASS |
| S6 | No error detail / stack trace in 401 response | `auth.middleware.test.ts` (body assertions) | PASS |
| S7 | Invalid request body → 422 | `chat.route.test.ts` #3 | PASS |
| S8 | Rate limit exceeded → 429 | `chat.route.test.ts` #5 | PASS |
| S9 | `jwt.verify()` used, not `jwt.decode()` | `auth.middleware.ts` code review | PASS |
| S10 | `JWT_SIGNING_KEY` from `env.ts` (Zod-validated), never hardcoded | `config/env.ts` + `auth.middleware.ts` | PASS |

---

## Layer Dependency Violations

None. All imports follow the approved dependency graph:

```
routes → interpreter → llm (ILlmProvider)
middleware → config/env
ALL → types
```

---

## Code Rules Compliance

| Rule | Status |
|------|--------|
| Every route handler wrapped in `try/catch → next(err)` | PASS |
| `req.body` parsed with Zod `safeParse()` before use | PASS |
| `process.env` never accessed directly (always via `env.ts`) | PASS |
| No `TODO` or `FIXME` in committed code | PASS |
| Every new service/route has a companion `*.test.ts` | PASS |

---

## Open Violations

None.

---

## Notes

- The 429 test mounts a dedicated `limitedApp` with `max: 1` rather than sending 20 requests against the production limiter. This keeps the test deterministic, fast, and isolated from other tests running in the same process.
- `express-rate-limit` uses an in-memory store by default; the `limitedApp` instance is scoped to the single test function, so counter state does not bleed across tests.
- The `punycode` deprecation warning from Node.js is a transitive dependency of `supertest` / `vitest` and is not actionable at the application level.
