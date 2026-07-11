# Epic E11 — Project Bootstrap & Health

**Priority:** 🔴 P1
**Status:** `CLOSED_DONE`
**Blocks:** E12, E13, E14, E15, E16, E17

---

## Goal

Verify the existing scaffold compiles, tests pass, and the health endpoint responds.
This is the "walking skeleton" confirmation — no new logic, only wiring.

---

## Scope

**In scope:**
- `npm install` clean run
- `npm run build` → 0 TypeScript errors
- `npm test` → all existing tests pass (mock.provider, tool-registry)
- `GET /bff/health` responds `200` with `{ orchestrator: "ok", kernel: "...", llmProvider: "mock" }`
- `LLM_PROVIDER=mock` in `.env` for local dev

**Out of scope:**
- Real LLM call (E12)
- Real Kernel connection (E15)
- Rate limiting tuning (E17)

---

## Acceptance Criteria

- [ ] `npm install` completes without errors
- [ ] `npm run build` → 0 TypeScript errors, 0 warnings
- [ ] `npm test` → 0 failed (existing test files pass)
- [ ] `npm run dev` starts without exception
- [ ] `GET /bff/health` → `200 { orchestrator: "ok" }`
- [ ] `.env` copied from `.env.example`, `LLM_PROVIDER=mock` works

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | npm install + build verification | `BACKLOG_READY` |
| T2 | Health endpoint smoke test | `BACKLOG_READY` |

---

## Definition of Done

- [ ] All AC checked
- [ ] `npm run build` → 0 errors
- [ ] `npm test` → 0 failed
- [ ] BACKLOG.md status updated to `CLOSED_DONE`
