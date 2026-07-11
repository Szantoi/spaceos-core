---
id: TASK-14-01
title: "TASK-14-01 — Transport Abstraction: Stdio + HTTP Factory Pattern"
epic: EPIC-14
milestone: M02
phase: "Phase 1 Foundation (Layer 1)"
created: 2026-03-09
type: "task-assignment"
status: "🟡 READY (Task specs finalized 2026-03-09)"
assignee: "Dev A"
effort: "15 hours"
duration: "4 calendar days (2026-03-19 Wed → 2026-03-22 Sat EOD)"
blocker: "None — critical path task"
blocks: ["TASK-14-02 (HTTP Transport)", "Tool handlers (all tasks)"]
ac_count: 15
priority: "P0 (Enterprise architecture, unblocks all)"
---

# TASK-14-01: Transport Abstraction — Stdio + HTTP Factory Pattern

## 📋 Problem Statement

The MCP server currently hardcodes `StdioServerTransport` in initialization. To support modern deployment patterns (CLI via stdio, remote deployment via HTTP), we need:

1. **Transport abstraction layer** — ITransport interface supporting multiple transport types
2. **Factory pattern** — Create transport based on environment variable (MCP_TRANSPORT)
3. **Backward compatibility** — Existing tools work unchanged via both transports
4. **Error handling** — Graceful shutdown, configuration validation
5. **No tool handler changes** — Tools stay transport-agnostic

**This is the foundation for EPIC-14 Phase 1. All other tasks depend on this transport layer.**

---

## ✅ Acceptance Criteria (15 Total)

### AC-1 through AC-5: Transport Abstraction Core

**AC-1: Default Transport (Backward Compatibility)**

- Given: Environment variable `MCP_TRANSPORT` not set
- When: Server starts with `npm start`
- Then: StdioServerTransport is used (default, preserves existing behavior)
- *Validation:* Integration test verifies stdio listener active by default

**AC-2: Environment Variable Configuration**

- Given: `MCP_TRANSPORT=stdio` environment variable set
- When: Server initializes via factory
- Then: StdioServerTransport instance created and active
- Then: No error; explicit stdio works
- *Validation:* Unit test with env var mocking

**AC-3: HTTP Transport Flag**

- Given: `MCP_TRANSPORT=http` environment variable set
- When: Server initializes
- Then: StreamableHTTPServerTransport instance created
- Then: Transport listens on configured port (default 3000)
- *Validation:* Unit test verifies HTTP transport creation

**AC-4: Port Configuration for HTTP**

- Given: `MCP_TRANSPORT=http` and `MCP_PORT=3001` set
- When: Server starts
- Then: HTTP server listens on port 3001 (not default 3000)
- Then: HTTP server responds to requests on :3001
- *Validation:* Integration test with custom port

**AC-5: Factory Method Type Safety**

- Given: `createTransport(type: string)` factory method
- When: Called with valid type ("stdio" | "http")
- Then: Returns ITransport implementation matching type
- Then: TypeScript strict mode: no `any` types in return
- *Validation:* Unit tests for both types + TypeScript compilation

---

### AC-6 through AC-10: Tool Compatibility & Error Handling

**AC-6: No Tool Handler Regressions**

- Given: Existing tool (e.g., bootstrap_agent) with known behavior
- When: Tool invoked via stdio transport (unchanged from before refactoring)
- Then: Response identical to pre-refactoring (regression test)
- When: Same tool invoked via HTTP transport
- Then: Response identical (cross-transport consistency)
- *Validation:* Regression test compares tool responses across transports

**AC-7: Transport-Agnostic Tool Handlers**

- Given: Tool handler function with transportation-specific assumptions (e.g., `req.type === "stdio"`)
- When: Code reviewed
- Then: No transport-specific logic in tool handlers
- Then: handlers use RequestContext (transport-independent)
- *Validation:* Code review + linter rule (no `transport` field in handlers)

**AC-8: Graceful Shutdown — Stdio**

- Given: Stdio transport active and server running
- When: SIGTERM signal received
- Then: Gracefully closes stdio connection
- Then: Shutdown completes in < 500ms
- Then: Pending requests allowed to finish (< 5s timeout)
- *Validation:* E2E test with signal injection

**AC-9: Graceful Shutdown — HTTP**

- Given: HTTP transport active with in-flight requests
- When: SIGTERM received
- Then: New requests rejected with 503 Unavailable
- Then: In-flight requests allowed to complete (< 2s deadline)
- Then: After all pending complete, HTTP listener closed
- Then: Total shutdown time < 5s
- *Validation:* E2E test simulating concurrent requests + signal

**AC-10: Configuration Error Handling**

