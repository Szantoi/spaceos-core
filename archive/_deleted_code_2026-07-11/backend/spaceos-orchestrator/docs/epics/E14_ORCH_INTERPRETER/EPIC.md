# Epic E14 — Interpreter Service (Agentic Loop)

**Priority:** 🔴 P1
**Status:** `CLOSED_DONE`
**Depends on:** E12 + E13
**Blocks:** E16

---

## Goal

`interpret()` correctly runs the agentic loop: user message → LLM → tool calls → Kernel → LLM → reply.
The loop respects `MAX_TOOL_ITERATIONS` and never runs indefinitely.

---

## Scope

**In scope:**
- Single-turn: user message → LLM stop → return reply
- Multi-turn with tools: LLM tool_use → executeToolCall → tool_result → LLM stop → return reply
- Loop cap: `MAX_TOOL_ITERATIONS` respected
- Context injection: `tenantId`/`facilityId` appear in system prompt
- Parallel tool execution: `Promise.all()` for multiple tool calls in one turn

**Out of scope:**
- Streaming SSE (future enhancement)
- Conversation memory across multiple HTTP requests

---

## Acceptance Criteria

- [x] `interpreter.service.test.ts` → all 5 tests pass
- [x] Real end-to-end: "list all tenants" → `get_all_tenants` tool called → Kernel responds → reply returned
- [x] Loop cap: if LLM keeps calling tools, stops at `MAX_TOOL_ITERATIONS` with graceful message
- [x] Context: `tenantId` in request → appears in system prompt sent to LLM
- [x] `toolsUsed` array in response lists every tool name called

---

## Tasks

| Task | Title | Status |
|------|-------|--------|
| T1 | Single-turn no-tool flow | `CLOSED_DONE` |
| T2 | Multi-turn tool flow | `CLOSED_DONE` |
| T3 | Chat route tests | `CLOSED_DONE` |

---

## Definition of Done

- [x] All AC checked
- [x] `npm run build` → 0 errors
- [x] `npm test` → 0 failed
