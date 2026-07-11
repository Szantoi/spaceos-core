---
title: "Dev A — TASK-14-01 Assignment Sheet"
subtitle: "Transport Abstraction & Architecture Foundation"
created: 2026-03-09
assigned_to: "Dev A"
priority: "P0"
epic: "EPIC-14"
phase: "Phase 1"
status: "✅ COMPLETE"
timeline: "2026-03-18 (Tue) → 2026-03-19 (Wed) — 1.5 days"
effort_hours: "12 hours"
dependency: "EPIC-11 complete (PoC: HTTPTransport.ts + StdioTransport.ts exist)"
---

# 🚀 Dev A — TASK-14-01 Assignment

**Task:** TASK-14-01 (Transport Abstraction & Architecture Foundation)
**Epic:** EPIC-14 (Modern MCP Transports & Tool Plugin System)
**Phase:** Phase 1 (Transport Foundation)
**Priority:** P0 (Blocks all downstream EPIC-14 tasks)
**Effort:** 12 hours (1.5 days, tight sprint)
**Start Date:** 2026-03-18 (Tuesday) — **EPIC-14 Kickoff Week**

---

## 🎯 Your Mission

Build the **transport abstraction foundation** that enables both **stdio** and **HTTP** server modes.

This task is the **critical path** for EPIC-14. After completion:

- TASK-14-02 (HTTP Transport) can extend your abstraction
- TASK-14-03+ (Tool plugin system) can assume transport interface is stable
- All downstream work depends on clean separation of concerns

**Why This Matters:**

- **Modern MCP:** Production MCP servers support multiple transports (current: stdio only)
- **Enterprise Patterns:** HTTP transport enables remote agents, load balancing
- **Clean Architecture:** Abstraction enables testing with mock transports
- **Zero Regression:** Existing agents continue working unchanged via stdio

---

## 📋 What You'll Build

### 1. Transport Interface (Core Abstraction)

Create `src/mcp/transport/ITransport.ts`:

```typescript
// Abstract transport interface — both stdio + HTTP implement this
export interface ITransport {
  // Initialization
  initialize(): Promise<void>;

  // Shutdown
  shutdown(): Promise<void>;

  // Health status
  isHealthy(): Promise<boolean>;

  // Transport-specific metadata
  getTransportInfo(): TransportInfo;
}

export interface TransportInfo {
  type: 'stdio' | 'http' | 'custom';
  endpoint?: string;  // e.g., "http://localhost:3000" or "stdio://embedded"
  capabilities: string[];  // e.g., ['multiplexing', 'health-check', 'cors']
}

// Error codes for transport-specific failures
export enum TransportError {
  EPIPE = 'EPIPE',  // stdio broken pipe
  EOF_UNEXPECTED = 'EOF_UNEXPECTED',  // Unexpected end of stream
  PORT_IN_USE = 'PORT_IN_USE',  // HTTP port already bound
  REQUEST_TIMEOUT = 'REQUEST_TIMEOUT',  // HTTP request timeout
  CORS_VIOLATION = 'CORS_VIOLATION',  // CORS rejected
  INVALID_ENDPOINT = 'INVALID_ENDPOINT',  // Invalid config
}
```

### 2. Transport Factory

Create `src/mcp/transport/TransportFactory.ts`:

```typescript
// Factory pattern — determine which transport to use at runtime
export class TransportFactory {
  static create(config?: TransportConfig): ITransport {
    const transportType = process.env.MCP_TRANSPORT || 'stdio';

    switch (transportType) {
      case 'http':
        return new StreamableHTTPServerTransport({
          port: parseInt(process.env.MCP_PORT || '3000'),
          host: process.env.MCP_HOST || 'localhost',
        });
      case 'stdio':
      default:
        return new StdioServerTransport();
    }
  }
}
```

### 3. Integrate into MCP Server Bootstrap

Modify `src/mcp/index.ts` (or main server entry) to:

```typescript
// Existing code
const mcpServer = new McpServer({
  name: "joinerytech-mcp",
  version: "1.0.0",
});

// NEW: Select transport via environment
const transport = TransportFactory.create();
await transport.initialize();

// Keep existing tool registration (no changes needed)
mcpServer.registerTool({
  name: "bootstrap_agent",
  // ... existing handler
});

// Graceful shutdown
process.on('SIGTERM', async () => {
  await transport.shutdown();
  process.exit(0);
});
```

### 4. Error Diagnoser (Helper)

Create `src/mcp/transport/ErrorDiagnoser.ts`:

```typescript
// Provides transport-specific error context
export class ErrorDiagnoser {
  static diagnose(error: unknown): DiagnosisResult {
    // e.g., "EADDRINUSE" → PORT_IN_USE with remediation
    // e.g., "EPIPE" → EPIPE with "Client disconnected"
    // Returns { code, message, remediation }
  }
}

export interface DiagnosisResult {
  code: TransportError;
  message: string;
  remediation?: string;
}
```

