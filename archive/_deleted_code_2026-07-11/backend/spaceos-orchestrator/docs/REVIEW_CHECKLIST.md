# SpaceOS.Orchestrator — Review Checklist

> Machine-readable checklist for the REVIEW phase.
> Every item maps to a CLAUDE.md rule. Violation → fix in place → log in REVIEW_REPORT.md.

---

## SECURITY (run before every CLOSED_DONE)

| ID | Rule | How to verify |
|----|------|---------------|
| S1 | User input never injected into system prompt string | Grep `buildSystemPrompt` — only validated UUIDs injected |
| S2 | JWT verified with `jwt.verify()` — never `jwt.decode()` | Grep `jwt.decode` in middleware/ |
| S3 | No API keys in source | Grep `sk-ant\|sk-\|Bearer ` in non-.env files |
| S4 | Kernel port not exposed — nginx proxies /bff/* only | Check `index.ts` listens on `127.0.0.1` |
| S5 | Tool registry is curated — no raw SQL or admin tools | Review `tool-registry.ts` tool descriptions |
| S6 | MAX_TOOL_ITERATIONS enforced | Grep `MAX_TOOL_ITERATIONS` in interpreter.service.ts |
| S7 | Rate limiting on /bff/chat (20/min) and /bff/api (100/min) | Check `index.ts` rate limiters |
| S8 | CORS origin explicit — no wildcard `*` in production | Check `cors({ origin: [...] })` in index.ts |
| S9 | Express listens on `127.0.0.1` only | Grep `app.listen` — must have `'127.0.0.1'` |
| S10 | Axios Kernel client has `timeout: 10_000` | Check axios create in `kernel.action.ts` |

---

## CONFIG

| ID | Rule | How to verify |
|----|------|---------------|
| C1 | Every env var used in code exists in `.env.example` | Diff `env.ts` schema vs `.env.example` |
| C2 | No `process.env.*` access outside `config/env.ts` | Grep `process.env` outside config/ |
| C3 | `env.ts` uses `safeParse` with `process.exit(1)` on failure | Check envSchema.safeParse pattern |

---

## TYPES

| ID | Rule | How to verify |
|----|------|---------------|
| T1 | `kernel.types.ts` mirrors match C# DTOs | Cross-check with Codebase_Status.md DTO section |
| T2 | No `any` type used | `grep -r ': any' src/` |
| T3 | `ILlmProvider` interface unchanged if adding a provider | Diff interface — must not grow |

---

## LLM MODULE

| ID | Rule | How to verify |
|----|------|---------------|
| L1 | No direct Anthropic SDK call outside `anthropic.provider.ts` | Grep `@anthropic-ai/sdk` outside llm/ |
| L2 | `complete()` catches SDK errors — never rethrows | Check try/catch in all provider `complete()` |
| L3 | Model string hardcoded in provider — not from user input | Check AnthropicProvider constructor |

---

## INTERPRETER

| ID | Rule | How to verify |
|----|------|---------------|
| I1 | `interpret()` uses `getLlmProvider()` — not SDK directly | Grep `import Anthropic` in interpreter/ |
| I2 | `kernel.action.ts` is only file with `/api/` paths | Grep `KERNEL_BASE_URL` — only in kernel.action.ts |
| I3 | Every tool in registry has a matching dispatch `case` | Compare tool names in tool-registry.ts vs switch cases |
| I4 | `Promise.all()` for parallel tool execution | Check interpreter.service.ts tool execution loop |
| I5 | Agentic loop has `< env.MAX_TOOL_ITERATIONS` guard | Verify while condition |

---

## ROUTES & MIDDLEWARE

| ID | Rule | How to verify |
|----|------|---------------|
| R1 | Every route: `try { } catch (err) { next(err); }` | Grep route files |
| R2 | Every `req.body` validated with Zod `safeParse` | Grep `req.body` — must follow safeParse |
| R3 | `requireAuth` applied to `/bff/chat` and `/bff/api/*` | Check index.ts route registration |
| R4 | `errorHandler` is last middleware in `index.ts` | Check position of `app.use(errorHandler)` |

---

## GENERAL

| ID | Rule | How to verify |
|----|------|---------------|
| G1 | No TODO/FIXME | `grep -r 'TODO\|FIXME' src/` |
| G2 | `npm run build` → 0 TypeScript errors | Run build |
| G3 | `npm test` → 0 failed | Run test |
| G4 | `.env` not committed | `git status` — .env must not appear |
| G5 | `.env.example` up to date | Every var in `env.ts` present in `.env.example` |

---

## REVIEW_REPORT.md Template

```markdown
# Review Report — [TASK_ID]
**Date:** YYYY-MM-DD
**Agent:** REVIEW
**Final status:** CLOSED_DONE | REVIEW_FAILED

## Violations Found & Fixed

| # | File | Violation | Fix Applied |
|---|------|-----------|-------------|
| 1 | src/routes/chat.route.ts | Missing try/catch | Added try/catch → next(err) |

## Unfixable Violations

| # | File | Issue | Why unfixable |
|---|------|-------|---------------|

## Build & Test Result
- Build: ✅ 0 TypeScript errors
- Tests: ✅ [N] passing, 0 failed
- Security: ✅ S1–S10 all passed
```
