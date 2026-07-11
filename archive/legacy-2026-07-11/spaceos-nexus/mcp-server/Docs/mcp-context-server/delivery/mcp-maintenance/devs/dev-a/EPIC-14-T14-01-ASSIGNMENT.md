---
id: DEV-A-EPIC-14-ASSIGNMENT
title: "Dev A — EPIC-14 Task Assignment (Transport Abstraction + Error Codes)"
created: 2026-03-08
status: "🟡 PENDING_TECH_LEAD_DECISION"
type: dev-assignment
developer: Dev A
epic: EPIC-14
phase: Phase 1 (Foundation)
---

# Dev A — EPIC-14 Task Assignment

**Duration:** 16 hours (base 15h + QA improvement 1h)
**AC Count:** 16 (base 15 + 1 QA improvement)
**Status:** ✅ **READY FOR IMMEDIATE START**

---

## 📋 Your Task: TASK-14-01 — Transport Abstraction

### What You're Building

A factory-pattern based transport abstraction layer that supports both **stdio** (existing) and **HTTP** (new) protocols. This is the foundation for all EPIC-14 features.

### Current Spec

**TASK-14-01.md base AC:** 15 acceptance criteria (generic template)

### NEW AC from Online Research

**Issue #5 — Transport Error Codes:**

Online research identified that transport-specific error codes are essential for debugging and retry logic. Currently, error codes are generic.

**NEW AC-41: Transport Error Enumeration**

```
Given: TransportError enum defined with transport-specific codes
When: Stdio transport encounters error (e.g., broken pipe)
Then: Error code ∈ {CONFIG_INVALID, CONNECTION_FAILED, EPIPE, EOF_UNEXPECTED, INTERNAL_ERROR}

Example:
  - EPIPE: Only stdio (broken pipe)
  - EOF_UNEXPECTED: Only stdio (premature EOF)
  - PORT_IN_USE: Only HTTP (EADDRINUSE)
  - REQUEST_TIMEOUT: Only HTTP (timeout)
```

**Your Change:** Add transport-specific error codes enum + documentation

**Effort:** +1h (total: 16h)

---

## 🎯 Implementation Checklist

### Phase 1: Study & Design (2-3 hours)

