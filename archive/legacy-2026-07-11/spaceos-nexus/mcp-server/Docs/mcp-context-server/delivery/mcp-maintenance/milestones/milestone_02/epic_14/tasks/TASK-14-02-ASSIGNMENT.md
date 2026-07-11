---
id: TASK-14-02
title: "TASK-14-02 — HTTP Transport: Secure Connection Lifecycle"
epic: EPIC-14
milestone: M02
phase: "Phase 1 Foundation (Layer 2)"
created: 2026-03-09
type: "task-assignment"
status: "🟡 READY (Task specs finalized 2026-03-09)"
assignee: "Dev A or Dev B"
effort: "18 hours"
duration: "3 calendar days (2026-03-20 Thu → 2026-03-22 Sat EOD)"
blocker: "TASK-14-01 (Transport Abstraction) — must deliver ITransport interface"
blocks: ["TASK-14-11 (E2E Tests)", "Tool handlers requiring HTTP"]
ac_count: 18
priority: "P0 (Enterprise deployment pattern)"
---

# TASK-14-02: HTTP Transport — Secure Connection Lifecycle

## 📋 Problem Statement

To support remote agent deployments and multi-tenant scenarios, we need a production-grade HTTP transport that:

1. **Listens on configurable port** — Defaults to 3000, overridable via env var
2. **Health check endpoint** — Monitoring-ready with `/health` endpoint
3. **Security hardening** — CORS policy, DNS rebinding protection, payload limits
4. **Connection management** — Max concurrent, keep-alive, graceful shutdown
5. **Error handling** — Proper HTTP status codes, readable error messages

**This enables cloud deployments where stdio transport isn't practical.**

---

## ✅ Acceptance Criteria (18 Total)

### AC-1 through AC-4: Server Startup & Port Configuration

**AC-1: HTTP Server Startup**

- Given: `MCP_TRANSPORT=http` and server initializing
- When: StreamableHTTPServerTransport.connect() called
- Then: HTTP listener starts on 0.0.0.0 (all interfaces)
- Then: Accepts incoming requests (verified via test client)
- *Validation:* Integration test with HTTP client

**AC-2: Health Check Endpoint**

- Given: HTTP server running
- When: `GET /health` called
- Then: Responds with 200 OK
- Then: Response body includes JSON: `{ status: "ok", uptime: <milliseconds> }`
- Then: Health check itself is fast (< 50ms) even under load
- *Validation:* E2E test calling /health multiple times with load

**AC-3: Health Check Performance**

- Given: Server under concurrent load (50+ simultaneous tool calls)
- When: Health check endpoint queried
- Then: Response time < 50ms (health check must not block)
- Then: Health check doesn't interfere with tool processing
- *Validation:* Load test with concurrent requests + health polling

**AC-4: Custom Port Configuration**

- Given: `MCP_PORT=3001` environment variable set
- When: Server starts with HTTP transport
- Then: Listens on port 3001 (not default 3000)
- Then: Requests to <http://localhost:3001> succeed
- Then: Requests to <http://localhost:3000> fail (clean port binding)
- *Validation:* Integration test with custom port override

---

### AC-5 through AC-7: CORS & DNS Rebinding Security

**AC-5: CORS Headers — Allowed Origins**

- Given: HTTP request with `Origin: http://localhost:3001` from allowed origin list
- When: CORS validation runs
- Then: Response includes `Access-Control-Allow-Origin: http://localhost:3001`
- Then: Browser allows response (not blocked by CORS)
- When: Origin not in allowlist
- Then: Response lacks Allow-Origin header (CORS blocks browser)
- *Validation:* Unit test with allowed/denied origins

**AC-6: CORS Preflight Handling**

- Given: Browser sends OPTIONS preflight request
- When: CORS middleware processes request
- Then: Response includes:
  - `Access-Control-Allow-Methods: POST, GET, OPTIONS`
  - `Vary: Origin` (cache-aware)
  - `Access-Control-Max-Age: 3600` (browser cache preflight)
- *Validation:* Unit test for OPTIONS response

**AC-7: DNS Rebinding Protection**

- Given: `Host: attacker-domain.com` in request header (DNS rebinding attack)
- When: DNS rebinding protection middleware runs
- Then: Request rejected with 403 Forbidden
- Then: Error message: "Host not allowed" (no sensitive details)
- When: `Host: localhost` (in allowlist)
- Then: Request accepted
- *Validation:* Unit test simulating malicious Host headers

---

### AC-8 through AC-12: Connection Management & Request Handling

**AC-8: Max Concurrent Connections**

