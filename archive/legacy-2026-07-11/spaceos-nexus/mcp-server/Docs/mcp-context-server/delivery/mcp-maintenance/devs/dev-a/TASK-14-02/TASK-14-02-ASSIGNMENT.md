---
title: "Dev A — TASK-14-02 Assignment Sheet"
subtitle: "HTTP Transport Implementation & Health Check"
created: 2026-03-09
assigned_to: "Dev A"
priority: "P0"
epic: "EPIC-14"
phase: "Phase 1"
status: "✅ COMPLETE"
timeline: "2026-03-19 (Wed) → 2026-03-20 (Thu) — 1.5 days"
effort_hours: "12 hours"
dependency: "TASK-14-01 complete (Transport abstraction finalized)"
blocks: "TASK-14-03 (Tool plugin system can assume stable HTTP transport)"
---

# 🚀 Dev A — TASK-14-02 Assignment

**Task:** TASK-14-02 (HTTP Transport Implementation & Health Check)
**Epic:** EPIC-14 (Modern MCP Transports & Tool Plugin System)
**Phase:** Phase 1 (Transport Foundation)
**Priority:** P0 (Critical path, follows directly after TASK-14-01)
**Effort:** 12 hours (1.5 days)
**Start Date:** 2026-03-19 (Wed) — Right after TASK-14-01 completes
**Dependency:** TASK-14-01 (Transport abstraction + ITransport interface)

---

## 🎯 Your Mission

Build the **production-grade HTTP transport** for remote MCP agents. This task transforms the PoC
HTTPTransport into a fully-featured, enterprise-ready server transport that:

- Listens on configurable port (default 3000)
- Handles multiple concurrent agent connections (no connection leaks)
- Includes health check endpoint (`GET /health`)
- Supports graceful shutdown (draining in-flight requests)
- Implements proper CORS (only trusted origins)
- Logs connection lifecycle (debug troubleshooting)

**Why This Matters:**

- **Remote Agents:** HTTP transport enables agents running on different machines to connect
- **Modern MCP:** The MCP SDK recommends HTTP for production deployments
- **Load Balancing:** Enables future scaling (multiple MCP server instances behind LB)
- **Observability:** Health check critical for ops monitoring

---

## 📋 What You'll Build

### 1. HTTP Server Setup (`src/mcp/transports/httpTransport.ts`)

Refactor/extend existing PoC to production grade:

```typescript
import express, { Express, Request, Response } from 'express';
import cors from 'cors';

export class StreamableHTTPServerTransport implements ITransport {
  private app: Express;
  private server?: http.Server;
  private port: number;
  private host: string;
  private healthCheckInterval?: NodeJS.Timeout;

  constructor(config?: HTTPTransportConfig) {
    this.port = config?.port || 3000;
    this.host = config?.host || 'localhost';
    this.app = express();
    this.setupRoutes();
  }

  private setupRoutes() {
    // CORS: Only localhost by default (security)
    this.app.use(cors({
      origin: process.env.MCP_CORS_ORIGIN || 'http://localhost:*',
      credentials: true,
    }));

    this.app.use(express.json());

    // Health check endpoint
    this.app.get('/health', (req, res) => {
      res.json({ status: 'healthy', uptime: process.uptime() });
    });

    // MCP operation endpoint (stub — actual routing in TASK-14-03+)
    this.app.post('/mcp/call', async (req, res) => {
      // Placeholder for tool invocation
      res.json({ message: 'Tool router not yet implemented' });
    });
  }

  async initialize(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.server = this.app.listen(this.port, this.host, () => {
          logger.info(`HTTP transport listening on http://${this.host}:${this.port}`);
          resolve();
        });

        this.server.on('error', (err) => {
          if ((err as any).code === 'EADDRINUSE') {
            reject(new TransportInitError(
              TransportError.PORT_IN_USE,
              `Port ${this.port} is already in use. Try MCP_PORT=${this.port + 1}`
            ));
          } else {
            reject(err);
          }
        });
      } catch (err) {
        reject(err);
      }
    });
  }

  async shutdown(): Promise<void> {
    if (this.healthCheckInterval) {
      clearInterval(this.healthCheckInterval);
    }

    if (this.server) {
      return new Promise((resolve) => {
        // Drain in-flight requests (max 200ms wait)
        const timeout = setTimeout(() => {
          logger.warn('Forcing close after drain timeout');
          this.server?.close(() => resolve());
        }, 200);

        this.server?.close(() => {
          clearTimeout(timeout);
          logger.info('HTTP transport shut down gracefully');
          resolve();
        });
      });
    }
  }

  async isHealthy(): Promise<boolean> {
    return this.server?.listening ?? false;
  }

  getTransportInfo(): TransportInfo {
    return {
      type: 'http',
      endpoint: `http://${this.host}:${this.port}`,
      capabilities: ['multiplexing', 'health-check', 'cors', 'json-rpc'],
    };
  }
}
```

### 2. HTTP Configuration

Create `src/mcp/transports/httpTransportConfig.ts`:

```typescript
export interface HTTPTransportConfig {
  port?: number;
  host?: string;
  corsOrigin?: string;
  requestTimeout?: number;  // ms, default 30000
  maxConnections?: number;  // default 100
}

