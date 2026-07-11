---
id: TASK-14-11-QUICKSTART
title: "TASK-14-11 Day-1 Quickstart — E2E Test Suite"
type: developer-quickstart
owner: "Dev C (or QA Lead)"
duration: "12 hours"
created: 2026-03-11
---

# 🚀 TASK-14-11 Quickstart — E2E Test Suite

**Your mission:** Write comprehensive end-to-end tests verifying all EPIC-14 architecture features work across both transports (stdio + HTTP).

**Duration:** 12 hours (~4 dev days)
**Files you'll touch:** `src/tests/e2e/`, multiple scenario test files
**Predecessor:** TASK-14-01..10 ✅ (all Phase 2 features complete)
**No blockers** → Start after Phase 2 features ready!

---

## What You'll Test

- ✅ Transport abstraction (stdio vs HTTP identical behavior)
- ✅ Plugin registration  (all plugins visible)
- ✅ Resource template resolution (dynamic URIs)
- ✅ Sampling & argument completion (LLM clarification)
- ✅ Notification debouncing (batched updates)
- ✅ Graceful shutdown (HTTP only)

---

## Architecture: E2E Test Pattern

```typescript
// src/tests/e2e/transports.scenario.test.ts

describe("E2E: Transport Abstraction", () => {
  let stdioServer: StdioTransport;
  let httpServer: HttpTransport;

  beforeAll(async () => {
    stdioServer = new StdioTransport();
    httpServer = new HttpTransport({ port: 3000 });
    await Promise.all([
      stdioServer.start(),
      httpServer.start()
    ]);
  });

  afterAll(async () => {
    await Promise.all([
      stdioServer.stop(),
      httpServer.stop()
    ]);
  });

  test("tool invocation returns identical results", async () => {
    // Call bootstrap_agent via stdio
    const stdioResult = await invokeTool(stdioServer, "bootstrap_agent", {
      domain: "engineering"
    });

    // Call bootstrap_agent via HTTP
    const httpResult = await invokeTool(httpServer, "bootstrap_agent", {
      domain: "engineering"
    });

    // Results must be identical
    expect(stdioResult).toEqual(httpResult);
  });
});
```

---

## ✅ Completion Status (2026-03-15)

- **Task status:** ✅ _Completed_
- **Implementation:** E2E test suite covering transport abstraction, plugin registration, resource templates, sampling/argument completion, notification debouncing, and graceful shutdown.
- **Tests:** Integration/E2E tests exist and validate all key EPIC-14 features across stdio + HTTP transports.

---

## Test Scenarios (6 Tests)

### Test 1: Tool Invocation (Both Transports)

**File:** `src/tests/e2e/tools.scenario.test.ts`

```typescript
describe("Scenario: Tool invocation identical via both transports", () => {
  it("should return same bootstrap_agent result", async () => {
    const args = { domain: "engineering", role: "agent" };

    const stdioResult = await stdlib.invoke("bootstrap_agent", args);
    const httpResult = await http.invoke("bootstrap_agent", args);

    expect(stdioResult).toEqual(httpResult);
    expect(stdioResult.context).toBeDefined();
  });

  it("should have same performance (<200ms)", async () => {
    const stdioTime = measureTime(() => stdlib.invoke(...));
    const httpTime = measureTime(() => http.invoke(...));

    expect(stdioTime).toBeLessThan(200);
    expect(httpTime).toBeLessThan(200);
  });
});
```

### Test 2: Plugin Registration

**File:** `src/tests/e2e/plugins.scenario.test.ts`

```typescript
describe("Scenario: All plugins registered and accessible", () => {
  it("should list all plugins", async () => {
    const tools = await server.listTools();

    expect(tools.some(t => t.name === 'bootstrap_agent')).toBe(true);
    expect(tools.some(t => t.name === 'save_episode')).toBe(true);
    expect(tools.some(t => t.name === 'query_memory')).toBe(true);
    expect(tools.length).toBeGreaterThanOrEqual(15);
  });

  it("should work via both transports", async () => {
    const stdioTools = await stdlib.listTools();
    const httpTools = await http.listTools();

    expect(stdioTools.length).toBe(httpTools.length);
  });
});
```

