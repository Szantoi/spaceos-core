# T1 — Route Integration Tests

**Epic:** E16_ORCH_TESTS
**Status:** `CLOSED_DONE`
**Last updated:** 2026-03-28

---

## Scope

Integration tests for all Express route handlers using `supertest` against isolated test apps. Routes are mounted without the production rate limiter so each test targets one concern.

---

## Files Covered

| File | Test file | Tests |
|------|-----------|-------|
| `src/routes/chat.route.ts` | `src/routes/chat.route.test.ts` | 5 |
| `src/routes/health.route.ts` | `src/routes/health.route.test.ts` | 4 |

---

## chat.route.test.ts — Coverage

| # | Test | Status code asserted |
|---|------|----------------------|
| 1 | No Authorization header | 401 |
| 2 | Invalid JWT | 401 |
| 3 | Valid JWT + empty messages array | 422 |
| 4 | Valid JWT + valid body | 200 |
| 5 | Rate limit exceeded (limiter max: 1) | 429 |

**Mocks:** `interpreter.service.interpret` is mocked via `vi.mock` to return `{ reply: 'Hello', toolsUsed: [], iterations: 1 }`.
**Auth:** `requireAuth` middleware is exercised in every test — it reads `JWT_SIGNING_KEY` from the vitest env override (`test-secret-key-minimum-32-characters-long`).

---

## health.route.test.ts — Coverage

| # | Test | Status code asserted |
|---|------|----------------------|
| 1 | Kernel unreachable (ECONNREFUSED) | 207 |
| 2 | Kernel reachable | 200 |
| 3 | `timestamp` field is ISO 8601 | 200 |
| 4 | `llmProvider` field equals "mock" | 200 |

**Mocks:** `axios` is fully mocked via `vi.mock('axios')`. No real HTTP calls are made.

---

## Test Patterns Applied

- Each test file creates its own lightweight `express()` app — no dependency on `src/index.ts`.
- `beforeEach(() => vi.clearAllMocks())` ensures mock call counts do not leak between tests.
- All assertions use `expect(res.status).toBe(N)` and `expect(res.body).toMatchObject(...)`.

---

## Definition of Done

- [x] `chat.route.test.ts` — 5 tests passing
- [x] `health.route.test.ts` — 4 tests passing
- [x] No real network calls or LLM API calls in any test
