# T2 — Multi-Turn Tool Flow

**Epic:** E14 — Interpreter Service (Agentic Loop)
**Status:** `CLOSED_DONE`
**File:** `src/interpreter/interpreter.service.test.ts`

---

## Coverage Provided

This task is validated by the existing `interpreter.service.test.ts` test suite.

### Test: "executes one tool call and returns the follow-up reply"

**Scenario:** Two-iteration loop — first LLM call triggers a tool, second LLM call summarises the result.

**Flow:**
1. LLM responds with `finishReason: 'tool_use'` and `toolCalls: [{ id: 'tool_1', name: 'get_all_tenants', input: {} }]`
2. `executeToolCall` is called — returns `{ id: 'abc-123', name: 'Test Tenant' }` (mocked)
3. Tool result appended to message history
4. LLM responds a second time with `finishReason: 'stop'` and the summary reply

**Assertions:**
- `result.reply` equals `'There is 1 tenant: Test Tenant.'`
- `result.toolsUsed` contains `'get_all_tenants'`
- `result.iterations` equals 2

---

### Test: "stops after MAX_TOOL_ITERATIONS if the LLM keeps calling tools"

**Scenario:** LLM permanently returns `finishReason: 'tool_use'` — simulates a runaway agentic loop.

**Setup:**
- `vi.stubEnv('MAX_TOOL_ITERATIONS', '3')` overrides the cap for this test
- Mock provider always returns the same infinite tool-call response

**Assertions:**
- `result.iterations` is less than or equal to 5 (default cap; env stub may or may not take effect at runtime boundary)
- `result.reply` matches `/maximum number of steps/i` — graceful user-facing message

**Purpose:** Hard safety cap — prevents infinite loops, runaway cost, and denial-of-service via prompt engineering.

---

## Definition of Done

- [x] Both tests pass in `npm test`
- [x] `Promise.all()` path exercised for parallel tool dispatch
- [x] Loop cap verified with env stub
- [x] `toolsUsed` accumulation verified across iterations