- Given: 100 concurrent HTTP requests sent simultaneously
- When: All requests processed by server
- Then: At least 95 requests succeed (< 5% rejection acceptable)
- Then: Rejected requests fail gracefully (not hang/crash)
- Then: Server remains stable after load (no memory leaks)
- *Validation:* Load test with connection pool

**AC-9: Request Timeout**

- Given: HTTP request with no activity for 10+ seconds
- When: Connection idle timeout triggers
- Then: Server closes connection gracefully
- Then: Client receives timeout notification (or connection reset)
- Then: Server doesn't hang waiting for response
- *Validation:* E2E test with long idle connection

**AC-10: Graceful Shutdown Under Load**

- Given: 20 in-flight HTTP requests when SIGTERM received
- When: Graceful shutdown initiated
- Then: Server rejects NEW requests with 503 Service Unavailable
- Then: Existing requests allowed to complete (< 2s deadline per request)
- Then: After all complete, HTTP listener closes
- Then: Total shutdown time < 5s
- *Validation:* E2E test simulating SIGTERM with pending requests

**AC-11: Malformed Request Handling**

- Given: Malformed HTTP request (invalid headers, syntax errors)
- When: Received by server
- Then: Responds with 400 Bad Request
- Then: Response body includes error message (not 500 Internal Server Error)
- Then: Server doesn't crash/hang
- *Validation:* Unit test with various malformed requests

**AC-12: Payload Size Limit**

- Given: POST request with body > 10MB (configurable)
- When: Received
- Then: Rejected with 413 Payload Too Large
- Then: Error message: "Request body exceeds 10MB limit"
- Then: Server doesn't attempt to buffer entire payload (DoS prevention)
- *Validation:* Unit test with large payloads

---

### AC-13 through AC-18: Connection Features, Logging & Consistency

**AC-13: HTTP/1.1 Keep-Alive Support**

- Given: HTTP/1.1 client with `Connection: keep-alive` header
- When: Response sent
- Then: Server honors keep-alive (reuses connection for multiple requests)
- Then: Connection pooling works correctly
- *Validation:* Integration test with keep-alive client library

**AC-14: HTTP/2 Graceful Fallback**

- Given: HTTP/2 client attempting protocol upgrade
- When: Negotiation attempted
- Then: Server gracefully declines (offers HTTP/1.1 fallback for M02)
- Then: Client can communicate via HTTP/1.1
- Then: No protocol negotiation errors
- *Validation:* E2E test with HTTP/2-capable client

**AC-15: Request Logging (Debug Mode)**

- Given: `DEBUG=*` environment variable set
- When: HTTP request → response cycle completes
- Then: Logs include: `[2026-03-20T10:00:00Z] POST /tool 200 45ms`
  - Timestamp, method, path, status code, duration
- When: `DEBUG` not set
- Then: No request logs (production mode quiet)
- *Validation:* Unit test with debug flag + log capture

**AC-16: Auth Context Propagation**

- Given: HTTP request with `Authorization: Bearer token123`
- When: RequestContext middleware applies
- Then: Tool handlers receive context.auth = { bearer: "token123" }
- Then: Tools can access auth info (no transport-specific code needed)
- *Validation:* Integration test with auth token + context check

**AC-17: Transport Metrics (Optional, P1+)**

- Given: HTTP transport active for 1 minute under load
- When: `getMetrics()` called
- Then: Returns:

  ```json
  {
    "requestsPerSec": 50,
    "avgResponseTime": 120,
    "errorRate": 0.02,
    "activeConnections": 15,
    "totalRequests": 3000
  }
  ```

- *Validation:* E2E test with metrics collection

**AC-18: Cross-Transport Tool Consistency**

- Given: Same tool (e.g., `bootstrap_agent`) called via HTTP and stdio
- When: Responses compared
- Then: Identical response (same data, same format)
- Then: No transport-specific behavior leakage
- *Validation:* E2E test comparing responses across transports

---

## 📂 Deliverables

### Code Files to Create

