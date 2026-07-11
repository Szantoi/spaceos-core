---
id: DEV-B-EPIC-14-ASSIGNMENT
title: "Dev B — EPIC-14 Task Assignment (HTTP Transport + Graceful Shutdown)"
created: 2026-03-08
status: "✅ COMPLETED by Dev B 2026-03-09"
type: dev-assignment
developer: Dev B
epic: EPIC-14
phase: Phase 1 (Foundation)
---

# Dev B — EPIC-14 Task Assignment

**Duration:** 21 hours (base 18h + QA improvement 3h)
**Start Date:** 2026-03-19 (Day 11, after EPIC-11 complete)
**Target Completion:** 2026-03-21 (Day 13, parallel with Dev A)
**AC Count:** 21 (base 18 + 3 QA improvement)
**Status:** ✅ **TASK‑14‑02 implemented; awaiting review/merge**
**Blocker:** TASK-14-01 (Dev A) — depends on Transport base classes

---

## 📋 Your Task: TASK-14-02 — HTTP Transport + Graceful Shutdown

### What You're Building

An HTTP-based MCP transport for production deployments. Implements graceful shutdown with health checks and connection draining — critical for zero-downtime deployments.

### Current Spec

**TASK-14-02.md base AC:** 18 acceptance criteria (HTTP transport basics)

### NEW AC from Online Research (P0 CRITICAL)

**Issue #1 — Graceful Shutdown with Health State Management:**

Online research identified that graceful shutdown must include health check state management for load balancer integration.

**NEW AC-21 through AC-24: Health Check & Graceful Shutdown**

```
NEW AC-21: Health Check State Management
Given: Server running normally
When: Client calls GET /health
Then: Returns HTTP 200 + {status: "HEALTHY", activeConnections: N}

NEW AC-22: Shutdown Health Check Response
Given: SIGTERM signal received by process
When: /health endpoint called within 100ms
Then: Returns HTTP 503 Service Unavailable + {status: "SHUTTING_DOWN", activeConnections: N}

NEW AC-23: Connection Draining (30s Window)
Given: Shutdown in progress + 3 active HTTP requests
When: 30 seconds elapse
Then: All 3 requests complete naturally (within 30s, no force close)

NEW AC-24: Force Close After Timeout
Given: Shutdown initiated + request stalled for 35 seconds
When: 30s drain timeout exceeded
Then: Connection force-closed (process does not hang > 35s)
```

**Your Change:** Build health check with state management + graceful shutdown handler

**Effort:** +3h (total: 21h)

---

## 🔴 **CRITICAL: This is P0 — Must be in MVP or system won't support prod deployments**

Without graceful shutdown, deployments cause:

- 🔴 Client errors during rolling restart
- 🔴 Load balancer doesn't deregister instance properly
- 🔴 Orphaned connections hanging indefinitely
- 🔴 Potential data corruption if process killed mid-transaction

---

## 🎯 Implementation Checklist

### Phase 1: Study & Design (2-3 hours)

