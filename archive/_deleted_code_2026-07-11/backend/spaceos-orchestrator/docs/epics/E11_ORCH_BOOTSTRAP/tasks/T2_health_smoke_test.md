# Task: T2 — Health endpoint smoke test
**Epic:** E11
**Agent:** CODE
**Status:** CODE_REVIEW
**Module:** routes

---

## Context

Files to read before starting:
- `src/routes/health.route.ts` — GET /bff/health; returns { orchestrator, kernel, llmProvider, timestamp }; 200 when kernel reachable, 207 when unreachable
- `src/index.ts` — mounts healthRouter at /bff/health with no auth and no rate limiter
- `src/config/env.ts` — KERNEL_BASE_URL and LLM_PROVIDER
- `src/routes/CLAUDE.md` — supertest pattern for route tests

What already exists:
- `health.route.ts` is fully implemented
- No test file exists for health.route yet — this task creates it

---

## Acceptance Criteria

- [ ] `src/routes/health.route.test.ts` exists next to `health.route.ts`
- [ ] Test: kernel unreachable → HTTP 207, `{ orchestrator: "ok", kernel: "unreachable", llmProvider: "mock" }`
- [ ] Test: kernel reachable → HTTP 200, `{ orchestrator: "ok", kernel: "ok" }`
- [ ] Test: response always includes `timestamp` as valid ISO 8601 string
- [ ] Kernel HTTP call mocked via `vi.mock('axios')` — no real network calls
- [ ] `npm test` passes with new test file included
- [ ] `npm run build` still exits 0

---

## Test Requirements

| Test | Type | Description |
|------|------|-------------|
| `GET /bff/health` — kernel unreachable | Integration | axios throws; response 207, kernel: "unreachable" |
| `GET /bff/health` — kernel reachable | Integration | axios resolves; response 200, kernel: "ok" |
| `GET /bff/health` — timestamp present | Integration | body.timestamp matches ISO 8601 |
| `GET /bff/health` — llmProvider field | Integration | body.llmProvider equals env.LLM_PROVIDER ("mock") |

Test file: `src/routes/health.route.test.ts`
Use supertest against app export from `src/index.ts`.
Mock axios with `vi.mock('axios')`.

**Note:** `supertest` is not yet in package.json — add as devDependency before implementing.

---

## CLAUDE_ORCH.md Gates (auto-checked by REVIEW agent)

- [ ] ILlmProvider never bypassed — all LLM calls through the interface
- [ ] No Kernel URL hardcoded — always from env.KERNEL_BASE_URL
- [ ] No API keys in source code — always from env.*
- [ ] ConfigureAwait equivalent: no unhandled promise rejections
- [ ] Zod validation on every route input
- [ ] Error responses always use the centralized errorHandler
- [ ] No TODO/FIXME in committed code

---

## Definition of Done

- [ ] All AC checked
- [ ] `npm run build` → 0 errors
- [ ] `npm test` → 0 failed
- [ ] REVIEW_REPORT.md → CLOSED_DONE