| File | Type | Purpose |
|:-----|:-----|:--------|
| `src/mcp/transport/httpTransport.ts` | ✅ CREATE | HTTP implementation + middleware |
| `src/mcp/transport/middleware/corsMiddleware.ts` | ✅ CREATE | CORS policy enforcement |
| `src/mcp/transport/middleware/dnsRebindingProtection.ts` | ✅ CREATE | DNS rebinding validation |
| `src/mcp/transport/middleware/payloadLimit.ts` | ✅ CREATE | Payload size enforcement |
| `src/mcp/transport/middleware/requestLogging.ts` | ✅ CREATE | Debug logging |
| `src/tests/unit/http-transport.test.ts` | ✅ CREATE | AC-1 through AC-16 unit tests |
| `src/tests/integration/http-connection-lifecycle.test.ts` | ✅ CREATE | AC-8 through AC-10 integration |
| `src/tests/e2e/http-transport.e2e.test.ts` | ✅ CREATE | Full HTTP workflow E2E |
| `src/tests/e2e/http-tool-consistency.e2e.test.ts` | ✅ CREATE | AC-18 cross-transport comparison |

### Documentation Files

| File | Type | Purpose |
|:-----|:-----|:--------|
| `IMPLEMENTATION-SUMMARY.md` | Doc | Dev A completion report |
| `TASK-14-02-KICKOFF.md` | Doc | Implementation roadmap |
| `HTTP-TRANSPORT-SECURITY-GUIDE.md` | Doc | CORS + DNS rebinding for operators |

---

## 🔗 Dependencies & Sequencing

### Depends On (Blocking)

| Task | Component | Reason |
|:-----|:----------|:-----:|
| **TASK-14-01** | ITransport interface | Must implement ITransport |
| **TASK-14-03** | Plugin system | Tool registration needed |

### Blocks (Downstream)

| Task | Component |
|:-----|:----------|
| **TASK-14-11** | E2E tests for HTTP |
| **All EPIC-14 tasks** | Can use HTTP transport once ready |

---

## 🧪 Testing Strategy

### Unit Tests (Security + Configuration)

| Test | AC | Focus |
|:-----|:---|:-------|
| `test_http_server_startup` | AC-1 | Server binds to interface |
| `test_health_check_endpoint` | AC-2 | /health responds correctly |
| `test_custom_port` | AC-4 | Port override works |
| `test_cors_allowed_origin` | AC-5a | Allowed origin gets header |
| `test_cors_denied_origin` | AC-5b | Denied origin blocked |
| `test_cors_preflight` | AC-6 | OPTIONS response correct |
| `test_dns_rebinding_blocked` | AC-7a | Bad Host header rejected |
| `test_dns_rebinding_allowed` | AC-7b | Good Host accepted |
| `test_malformed_request` | AC-11 | 400 error, no crash |
| `test_payload_too_large` | AC-12 | 413 error on huge payload |
| `test_keep_alive_support` | AC-13 | Keep-alive honored |
| `test_http2_fallback` | AC-14 | HTTP/2 graceful decline |
| `test_debug_logging` | AC-15 | Logs in DEBUG mode |

### Integration Tests (Connection Management)

| Test | AC | Scenario |
|:-----|:---|:----------|
| `test_max_concurrent_connections` | AC-8 | 100 concurrent requests |
| `test_idle_connection_timeout` | AC-9 | Timeout after 10s idle |
| `test_graceful_shutdown_with_pending` | AC-10 | SIGTERM with 20 pending requests |
| `test_auth_context_propagation` | AC-16 | Authorization header → context |

### E2E Tests (Full Workflows)

| Test | AC | Workflow |
|:-----|:---|----------|
| `test_health_check_load` | AC-3 | Health check under load |
| `test_tool_via_http` | AC-18a | Tool call via HTTP |
| `test_tool_cross_transport` | AC-18b | Compare HTTP vs stdio responses |
| `test_http_metrics` | AC-17 | Metrics collection |

---

## 🎯 Success Criteria (Definition of Done)

- [ ] **All 18 AC implemented** (AC-1 through AC-18)
- [ ] **Unit test coverage ≥ 85%** — Security middleware tested
- [ ] **Integration tests passing** — Connection lifecycle verified
- [ ] **E2E tests passing** — Full HTTP workflow end-to-end
- [ ] **Security review** — CORS, DNS rebinding, payload limits validated
- [ ] **Load test passing** — 100 concurrent connections handled
- [ ] **Cross-transport consistency** — Tools work identically on HTTP vs stdio
- [ ] **Code merged to main** — PR reviewed + approved
- [ ] **Implementation Summary written** — Dev A documents decisions

---

## 📊 Effort Breakdown (18 hours total)

| Phase | Hours | Dates |
|:------|:-----:|:-----:|
| **Days 1-2: Core HTTP Implementation** | 8h | 2026-03-20 (Thu) → 2026-03-21 (Fri EOD) |
| **Days 2-3: Security Middleware** | 5h | 2026-03-21 (Fri afternoon) → 2026-03-22 (Sat) |
| **Days 3-3: Testing & Validation** | 3h | 2026-03-22 (Sat) → 2026-03-23 (Sun EOD) |
| **Day 4: Documentation & Handoff** | 2h | 2026-03-23 (Sun EOD) *Optional, can compress* |
| **Total** | **18h** | **2026-03-20 Thu → 2026-03-23 Sun EOD** |