- [ ] Read [EPIC-14/state.md](../../../../milestones/milestone_02/epic_14/state.md)
- [ ] Read TASK-14-01.md (full specification)
- [ ] Read EPIC-14-ONLINE-RESEARCH-REVIEW.md (Finding #3 — transport factory validation)
- [ ] Review TypeScript patterns in this document (below)
- [ ] Create ER diagram: Transport → implementations (Stdio, HTTP)

### Phase 2: Implementation (12-13 hours)

#### 2.1 Transport Abstraction Base (5-6h)

```typescript
// src/mcp/transports/ITransport.ts

enum TransportType {
  STDIO = "stdio",
  HTTP = "http"
}

enum TransportState {
  INITIALIZING = "INITIALIZING",
  CONNECTED = "CONNECTED",
  DISCONNECTING = "DISCONNECTING",
  DISCONNECTED = "DISCONNECTED",
  ERROR = "ERROR"
}

enum TransportError {
  // Generic (both transports)
  CONFIG_INVALID = "CONFIG_INVALID",
  CONNECTION_FAILED = "CONNECTION_FAILED",
  TIMEOUT = "TIMEOUT",
  INTERNAL_ERROR = "INTERNAL_ERROR",

  // Stdio-specific
  EPIPE = "EPIPE",                            // NEW: broken pipe
  EOF_UNEXPECTED = "EOF_UNEXPECTED",          // NEW: premature EOF

  // HTTP-specific
  PORT_IN_USE = "PORT_IN_USE",                // NEW: port bind failure
  INVALID_CERTIFICATE = "INVALID_CERTIFICATE", // NEW: TLS error
  REQUEST_TIMEOUT = "REQUEST_TIMEOUT",        // NEW: HTTP request timeout
  PAYLOAD_TOO_LARGE = "PAYLOAD_TOO_LARGE"     // NEW: HTTP 413
}

interface TransportErrorContext {
  code: TransportError;
  message: string;
  transport: TransportType;
  retryable: boolean;
  retryAfterMs?: number;
  originalError?: Error;
}

interface ITransport {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  getState(): TransportState;
  isConnected(): boolean;
  getConfig(): TransportConfig;
  diagnoseError(error: Error): Promise<TransportErrorContext>;  // NEW
}

abstract class BaseTransport implements ITransport {
  protected state: TransportState = TransportState.INITIALIZING;
  protected config: TransportConfig;

  getState(): TransportState {
    return this.state;
  }

  isConnected(): boolean {
    return this.state === TransportState.CONNECTED;
  }

  getConfig(): TransportConfig {
    return this.config;
  }

  abstract connect(): Promise<void>;
  abstract disconnect(): Promise<void>;
  abstract diagnoseError(error: Error): Promise<TransportErrorContext>;
}
```

**AC-01 through AC-06 verification:**

- [ ] AC-01: ITransport interface defined
- [ ] AC-02: TransportType enum prevents typos ("htp" → compile error)
- [ ] AC-03: TransportState tracks lifecycle correctly
- [ ] AC-04: BaseTransport abstract class enforces interface contract
- [ ] AC-05: isConnected() returns true only when CONNECTED
- [ ] AC-06: getConfig() returns transport configuration

---

#### 2.2 Transport Error Context (3-4h) — NEW QA IMPROVEMENT

```typescript
// src/mcp/transports/ErrorDiagnoser.ts

class ErrorDiagnoser {
  // Stdio-specific error diagnosis
  static diagnoseStdioError(error: Error): TransportErrorContext {
    const code = this.mapStdioError(error);
    return {
      code,
      message: this.getErrorMessage(code),
      transport: TransportType.STDIO,
      retryable: this.isRetryable(code),
      retryAfterMs: this.getRetryAfterMs(code),
      originalError: error
    };
  }

  // HTTP-specific error diagnosis
  static diagnoseHTTPError(error: Error, statusCode?: number): TransportErrorContext {
    const code = this.mapHTTPError(error, statusCode);
    return {
      code,
      message: this.getErrorMessage(code),
      transport: TransportType.HTTP,
      retryable: this.isRetryable(code),
      retryAfterMs: this.getRetryAfterMs(code),
      originalError: error
    };
  }

  private static mapStdioError(error: Error): TransportError {
    if (error.message.includes("EPIPE")) return TransportError.EPIPE;
    if (error.message.includes("EOF")) return TransportError.EOF_UNEXPECTED;
    return TransportError.INTERNAL_ERROR;
  }

  private static mapHTTPError(error: Error, statusCode?: number): TransportError {
    if (statusCode === 413) return TransportError.PAYLOAD_TOO_LARGE;
    if (error.message.includes("EADDRINUSE")) return TransportError.PORT_IN_USE;
    if (error.message.includes("ETIMEDOUT")) return TransportError.REQUEST_TIMEOUT;
    if (error.message.includes("CERTIFICATE")) return TransportError.INVALID_CERTIFICATE;
    return TransportError.INTERNAL_ERROR;
  }

  private static isRetryable(code: TransportError): boolean {
    return [
      TransportError.TIMEOUT,
      TransportError.EPIPE,
      TransportError.REQUEST_TIMEOUT
    ].includes(code);
  }

  private static getRetryAfterMs(code: TransportError): number {
    switch(code) {
      case TransportError.REQUEST_TIMEOUT:
        return 5000; // 5s backoff
      case TransportError.EPIPE:
        return 1000; // 1s backoff
      default:
        return 0;
    }
  }

  private static getErrorMessage(code: TransportError): string {
    // Map error codes to human-readable messages
    const messages: Record<TransportError, string> = {
      [TransportError.CONFIG_INVALID]: "Transport configuration is invalid. Check host/port.",
      [TransportError.CONNECTION_FAILED]: "Connection failed. Check network connectivity.",
      [TransportError.EPIPE]: "Broken pipe. Child process died or connection closed.",
      [TransportError.EOF_UNEXPECTED]: "Unexpected EOF. Process terminated unexpectedly.",
      [TransportError.PORT_IN_USE]: "Port already in use. Check if server is already running.",
      [TransportError.REQUEST_TIMEOUT]: "HTTP request timed out. Increase timeout or check server.",
      [TransportError.PAYLOAD_TOO_LARGE]: "HTTP request payload too large (max 10MB).",
      [TransportError.INVALID_CERTIFICATE]: "TLS certificate validation failed.",
      [TransportError.TIMEOUT]: "General timeout. Retrying...",
      [TransportError.INTERNAL_ERROR]: "Internal transport error."
    };
    return messages[code] || "Unknown error";
  }
}
```

**AC-41 verification (NEW):**

- [ ] AC-41: TransportError enum includes stdio-specific codes (EPIPE, EOF_UNEXPECTED)
- [ ] AC-41: TransportError enum includes HTTP-specific codes (PORT_IN_USE, REQUEST_TIMEOUT, PAYLOAD_TOO_LARGE)
- [ ] AC-41: diagnoseError() returns transport-specific error code
- [ ] AC-41: Error messages are actionable (include recovery hint)

**AC-42 through AC-44 verification:**

- [ ] AC-42: Stdio errors mapped correctly (EPIPE pattern recognized)
- [ ] AC-43: Error context propagation includes retryable + retryAfterMs
- [ ] AC-44: Retry guidance provided per error code (REQUEST_TIMEOUT → retryAfterMs=5000)

---

#### 2.3 Transport Factory (4-5h)

```typescript
// src/mcp/transports/TransportFactory.ts

class TransportFactory {
  static validate(config: TransportConfig): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Validate transport type
    if (!Object.values(TransportType).includes(config.type)) {
      errors.push(`Invalid transport type: ${config.type}. Must be one of: ${Object.values(TransportType).join(", ")}`);
    }

    // Validate HTTP-specific config
    if (config.type === TransportType.HTTP) {
      // allow zero for ephemeral (auto-assigned) ports
      if (config.port !== undefined && config.port !== 0 && (config.port < 1 || config.port > 65535)) {
        errors.push(`Invalid port: ${config.port}. Must be between 1-65535 (0 allowed for ephemeral)`);
      }
      if (config.host && config.host.length === 0) {
        errors.push(`Invalid host: empty string`);
      }
    }

    return { valid: errors.length === 0, errors };
  }

  static create(config: TransportConfig): ITransport {
    const { valid, errors } = this.validate(config);
    if (!valid) {
      throw new ConfigurationError(`Invalid transport configuration: ${errors.join("; ")}`);
    }

    switch (config.type) {
      case TransportType.STDIO:
        return new StdioTransport();
      case TransportType.HTTP:
        return new HTTPTransport(config.port || 3000, config.host || "localhost");
      default:
        throw new ConfigurationError(`Unsupported transport type: ${config.type}`);
    }
  }
}
```

**AC-07 through AC-15 verification:**

- [ ] AC-07: Factory validates config before creating transport
- [ ] AC-08: Invalid transport type rejected (typos caught at setup time)
- [ ] AC-09: Port validation enforces 1-65535 range (port 0 permitted for ephemeral binding)
- [ ] AC-10: HTTP config requires valid host
- [ ] AC-11: Defaults applied (port 3000 if not specified)
- [ ] AC-12: ConfigError thrown with clear message
- [ ] AC-13: StdioTransport created for STDIO type
- [ ] AC-14: HTTPTransport created for HTTP type
- [ ] AC-15: Factory cannot create unsupported type (extensible design)

---

### Phase 3: Testing (2-3 hours)

**Unit Tests (8 test cases — UT-01-08 from EPIC-14-QA-TEST-STRATEGY.md):**

```typescript
// src/tests/unit/transports.factory.test.ts

describe("TransportFactory", () => {
  test("UT-01: Valid stdio config", () => {
    const transport = TransportFactory.create({ type: TransportType.STDIO });
    expect(transport).toBeInstanceOf(StdioTransport);
  });

  test("UT-02: Valid HTTP config", () => {
    const transport = TransportFactory.create({ type: TransportType.HTTP, port: 8080 });
    expect(transport).toBeInstanceOf(HTTPTransport);
  });

  test("UT-03: Invalid transport type (BLOCKED BY TYPESCRIPT)", () => {
    // This is caught at compile-time, not runtime
    // Verify no 'htp' typo is possible: TransportType.STDIIO
    @ts-expect-error
    const invalid = TransportType.STDIIO; // Should fail
  });

  test("UT-04: HTTP port < 1 rejected (negative values only)", () => {
    expect(() => {
      TransportFactory.create({ type: TransportType.HTTP, port: -1 });
    }).toThrow(ConfigurationError);
  });

  test("UT-05a: HTTP port 0 (ephemeral) is allowed", () => {
    const transport = TransportFactory.create({ type: TransportType.HTTP, port: 0 }) as HTTPTransport;
    expect(transport.getConfig().port).toBe(0);
  });

  test("UT-05: HTTP port > 65535 rejected", () => {
    expect(() => {
      TransportFactory.create({ type: TransportType.HTTP, port: 99999 });
    }).toThrow(ConfigurationError);
  });

  test("UT-06: Default port applied", () => {
    const transport = TransportFactory.create({ type: TransportType.HTTP }) as HTTPTransport;
    expect(transport.getConfig().port).toBe(3000);
  });

  test("UT-07: Initial state is INITIALIZING", () => {
    const transport = TransportFactory.create({ type: TransportType.STDIO });
    expect(transport.getState()).toBe(TransportState.INITIALIZING);
  });

  test("UT-08: Enum prevents typos at compile-time", () => {
    // All valid types are in the enum
    const validTypes = Object.values(TransportType);
    expect(validTypes).toContain("stdio");
    expect(validTypes).toContain("http");
  });
});

describe("ErrorDiagnoser", () => {
  test("Diagnose EPIPE error (stdio-specific)", () => {
    const error = new Error("EPIPE: write EPIPE");
    const context = ErrorDiagnoser.diagnoseStdioError(error);
    expect(context.code).toBe(TransportError.EPIPE);
    expect(context.transport).toBe(TransportType.STDIO);
    expect(context.retryable).toBe(true);
  });

  test("Diagnose PORT_IN_USE error (HTTP-specific)", () => {
    const error = new Error("bind EADDRINUSE");
    const context = ErrorDiagnoser.diagnoseHTTPError(error);
    expect(context.code).toBe(TransportError.PORT_IN_USE);
    expect(context.transport).toBe(TransportType.HTTP);
    expect(context.retryable).toBe(false);
  });
});
```

**Checklist:**

- [ ] 8 unit tests pass (UT-01-08)
- [ ] Transport factory creates instances correctly
- [ ] Error diagnostics distinguish stdio vs HTTP errors
- [ ] Enum type safety verified

---

### Phase 4: Documentation (1-2 hours)

**Deliverables:**

- [ ] Update TASK-14-01.md with 16 AC (base 15 + QA improvement 1)
- [ ] Create src/mcp/transports/README.md (API guide)
- [ ] Add TypeScript types documentation (jsdoc)

**README example:**

```markdown
# Transport Layer API

## Supported Transports

### Stdio Transport (Default)
- Uses stdin/stdout streams
- Error codes: CONFIG_INVALID, CONNECTION_FAILED, EPIPE, EOF_UNEXPECTED
- Use case: Child process, testing, simple deployments

### HTTP Transport (New)
- HTTP server with graceful shutdown
- Error codes: PORT_IN_USE, REQUEST_TIMEOUT, PAYLOAD_TOO_LARGE, INVALID_CERTIFICATE
- Use case: Production deployments, load balancer integration, scalability

## Error Handling Example

```typescript
try {
  const tool = await mcpServer.callTool("getRoles", {});
} catch (error) {
  const context = await transport.diagnoseError(error);

  if (context.retryable) {
    // Implement retry logic
    await sleep(context.retryAfterMs);
    // Retry...
  } else {
    // Non-retryable error, log and escalate
    console.error(`[${context.code}] ${context.message}`);
  }
}
```

```

---

## ✅ Acceptance Criteria (16 Total)

### Base AC (15):
- [ ] AC-01: ITransport interface with 5+ methods
- [ ] AC-02: TransportType enum (STDIO, HTTP) enforced at compile-time
- [ ] AC-03: TransportState enum tracks lifecycle (INIT → CONNECTED → DISCONNECTED)
- [ ] AC-04: BaseTransport abstract class
- [ ] AC-05: isConnected() == true only when CONNECTED state
- [ ] AC-06: getConfig() returns transport configuration
- [ ] AC-07: TransportFactory validates config before creation
- [ ] AC-08: Invalid type rejected (compile-time type safety)
- [ ] AC-09: Port validation (1-65535 for HTTP)
- [ ] AC-10: Host validation (non-empty for HTTP)
- [ ] AC-11: Default port applied (3000 if not specified)
- [ ] AC-12: ConfigError includes actionable message
- [ ] AC-13: StdioTransport created for STDIO type
- [ ] AC-14: HTTPTransport created for HTTP type
- [ ] AC-15: Extensible design (new transport types can be added)

### QA Improvement AC (1):
- [ ] **AC-41 (NEW):** TransportError enum with transport-specific codes

**Total:** 16 AC → verification checklist above

---

## 📚 Resources

**Online Research:**
- EPIC-14-ONLINE-RESEARCH-REVIEW.md (Finding #3 — Transport Factory Validation)
- EPIC-14-QA-TEST-STRATEGY.md (UT-01-08, IT-14-15)

**Code Examples:**
- TypeScript implementation patterns (TypeScript diagrams included above)
- Error context pattern for retry logic

**Related Tasks:**
- T14-02 depends on your transport abstraction (HTTP implementation)
- T14-01-based error codes used by all other transport consumers

---

## 🔗 QA Validation

**QA Test Matrix:**
- UT-01 through UT-08: Factory + configuration validation
- IT-14: Stdio EPIPE error code mapping
- IT-15: HTTP PORT_IN_USE error code mapping

**QA Sign-Off Criteria:**
- All 8 unit tests passing
- All 16 AC verified
- No compile-time type safety violations
- Error messages human-readable + actionable

**Expected Pass Rate:** 100% (8/8 unit tests)

---

## 📅 Timeline

| Day | Task | Hours | Status |
|:---:|:----:|:-----:|:------:|
| **Day 11** (3/19) | Study + Design + Setup | 3h | ⏳ Awaiting start |
| **Day 12** (3/20) | Implement Transport Base + Factory | 6h | ⏳ Awaiting start |
| **Day 13** (3/21) | Error Context + Tests + Docs | 7h | ⏳ Awaiting start |

**Total:** 16 hours (base 15h + QA improvement 1h)

---

## 🚀 Next Steps (After Tech Lead Decision)

1. **2026-03-14 EOD:** Tech Lead decides (Option A/B/C in TECH-LEAD-EPIC-14-ACTION-PROMPT.md)
2. **Dev A kickoff:** Clarify any AC ambiguities before starting
3. **Begin implementation:** TASK-14-01 can start immediately
4. **2026-03-21 18:00:** Dev A submits PR (16 AC verified)
5. **2026-03-22:** Dev A begins TASK-14-04 (Bootstrap plugin) or peer review

---

**Assignment:** DEV-A-EPIC-14-ASSIGNMENT
**Status:** 🟡 **PENDING_TECH_LEAD_GO/NO-GO**
**Prepared by:** QA Tester Agent
**Date:** 2026-03-08