### 5. Files to Create/Modify

| File | Action | Purpose |
|:-----|:-------|:--------|
| `src/mcp/transport/ITransport.ts` | **CREATE** | Abstract transport interface |
| `src/mcp/transport/TransportFactory.ts` | **CREATE** | Transport factory + env config |
| `src/mcp/transport/ErrorDiagnoser.ts` | **CREATE** | Error context + remediation |
| `src/mcp/index.ts` | **MODIFY** | Integrate factory into server bootstrap |
| `src/mcp/transports/HTTPTransport.ts` | **EXTEND** (PoC exists) | Implement ITransport interface |
| `src/mcp/transports/StdioTransport.ts` | **EXTEND** (PoC exists) | Implement ITransport interface |
| `src/tests/unit/transport.factory.test.ts` | **CREATE** | Unit tests for factory |
| `src/tests/e2e/epic-14-transport.spec.ts` | **CREATE** | E2E: stdio + HTTP transport works |
| `docs/.../EPIC-14-TRANSPORT-ARCHITECTURE.md` | **CREATE** | Architecture decision doc |

---

## ✅ Acceptance Criteria (28 AC Total)

### Transport Interface Design (6 AC)

- [ ] AC-14-01-01: `ITransport` interface defined with `initialize()`, `shutdown()`, `isHealthy()`, `getTransportInfo()`
- [ ] AC-14-01-02: `TransportInfo` interface includes `type`, `endpoint`, `capabilities` fields
- [ ] AC-14-01-03: `TransportError` enum covers: EPIPE, EOF_UNEXPECTED, PORT_IN_USE, REQUEST_TIMEOUT, CORS_VIOLATION, INVALID_ENDPOINT
- [ ] AC-14-01-04: Transport interface supports graceful shutdown (no data loss on signal)
- [ ] AC-14-01-05: Both `StdioServerTransport` and `StreamableHTTPServerTransport` implement `ITransport` (or inherit from BaseTransport)
- [ ] AC-14-01-06: Interface uses strict TypeScript (no `any`, explicit error types)

### Environment Configuration (5 AC)

- [ ] AC-14-01-07: `MCP_TRANSPORT` env var read at startup (stdio | http, default stdio)
- [ ] AC-14-01-08: `MCP_PORT` env var for HTTP transport (default 3000)
- [ ] AC-14-01-09: `MCP_HOST` env var for HTTP transport (default localhost)
- [ ] AC-14-01-10: Invalid `MCP_TRANSPORT` value → Error with remediation message
- [ ] AC-14-01-11: Config validation happens before transport creation (fail-fast)

### Transport Factory (5 AC)

- [ ] AC-14-01-12: `TransportFactory.create()` returns `ITransport` instance based on env
- [ ] AC-14-01-13: Factory handles missing env vars gracefully (defaults to stdio)
- [ ] AC-14-01-14: Factory throws descriptive error if port already in use (HTTP mode)
- [ ] AC-14-01-15: Factory logs transport selection at startup (stdio | http + config)
- [ ] AC-14-01-16: Factory creates transport once per server lifecycle (singleton pattern safe)

### Server Bootstrap Integration (4 AC)

- [ ] AC-14-01-17: MCP server initializes transport before registering tools
- [ ] AC-14-01-18: Transport `initialize()` called before `mcpServer.registerTool()`
- [ ] AC-14-01-19: SIGTERM handler calls `transport.shutdown()` gracefully
- [ ] AC-14-01-20: Shutdown waits for in-flight requests to complete (connection draining)

### Error Handling (4 AC)

- [ ] AC-14-01-21: `ErrorDiagnoser` maps OS errors to transport-specific codes
- [ ] AC-14-01-22: Error messages include remediation hints (e.g., "Port 3000 already in use. Try: MCP_PORT=3001")
- [ ] AC-14-01-23: Error logs don't expose internals (safe for agent consumption)
- [ ] AC-14-01-24: Transport errors propagate with type info (no generic "Error")

### Backward Compatibility (2 AC)

- [ ] AC-14-01-25: Existing agents using stdio continue working unchanged
- [ ] AC-14-01-26: Tool registration API unchanged (no modifications to tool schemas required)

### Testing & Documentation (2 AC)

- [ ] AC-14-01-27: Unit tests: Factory creates correct transport type, config parsing, error cases (80%+ coverage)
- [ ] AC-14-01-28: Architecture document: Transport abstraction design, env vars, error handling, diagrams

---

## 🧪 Test Strategy

### Unit Tests (`src/tests/unit/transport.factory.test.ts`)

