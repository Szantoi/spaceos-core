# CLAUDE.md — src/interpreter/

**Module:** Agentic loop, tool registry, system prompt, Kernel action dispatch
**This is the core of the Orchestrator.** All NLP → action translation happens here.

---

## What lives here

| File | Purpose |
|---|---|
| `tool-registry.ts` | 14 `ToolSchema` objects — the LLM's curated API surface |
| `system-prompt.ts` | `buildSystemPrompt(context)` — DesignPortal persona |
| `kernel.action.ts` | `executeToolCall()` — dispatch table: tool name → Kernel HTTP |
| `interpreter.service.ts` | `interpret()` — the agentic loop (max 5 iterations) |

---

## tool-registry.ts rules

- This is what the LLM sees — NOT the Swagger spec
- Every tool name must have a matching `case` in `kernel.action.ts`
- Descriptions must be LLM-friendly: explain the "when to use" not just the "what"
- Required fields must list every field the Kernel endpoint actually requires
- **Never expose** internal admin endpoints, raw SQL tools, or audit-write tools

## system-prompt.ts rules

- `buildSystemPrompt(context)` accepts `{ tenantId?, facilityId? }` — validated UUIDs only
- **User input NEVER enters the system prompt string** (OWASP LLM01 — Prompt Injection)
- The persona is fixed: DesignPortal assistant. Do not make it configurable via user input.

## kernel.action.ts rules

- `setKernelAuthToken(token)` called once per request — JWT forwarded from user
- The `dispatch()` switch is the **only place** Kernel API paths are constructed
- Every case maps 1:1 to a tool in `tool-registry.ts`
- Axios errors are caught and returned as `{ error: true, ... }` — never rethrow to LLM

## interpreter.service.ts rules

- `while (iterations < env.MAX_TOOL_ITERATIONS)` — hard cap, no exceptions
- Tool calls executed with `Promise.all()` — parallel, not sequential
- Message history is append-only within a single `interpret()` call
- Returns `ChatResponse` — the route handler never sees LLM internals

---

## Adding a new tool (checklist)

- [ ] Add `ToolSchema` to `tool-registry.ts`
- [ ] Add `case 'tool_name':` to `kernel.action.ts` dispatch
- [ ] Verify the Kernel endpoint exists in `kernel.types.ts`
- [ ] Add/update test in `tool-registry.test.ts`
- [ ] Update `interpreter.service.test.ts` if the new tool changes loop behavior
