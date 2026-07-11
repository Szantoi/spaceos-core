# T1 — Single-Turn No-Tool Flow

**Epic:** E14 — Interpreter Service (Agentic Loop)
**Status:** `CLOSED_DONE`
**File:** `src/interpreter/interpreter.service.test.ts`

---

## Coverage Provided

This task is validated by the existing `interpreter.service.test.ts` test suite.

### Test: "returns the LLM reply directly when no tools are called"

**Scenario:** User sends a single message. LLM responds with `finishReason: 'stop'` and no tool calls.

**Assertions:**
- `result.reply` equals the LLM content string
- `result.toolsUsed` has length 0
- `result.iterations` equals 1

**Mock strategy:**
- `getLlmProvider` mocked via `vi.mock('../llm/llm.provider')`
- `executeToolCall` mocked via `vi.mock('./kernel.action')` — no HTTP calls
- Single `LlmResponse` with `finishReason: 'stop'` and empty `toolCalls`

---

### Test: "returns fallback message when LLM returns null content with no tools"

**Scenario:** LLM returns `content: null` with `finishReason: 'stop'` and no tool calls.

**Assertions:**
- `result.reply` equals `'(No response generated)'`

**Purpose:** Guards against null content propagating to the HTTP response as an invalid JSON field.

---

### Test: "injects tenantId context into the system prompt"

**Scenario:** Request body includes `context.tenantId`. The system prompt builder is verified to receive it.

**Assertions:**
- Captured `systemPrompt` string contains the tenantId UUID

**Purpose:** Ensures `buildSystemPrompt()` is called with context and that the LLM receives scoped context — OWASP LLM01 compliant (no user input in system prompt).

---

## Definition of Done

- [x] All three tests pass in `npm test`
- [x] No real HTTP calls (kernel.action fully mocked)
- [x] No real LLM calls (llm.provider fully mocked)