### Test 3: Resource Template Resolution

**File:** `src/tests/e2e/resources.scenario.test.ts`

```typescript
describe("Scenario: Resource templates resolve dynamically", () => {
  it("should resolve role resource", async () => {
    const resource = await server.readResource(
      "resource://role/engineering/agent"
    );

    expect(resource).toContain("role:");
    expect(resource).toContain("permissions:");
  });

  it("should 404 missing resource", async () => {
    expect(async () => {
      await server.readResource("resource://role/nonexistent/role");
    }).rejects.toThrow();
  });
});
```

### Test 4: Sampling/Argument Completion

**File:** `src/tests/e2e/sampling.scenario.test.ts`

```typescript
describe("Scenario: LLM sampling clarifies ambiguous args", () => {
  it("should request & receive clarification", async () => {
    const tool = "request_context";
    const args = {
      domain: "engineering",
      role: "agent",
      filters: []  // Ambiguous!
    };

    // Mock LLM provides clarification
    mockLlmResponse({
      sampled_args: { filters: ["by-role"] }
    });

    const result = await server.invokeTool(tool, args);
    expect(result.status).toBe('success');
    expect(result.context).toBeDefined();
  });
});
```

### Test 5: Notification Debouncing

**File:** `src/tests/e2e/debouncing.scenario.test.ts`

```typescript
describe("Scenario: Bulk operations trigger 1 batched notification", () => {
  it("should batch 50 role saves into 1 notification", async () => {
    let notificationCount = 0;
    server.onNotification(() => { notificationCount++; });

    for (let i = 0; i < 50; i++) {
      await server.saveRole({ name: `role-${i}` });
    }

    await sleep(200);  // Wait for debounce

    // Only 1-2 notifications (batched)
    expect(notificationCount).toBeLessThanOrEqual(2);
  });
});
```

### Test 6: Graceful Shutdown (HTTP only)

**File:** `src/tests/e2e/shutdown.scenario.test.ts`

```typescript
describe("Scenario: HTTP graceful shutdown", () => {
  it("should complete in-flight requests", async () => {
    const slowRequest = server.invokeTool("slow_tool", {});

    await sleep(100);  // Mid-request
    server.shutdown();  // Gracefully shut down

    const result = await slowRequest;
    expect(result.status).toBe('success');  // Request completed!
  });
});
```

---

## Implementation Schedule (4 days)

**Day 1 (3h):**

- Set up E2E test infrastructure
- Create transport abstraction (stdio + HTTP clients)
- Implement `invokeTool()` helper

**Day 2 (3h):**

- Tests 1-2: Tool invocation + plugins
- Run against both transports

**Day 3 (3h):**

- Tests 3-4: Resources + sampling
- Mock LLM responses

**Day 4 (3h):**

- Tests 5-6: Debouncing + shutdown
- Final cleanup + coverage report

---

## Key Test Helpers

```typescript
// Helper: Invoke tool via any transport
async function invokeTool(
  transport: ITransport,
  toolName: string,
  args: any
): Promise<any> {
  const response = await transport.invoke({
    tool: toolName,
    arguments: args
  });
  return response;
}

// Helper: Wait for condition
async function waitFor(condition: () => boolean, maxMs = 5000) {
  const start = Date.now();
  while (!condition() && Date.now() - start < maxMs) {
    await sleep(50);
  }
  if (!condition()) throw new Error("Timeout");
}
```

---

## Completion Sign-Off

When done:

1. All 6 test scenarios implemented ✅
2. Both transports tested ✅
3. All AC from `TASK-14-11-E2E-TESTS.md` passing ✅
4. Coverage report >90% ✅
5. Post completion report!