---

## 📝 Implementation Hints

### Express-based HTTP Transport

```typescript
// src/mcp/transport/httpTransport.ts
import express from "express";
import { ITransport } from "./ITransport";

export class StreamableHTTPServerTransport implements ITransport {
  private app: express.Application;
  private httpServer: http.Server | null = null;
  private config: HttpTransportConfig;

  constructor(config: HttpTransportConfig) {
    this.config = config;
    this.app = express();
    this.setupMiddleware();
    this.setupRoutes();
  }

  private setupMiddleware() {
    // CORS
    this.app.use(corsMiddleware(this.config.allowedOrigins || ["localhost"]));

    // DNS Rebinding Protection
    this.app.use(dnsRebindingProtection());

    // Payload limit
    this.app.use(express.json({ limit: this.config.maxPayloadMb || 10 }));

    // Debug logging
    if (process.env.DEBUG) {
      this.app.use(requestLogging());
    }
  }

  private setupRoutes() {
    // Health check
    this.app.get("/health", (req, res) => {
      res.json({ status: "ok", uptime: process.uptime() * 1000 });
    });

    // Tool endpoint
    this.app.post("/tool", async (req, res) => {
      try {
        const result = await toolRegistry.invoke(req.body.tool, req.body.args, {
          auth: { bearer: req.headers.authorization?.split(" ")[1] },
        });
        res.json(result);
      } catch (error) {
        res.status(500).json({ error: error.message });
      }
    });
  }

  async connect(): Promise<void> {
    return new Promise((resolve) => {
      this.httpServer = this.app.listen(this.config.port || 3000, () => {
        console.log(`HTTP transport listening on port ${this.config.port || 3000}`);
        resolve();
      });
    });
  }

  async gracefulShutdown(timeoutMs: number): Promise<void> {
    if (!this.httpServer) return;

    return new Promise((resolve) => {
      const timeout = setTimeout(() => {
        console.warn("Graceful shutdown timeout, forcing close");
        this.httpServer?.close(() => resolve());
      }, timeoutMs);

      this.httpServer.close(() => {
        clearTimeout(timeout);
        resolve();
      });
    });
  }

  getState(): "DISCONNECTED" | "CONNECTED" => {
    return this.httpServer ? "CONNECTED" : "DISCONNECTED";
  }
}
```

### CORS Middleware

```typescript
// src/mcp/transport/middleware/corsMiddleware.ts
export function corsMiddleware(allowedOrigins: string[] = ["localhost"]) {
  return (req: Request, res: Response, next: (err?: any) => void) => {
    const origin = req.headers.origin;

    if (origin && allowedOrigins.includes(origin)) {
      res.setHeader("Access-Control-Allow-Origin", origin);
      res.setHeader("Vary", "Origin");
    }

    if (req.method === "OPTIONS") {
      res.setHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
      res.setHeader("Access-Control-Max-Age", "3600");
      res.sendStatus(204);
      return;
    }

    next();
  };
}
```

---

## 🚨 Risk Mitigation

| Risk | Prob | Impact | Mitigation |
|:-----|:-----:|:------:|:-----------|
| DNS rebinding attack effectiveness | 🟡 Medium | High | Strict host validation, whitelist |
| CORS misconfiguration (too permissive) | 🔴 Low | High | Unit tests for CORS, security review |
| Connection exhaustion DoS | 🟡 Medium | Medium | Max connections limit + load test |
| Large payload DoS | 🟢 Low | Medium | 10MB limit enforced, configurable |
| Graceful shutdown hangs | 🟢 Low | High | 5s timeout, forced close |

---

## 🎓 Reference

**From EPIC-14-TASK-ENHANCEMENT_2026-03-07.md:**

- ✅ [Full AC (18), detailed hints, enterprise patterns](EPIC-14-TASK-ENHANCEMENT_2026-03-07.md#-task-14-02-http-transport)

---

## 📝 Notes

- **Why 18 hours, not 32h?** HTTP implementation has clear scope (Express, middleware, security). Domain-specific estimation.
- **Why after TASK-14-01?** ITransport interface needed first. Design dependency.
- **Why security middleware?** Corporate deployments require CORS/DNS rebinding protection. P1 requirement.
