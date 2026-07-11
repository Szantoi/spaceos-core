# Epic E12 — LLM Provider Abstraction

**Priority:** 🔴 P1
**Status:** `CLOSED_DONE`
**Depends on:** E11
**Blocks:** E14

---

## Goal

Verify that `ILlmProvider` is correctly abstracted and the Anthropic provider works end-to-end.
The Mock provider must remain functional for tests.

---

## Scope

**In scope:**
- `AnthropicProvider.complete()` makes a real API call and returns `LlmResponse`
- `MockProvider.complete()` echoes input — used in all tests
- `getLlmProvider()` factory switches correctly on `LLM_PROVIDER` env var
- No tool calls in this epic — simple text completion only

**Out of scope:**
- Tool calling (E13 + E14)
- OpenAI provider (future)

---

## Acceptance Criteria

- [ ] `ANTHROPIC_API_KEY=real-key npm run dev` → Anthropic responds to a simple prompt
- [ ] `LLM_PROVIDER=mock` → MockProvider returns echo without API call
- [ ] `LLM_PROVIDER=openai` → throws clear error "not yet implemented"
- [ ] Missing `ANTHROPIC_API_KEY` with `LLM_PROVIDER=anthropic` → fail-fast at startup
- [ ] `npm test` → `mock.provider.test.ts` passes

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | AnthropicProvider manual smoke test | `CLOSED_DONE` |
| T2 | getLlmProvider factory edge cases | `CLOSED_DONE` |

---

## Definition of Done

- [ ] All AC checked
- [ ] `npm run build` → 0 errors
- [ ] `npm test` → 0 failed
