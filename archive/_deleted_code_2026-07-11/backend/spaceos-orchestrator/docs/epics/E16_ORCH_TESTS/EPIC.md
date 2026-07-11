# Epic E16 — Unit & Integration Tests

**Priority:** 🟡 P2
**Status:** `CLOSED_DONE`
**Depends on:** E12 + E13 + E14 + E15

---

## Goal

Full test coverage on all modules. Every module has a companion `*.test.ts`.
`npm test` → 0 failed before any deploy.

---

## Scope

**In scope:**
- Unit tests: all interpreter files, all llm providers, all middleware
- Integration tests: `/bff/chat` end-to-end with MockProvider
- Integration tests: `/bff/health` with mocked Kernel
- Security tests: 401 on missing JWT, 422 on invalid body, 429 on rate limit

**Out of scope:**
- Real Anthropic API in CI (use mock)
- Load testing

---

## Acceptance Criteria

- [x] `interpreter.service.test.ts` → 5 tests pass
- [x] `tool-registry.test.ts` → 4 tests pass
- [x] `mock.provider.test.ts` → 2 tests pass
- [x] `chat.route.test.ts` → 401, 422, 200, 429 cases
- [x] `health.route.test.ts` → 200, 207 cases
- [x] `auth.middleware.test.ts` → valid/invalid/missing JWT
- [x] `npm test` → 0 failed

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | Route integration tests (chat + health) | `CLOSED_DONE` |
| T2 | Middleware unit tests | `CLOSED_DONE` |
| T3 | Security edge cases (401, 422, 429) | `CLOSED_DONE` |

---

## Definition of Done

- [x] All AC checked
- [x] `npm test` → 0 failed
- [x] Security checklist S1–S10 from Master Prompt verified