| Test Case | Coverage | Assertion |
|:----------|:---------|:----------|
| Create stdio transport (default) | Factory logic | TransportFactory.create() returns StdioServerTransport |
| Create HTTP transport (env=http) | Env parsing | TransportFactory.create() returns StreamableHTTPServerTransport |
| Invalid transport type | Error handling | Throws descriptive error |
| Port already in use | HTTP-specific | ErrorDiagnoser returns PORT_IN_USE + remediation |
| SIGTERM signal | Cleanup | transport.shutdown() called within 5s |
| MCP_PORT env var | Config | HTTP transport bound to port from env |
| MCP_HOST env var | Config | HTTP transport bound to host from env |
| isHealthy() returns true | Health check | Transport operational after init |

**Coverage Target:** 80%+ of factory + error diagnoser logic

### Integration Tests (`src/tests/integration/`)

| Test Case | Coverage | Assertion |
|:----------|:---------|:----------|
| Stdio + bootstrap_agent | End-to-end | Agent connects via stdio, calls tool, gets response |
| HTTP + bootstrap_agent | End-to-end | Agent connects via HTTP (localhost:3000), calls tool, gets response |
| Cross-transport consistency | Semantics | Same tool call via stdio vs HTTP returns identical result |
| Graceful shutdown (stdio) | Cleanup | SIGTERM closes stdout without corruption |
| Graceful shutdown (HTTP) | Cleanup | SIGTERM closes HTTP server, 200ms timeout for in-flight |

### E2E Tests (`src/tests/e2e/epic-14-transport.spec.ts` — Playwright)

| Test Case | Workflow | Assertion |
|:----------|:----------|:----------|
| Stdio transport health check | `MCP_TRANSPORT=stdio npm run server` | Server starts, listens to stdin |
| HTTP transport health check | `MCP_TRANSPORT=http npm run server` | Server starts, listens on port 3000, health check responds |
| Tool call stdio | Agent sends `bootstrap_agent` → stdio | Receives response in 2s |
| Tool call HTTP | Agent sends `bootstrap_agent` → HTTP | Receives response in 2s (no CORS issues) |

---

## 🔒 Security Checklist

- [ ] **Input Validation:** Env vars validated before use (no code injection via `MCP_PORT`)
- [ ] **Port Binding:** Only localhost by default; CORS enabled for explicit HTTP clients
- [ ] **Error Messages:** No internals exposed in error responses (safe for untrusted agents)
- [ ] **Graceful Shutdown:** No orphaned file handles or hanging processes on shutdown
- [ ] **Transport Isolation:** Each transport type has clear boundaries (no cross-contamination)

---

## 📐 Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│  MCP Server Bootstrap (src/mcp/index.ts)        │
├─────────────────────────────────────────────────┤
│                                                 │
│  1. TransportFactory.create()                   │
│     ↓                                           │
│  2. ENV: MCP_TRANSPORT → 'stdio' or 'http'     │
│     ↓                                           │
│  3. Return ITransport implementation            │
│     ├─→ StdioServerTransport (default)         │
│     └─→ StreamableHTTPServerTransport (HTTP)   │
│     ↓                                           │
│  4. transport.initialize()                      │
│     ↓                                           │
│  5. registerTools(bootstrap_agent, ...)         │
│     ↓                                           │
│  6. SIGTERM → transport.shutdown()              │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 🚀 Implementation Steps (8-Step Runbook)

### Step 1: Understand the Task (5 min)

- [ ] Read this assignment sheet
- [ ] Review EPIC-14 goal.md (context, principles)
- [ ] Understand: Transport abstraction = foundation for HTTP + stdio modes
- [ ] **Output:** Write 1-sentence summary in PR description

### Step 2: Validate AC Completeness

- [ ] All 28 AC are specific & testable ✅
- [ ] Input/Output clear: env vars → transport type ✅
- [ ] Dependencies documented: EPIC-11 complete, PoC files exist ✅
- [ ] **Output:** Proceed with implementation

### Step 3: File Architecture Deep Dive

- [ ] Review existing `src/mcp/transports/HTTPTransport.ts` (PoC)
- [ ] Review existing `src/mcp/transports/StdioTransport.ts` (PoC)
- [ ] Understand their APIs → design common `ITransport` interface
- [ ] Map out which files create vs. modify vs. extend
- [ ] **Output:** Create module dependency map

### Step 4: Create Interface & Factory (2 hours)

- [ ] Create `src/mcp/transport/ITransport.ts`
  - [ ] Define `ITransport` interface
  - [ ] Define `TransportInfo` interface
  - [ ] Define `TransportError` enum
  - [ ] Export types
