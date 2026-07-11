# CLAUDE.md — src/llm/

**Module:** LLM provider adapters
**Golden Rule:** `ILlmProvider` is the only LLM entry point. No other file may call any LLM SDK directly.

---

## What lives here

| File | Purpose |
|---|---|
| `llm.provider.ts` | `getLlmProvider()` singleton factory — reads `env.LLM_PROVIDER` |
| `anthropic.provider.ts` | Claude Sonnet via `@anthropic-ai/sdk` |
| `mock.provider.ts` | Echo mock — no API key, used in dev and tests |
| `openai.provider.ts` | _(not yet implemented)_ — add when needed |

## Rules

- Every provider implements `ILlmProvider` from `../types/llm.types`
- `getLlmProvider()` returns the singleton — call it once per request cycle
- Provider constructors must throw if required API key is missing
- `complete()` must never throw to the caller — catch SDK errors and return `finishReason: 'error'`
- Model string is hardcoded per provider — never accept it from user input

## Adding a new provider

1. Create `[name].provider.ts` implementing `ILlmProvider`
2. Add the provider name to `env.ts` `LLM_PROVIDER` enum
3. Add a `case` in `llm.provider.ts` factory switch
4. Add to `.env.example` with a comment
5. Write at least one unit test

## Test pattern

```typescript
// mock the factory, never the SDK directly
vi.mock('../llm/llm.provider', () => ({
  getLlmProvider: vi.fn().mockResolvedValue(mockProvider),
}));
```