- Given: `MCP_TRANSPORT=invalid` (unknown transport type)
- When: Server starts
- Then: Throws ConfigurationError with message: "Unknown transport type: invalid"
- Then: No ambiguous error (server doesn't start with wrong transport)
- *Validation:* Unit test expects ConfigurationError

---

### AC-11 through AC-15: Interface Design & State Management

**AC-11: Transport Interface Consistency**

- Given: ITransport interface defined with required methods
- When: StdioServerTransport + StreamableHTTPServerTransport implementations checked
- Then: Both fully implement ITransport (no "partial" implementations)
- Then: All methods return Promise<T> (async consistency)
- *Validation:* TypeScript interface satisfaction checks

**AC-12: Type Safety — TypeScript Strict Mode**

- Given: All transport code
- When: TypeScript compiler runs with strict mode
- Then: No implicit `any`
- Then: All function parameters typed
- Then: No type assertion (`as any`)
- *Validation:* TypeScript strict compilation passes; tslint clean

**AC-13: Connection State Machine**

- Given: Transport lifecycle (init → connect → connected → disconnect)
- When: State queried via `transport.getState()` at each stage
- Then: Returns: "DISCONNECTED" | "CONNECTING" | "CONNECTED"
- Then: Invalid transitions rejected (e.g., can't disconnect twice)
- *Validation:* State machine unit test

**AC-14: Error Recovery Without Singleton Poison**

- Given: Initial transport creation fails (e.g., port 3000 in use)
- When: Manual retry via `createTransport()` again
- Then: New attempt succeeds (factory not poisoned by first failure)
- Then: No global state corruption
- *Validation:* Unit test simulating failed init + retry

**AC-15: RequestContext Propagation**

- Given: HTTP request with Authorization header
- When: RequestContext middleware applied before transport
- Then: Tool handlers receive context with auth info
- Given: Stdio transport receives stdin request
- When: RequestContext middleware applied
- Then: Tool handlers receive context (consistent interface)
- *Validation:* Integration test verifies context available in both transports

---

## 📂 Deliverables

### Code Files to Create/Modify

| File | Type | Purpose | Status |
|:-----|:-----|:--------|:-------|
| `src/mcp/transport/ITransport.ts` | ✅ CREATE | Interface definition | New |
| `src/mcp/transport/factory.ts` | ✅ CREATE | Factory + environment config | New |
| `src/mcp/transport/index.ts` | ✅ CREATE | Public exports | New |
| `src/mcp/mcpServer.ts` | ✅ MODIFY | Use factory instead of hardcoded transport | Existing |
| `src/mcp/transport/StdioServerTransport.ts` | ⏳ REFACTOR | Ensure ITransport compliance | May exist or be new |
| `src/tests/unit/transport-abstraction.test.ts` | ✅ CREATE | AC-1 through AC-12 unit tests | New |
| `src/tests/integration/transport-lifecycle.test.ts` | ✅ CREATE | AC-8, AC-9 shutdown tests | New |
| `src/tests/e2e/transport-regression.e2e.test.ts` | ✅ CREATE | AC-6 tool consistency E2E | New |

### Documentation Files

| File | Type | Purpose |
|:-----|:-----|:--------|
| `IMPLEMENTATION-SUMMARY.md` | Doc | Dev A completion report (to fill after implementation) |
| `TASK-14-01-KICKOFF.md` | Doc | Implementation roadmap (companion file) |

---

## 🔗 Dependencies & Critical Path

### Depends On (Blocking Tasks)

**None** — This is Layer 1 Foundation. Unblocked, can start immediately.

### Blocks (Downstream Tasks)

| Task | Component | Reason |
|:-----|:----------|:-----:|
| **TASK-14-02** | HTTP Transport | Depends on ITransport interface |
| **All tool handlers** | Tool routing | Handlers must be transport-agnostic |
| **TASK-14-08** | Resource templates | Resource loading via transport |

---

## 🧪 Testing Strategy

### Unit Tests (80%+ coverage required)

| Test | AC | Coverage | Mock Strategy |
|:-----|:---|:---------|:------|
| `test_default_transport_stdio` | AC-1 | Default behavior | Env var mock |
| `test_env_var_stdio_explicit` | AC-2 | Explicit stdio flag | Env var mock |
| `test_http_transport_creation` | AC-3 | HTTP factory | No mock (real HTTP still off) |
| `test_http_port_config` | AC-4 | Port override | Env var mock |
| `test_factory_type_safety` | AC-5 | Factory return type | TypeScript check |
| `test_invalid_transport_error` | AC-10 | Invalid type handling | Error expectation |
| `test_interface_compliance` | AC-11 | ITransport satisfied | Interface check |
| `test_connection_state_machine` | AC-13 | State transitions | FSM validation |
| `test_error_recovery_retry` | AC-14 | No poison on failure | Retry after failure |

### Integration Tests (Tool Compatibility)

| Test | AC | Scenario |
|:-----|:---|:----------|
| `test_tool_regression_stdio` | AC-6a | bootstrap_agent via stdio (before vs after) |
| `test_tool_consistency_cross_transport` | AC-6b | Same tool, both transports, identical response |
| `test_context_propagation_stdio` | AC-15a | RequestContext available in stdio |
| `test_context_propagation_http` | AC-15b | RequestContext available in HTTP |

### E2E Tests (Full Workflow)

| Test | AC | Workflow |
|:-----|:---|----------|
| `test_shutdown_signal_stdio` | AC-8 | Send SIGTERM → graceful close < 500ms |
| `test_shutdown_signal_http_drain` | AC-9 | Send SIGTERM with pending requests → drain |
| `test_startup_default_transport` | AC-1 | Start without MCP_TRANSPORT → stdio active |
| `test_startup_http_transport` | AC-3 | Start with MCP_TRANSPORT=http → HTTP active |

---

## 🎯 Success Criteria (Definition of Done)

- [ ] **All 15 AC implemented** (AC-1 through AC-15)
- [ ] **Unit test coverage ≥ 85%** — Tests cover all AC groups
- [ ] **Integration tests passing** — Tool regression tests pass
- [ ] **E2E tests passing** — Shutdown + startup workflows verified
- [ ] **TypeScript strict mode** — No implicit `any`, full typed
- [ ] **No breaking changes** — Existing tools work unchanged
- [ ] **Code merged to main** — PR reviewed + approved
- [ ] **Implementation Summary written** — Dev A documents what was built + decisions

---

## 📊 Effort Breakdown (15 hours total)

| Phase | Hours | Dates |
|:------|:-----:|:-----:|
| **Days 1-2: Core Implementation** | 8h | 2026-03-19 (Wed afternoon) → 2026-03-20 (Thu EOD) |
| **Day 3: Testing & Validation** | 4h | 2026-03-21 (Fri) |
| **Day 4: Documentation & Handoff** | 3h | 2026-03-22 (Sat EOD) |
| **Total** | **15h** | **2026-03-19 Wed → 2026-03-22 Sat EOD** |

---

## 📝 Implementation Hints

### ITransport Interface (AC-11)

```typescript
// src/mcp/transport/ITransport.ts
export interface ITransport {
  // Lifecycle
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  isConnected(): boolean;
  getState(): "DISCONNECTED" | "CONNECTING" | "CONNECTED";

  // Transport metadata
  getType(): "stdio" | "http";
  getConfig(): Record<string, any>;

  // Event handling
  on(event: "request" | "error" | "close", handler: (data: any) => void): void;
  off(event: string, handler: Function): void;

  // Graceful shutdown
  gracefulShutdown(timeoutMs: number): Promise<void>;
}
```

### Factory Pattern (AC-5, AC-10)

```typescript
// src/mcp/transport/factory.ts
export function createTransport(type?: string): ITransport {
  const transportType = type || process.env.MCP_TRANSPORT || "stdio";

  switch (transportType.toLowerCase()) {
    case "stdio":
      return new StdioServerTransport();
    case "http":
      return new StreamableHTTPServerTransport({
        port: Number(process.env.MCP_PORT || 3000),
      });
    default:
      throw new ConfigurationError(
        `Unknown transport type: ${transportType}. Valid: stdio, http`
      );
  }
}
```

### Integration in mcpServer (AC-7)

```typescript
// src/mcp/mcpServer.ts (modified)
export async function startServer() {
  // Use factory instead of hardcoded transport
  const transport = createTransport();

  // Tool handlers remain unchanged (no transport logic needed)
  const tools = await loadToolModules();
  const toolRegistry = registerTools(tools);

  // Register tools, start server
  server.connect(transport);

  // Graceful shutdown (works for both transports)
  process.on("SIGTERM", async () => {
    console.log("Shutting down gracefully...");
    await transport.gracefulShutdown(5000);
  });
}
```

---

## 🚨 Risk Mitigation

| Risk | Probability | Impact | Mitigation |
|:-----|:-----------:|:------:|:-----------|
| Tool handlers break on refactoring | 🟡 Medium | High | Regression test all existing tools |
| HTTP port conflicts during dev | 🟡 Medium | Medium | Use dynamic port in tests; docs for prod |
| Graceful shutdown hangs | 🔴 Low | High | Timeout after 5s, force close |
| Type safety lost during refactoring | 🟢 Low | High | Strict TypeScript mode enforced |

---

## 🎓 Reference Materials

**From EPIC-14-TASK-ENHANCEMENT_2026-03-07.md:**

- ✅ [Full AC (15), detailed hints, and effort breakdown](EPIC-14-TASK-ENHANCEMENT_2026-03-07.md)

**Tech Lead Warrant:**

- ✅ [TECH-LEAD-EPIC-14-DECISION-WARRANT_2026-03-09.md](../../TECH-LEAD-EPIC-14-DECISION-WARRANT_2026-03-09.md) — Option A approved

---

## 📝 Notes

- **Why 15 hours, not 32h generic?** Transport abstraction has clear, bounded scope (2 transports, 1 factory pattern, graceful shutdown). Domain-specific effort estimation.
- **Why critical path?** All other tasks depend on ITransport interface. HTTP transport (TASK-14-02) blocked until this is done.
- **Why no tool handler changes needed?** If interface is designed correctly, tools stay transport-agnostic. Design goal.