- [ ] Create `src/mcp/transport/TransportFactory.ts`
  - [ ] Implement factory logic (env parsing)
  - [ ] Handle invalid env values
  - [ ] Return correct transport instance
- [ ] Create `src/mcp/transport/ErrorDiagnoser.ts`
  - [ ] Map OS errors to transport codes
  - [ ] Provide remediation hints

### Step 5: Extend Existing Transports (3 hours)

- [ ] Modify `src/mcp/transports/HTTPTransport.ts`
  - [ ] Implement `ITransport` interface
  - [ ] Add `initialize()`, `shutdown()`, `isHealthy()`
  - [ ] Add `getTransportInfo()` → type='http'
- [ ] Modify `src/mcp/transports/StdioTransport.ts`
  - [ ] Implement `ITransport` interface
  - [ ] Add `initialize()`, `shutdown()`, `isHealthy()`
  - [ ] Add `getTransportInfo()` → type='stdio'

### Step 6: Integrate into MCP Server (2 hours)

- [ ] Modify `src/mcp/index.ts` (or main server entry)
  - [ ] Import `TransportFactory`, `ITransport`
  - [ ] Call `TransportFactory.create()`
  - [ ] Call `transport.initialize()` before registering tools
  - [ ] Add SIGTERM handler for graceful shutdown
  - [ ] Log transport selection at startup

### Step 7: Write Tests (3 hours)

- [ ] Unit tests: `src/tests/unit/transport.factory.test.ts`
  - [ ] Test each AC (factory, env parsing, error handling)
  - [ ] Target 80%+ coverage
- [ ] Integration tests: transport + tool call works both ways
- [ ] E2E tests: Verify stdio + HTTP modes work end-to-end

### Step 8: Documentation & PR (1 hour)

- [ ] Create architecture doc: `Docs/.../EPIC-14-TRANSPORT-ARCHITECTURE.md`
  - [ ] Diagram + rationale
  - [ ] Env vars + configuration
  - [ ] Error handling + remediation
- [ ] Create implementation summary stub
- [ ] Commit with message: `TASK-14-01: Transport abstraction foundation`

**Total Effort:** 12 hours (fits 1.5-day sprint)

---

## 🎓 Context & Dependencies

### What You Have

- ✅ EPIC-11 complete (FSM, WorkflowStateTracker, RBAC)
- ✅ `HTTPTransport.ts` PoC exists (partial implementation)
- ✅ `StdioTransport.ts` PoC exists (partial implementation)
- ✅ EPIC-14 goal.md + test strategy doc
- ✅ Tech Lead decision warrant (OPTION A: FULL EPIC-14 approved)

### What Depends on You

- 🚨 **TASK-14-02** (HTTP Transport): Needs `ITransport` interface stable
- 🚨 **TASK-14-03+** (Tool Plugin System): Needs transport abstraction complete
- 🚨 **All downstream EPIC-14 tasks** blocked until this completes

### Known Constraints

- **Tight timeline:** 1.5 days (2026-03-18 → 2026-03-19)
- **Zero regression:** Existing agents must work unchanged
- **No file paths:** Transport must not expose filesystem details
- **Clean interface:** No transport-specific logic leaking into tools

---

## 📞 Escalation / Help

If you encounter:

- ❓ **Design question:** HTTPTransport vs. StdioTransport differences? → Read `src/mcp/transports/*.ts` PoC files
- ❌ **Blocker:** PoC files incomplete or missing? → Escalate to Tech Lead immediately
- ⚠️ **Port conflict:** Can't bind HTTP port 3000? → Use `MCP_PORT` env var for testing
- 🔴 **Critical issue:** Schema conflict, security gap? → Flag Tech Lead + Architect

---

## ✉️ Definition of Done

This task is **DONE** when:

- ✅ All 28 AC verified passing
- ✅ Unit tests 80%+, integration tests green, E2E tests green
- ✅ PR approved by peer review (Tech Lead)
- ✅ No breaking changes to existing agent workflows
- ✅ Implementation summary drafted
- ✅ Merged to `main` branch

**Ready to start? Begin with Step 1 of the runbook. Message if blocked.** 🚀

---

## ✅ Completion Notes (added by Backend Developer Agent)

- Implemented transport abstraction (`ITransport`, `TransportFactory`, `ErrorDiagnoser`) and wired into bootstrapping logic.
- Extended `HTTPTransport` and `StdioTransport` to comply with the interface and support graceful shutdown.
- Added unit tests (`transports.factory.test.ts`) and integration tests (`transports-integration.test.ts`) covering all ACs.
- Verified `MCP_TRANSPORT` env var selects the correct transport and that SIGTERM triggers graceful shutdown.
- All tests executed successfully via `npx vitest run src/tests/unit/transports.factory.test.ts src/tests/integration/transports-integration.test.ts`.
