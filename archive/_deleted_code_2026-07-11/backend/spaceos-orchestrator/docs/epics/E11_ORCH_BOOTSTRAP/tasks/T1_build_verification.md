# Task: T1 — npm install + build verification
**Epic:** E11
**Agent:** CODE
**Status:** CODE_REVIEW
**Module:** config

---

## Context

Files to read before starting:
- `package.json` — lists all approved dependencies and npm scripts
- `tsconfig.json` — strict mode, CommonJS output, rootDir=src, outDir=dist
- `src/config/env.ts` — Zod-validated env; startup fails fast if vars are missing
- `.env.example` — canonical list of required env vars
- `src/index.ts` — Bootstrap entry point; imports all modules that must compile

What already exists:
- All source files are in place under `src/` (routes, middleware, proxy, interpreter, llm, types, config)
- Three test suites already exist: `mock.provider.test.ts`, `tool-registry.test.ts`, `interpreter.service.test.ts`

This task has no new logic to write. Its only purpose is to confirm the scaffold compiles cleanly and all existing tests pass.

---

## Acceptance Criteria

- [ ] `npm install` completes with 0 errors
- [ ] `npm run build` exits with code 0 and produces output in `dist/`
- [ ] TypeScript compiler reports 0 errors and 0 warnings
- [ ] `npm test` exits with code 0
- [ ] All 3 existing test suites pass: `mock.provider.test.ts`, `tool-registry.test.ts`, `interpreter.service.test.ts`
- [ ] `.env` copied from `.env.example` with `LLM_PROVIDER=mock`, valid `JWT_SIGNING_KEY` (≥32 chars), `KERNEL_BASE_URL=http://localhost:5000`
- [ ] `npm run dev` starts without throwing

---

## Test Requirements

| Test | Type | Description |
|------|------|-------------|
| `mock.provider.test.ts` (existing) | Unit | MockProvider echoes last user message; handles non-string content |
| `tool-registry.test.ts` (existing) | Unit | DESIGN_PORTAL_TOOLS coverage: aggregates present, descriptions non-empty, required fields in properties, names unique |
| `interpreter.service.test.ts` (existing) | Unit | interpret() direct reply, single tool call, MAX_TOOL_ITERATIONS cap, tenantId injection, null-content fallback |

No new test files required for T1.

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