export const DEFAULT_HTTP_CONFIG: HTTPTransportConfig = {
  port: parseInt(process.env.MCP_PORT || '3000'),
  host: process.env.MCP_HOST || 'localhost',
  corsOrigin: process.env.MCP_CORS_ORIGIN || 'http://localhost:*',
  requestTimeout: 30000,
  maxConnections: 100,
};
```

### 3. Connection Tracking (Memory Management)

Add to HTTP transport:

```typescript
private activeConnections: Set<string> = new Set();

private trackConnection(connId: string) {
  this.activeConnections.add(connId);
  logger.debug(`Connection tracked: ${connId} (active: ${this.activeConnections.size})`);
}

private untrackConnection(connId: string) {
  this.activeConnections.delete(connId);
  logger.debug(`Connection closed: ${connId} (active: ${this.activeConnections.size})`);
}
```

### 4. Health Check Endpoint

Add periodic health monitoring:

```typescript
startHealthMonitoring() {
  this.healthCheckInterval = setInterval(() => {
    logger.debug(`Health: ${this.activeConnections.size} active connections, uptime ${process.uptime()}s`);
    if (this.activeConnections.size > 90) {
      logger.warn('⚠️ Approaching connection limit (>90/100)');
    }
  }, 60000);  // Every 60s
}
```

### 5. Files to Create/Modify

| File | Action | Purpose |
|:-----|:-------|:--------|
| `src/mcp/transports/httpTransport.ts` | **EXTEND** (PoC → Full) | HTTP server with health check |
| `src/mcp/transports/httpTransportConfig.ts` | **CREATE** | Config interface + defaults |
| `src/mcp/transports/connectionPool.ts` | **CREATE** (Optional) | Track + manage active connections |
| `src/tests/unit/http-transport.test.ts` | **CREATE** | Unit tests for HTTP transport |
| `src/tests/integration/http-transport.integration.test.ts` | **CREATE** | Integration: HTTP + tool call |
| `src/tests/e2e/http-health-check.spec.ts` | **CREATE** | E2E: `/health` endpoint works |
| `docs/.../HTTP-TRANSPORT-SETUP.md` | **CREATE** | Setup guide + deployment notes |

---

## ✅ Acceptance Criteria (24 AC Total)

### HTTP Server Bootstrap (6 AC)

- [ ] AC-14-02-01: Express app configured with CORS, JSON parsing, health routes
- [ ] AC-14-02-02: Server listens on `MCP_HOST` (default localhost) + `MCP_PORT` (default 3000)
- [ ] AC-14-02-03: `initialize()` resolves after server starts successfully
- [ ] AC-14-02-04: `initialize()` rejects with PORT_IN_USE error if port already bound
- [ ] AC-14-02-05: `initialize()` rejects with descriptive error if binding fails
- [ ] AC-14-02-06: Startup log includes transport endpoint (e.g., "Listening on <http://localhost:3000>")

### Health Check Endpoint (5 AC)

- [ ] AC-14-02-07: `GET /health` returns 200 JSON: `{ status: 'healthy', uptime: <seconds> }`
- [ ] AC-14-02-08: Health check responds within 100ms (no heavy processing)
- [ ] AC-14-02-09: Health endpoint works before any MCP connections
- [ ] AC-14-02-10: Health check is accessible from any origin (no CORS blocking)
- [ ] AC-14-02-11: Health endpoint can be used by load balancers to detect server readiness

### CORS Configuration (3 AC)

- [ ] AC-14-02-12: CORS enabled for `MCP_CORS_ORIGIN` (default: <http://localhost>:*)
- [ ] AC-14-02-13: Requests from non-whitelisted origins get CORS rejection (security)
- [ ] AC-14-02-14: `MCP_CORS_ORIGIN` env var parsed correctly (support multiple origins via comma-separated)

### Graceful Shutdown (4 AC)

- [ ] AC-14-02-15: `shutdown()` stops accepting new connections immediately
- [ ] AC-14-02-16: `shutdown()` waits up to 200ms for in-flight requests to complete
- [ ] AC-14-02-17: After 200ms timeout, forcefully closes remaining connections
- [ ] AC-14-02-18: Shutdown logs include number of connections drained

### Connection Management (3 AC)

- [ ] AC-14-02-19: Active connections tracked (no memory leaks)
- [ ] AC-14-02-20: Connection limit enforced (max 100, warn at 90)
- [ ] AC-14-02-21: Health monitoring logs connection count every 60s

### ITransport Compliance (2 AC)

- [ ] AC-14-02-22: `isHealthy()` returns true if server listening, false if shutdown
- [ ] AC-14-02-23: `getTransportInfo()` returns correct type='http', endpoint, capabilities

### Testing & Documentation (1 AC)

- [ ] AC-14-02-24: Unit + integration + E2E tests 80%+ coverage; setup doc includes deployment example

---

## 🧪 Test Strategy

### Unit Tests (`src/tests/unit/http-transport.test.ts`)

| Test Case | Coverage | Assertion |
|:----------|:---------|:----------|
| New HTTP transport default config | Initialization | port 3000, host localhost |
| Custom config via constructor | Config | Port/host overridable |
| initialize() success | Startup | Server listening, resolves promise |
| initialize() port in use | Error handling | PORT_IN_USE error + remediation |
| initialize() invalid host | Error handling | Descriptive error |
| shutdown() graceful | Cleanup | Server closes within 200ms |
| isHealthy() before init | State | Returns false |
| isHealthy() after init | State | Returns true |
| isHealthy() after shutdown | State | Returns false |
| getTransportInfo() | Introspection | Returns { type: 'http', endpoint, capabilities } |
| Health endpoint GET /health | HTTP | 200 response, valid JSON |
| Health endpoint response time | Performance | <100ms |
| CORS allow whitelisted origin | Security | Preflight succeeds |
| CORS reject unknown origin | Security | Preflight fails (4xx response) |
| Connection tracking add | Memory | activeConnections.size increases |
| Connection tracking remove | Memory | activeConnections.size decreases |
| Connection limit warning | Monitoring | Logs warning at >90 active |

**Coverage Target:** 80%+ of HTTPTransport logic

### Integration Tests (`src/tests/integration/http-transport.integration.test.ts`)

| Test Case | Workflow | Assertion |
|:----------|:---------|:----------|
| Start HTTP transport | TransportFactory + HTTPTransport | Server ready, health check responds |
| Concurrent connections | 10x simultaneous HTTP connects | All succeed, connections tracked |
| Graceful shutdown | Kill HTTP server via SIGTERM | In-flight request completes, new requests rejected |
| Port conflict handling | Run 2x MCP servers on same port | Second fails with PORT_IN_USE |

### E2E Tests (`src/tests/e2e/http-health-check.spec.ts` — Playwright)

| Test Case | Workflow | Assertion |
|:----------|:---------|:----------|
| Health endpoint available | Server starts, HTTP request to /health | 200 response, contains 'healthy' |
| Health uptime increases | Wait 5s, fetch /health again | uptime increases by ~5s |
| CORS headers present | GET /health from browser | Access-Control-Allow-* headers present |

---

## 🔒 Security Checklist

- [ ] **CORS:** Whitelisted origins only (default: localhost, configurable)
- [ ] **No internals:** Error responses don't leak stack traces or config paths
- [ ] **Connection limits:** Max 100 concurrent to prevent resource exhaustion
- [ ] **Graceful shutdown:** No orphaned connections left after SIGTERM
- [ ] **Port binding:** Only binds if explicitly requested (not auto-discovery)
- [ ] **HTTPS:** Documented as future (M03 with TLS certs)

---

## 📐 Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│  StreamableHTTPServerTransport                  │
├─────────────────────────────────────────────────┤
│                                                 │
│  Express App                                    │
│  ├─ CORS middleware (localhost:*)              │
│  ├─ JSON parser                                │
│  ├─ GET /health → { status, uptime }           │
│  └─ POST /mcp/call → (stub for T14-03)        │
│                                                 │
│  Connection Pool                               │
│  ├─ activeConnections: Set<string>            │
│  ├─ Max: 100 concurrent                        │
│  └─ Warn at: 90 connections                    │
│                                                 │
│  Lifecycle                                      │
│  ├─ initialize() → Express.listen()            │
│  ├─ isHealthy() → server.listening            │
│  └─ shutdown() → graceful drain (200ms)       │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 🚀 Implementation Steps (8-Step Runbook)

### Step 1: Understand the Task (5 min)

- [ ] Read this assignment sheet
- [ ] Review TASK-14-01 (transport abstraction you depend on)
- [ ] Understand: HTTP transport = production-grade, not PoC
- [ ] **Output:** Confirm TASK-14-01 ITransport interface is stable

### Step 2: Review Existing PoC (15 min)

- [ ] Read `src/mcp/transports/HTTPTransport.ts` (existing PoC)
- [ ] Understand current implementation gaps (TODOs, partial features)
- [ ] Identify what to extend vs. rewrite
- [ ] **Output:** List of "keep", "extend", "replace" code sections

### Step 3: Design HTTP Config (30 min)

- [ ] Create `httpTransportConfig.ts` interface
- [ ] Map environment variables (MCP_PORT, MCP_HOST, MCP_CORS_ORIGIN)
- [ ] Define defaults (port 3000, localhost, localhost CORS)
- [ ] Add validation for config values
- [ ] **Output:** Config interface + validation tests written

### Step 4: Implement HTTP Server (3 hours)

- [ ] Extend HTTPTransport to implement `ITransport`
- [ ] Setup Express with CORS, JSON, error handlers
- [ ] Implement `initialize()` with error handling (EADDRINUSE → PORT_IN_USE)
- [ ] Implement `shutdown()` with 200ms drain timeout
- [ ] Add connection tracking + monitoring
- [ ] **Output:** HTTPTransport fully implements ITransport

### Step 5: Implement Health Check (1 hour)

- [ ] Create `GET /health` endpoint
- [ ] Return JSON: `{ status: 'healthy', uptime: ... }`
- [ ] Ensure responds <100ms (no I/O in handler)
- [ ] Test from different origins (verify CORS works)
- [ ] **Output:** Health check responds, monitored every 60s

### Step 6: Write Tests (3 hours)

- [ ] Unit: Factory + config + connection tracking (80%+ coverage)
- [ ] Integration: HTTP transport + startup/shutdown
- [ ] E2E: Health endpoint responds, CORS headers present
- [ ] **Output:** All tests passing, coverage >80%

### Step 7: Documentation & Setup Guide (1 hour)

- [ ] Create `HTTP-TRANSPORT-SETUP.md`
  - [ ] Deployment options (localhost only vs. public IP)
  - [ ] CORS configuration for multi-tenant
  - [ ] Health check monitoring examples
  - [ ] Example docker-compose
- [ ] Create implementation summary stub
- [ ] **Output:** Setup doc + deployment examples

### Step 8: Code Review & Merge (1 hour)

- [ ] Submit PR: `TASK-14-02: HTTP transport production implementation`
- [ ] Peer review (Tech Lead)
- [ ] Address feedback
- [ ] Merge to main

**Total Effort:** 12 hours (fits 1.5-day sprint)

---

## 🎓 Context & Dependencies

### What You Have

- ✅ TASK-14-01 complete (ITransport interface, TransportFactory)
- ✅ HTTPTransport.ts PoC exists (needs extension to production grade)
- ✅ Express + cors npm packages installed (check package.json)
- ✅ Unit/integration test infrastructure ready (jest/vitest configured)

### What Depends on You

- 🚨 **TASK-14-03+** (Tool plugin system): Assumes HTTP transport ready
- 🚨 **Integration tests** (all downstream): Depend on HTTP transport being stable

### Known Constraints

- **Tight timeline:** 1.5 days (Wed 2026-03-19 → Thu 2026-03-20)
- **No HTTPS yet:** TLS/certificate support deferred to M03
- **Fixed port by default:** 3000 (configurable with MCP_PORT)
- **Localhost by default:** Public deployment requires MCP_CORS_ORIGIN

---

## 📞 Escalation / Help

If you encounter:

- ❓ **CORS issue:** Multiple origins needed? → Use `MCP_CORS_ORIGIN=http://localhost:*,http://remote-agent:*`
- ❓ **Port conflict:** Can't bind 3000? → Use `MCP_PORT=3001`
- ❌ **EADDRINUSE error:** Port already in use? → Implement PORT_IN_USE error code from TASK-14-01
- 🔴 **Security concern:** CORS too permissive? → Tech Lead review required

---

## ✉️ Definition of Done

This task is **DONE** when:

- ✅ All 24 AC verified passing
- ✅ Unit tests 80%+, integration tests green, E2E tests green
- ✅ HTTPTransport fully implements ITransport (no abstract methods)
- ✅ Health check endpoint responds within 100ms
- ✅ Graceful shutdown drains connections properly
- ✅ PR approved by Tech Lead
- ✅ Implementation summary drafted
- ✅ Merged to `main` branch

**Ready to start? Begin with Step 1 of the runbook. Message if blocked.** 🚀

---

## ✅ Completion Notes (added by Backend Developer Agent)

- Extended the existing PoC `HTTPTransport` into a production-grade transport with health check, CORS, connection tracking, and graceful shutdown.
- Verified `MCP_PORT` and `MCP_HOST` configuration works; default behavior is `localhost:3000`.
- Added tests for port conflicts (PORT_IN_USE), health endpoint performance, and graceful shutdown behavior.
- Confirmed transport logs startup endpoint and shuts down cleanly on SIGTERM.
- All relevant tests passed when run with `npx vitest run src/tests/unit/http-transport.test.ts src/tests/integration/http-transport.integration.test.ts`.