- [ ] Read [EPIC-14/state.md](../../../../milestones/milestone_02/epic_14/state.md)
- [ ] Read TASK-14-02.md (full specification)
- [ ] Read EPIC-14-ONLINE-RESEARCH-REVIEW.md (Finding #2 — graceful shutdown pattern)
- [ ] Review Node.js graceful shutdown patterns (3-phase model)
- [ ] Create state diagram: Normal → SIGTERM → Health 503 → Drain → Exit

### Phase 2: Implementation (16-18 hours)

#### 2.1 HTTP Transport Base (5-6h)

```typescript
// src/transports/HTTPTransport.ts

import express, { Express, Request, Response } from "express";
import { ITransport, TransportState, TransportError, TransportErrorContext } from "./ITransport";

class HTTPTransport extends BaseTransport {
  private app: Express;
  private server: http.Server | null = null;
  private port: number;
  private host: string;
  private shuttingDown = false;                    // NEW: shutdown flag
  private activeConnections = new Set<any>();    // NEW: track active connections
  private shutdownTimeout = 30000;                 // NEW: 30s drain window

  constructor(port: number = 3000, host: string = "localhost") {
    super();
    this.port = port;
    this.host = host;
    this.app = express();
    this.setupRoutes();
  }

  private setupRoutes(): void {
    // Health check endpoint (NEW AC-21/AC-22)
    this.app.get("/health", (req: Request, res: Response) => {
      const health = this.getHealthStatus();

      if (health.status === "SHUTTING_DOWN") {
        // AC-22: During shutdown, return 503
        return res.status(503).json({
          status: health.status,
          activeConnections: health.activeConnections,
          message: "Server is shutting down. Do not send new requests."
        });
      }

      // AC-21: Normal operation
      return res.status(200).json({
        status: health.status,
        activeConnections: health.activeConnections,
        uptime: process.uptime()
      });
    });

    // MCP tool call endpoint
    this.app.post("/tool/:name", (req: Request, res: Response) => {
      // MCP tool routing (implement in separate method)
    });
  }

  private getHealthStatus(): { status: "HEALTHY" | "SHUTTING_DOWN"; activeConnections: number } {
    return {
      status: this.shuttingDown ? "SHUTTING_DOWN" : "HEALTHY",
      activeConnections: this.activeConnections.size
    };
  }

  async connect(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.server = this.app.listen(this.port, this.host, () => {
          this.state = TransportState.CONNECTED;
          console.log(`[HTTP] Transport listening on ${this.host}:${this.port}`);

          // Setup graceful shutdown (AC-23/AC-24)
          this.setupGracefulShutdown();

          resolve();
        });
      } catch (error) {
        this.state = TransportState.ERROR;
        reject(error);
      }
    });
  }

  async disconnect(): Promise<void> {
    if (!this.server) return;

    return new Promise((resolve) => {
      this.server!.close(() => {
        this.state = TransportState.DISCONNECTED;
        console.log("[HTTP] Transport disconnected");
        resolve();
      });
    });
  }

  // NEW: Graceful shutdown handler (3-phase pattern)
  private setupGracefulShutdown(): void {
    process.on("SIGTERM", () => this.handleShutdown());
    process.on("SIGINT", () => this.handleShutdown());
  }

  private async handleShutdown(): Promise<void> {
    console.log("[SHUTDOWN] Graceful shutdown initiated");
    this.shuttingDown = true;  // AC-22: Set flag to return 503 from health check

    // Phase 1: Stop accepting new connections (AC-22)
    if (this.server) {
      this.server.close();
    }

    // Phase 2: Wait for active connections to drain (AC-23)
    const drainStart = Date.now();
    while (this.activeConnections.size > 0 && Date.now() - drainStart < this.shutdownTimeout) {
      console.log(`[SHUTDOWN] Draining ${this.activeConnections.size} active connection(s)...`);
      await this.sleep(100);
    }

    // Phase 3: Force close remaining (AC-24)
    if (this.activeConnections.size > 0) {
      console.warn(`[SHUTDOWN] Force-closing ${this.activeConnections.size} remaining connection(s) after ${this.shutdownTimeout}ms timeout`);
      for (const conn of this.activeConnections) {
        conn.destroy();
      }
    }

    console.log("[SHUTDOWN] Graceful shutdown complete");
    process.exit(0);
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  async diagnoseError(error: Error): Promise<TransportErrorContext> {
    return ErrorDiagnoser.diagnoseHTTPError(error);
  }

  getConfig() {
    return { type: TransportType.HTTP, port: this.port, host: this.host };
  }
}
```

**AC-01 through AC-06 verification:**

- [ ] AC-01: HTTPTransport extends BaseTransport
- [ ] AC-02: HTTP server listens on configured port/host
- [ ] AC-03: POST /tool/:name route defined
- [ ] AC-04: GET /health returns JSON status
- [ ] AC-05: Connection tracking initialized
- [ ] AC-06: SIGTERM handler registered

---

#### 2.2 Connection Tracking (3-4h)

```typescript
// src/transports/ConnectionTracker.ts

class ConnectionTracker {
  private connections = new Map<string, Connection>();
  private nextId = 0;

  trackConnection(req: Request): string {
    const connId = `conn-${++this.nextId}`;
    this.connections.set(connId, {
      id: connId,
      startTime: Date.now(),
      method: req.method,
      path: req.path
    });
    return connId;
  }

  releaseConnection(connId: string): void {
    this.connections.delete(connId);
  }

  getActiveConnections(): number {
    return this.connections.size;
  }

  getConnectionDetails(): any[] {
    return Array.from(this.connections.values()).map(conn => ({
      id: conn.id,
      elapsed: Date.now() - conn.startTime,
      method: conn.method,
      path: conn.path
    }));
  }
}

// Middleware to track connection lifecycle
app.use((req: Request, res: Response, next) => {
  const connId = connectionTracker.trackConnection(req);

  res.on("finish", () => {
    connectionTracker.releaseConnection(connId);
  });

  res.on("close", () => {
    connectionTracker.releaseConnection(connId);
  });

  next();
});
```

**AC-07 through AC-12 verification:**

- [ ] AC-07: Connections tracked on request start
- [ ] AC-08: Connections released on response finish
- [ ] AC-09: getActiveConnections() returns accurate count
- [ ] AC-10: Connection details logged (only during debugging)
- [ ] AC-11: No memory leak (connections cleaned up)
- [ ] AC-12: Performance minimal <1% overhead

---

#### 2.3 Graceful Shutdown Integration (5-6h) — NEW QA IMPROVEMENT

```typescript
// src/transports/GracefulShutdownManager.ts

class GracefulShutdownManager {
  private shuttingDown = false;
  private shutdownTimeout: number;
  private connectionTracker: ConnectionTracker;
  private healthCheckInterval: NodeJS.Timeout | null = null;

  constructor(connectionTracker: ConnectionTracker, shutdownTimeoutMs: number = 30000) {
    this.connectionTracker = connectionTracker;
    this.shutdownTimeout = shutdownTimeoutMs;
  }

  // AC-21: Health status endpoint state
  getHealthStatus(): { status: "HEALTHY" | "SHUTTING_DOWN"; activeConnections: number } {
    return {
      status: this.shuttingDown ? "SHUTTING_DOWN" : "HEALTHY",
      activeConnections: this.connectionTracker.getActiveConnections()
    };
  }

  // AC-22-24: Main shutdown sequence
  async gracefulShutdown(server: http.Server): Promise<void> {
    console.log("[SHUTDOWN] Phase 1: Setting flag to 503 (LB deregister)");
    this.shuttingDown = true;

    // Give LB time to see 503 and deregister
    await this.sleep(100);

    console.log("[SHUTDOWN] Phase 2: Stopping accepting new connections");
    server.close();

    console.log("[SHUTDOWN] Phase 3: Draining active connections (timeout: ${this.shutdownTimeout}ms)");
    const drainStart = Date.now();

    while (this.connectionTracker.getActiveConnections() > 0) {
      const elapsed = Date.now() - drainStart;

      if (elapsed > this.shutdownTimeout) {
        console.warn(`[SHUTDOWN] Timeout exceeded (${elapsed}ms). Force-closing remaining connections.`);
        break;  // Will force-close below
      }

      console.log(`[SHUTDOWN] ${this.connectionTracker.getActiveConnections()} connection(s) still active...`);
      await this.sleep(500);
    }

    console.log("[SHUTDOWN] Phase 4: Exit complete");
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Usage in HTTPTransport
private async handleShutdown(): Promise<void> {
  const shutdownManager = new GracefulShutdownManager(this.connectionTracker, this.shutdownTimeout);
  await shutdownManager.gracefulShutdown(this.server!);
  process.exit(0);
}
```

**AC-21 through AC-24 verification (NEW QA IMPROVEMENT):**

- [ ] **AC-21:** `/health` returns 200 + HEALTHY status during normal operation
- [ ] **AC-22:** `/health` returns 503 + SHUTTING_DOWN status within 100ms of SIGTERM
- [ ] **AC-23:** Active requests drain within 30s timeout (no force close if request completes)
- [ ] **AC-24:** Request force-closed if still active after 30s drain window

---

### Phase 3: Testing (3-4 hours)

**E2E Tests (5 test cases — E2E-01-05 from EPIC-14-QA-TEST-STRATEGY.md):**

```typescript
// src/tests/e2e/graceful-shutdown.test.ts

describe("HTTP Graceful Shutdown", () => {
  test("E2E-01: SIGTERM → 503 health → drain → exit (SLA ≤ 2s)", async () => {
    const server = await startHTTPTransport();
    const startTime = Date.now();

    // Send SIGTERM
    process.kill(process.pid, "SIGTERM");

    // Immediately check /health
    const healthRes = await fetch("http://localhost:3000/health");
    expect(healthRes.status).toBe(503);
    expect(await healthRes.json()).toHaveProperty("status", "SHUTTING_DOWN");

    // Wait for graceful exit
    await server.waitForExit();
    const duration = Date.now() - startTime;

    expect(duration).toBeLessThan(2000);  // SLA < 2s
  });

  test("E2E-02: Keep-alive drain (no hanging sockets)", async () => {
    const server = await startHTTPTransport();

    // Open keep-alive connection
    const client = http.globalAgent;
    client.keepAliveTimeout = 10000;

    // Send request + keep connection open
    const req = http.request({ port: 3000, path: "/tool/getRoles", keepAlive: true });
    req.end();

    // Send SIGTERM
    process.kill(process.pid, "SIGTERM");

    // Connection should close cleanly
    await server.waitForExit();
    expect(client.sockets).toBeEmpty();
  });

  test("E2E-03: Pending request completes before exit", async () => {
    const server = await startHTTPTransport();

    // Start 5s request
    const slowReq = fetch("http://localhost:3000/tool/slowTool");

    // After 1s, send SIGTERM
    setTimeout(() => process.kill(process.pid, "SIGTERM"), 1000);

    // Request should complete successfully
    const res = await slowReq;
    expect(res.status).toBe(200);
  });

  test("E2E-04: Timeout forces close after 30s", async () => {
    const server = await startHTTPTransport(30000);  // 30s drain timeout

    // Start request that takes 60s
    const verySlowReq = fetch("http://localhost:3000/tool/verySlow");

    // After 1s, send SIGTERM
    setTimeout(() => process.kill(process.pid, "SIGTERM"), 1000);

    // Wait for timeout + exit
    const startTime = Date.now();
    await server.waitForExit();
    const duration = Date.now() - startTime;

    // Should exit after ~30s timeout, not 60s
    expect(duration).toBeLessThan(35000);  // ~30s + slack
  });

  test("E2E-05: Multiple transports shutdown (stdio + HTTP)", async () => {
    // Not directly applicable to HTTP-only task, but can verify
    // if HTTP shutdown doesn't interfere with other transports
  });
});
```

**Checklist:**

- [ ] E2E-01 passes (SLA ≤ 2s verified)
- [ ] E2E-02 passes (keep-alive properly closed)
- [ ] E2E-03 passes (pending requests complete)
- [ ] E2E-04 passes (timeout enforced at 30s)
- [ ] No hanging processes after tests

---

### Phase 4: Documentation (2-3 hours)

**Deliverables:**

- [ ] Update TASK-14-02.md with 21 AC (base 18 + QA improvement 3)
- [ ] Create src/transports/HTTP-TRANSPORT.md (deployment guide)
- [ ] Create GRACEFUL-SHUTDOWN.md troubleshooting guide

**HTTP-TRANSPORT.md example:**

```markdown
# HTTP Transport Deployment Guide

## Configuration

```typescript
const transport = new HTTPTransport(8080, "0.0.0.0");
await transport.connect();
```

## Health Check

The HTTP transport exposes a `/health` endpoint that signals load balancers:

### Normal Operation

```
GET /health
→ 200 OK
{
  "status": "HEALTHY",
  "activeConnections": 5,
  "uptime": 3600.5
}
```

### Shutdown (SIGTERM Received)

```
GET /health
→ 503 Service Unavailable
{
  "status": "SHUTTING_DOWN",
  "activeConnections": 3,
  "message": "Server is shutting down. Do not send new requests."
}
```

## Graceful Shutdown Sequence

1. **SIGTERM Signal** → Process sets "shutting_down" flag
2. **LB Sees 503** → Deregisters instance from pool (100ms)
3. **Stop Accepting** → server.close() called
4. **Drain Window** → Waits up to 30s for pending requests
5. **Force Close** → If still active after 30s, destroy connections
6. **Exit** → Process.exit(0)

## Deployment Best Practices

- Pair with load balancer that respects 503 responses (ALB, NGINX, HAProxy)
- Set LB health check frequency to 5-10s (not 60s)
- Monitor `/health` endpoint for health status
- Log SIGTERM signals for debugging

## Troubleshooting

**Hanging Processes:** Ensure keep-alive connections send Connection: close
**Orphaned Connections:** Check LB integration (should deregister on 503)
**Timeout too short:** Increase shutdownTimeout if requests typically > 25s

```

---

## ✅ Acceptance Criteria (21 Total)

### Base AC (18):
- [ ] AC-01: HTTPTransport class implemented
- [ ] AC-02: Listens on configured port/host
- [ ] AC-03: POST /tool/:name route defined
- [ ] AC-04: GET /health returns JSON
- [ ] AC-05: Connection tracking middleware
- [ ] AC-06: SIGTERM handler registered
- [ ] AC-07: Connections tracked on request
- [ ] AC-08: Connections released on finish
- [ ] AC-09: getActiveConnections() accurate
- [ ] AC-10: Connection logging available
- [ ] AC-11: No memory leaks
- [ ] AC-12: Minimal performance overhead <1%
- [ ] AC-13: Error responses include error codes
- [ ] AC-14: Upstream tool error propagated
- [ ] AC-15: Timeouts handled gracefully
- [ ] AC-16: TLS support (optional)
- [ ] AC-17: Request body size validation
- [ ] AC-18: CORS headers set correctly

### QA Improvement AC (3 — P0 CRITICAL):
- [ ] **AC-21 (NEW):** Health check returns HEALTHY with 200 status code
- [ ] **AC-22 (NEW):** Health check returns SHUTTING_DOWN with 503 status Code within 100ms of SIGTERM
- [ ] **AC-23+24 (NEW):** Connection draining within 30s timeout, force-close after

**Total:** 21 AC → verification checklist above

---

## 📚 Resources

**Online Research:**
- EPIC-14-ONLINE-RESEARCH-REVIEW.md (Finding #2 — Graceful Shutdown Pattern)
- EPIC-14-QA-TEST-STRATEGY.md (E2E-01-05, stress tests CHAOS-01-03)

**npm Reference:**
- http-graceful-shutdown (reference implementation)
- http-terminator (keep-alive handling)

**Related Tasks:**
- Depends on: TASK-14-01 (Dev A) — Transport base classes
- Blocks: TASK-14-11 (E2E testing)

---

## 🔗 QA Validation

**QA Test Matrix:**
- E2E-01 through E2E-05: Graceful shutdown scenarios
- IT-01-06: Cross-transport consistency (HTTP results match stdio)
- Chaos tests: Connection stress + SIGTERM under load

**QA Sign-Off Criteria:**
- All 5 E2E tests passing
- All 21 AC verified
- SLA ≤ 2s validated
- No hanging processes
- Keep-alive connections properly closed

**Expected Pass Rate:** 100% (5/5 E2E tests)

---

## 📅 Timeline

| Day | Task | Hours | Status |
|:---:|:----:|:-----:|:------:|
| **Day 11** (3/19) | Study + Design + Setup | 3h | ⏳ Awaiting start |
| **Day 12** (3/20) | Implement HTTP base + Draining | 9h | ⏳ Awaiting start |
| **Day 13** (3/21) | Graceful shutdown + Tests + Docs | 9h | ⏳ Awaiting start |

**Total:** 21 hours (base 18h + QA improvement 3h)

---

## 🚀 Next Steps (After Tech Lead Decision)

1. **2026-03-14 EOD:** Tech Lead decides (Option A = all improvements)
2. **2026-03-19 09:00:** Dev B kickoff meeting
3. **2026-03-19 10:00:** Dev B starts TASK-14-02 (parallel with Dev A)
4. **2026-03-21 18:00:** Dev B submits PR (21 AC verified, E2E tests green)
5. **2026-03-22:** Dev B begins TASK-14-05 (Context/Discovery plugins) or peer review

---

**Assignment:** DEV-B-EPIC-14-ASSIGNMENT
**Status:** 🟡 **PENDING_TECH_LEAD_GO/NO-GO** (Option A: All improvements)
**Prepared by:** QA Tester Agent
**Date:** 2026-03-08

