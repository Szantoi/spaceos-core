# Task T2 — getLlmProvider Factory Edge Cases

**Epic:** E12 — LLM Provider Abstraction
**Type:** Automated unit tests
**Status:** `BACKLOG_READY`
**Test file:** `src/llm/llm.provider.test.ts`

---

## Goal

Verify that `getLlmProvider()` handles all configured provider variants correctly, including failure modes.

---

## Edge Cases Under Test

| # | Scenario | Expected |
|---|----------|----------|
| 1 | `LLM_PROVIDER=openai` | Throws `"not yet implemented"` |
| 2 | `LLM_PROVIDER=anthropic`, no `ANTHROPIC_API_KEY` | `AnthropicProvider` constructor throws |
| 3 | `LLM_PROVIDER=mock` | Returns a `MockProvider` instance |

---

## Implementation Notes

- `getLlmProvider()` uses a module-level `_instance` singleton — modules must be reset between tests
- Use `vi.resetModules()` in `beforeEach` and dynamic `import()` per test to bypass the singleton
- Use `vi.stubEnv()` to set env vars per test without mutating `process.env` directly
- The `env` object in `config/env.ts` is parsed at module load time — `vi.resetModules()` ensures a fresh parse on each dynamic import

---

## Test File

`src/llm/llm.provider.test.ts`

---

## Acceptance Criteria

- [ ] Test 1: `LLM_PROVIDER=openai` → `getLlmProvider()` rejects with message matching `"not yet implemented"`
- [ ] Test 2: `LLM_PROVIDER=anthropic` + no `ANTHROPIC_API_KEY` → `getLlmProvider()` rejects (constructor throws)
- [ ] Test 3: `LLM_PROVIDER=mock` → `getLlmProvider()` resolves to an instance of `MockProvider`
- [ ] `npm test` → all three tests pass alongside existing `mock.provider.test.ts`
- [ ] No `TODO`/`FIXME` in the test file

---

## Run Command

```bash
npm test
```

Expected output: `0 failed` across all test files.
