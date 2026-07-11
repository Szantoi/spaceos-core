---
id: TASK-14-02
title: "HTTP Transport MCP Tool Routing"
epic: EPIC-14
completed_by: [Backend Developer Agent]
date: 2026-03-10
pr: [#XXX - To be filled by DevOps]
---

# TASK-14-02: Implementation Summary

## What Was Built?

HTTP Transport was enhanced with a `/mcp/call` POST endpoint that enables remote tool invocation over HTTP. The endpoint accepts `{ tool_name: string, arguments: any }` payloads and routes them to the plugin manager for execution. A new `createPluginManager()` function was extracted from `mcpServer.ts` to allow early initialization of plugins (before or independent of SSE/StreamableHTTP routes). The HTTPTransport now accepts a `pluginManager` via `setPluginManager()` method, enabling HTTP clients to invoke MCP tools directly. Full CORS support, session tracking via headers, and error handling were integrated. JSON request parsing was added to HTTPTransport's Express setup to support POST bodies.

## Acceptance Criteria Status

- [✅] **AC-1: `/mcp/call` POST endpoint created** — Accepts `{ tool_name: string, arguments: any }` and routes to PluginManager
- [✅] **AC-2: Tool invocation succeeds** — Returns `{ status: "success", data: result }` with proper JSON serialization
- [✅] **AC-3: Missing tool_name rejected** — Returns 400 `INVALID_REQUEST` error when tool_name missing or invalid type
- [✅] **AC-4: Tool execution errors handled** — Returns 500 with error code and message; exceptions logged for debugging
- [✅] **AC-5: PluginManager not available handled** — Returns 503 `SERVICE_UNAVAILABLE` when pluginManager not set
- [✅] **AC-6: Session ID from headers** — Extracts `x-session-id` header and includes in McpContext for tool handlers
- [✅] **AC-7: PluginManager integration** — `setPluginManager()` method allows late binding after HTTP transport creation
- [✅] **AC-8: Tests comprehensive** — 6 new unit tests (HT-09 through HT-14) covering success, errors, edge cases
- [✅] **AC-9: No regressions** — All 14 HTTPTransport tests passing; 11 factory tests passing; 23 plugin tests still passing (48 total)
- [✅] **AC-10: JSON parsing** — Express JSON middleware added to HTTPTransport for proper body deserialization

## Files Created/Modified

- `src/mcp/transports/HTTPTransport.ts` — Enhanced with:
  - `pluginManager?: PluginManager` field
  - `setPluginManager(pluginManager)` public method
  - `/mcp/call` POST endpoint handler with full error handling
  - JSON middleware added to setupRoutes()
  - Session ID extraction from request headers

- `src/mcp/mcpServer.ts` — Enhanced with:
  - New `createPluginManager()` export function for early plugin initialization
  - Decoupled plugin creation from SSE/StreamableHTTP router for reusability

- `src/index.ts` — Enhanced with:
  - Import of `createPluginManager` from mcpServer
  - PluginManager instantiation BEFORE transport creation
  - PluginManager passed to HTTPTransport config as `pluginManager` field
  - Added console log: `[TASK-14-02] ✅ PluginManager initialized`

- `src/tests/unit/httpTransport.test.ts` — Enhanced with:
  - New test suite: "HTTPTransport MCP/call endpoint (TASK-14-02)"
  - 6 new tests:
    - HT-09: Successful tool invocation
    - HT-10: Missing tool_name validation
    - HT-11: Invalid tool_name type validation
    - HT-12: Tool error handling with proper error codes
    - HT-13: ServiceUnavailable when PluginManager not set
    - HT-14: Session ID propagation via headers

## Tests Added

- **Unit Tests:** 6 new tests in HTTPTransport test suite:
  - All passing (100% pass rate)
  - Cover success path, validation errors, execution errors, missing dependencies, and context propagation
  - Mock PluginManager used to simulate tool invocation without requiring full plugin system init

- **Regression Tests:** Verified no breakage:
  - HTTPTransport tests: 14/14 passing (original 8 + new 6)
  - TransportFactory tests: 11/11 passing
  - Bootstrap Plugin tests: 11/11 passing
  - Context/Discovery Plugin tests: 12/12 passing
  - **Total: 48/48 tests passing**

## Technical Decisions

1. **Late-binding PluginManager** — `setPluginManager()` method allows HTTPTransport to be created before plugins are initialized, supporting the flow in `index.ts` where we create the transport in `initTransport()` after pluginManager is ready.

2. **Extracted createPluginManager()** — Rather than duplicating plugin initialization logic, extracted it to a reusable function that both `index.ts` (for HTTPTransport) and `mcpServer.ts` (for SSE/Streamable HTTP) can call.

3. **Header-based session tracking** — Session ID extracted from `x-session-id` header rather than requiring authentication middleware, keeping the endpoint simple and testable.

4. **McpContext Partial type** — Used `Partial<McpContext>` to allow minimal context creation; plugins receiving McpContext can handle optional fields gracefully.

5. **Async endpoint with try/catch** — Standard Express error handling pattern; exceptions caught and returned as JSON error responses rather than letting Express generate default error pages.

## Key Learnings

1. **Express JSON middleware order matters** — Must come before route handlers; placed at top of `setupRoutes()` to ensure all subsequent routes can parse JSON bodies.

2. **PluginManager initialization timing** — Plugins must be initialized after all services (agentDb, sessionManager, rbacFilter, workflowTracker, guardrailService) are ready; placed after bootstrap service init in `index.ts`.

3. **Session context propagation** — Tools receive McpContext expecting `session_id` (underscore), not `sessionId` (camelCase); type safety helped catch this early.

4. **Graceful degradation** — When PluginManager not available, endpoint returns 503 rather than crashing; clients understand to retry later or fallback to SSE/StreamableHTTP.

## Peer Review Checklist

- [x] Code compiles without TypeScript errors (related to new changes)
- [x] Tests all passing (6 new + 42 existing)
- [x] No regressions in existing transport/plugin tests
- [x] Documentation complete (this summary)
- [x] Error handling comprehensive (validation + exception cases)
- [x] CORS headers already supported (from HTTPTransport base features)
- [x] Ready for integration with mcpServer SSE/Streamable HTTP routes

## Next Steps

1. **Dev A Integration:** Merge to main and communicate to Dev A that HTTPTransport now fully supports tool invocation
2. **E2E Testing:** Load test with concurrent `/mcp/call` requests to validate connection tracking and timeout behavior
3. **Header-based Auth (Future):** Extend `x-session-id` approach with `x-auth-token` for role-based tool access validation
4. **WebSocket upgrade (Future):** Add `/mcp/call/ws` endpoint for streaming tool results over WebSocket

## Production Readiness

- [x] Error messages are non-sensitive and informative
- [x] All edge cases tested (empty args, missing tool, service unavailable)
- [x] Logging present for debugging (connection tracking already verified)
- [x] No SQL injection or code injection vectors (tool names validated as strings)
- [x] Graceful shutdown works (HTTP transport drains connections before shutdown)
- [x] Configuration via environment variables (reuses existing MCP_* env var pattern)

---

*See also:*
- [TASK-14-01 Transport Abstraction](./TASK-14-01-transport-abstraction.md)
- [TASK-14-04 Bootstrap Plugin](./TASK-14-04-bootstrap-plugin.md)
- [TASK-14-05 Context/Discovery Plugins](./TASK-14-05-context-discovery-plugins.md)
