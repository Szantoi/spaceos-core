---
id: EPIC-14-TASK-ENHANCEMENT
title: "EPIC-14 Task Enhancement: Concrete AC, DoD, Implementation Guidance (Based on EPIC-11/12 Learnings)"
type: technical-guidance
epic: EPIC-14
milestone: M02
created: 2026-03-07
version: 1.0
---

# 📋 EPIC-14 Task Enhancement & Developer Guidance

## Executive Summary

**Problem:** EPIC-14 has 12 tasks with generic placeholders (0 concrete AC, vague testing specs)

**Solution:** Apply EPIC-11/12 learnings (FSM domain-specificity, concurrency patterns, storage SLAs) to define:

- ✅ Concrete AC (40-50 per task, Given/When/Then format)
- ✅ Testable DoD (unit + E2E coverage, security review gates)
- ✅ Implementation hints (code samples, key gotchas)
- ✅ Dependency order (critical path analysis)
- ✅ Realistic effort (domain-specific, not generic 32h)

**Expected Outcome:** 15-25 AC/task × 12 tasks = ~200+ AC total, 62-80h effort, developer-ready

---

## 🧭 Dependency Graph & Execution Sequence

```
Layer 1 (Infrastructure Foundation - Days 1-2):
  ├─ T14-01: Transport abstraction (4 days)
  │  ├─ Stdio transport (existing, baseline)
  │  ├─ HTTP transport factory pattern
  │  ├─ Health check endpoint
  │  └─ Graceful shutdown
  └─ T14-03: Plugin system (3 days, parallel with T14-01)
     ├─ IToolModule interface
     ├─ Plugin loader (dynamic import)
     ├─ Tool registry Map<name, handler>
     └─ Module resolver

Layer 2 (HTTP-specific - Day 2.5):
  └─ T14-02: HTTP transport (3 days, blocked by T14-01)
     ├─ StreamableHTTPServerTransport setup
     ├─ CORS + DNS rebinding protection
     ├─ /health endpoint
     └─ Connection lifecycle

Layer 3 (Plugin Modules - Days 3-4, parallel with T14-01/02):
  ├─ T14-04: Bootstrap plugin (2 days)
  ├─ T14-05: Context & discovery plugins (3 days)
  ├─ T14-06: Memory plugins (2 days)
  └─ T14-07: Legacy adapter (2 days)

Layer 4 (Enterprise Patterns - Days 4-5, blocked by T14-03):
  ├─ T14-08: Resource templates (2 days)
  ├─ T14-09: Sampling & completion (2 days)
  └─ T14-10: Notification debouncing (2 days)

Layer 5 (Testing & Documentation - Days 6-7, blocked by T14-01-10):
  ├─ T14-11: E2E transport tests (3 days)
  └─ T14-12: Architecture documentation (2 days)

Critical Path: T14-01 (4d) → T14-02 (3d) → [parallel: T14-04-07] → [parallel: T14-08-10] → T14-11 (3d) → T14-12 (2d)
Total Wall-Clock: 8 days (dev1) + parallel (dev2-3) = 6-8 day resource-optimal execution
```

---

## 📌 Task Enhancements (Concrete AC + DoD + Hints)

### ✨ TASK-14-01: Transport Abstraction

**Goal:** Implement abstraction layer that supports stdio (default) and HTTP (opt-in) transports without breaking existing tool APIs.

**AC (15 total):**

1. **AC-01-Setup:** Given env var not set, when server starts, then uses StdioServerTransport (backward compat)
2. **AC-02-EnvVar:** Given `MCP_TRANSPORT=stdio`, when server initializes, then StdioServerTransport active
3. **AC-03-HttpFlag:** Given `MCP_TRANSPORT=http`, when server initializes, then StreamableHTTPServerTransport active
4. **AC-04-PortConfig:** Given `MCP_TRANSPORT=http` and `MCP_PORT=3001`, when server starts, then listens on port 3001 (default 3000)
5. **AC-05-FactoryMethod:** Given transport type string, when factory.create(type) called, then returns corresponding transport + no type errors
6. **AC-06-ExistingToolsWork:** Given existing tool (bootstrap_agent), when called via stdio transport, then response identical to before refactor (regression test)
7. **AC-07-NoHardcoding:** Given any transport type, when tool handler invoked, then no hardcoded transport assumptions in handler code
8. **AC-08-ShutdownStdio:** Given stdio transport active, when SIGTERM received, then gracefully closes stdio connection (< 500ms)
9. **AC-09-ShutdownHttp:** Given HTTP transport active, when SIGTERM received, then closes HTTP listener + drains pending requests (< 2s SLA)
10. **AC-10-ConfigError:** Given invalid `MCP_TRANSPORT=invalid`, when server starts, then throws ConfigurationError with message "Unknown transport type: invalid"
11. **AC-11-ErrorRecovery:** Given transport fails to initialize, when manual retry via factory.create(), then succeeds (no singleton poison)
12. **AC-12-TypeSafety:** Given factory method inputs, when TypeScript strict mode on, then no `any` types in transport abstraction
13. **AC-13-Interfaces:** Given ITransport interface defined, when transport implementations checked, then both stdio + HTTP fully satisfy interface
14. **AC-14-ConnectionState:** Given transport active, when server state queried, then reports "CONNECTED" or "DISCONNECTED" (testable state machine)
15. **AC-15-RequestContext:** Given request routed through transport, when RequestContext middleware applied, then context propagated regardless of transport type

**DoD:**

- ✅ All 15 AC passing (unit tests)
- ✅ Regression test: all existing tools work via stdio
- ✅ E2E test: HTTP transport baseline working
- ✅ Code coverage: ≥ 85% for src/mcp/transport/ (+ branching for error paths)
- ✅ Security review: transport initialization validated (no injection)
- ✅ Documentation: Transport Selection Guide (when to use each)
- ✅ No breaking changes to tool API (verified via existing tool tests)

**Affected Files:**

- [NEW] `src/mcp/transport/index.ts` — Factory + interface
- [NEW] `src/mcp/transport/ITransport.ts` — Interface definition
- [MODIFY] `src/mcp/mcpServer.ts` — Use factory instead of hardcoded StdioServerTransport
- [NEW] `src/tests/unit/transport-abstraction.spec.ts` — 15 unit tests

**Implementation Hints:**

```typescript
// src/mcp/transport/ITransport.ts
export interface ITransport {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  isConnected(): boolean;
  getType(): "stdio" | "http";
  on(event: "request" | "error", handler: Function): void;
}

// src/mcp/transport/index.ts
export function createTransport(type: string): ITransport {
  switch(type) {
    case "stdio":
      return new StdioServerTransport();
    case "http":
      return new StreamableHTTPServerTransport({ port: Number(process.env.MCP_PORT || 3000) });
    default:
      throw new ConfigurationError(`Unknown transport type: ${type}`);
  }
}

// Usage in mcpServer startup:
const transportType = process.env.MCP_TRANSPORT || "stdio";
const transport = createTransport(transportType);
await server.connect(transport);
```

**Risk:** If migrations logic hidden, could break tool invocations. Mitigate: Regression test for all existing tools.

**Effort (realistic):**

- Transport interface design: 2h
- Stdio implementation (refactor existing): 2h
- Factory + error handling: 3h
- Shutdown logic: 2h
- Comprehensive tests: 6h
- **Total: 15h (not generic 32h)** — domain has clear bounded scope

---

### ✨ TASK-14-02: HTTP Transport

**Goal:** Implement StreamableHTTPServerTransport with security (CORS, DNS rebinding protection), health check, and connection lifecycle.

**AC (18 total):**

1. **AC-01-Startup:** Given `MCP_TRANSPORT=http` and `MCP_PORT=3000`, when server starts, then HTTP listener active on 0.0.0.0:3000 (all interfaces)
2. **AC-02-HealthCheck:** Given HTTP server running, when GET /health called, then returns 200 + JSON { status: "ok", uptime: <ms> }
3. **AC-03-HealthLatency:** Given server under load, when /health endpoint queried, then response < 50ms (health check must not block)
4. **AC-04-CORS-Origin:** Given request with Origin: <http://localhost:3001>, when CORS headers checked, then responds with Access-Control-Allow-Origin for allowed origins (not * by default)
5. **AC-05-CORS-Methods:** Given CORS enabled, when OPTIONS preflight sent, then responds with Allow-Methods: [POST, GET, OPTIONS] + Vary: Origin
6. **AC-06-DNSRebindingProtection:** Given request with Host: attacker.com, when Host validation runs, then rejects if not in configured allowlist (security gate)
7. **AC-07-TLSNotRequired:** Given MCP_TRANSPORT=http (note: not HTTPS), when client connects, then plaintext allowed (TLS optional for M02, can enforce later)
8. **AC-08-MaxConcurrentConnections:** Given 100 concurrent requests, when all sent to HTTP endpoint, then < 5 rejected due to resource limits (graceful degradation)
9. **AC-09-RequestTimeout:** Given request with no activity, when timeout threshold (10s default) exceeded, then connection closed + client notified
10. **AC-10-GracefulShutdown:** Given SIGTERM signal, when server received, then:
    - Completes in-flight requests (< 2s deadline)
    - Rejects new requests with 503 Unavailable
    - Closes listener after drain
11. **AC-11-ErrorResponse:** Given malformed HTTP request, when parsed, then responds with 400 Bad Request + error message (not 500)
12. **AC-12-LargePayload:** Given request body > 10MB, when received, then rejected with 413 Payload Too Large (prevents DoS)
13. **AC-13-KeepAlive:** Given HTTP/1.1 client, when Connection: keep-alive sent, then server honors (pools connections)
14. **AC-14-HTTP2Support:** Given HTTP/2 client, when protocol negotiation attempted, then server gracefully handled (HTTP/1.1 acceptable fallback for M02)
15. **AC-15-RequestLogging:** Given request → response cycle, when DEBUG env var set, then logs [timestamp] METHOD path statusCode duration (for troubleshooting)
16. **AC-16-ContextPropagation:** Given HTTP request with Authorization header, when RequestContext middleware applied, then auth context available to tool handlers
17. **AC-17-TransportMetrics:** Given HTTP transport active, when metrics queried, then reports:
    - Requests/sec
    - Avg response time
    - Error rate
    - Active connections
18. **AC-18-Interoperability:** Given tool called via HTTP vs. stdio, when responses compared, then identical (no transport-specific behavior leakage)

**DoD:**

- ✅ All 18 AC passing
- ✅ Load test: 100 concurrent requests, < 5% error rate
- ✅ Security review: DNS rebinding, CORS, payload limits verified
- ✅ Code coverage: ≥ 85% for src/mcp/transport/httpTransport.ts
- ✅ Cross-transport consistency test (same tool, both transports)
- ✅ Documentation: HTTP Transport Setup Guide + troubleshooting

**Affected Files:**

- [NEW] `src/mcp/transport/httpTransport.ts` — HTTP implementation
- [NEW] `src/tests/e2e/http-transport.spec.ts` — End-to-end HTTP tests
- [NEW] `src/tests/integration/transport-consistency.spec.ts` — Cross-transport comparison
- [MODIFY] `src/mcp/transport/index.ts` — Add HTTP factory

**Implementation Hints:**

```typescript
// src/mcp/transport/httpTransport.ts
export class StreamableHTTPServerTransport implements ITransport {
  private server: express.Application;
  private httpServer: http.Server;
  private allowedHosts: Set<string>;

  constructor(config: { port: number; cors: boolean; enableDnsRebindingProtection: boolean }) {
    this.server = express();
    this.allowedHosts = new Set(["localhost", "127.0.0.1"]);

    // Middleware: DNS rebinding protection
    this.server.use((req, res, next) => {
      if (config.enableDnsRebindingProtection && !this.allowedHosts.has(req.hostname)) {
        return res.status(403).json({ error: "Host not allowed" });
      }
      next();
    });

    // Health check
    this.server.get("/health", (req, res) => {
      res.json({ status: "ok", uptime: process.uptime() * 1000 });
    });

    // Tool call endpoint
    this.server.post("/tool", async (req, res) => {
      // Tool invocation logic
    });
  }

  async connect(): Promise<void> {
    this.httpServer = this.server.listen(this.config.port);
  }

  async disconnect(): Promise<void> {
    await new Promise<void>((resolve) => {
      this.httpServer.close(() => resolve());
    });
  }
}
```

**Risk:** HTTP connection pooling could consume memory under attack. Mitigate: Max connections limit + load test.

**Effort (realistic):**

- Express middleware setup: 2h
- Health check + connection lifecycle: 3h
- Security (CORS, DNS rebinding, payload limits): 4h
- Graceful shutdown: 2h
- Integration with tool routing: 3h
- Load testing + optimization: 4h
- **Total: 18h (vs generic 32h)**

---

### ✨ TASK-14-03: Plugin System

**Goal:** Implement dynamic tool plugin loader + centralized registry. Move from hardcoded mcpServer.ts to modular tool modules (`src/mcp/tools/*.ts`).

**AC (20 total):**

1. **AC-01-Interface:** Given IToolModule interface defined, when implementations checked, then all satisfy:
   - name: string
   - tools: MCP.Tool[] (schema, description, name)
   - handlers: { [toolName]: (args, context) => Promise<ToolResponse> }

2. **AC-02-BootstrapModule:** Given src/mcp/tools/bootstrap.ts exists, when loaded, then exports default IToolModule with:
   - name: "bootstrap"
   - tools: [{ name: "bootstrap_agent", ... }]
   - handlers.bootstrap_agent: executable handler

3. **AC-03-LoopDetection:** Given duplicate tool name in two modules, when loader runs, then throws error: "Duplicate tool: bootstrap_agent in bootstrap.ts and admin.ts"

4. **AC-04-DynamicLoad:** Given src/mcp/tools/discovery.ts new file added, when loader.loadModules() called, then automatically discovers and registers without code change

5. **AC-05-Lazy-Load:** Given plugin modules, when server starts, then plugins loaded on-demand (not all at startup) to reduce boot time

6. **AC-06-Registry:** Given all modules loaded, when toolRegistry.get("bootstrap_agent") called, then returns handler sans error

7. **AC-07-ListTools:** Given toolRegistry.listAll() called, when result examined, then returns [{ name, description, inputSchema, module }] sorted by name

8. **AC-08-HandlerInvocation:** Given tool call request with args, when registry.invoke("bootstrap_agent", args, context) executed, then:
   - Correct handler invoked
   - RequestContext propagated
   - Error (if any) properly wrapped in ToolErrorResponse

9. **AC-09-MissingModule:** Given tool module file referenced but deleted, when loader runs, then gracefully logs warning (no crash), tool skipped

10. **AC-10-InvalidModule:** Given module with syntax error in src/mcp/tools/broken.ts, when loaded, then caught + logged (no crash, other modules loaded)

11. **AC-11-Performance:** Given 50 tools loaded, when registry fully initialized, then boot time < 500ms (acceptable for CLI startup)

12. **AC-12-Unregister:** Given toolRegistry.unregister("old_tool"), when called, then subsequent invoke throws "Tool not found: old_tool"

13. **AC-13-ModuleMetadata:** Given module metadata stored, when queried, then returns { module: "bootstrap", version: "1.0", maintainer: "<dev1@joinerytech.com>" }

14. **AC-14-HotReload-Disabled:** Given production env, when hot reload attempt, then ignored (safety gate, hot reload opt-in only in dev)

15. **AC-15-TypeScriptStrict:** Given src/mcp/tools/* files, when TypeScript strict mode on, then no `any` types in module definitions

16. **AC-16-Integration:** Given module loaded, when mcpServer.registerTools() called with registry, then all tools available to clients

17. **AC-17-ToolSchema:** Given tool module with Zod schema, when tool called with bad args, then error response includes schema violation details

18. **AC-18-ErrorContext:** Given handler throws custom error, when caught, then error context (tool name, args, context) included in log/response for debugging

19. **AC-19-PluginDependency:** Given module A depends on configuration from module B, when loader respects load order, then dependency satisfied

20. **AC-20-Testability:** Given plugin system, when unit test written, then can mock specific tool handler without affecting others (no global state pollution)

**DoD:**

- ✅ All 20 AC passing
- ✅ Loader unit tests (happy path + error cases)
- ✅ Plugin interface validation (all modules satisfy)
- ✅ Code coverage: ≥ 85% for loader
- ✅ Performance benchmark: boot time < 500ms
- ✅ Documentation: Plugin Developer Guide (how to add new module)
- ✅ All existing tools refactored to modules

**Affected Files:**

- [NEW] `src/mcp/tools/IToolModule.ts` — Interface definition
- [NEW] `src/mcp/tools/loader.ts` — Dynamic loader
- [NEW] `src/mcp/tools/registry.ts` — Centralized registry
- [MODIFY] `src/mcp/mcpServer.ts` — Use registry instead of inline registration
- [NEW] `src/tests/unit/plugin-system.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/tools/IToolModule.ts
export interface IToolModule {
  name: string;
  tools: MCP.Tool[];
  handlers: { [toolName: string]: ToolHandler };
  metadata?: { version: string; maintainer: string };
}

// src/mcp/tools/loader.ts
export async function loadModules(pluginDir: string): Promise<IToolModule[]> {
  const modules = [];
  const files = readdirSync(pluginDir).filter(f => f.endsWith(".ts") && !f.startsWith("I"));

  for (const file of files) {
    try {
      const module = await import(join(pluginDir, file));
      const mod = module.default || module[Object.keys(module)[0]];
      if (satisfiesInterface(mod)) {
        modules.push(mod);
      } else {
        logger.warn(`Module ${file} does not satisfy IToolModule`);
      }
    } catch (err) {
      logger.error(`Failed to load ${file}:`, err);
    }
  }
  return modules;
}

// src/mcp/tools/registry.ts
export class ToolRegistry {
  private tools = new Map<string, { module: string; handler: ToolHandler }>();

  register(module: IToolModule) {
    for (const tool of module.tools) {
      if (this.tools.has(tool.name)) {
        throw new Error(`Duplicate tool: ${tool.name}`);
      }
      this.tools.set(tool.name, { module: module.name, handler: module.handlers[tool.name] });
    }
  }

  async invoke(toolName: string, args: any, context: RequestContext): Promise<ToolResponse> {
    const entry = this.tools.get(toolName);
    if (!entry) throw new ToolNotFoundError(toolName);
    return entry.handler(args, context);
  }

  listAll(): ToolInfo[] {
    return Array.from(this.tools.entries())
      .map(([name, {module, handler}]) => ({ name, module }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }
}
```

**Risk:** Circular dependencies between modules could cause load failure. Mitigate: Explicit dependency declaration in module interface.

**Effort (realistic):**

- Interface + loader design: 3h
- Registry implementation: 3h
- Refactor existing tools to modules: 6h
- Error handling + resilience: 4h
- Comprehensive tests: 6h
- **Total: 22h**

---

### ✨ TASK-14-04: Bootstrap Plugin

**Goal:** Refactor bootstrap_agent tool into plugin module (`src/mcp/tools/bootstrap.ts`) with concrete AC for agent initialization sequence.

**AC (16 total):**

1. **AC-01-ModuleStructure:** Given bootstrap.ts, when examined, then exports default IToolModule with name="bootstrap"
2. **AC-02-ToolName:** Given module tools array, when checked, then contains tool { name: "bootstrap_agent", description: "Load complete agent context", ... }
3. **AC-03-InputSchema:** Given bootstrap_agent tool, when schema validated, then requires:
   - agentId: string (UUID format)
   - sessionId: string (optional, defaults to new UUID)
   - discoveryPhase: string (enum: "discovery", "delivery", "evaluation")
4. **AC-04-OutputSchema:** Given tool executed successfully, when response examined, then includes:
   - agentContext: { roles, permissions, workflows, templates }
   - sessionId: string
   - timestamp: ISO8601
5. **AC-05-LoadRoles:** Given bootstrap_agent called with discoveryPhase="discovery", when executed, then loads all discovery roles from database (not hardcoded)
6. **AC-06-LoadPermissions:** Given roles loaded, when RBAC filter applied, then agent only sees tools/resources matching role permissions
7. **AC-07-WorkflowLoading:** Given bootstrap_agent called, when workflow definitions requested, then returns FSM state machines for agent's assigned workflows
8. **AC-08-TemplateLoading:** Given bootstrap_agent called, when templates requested, then returns discovery/delivery/evaluation templates for phase
9. **AC-09-SessionCreation:** Given new session requested, when bootstrap_agent executes, then creates agent_session record in database with:
   - sessionId, agentId, createdAt, state="INITIALIZED", context
10. **AC-10-SessionRecovery:** Given existing sessionId provided, when bootstrap_agent called, then loads previous session context (resumption scenario)
11. **AC-11-Validation:** Given invalid agentId format, when bootstrap_agent called, then throws ValidationError with message: "Invalid agentId: <val> (expected UUID)"
12. **AC-12-PermissionCheck:** Given agent with restricted permissions, when bootstrap_agent called, then response only includes permitted tools/workflows
13. **AC-13-ErrorHandling:** Given database query fails, when bootstrap_agent executes, then caught + responded with ToolErrorResponse { code: "DB_ERROR", message: "..." }
14. **AC-14-Performance:** Given bootstrap_agent called, when execution completes, then runtime < 500ms (fast agent startup SLA)
15. **AC-15-Idempotency:** Given bootstrap_agent called twice with same sessionId, when second call executed, then returns same context + no duplicate session records
16. **AC-16-ContextMigration:** Given old agent context format, when bootstrap_agent called, then migrates to v1.0 schema (backward compatibility)

**DoD:**

- ✅ All 16 AC passing
- ✅ Integration test with real database
- ✅ Performance benchmark: < 500ms
- ✅ Code coverage: ≥ 85%
- ✅ Documentation: Bootstrap Agent Usage Guide

**Affected Files:**

- [NEW] `src/mcp/tools/bootstrap.ts` — Bootstrap plugin
- [MODIFY] `src/mcp/AgentSessionBootstrap.ts` — Integration with bootstrap tool
- [NEW] `src/tests/integration/bootstrap-plugin.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/tools/bootstrap.ts
export const bootstrapToolModule: IToolModule = {
  name: "bootstrap",
  tools: [{
    name: "bootstrap_agent",
    description: "Load complete agent context (roles, permissions, workflows, templates)",
    inputSchema: z.object({
      agentId: z.string().uuid("Invalid agentId format"),
      sessionId: z.string().uuid().optional(),
      discoveryPhase: z.enum(["discovery", "delivery", "evaluation"]),
    }),
  }],
  handlers: {
    async bootstrap_agent(args, context) {
      const { agentId, sessionId: providedSessionId, discoveryPhase } = args;
      const sessionId = providedSessionId || uuidv4();

      // Load agent + roles from database
      const agent = await db.getAgent(agentId);
      const roles = await db.getRolesByPhase(discoveryPhase);
      const filteredRoles = rbacFilter.filterRoles(roles, agent.permissions);

      // Create or recover session
      const session = providedSessionId
        ? await db.getSession(providedSessionId)
        : await db.createSession(sessionId, agentId, { roles: filteredRoles });

      return {
        success: true,
        data: {
          agentContext: {
            agentId,
            sessionId,
            roles: filteredRoles,
            permissions: agent.permissions,
            workflows: await db.getWorkflows(agent.role),
            templates: await db.getTemplates(discoveryPhase),
          },
          timestamp: new Date().toISOString(),
        }
      };
    }
  }
};
```

**Risk:** Database load could cause latency spikes. Mitigate: Caching + async preloading.

**Effort (realistic):**

- Module refactoring: 2h
- Schema validation + error handling: 3h
- Database integration: 4h
- Performance optimization: 3h
- Tests + documentation: 4h
- **Total: 16h**

---

### ✨ TASK-14-05: Context & Discovery Plugins

**Goal:** Refactor request_context + lookup_context (context plugin) and discovery tools into plugin modules.

**AC (22 total - Context + Discovery combined)**

**Context Plugin (request_context, lookup_context):**

1. **AC-01:** Given request_context tool called, when executed, then returns current RequestContext (session, agent, user, audit info)
2. **AC-02:** Given RequestContext includes identity fields, when schema validated, then contains: sessionId, agentId, userId, roleId, permissions, auditTrail
3. **AC-03:** Given lookup_context called with ContextId, when executed, then retrieves stored context from database (historical queries)
4. **AC-04:** Given context lookup for non-existent ID, when called, then throws ContextNotFoundError (not silent failure)
5. **AC-05:** Given RequestContext with restricted permissions, when context exposed to tool, then sensitive fields redacted based on agent role
6. **AC-06:** Given context middleware propagates state, when multiple tools chained, then original context preserved (no mutation)

**Discovery Plugin (discovery tools - phase discovery, workflow discovery, role discovery):**

1. **AC-07:** Given discovery_roles tool called with phase="discovery", when executed, then returns all roles applicable to discovery phase (filtered by agent permissions)
2. **AC-08:** Given discovery_workflows tool called, when executed, then returns FSM workflows + transitions for current agent context
3. **AC-09:** Given discovery_templates tool called with category="requirements", when executed, then returns templates for discovering requirements (templating system integration)
4. **AC-10:** Given discovery tool called with filter (e.g., domain="engineering"), when executed, then filtered results returned (not all docs)
5. **AC-11:** Given discovery results large (1000+ items), when pagination params provided, then respects limit/offset (chunked responses)
6. **AC-12:** Given discovery_roles called with unknown phase, when executed, then throws ValidationError: "Unknown phase: <val>" (not silent null)
7. **AC-13:** Given discovery search term provided, when full-text search executed, then returns matching roles/workflows/templates (semantic relevance)
8. **AC-14:** Given discovery tool called in rapid succession, when requests batched, then uses notification debouncing (< 5 notifications/sec SLA)

**Module Structure:**

1. **AC-15:** Given src/mcp/tools/context.ts exists, when examined, then exports default IToolModule with tools=[request_context, lookup_context]
2. **AC-16:** Given src/mcp/tools/discovery.ts exists, when examined, then exports default IToolModule with tools=[discovery_roles, discovery_workflows, discovery_templates, discovery_search]
3. **AC-17:** Given context + discovery plugins loaded, when tool count checked, then registry contains all 6 tools (no duplicates)
4. **AC-18:** Given discovery plugin, when RequestContext middleware propagated, then agent permissions applied to discovery results (RBAC enforcement)

**Error Handling & Resilience:**

1. **AC-19:** Given database query timeout in discovery, when executed, then caught + returned with graceful error message (not crash)
2. **AC-20:** Given context lookup with malformed ID, when validated, then throws ValidationError with clear message
3. **AC-21:** Given permission check fails, when discovery executed, then returns empty result (not 403, as per UX)
4. **AC-22:** Given discovery performance > 1s, when monitored, then logged + metrics tracked for optimization

**DoD:**

- ✅ All 22 AC passing
- ✅ Context propagation tested (chain of 5+ tools)
- ✅ RBAC enforcement validated (agent with restricted perms sees filtered results)
- ✅ Performance: discovery < 500ms for 1000 items
- ✅ Code coverage: ≥ 85%

**Affected Files:**

- [NEW] `src/mcp/tools/context.ts` — Context plugin (request_context, lookup_context)
- [NEW] `src/mcp/tools/discovery.ts` — Discovery plugin (4 tools)
- [MODIFY] `src/mcp/RbacFilter.ts` — RBAC enforcement in discovery
- [NEW] `src/tests/integration/context-discovery-plugins.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/tools/context.ts
export const contextToolModule: IToolModule = {
  name: "context",
  tools: [
    {
      name: "request_context",
      description: "Get current request context (session, agent, permissions, audit trail)",
      inputSchema: z.object({}).strict(),
    },
    {
      name: "lookup_context",
      description: "Look up historical context by ID",
      inputSchema: z.object({ contextId: z.string().uuid() }),
    }
  ],
  handlers: {
    async request_context(args, context) {
      // Return current context (from RequestContext middleware)
      return {
        sessionId: context.sessionId,
        agentId: context.agentId,
        userId: context.userId,
        roleId: context.roleId,
        permissions: context.permissions,
        auditTrail: await db.getAuditTrail(context.sessionId),
      };
    },
    async lookup_context(args, context) {
      const { contextId } = args;
      const stored = await db.getStoredContext(contextId);
      if (!stored) throw new ContextNotFoundError(contextId);
      return stored;
    }
  }
};

// src/mcp/tools/discovery.ts (excerpt)
export const discoveryToolModule: IToolModule = {
  name: "discovery",
  tools: [/* 4 tools */],
  handlers: {
    async discovery_roles(args, context) {
      const { phase = "discovery", filter = {} } = args;
      let roles = await db.getRolesByPhase(phase);

      // Apply RBAC filter based on agent permissions
      roles = rbacFilter.filterRoles(roles, context.permissions);

      // Apply additional filters
      if (filter.domain) roles = roles.filter(r => r.domain === filter.domain);

      return { roles, count: roles.length };
    },
    // ... discovery_workflows, discovery_templates, discovery_search
  }
};
```

**Effort (realistic):**

- Refactor context tools to module: 3h
- Discovery tools implementation: 6h
- RBAC enforcement integration: 4h
- Performance optimization (caching, pagination): 4h
- Tests + documentation: 5h
- **Total: 22h**

---

### ✨ TASK-14-06: Memory Plugins

**Goal:** Implement episodic memory tools into plugin module (`src/mcp/tools/memory.ts`) with concrete AC for store/retrieve episode workflows.

**AC (18 total):**

1. **AC-01:** Given episodic_memory_store tool called with episode data, when executed, then stores episode in SQLite with:
   - episodeId (UUID)
   - assetLabel (episode name)
   - tool_calls (array of calls)
   - artifacts (array of generated outputs)
   - outcome (success/failure)
   - timestamp (ISO8601)

2. **AC-02-Performance:** Given 100 episodes stored in bulk, when batch insert executed, then completes in < 5s total (SLA from EPIC-12)

3. **AC-03-Indexing:** Given episodes stored, when FTS5 index queried, then full-text search works (< 50ms for 1000 episodes)

4. **AC-04-Semantic:** Given episodes stored, when ChromaDB embedding sync queued, then semantic search available (< 200ms)

5. **AC-05-Retrieve:** Given episodic_memory_retrieve called with query, when executed, then returns top-K matching episodes (keyword + semantic combined)

6. **AC-06-Filtering:** Given retrieve called with filters { from: date, to: date, role: "engineer" }, when applied, then returns episodes matching all filters

7. **AC-07-Ranking:** Given 10 matching episodes returned, when ranked by relevance, then most relevant first (based on keyword score + semantic similarity)

8. **AC-08-Error-Handling:** Given malformed episode data, when store called, then throws ValidationError with schema violation details (not stored)

9. **AC-09-Idempotency:** Given same episode stored twice, when deduplicated, then only 1 record in database (based on episodeId or hash)

10. **AC-10-Graceful-Degradation:** Given ChromaDB unavailable, when semantic search attempted, then falls back to FTS5 (search still works)

11. **AC-11-SchemaMigration:** Given old episode format (v0), when stored, then migrated to v1 schema transparently

12. **AC-12-ContextIntegration:** Given episode stored, when RequestContext propagated, then episode tagged with agentId + sessionId for audit trail

13. **AC-13-Search-Performance:** Given 10,000 episodes in database, when search executed, then < 250ms response time (SLA from EPIC-12)

14. **AC-14-BatchStore:** Given episodic_memory_store_batch called with 50 episodes, when executed, then all stored + 1 notification sent (debouncing from T14-10)

15. **AC-15-Expiry:** Given episode created > 90 days ago, when cleanup job runs, when archived to cold storage (optional, M02 can skip)

16. **AC-16-Privacy:** Given episode contains sensitive data, when retrieved by agent with restricted permissions, then redacted fields removed (RBAC)

17. **AC-17-Sampling:** Given episode retrieval ambiguous (5+ equally relevant results), when sampling enabled, then tool asks LLM for clarification (T14-09 integration)

18. **AC-18-Module:** Given src/mcp/tools/memory.ts exists, then exports IToolModule with tools=[episodic_memory_store, episodic_memory_retrieve, episodic_memory_search] + metadata

**DoD:**

- ✅ All 18 AC passing
- ✅ Storage integration test (insert + retrieve round-trip)
- ✅ FTS5 + ChromaDB index tests
- ✅ Performance benchmark: < 5s for 100 bulk inserts
- ✅ Graceful degradation test (ChromaDB down)
- ✅ Code coverage: ≥ 85%

**Affected Files:**

- [NEW] `src/mcp/tools/memory.ts` — Memory plugin
- [MODIFY] `src/metadata/ContextSchema.ts` — Episode schema integration
- [NEW] `src/tests/integration/memory-plugin.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/tools/memory.ts
export const memoryToolModule: IToolModule = {
  name: "memory",
  tools: [
    { name: "episodic_memory_store", ... },
    { name: "episodic_memory_retrieve", ... },
    { name: "episodic_memory_search", ... }
  ],
  handlers: {
    async episodic_memory_store(args, context) {
      const { episodes } = args; // Array or single episode
      const toStore = Array.isArray(episodes) ? episodes : [episodes];

      // Validate schema
      for (const ep of toStore) {
        episodeSchema.parse(ep); // Throws ValidationError if invalid
      }

      // Store in SQLite + queue ChromaDB sync
      const stored = await db.storeEpisodes(toStore, context.sessionId);
      await embeddingQueue.enqueue({ episodes: stored, action: "sync_chromadb" });

      return { stored: stored.length, ids: stored.map(e => e.episodeId) };
    },

    async episodic_memory_retrieve(args, context) {
      const { query, limit = 10, filters = {} } = args;

      // FTS5 search
      const ftsResults = await db.searchFTS5(query, limit, filters);

      // ChromaDB search (if available)
      let chromaResults = [];
      try {
        chromaResults = await chromadb.search(query, limit, filters);
      } catch (err) {
        logger.warn("ChromaDB unavailable, using FTS5 only:", err);
      }

      // Merge + rank
      const merged = mergeAndRankResults(ftsResults, chromaResults);
      return { episodes: merged.slice(0, limit), count: merged.length };
    }
  }
};
```

**Effort (realistic):**

- Memory plugin structure: 2h
- Store logic + validation: 4h
- Retrieve + search + ranking: 5h
- FTS5 + ChromaDB integration: 5h
- Error handling + graceful degradation: 3h
- Tests + documentation: 4h
- **Total: 23h**

---

### ✨ TASK-14-07: Legacy Adapter

**Goal:** Implement backward compatibility adapter for old file-based tool definitions (if any) into plugin system.

**AC (12 total):**

1. **AC-01:** Given legacy file-based tools exist (e.g., `tools/legacy/*.md`), when loaded, then converted to IToolModule format
2. **AC-02:** Given legacy tool schema as Zod, when parsed, then migrated to MCP.Tool schema (compatible)
3. **AC-03:** Given legacy handler as Promise function, when invoked, then works seamlessly with new router
4. **AC-04:** Given legacy + modern tools mix, when registry checked, then both present + no conflicts
5. **AC-05:** Given deprecation warning needed, when legacy tool used, then logged warning: "Tool <name> is deprecated, migrate to src/mcp/tools/<module>"
6. **AC-06:** Given legacy tool removed, when adapter checked, then gracefully ignored (no crash)
7. **AC-07:** Given legacy + modern tool same name, when conflict detected, then modern tool takes precedence + warning logged
8. **AC-08:** Given legacy toolmapping configured, when adapter applied, then transforms old API → new API
9. **AC-09:** Given performance impact, when legacy tools loaded, then boot time increase < 100ms (acceptable overhead for backward compat)
10. **AC-10:** Given legacy tool tests exist, when compatibility tests run, then responses identical to old implementation (regression test)
11. **AC-11:** Given migration deadline set, when legacy tool age > 6 months, then error raised: "Tool <name> expired, must migrate to new plugin system"
12. **AC-12:** Given all legacy tools migrated, when legacy adapter checked, then empty (ready for removal)

**DoD:**

- ✅ All 12 AC passing
- ✅ Backward compatibility regression tests
- ✅ Performance overhead < 100ms
- ✅ Documentation: Migration Guide (legacy → plugin)

**Affected Files:**

- [NEW] `src/mcp/tools/legacy.ts` — Legacy adapter
- [NEW] `src/tests/integration/legacy-adapter.spec.ts`

**Effort (realistic):**

- Legacy adapter structure: 2h
- Compatibility layer: 3h
- Migration guidance: 2h
- Regression tests: 3h
- **Total: 10h (lighter task, fewer AC)**

---

### ✨ TASK-14-08: Resource Templates

**Goal:** Implement ResourceTemplate pattern for dynamic URI resolution (resource://role/{domain}/{role}, resource://task/{id}, etc.).

**AC (16 total):**

1. **AC-01:** Given ResourceTemplate("resource://role/{domain}/{role}", ...) registered, when client requests resource, then URI pattern matched + parameters extracted
2. **AC-02:** Given client requests resource://role/engineering/backend_developer, when resolved, then returns role definition markdown
3. **AC-03:** Given template for resource://workflow/{type}, when resolved, then returns FSM state machine JSON
4. **AC-04:** Given template for resource://task/{task_id}, when resolved, then returns task AC + DoD + implementation hints
5. **AC-05:** Given multiple templates registered, when listResources() called, then returns all with descriptions (discoverable)
6. **AC-06:** Given invalid URI pattern (resource://unknown/{id}), when resolved, then throws error: "Unknown resource template: unknown"
7. **AC-07:** Given template parameter missing (resource://role/engineering/), when parsed, then throws error: "Missing parameter: role"
8. **AC-08:** Given resource template with caching enabled, when same resource requested twice, then cached result returned (< 10ms second request)
9. **AC-09:** Given resource cache stale, when invalidation triggered, then cache cleared + fresh data loaded on next request
10. **AC-10:** Given permissions checked, when resource requested by agent lacking permission, then access denied (RBAC gate)
11. **AC-11:** Given resource requested with audit context, when returned, then tagged with requestor + timestamp
12. **AC-12:** Given large resource (> 1MB), when transferred, then chunked or streamed (not buffered entirely in memory)
13. **AC-13:** Given resource template MIMEtype specified, when content served, then correct Content-Type header set
14. **AC-14:** Given unknown resource requested, when resolved, then graceful error (not 500, but 404)
15. **AC-15:** Given performance monitored, when template resolution > 100ms, then logged for investigation
16. **AC-16:** Given all 5 templates (role, workflow, task, template, discovery) implemented, when feature complete, then all working

**DoD:**

- ✅ All 16 AC passing
- ✅ Resource integration test (all 5 template types)
- ✅ RBAC enforcement test
- ✅ Caching performance test
- ✅ Code coverage: ≥ 85%

**Affected Files:**

- [NEW] `src/mcp/resources/resourceTemplates.ts` — Template definitions + resolver
- [NEW] `src/tests/integration/resource-templates.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/resources/resourceTemplates.ts
export class ResourceTemplateResolver {
  private templates = new Map<string, ResourceTemplate>();
  private cache = new Map<string, any>();

  register(pattern: string, description: string, resolver: (uri: string, context: RequestContext) => Promise<any>) {
    const template = new ResourceTemplate(pattern, description, resolver);
    this.templates.set(pattern, template);
  }

  async resolve(uri: string, context: RequestContext): Promise<any> {
    // Check cache
    const cacheKey = `${uri}:${context.agentId}`;
    if (this.cache.has(cacheKey)) return this.cache.get(cacheKey);

    // Find matching template
    const template = Array.from(this.templates.values()).find(t => t.matches(uri));
    if (!template) throw new ResourceNotFoundError(uri);

    // Check permissions
    if (!rbacFilter.canAccess(template.requiredPermission, context.permissions)) {
      throw new UnauthorizedError(`Access denied to resource: ${uri}`);
    }

    // Resolve
    const result = await template.resolve(uri, context);
    this.cache.set(cacheKey, result);
    return result;
  }

  listResources(): ResourceInfo[] {
    return Array.from(this.templates.values()).map(t => ({
      pattern: t.pattern,
      description: t.description
    }));
  }
}

// Register templates
const resolver = new ResourceTemplateResolver();
resolver.register("resource://role/{domain}/{role}", "Role definition", async (uri, context) => {
  const [domain, role] = extractParams(uri, "resource://role/{domain}/{role}");
  return await db.getRole(domain, role);
});
```

**Effort (realistic):**

- Resource template design: 3h
- URI pattern matching: 3h
- Caching + invalidation: 2h
- RBAC integration: 2h
- Tests + documentation: 4h
- **Total: 14h**

---

### ✨ TASK-14-09: Sampling & Argument Completion

**Goal:** Implement sampling support for complex tools so LLM can clarify ambiguous arguments via McpServer.createMessage().

**AC (14 total):**

1. **AC-01:** Given tool with many optional parameters (e.g., request_context), when invoked with ambiguous args, then sampling enabled
2. **AC-02:** Given sampling mode enabled, when arguments disambiguation needed, then McpServer.createMessage() called to ask LLM
3. **AC-03:** Given LLM clarification response received, when applied, then retry tool with clarified args
4. **AC-04:** Given LLM declines to clarify, when timeout reached, then tool called with defaults or error response
5. **AC-05:** Given sampling config { enabled: true, timeout: 5000 }, when applied, then clarification rounds timeout < 5s
6. **AC-06:** Given complex enum field (e.g., discoveryPhase with 10+ options), when sampling invoked, then LLM presented with options
7. **AC-07:** Given user-provided args + LLM-clarified args conflict, when resolved, then user args take precedence (safety principle)
8. **AC-08:** Given sampling enabled, when error context logged, then includes: tool name, ambiguous params, clarification request, LLM response
9. **AC-09:** Given performance considered, when sampling used, then response latency < 10s total (5s LLM call + overhead)
10. **AC-10:** Given sampling disabled (prod env), when tool called, then fails with clear error (not silent fallback)
11. **AC-11:** Given multiple rounds of clarification needed, when iteration limit reached (N=3), then returns error: "Unable to clarify arguments after 3 attempts"
12. **AC-12:** Given sampling info in response, when returned, then includes metadata: { clarificationRequest, llmResponse, numRounds }
13. **AC-13:** Given tool with sampling decorator, when defined, then specification clear + exported in module
14. **AC-14:** Given sampling tests, when run, then mock LLM works + clarification validated

**DoD:**

- ✅ All 14 AC passing
- ✅ Integration test: request_context with sampling
- ✅ Mock LLM for testing
- ✅ Code coverage: ≥ 85%

**Affected Files:**

- [NEW] `src/mcp/sampling/samplingUtil.ts` — Sampling coordinator
- [MODIFY] `src/mcp/mcpRouter.ts` — Integration with tool routing
- [NEW] `src/tests/integration/sampling.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/sampling/samplingUtil.ts
export async function sampleForClarification(
  toolName: string,
  ambiguousArgs: any,
  server: McpServer,
  timeout: number = 5000
): Promise<any> {
  // Build clarification request
  const clarificationMsg = `Tool "${toolName}" received ambiguous arguments. Please clarify:\n${JSON.stringify(ambiguousArgs, null, 2)}`;

  // Use server.createMessage() to ask LLM
  const response = await Promise.race([
    server.createMessage({
      role: "user",
      content: clarificationMsg,
    }),
    new Promise((_, reject) => setTimeout(() => reject(new TimeoutError("Sampling timeout")), timeout))
  ]);

  // Parse LLM response
  const clarified = parseClusterResponse(response, toolName);
  return clarified;
}

// Decorator for tools with sampling
export function WithSampling(options: { timeout?: number } = {}) {
  return (target: any, propertyKey: string, descriptor: PropertyDescriptor) => {
    const originalMethod = descriptor.value;
    descriptor.value = async function(args: any, context: RequestContext) {
      if (isAmbiguous(args)) {
        const clarified = await sampleForClarification(propertyKey, args, context.server, options.timeout);
        args = { ...args, ...clarified };
      }
      return originalMethod.call(this, args, context);
    };
    return descriptor;
  };
}
```

**Effort (realistic):**

- Sampling coordinator design: 4h
- McpServer.createMessage() integration: 3h
- Argument parsing + validation: 3h
- Tests with mock LLM: 4h
- **Total: 14h**

---

### ✨ TASK-14-10: Notification Debouncing

**Goal:** Implement debouncer wrapper for bulk operationsto limit notifications to < 5/sec SLA.

**AC (13 total):**

1. **AC-01:** Given bulk operation (e.g., seeder loads 50 roles), when notifications emitted, then debounced to max 1 per 100ms
2. **AC-02:** Given 100 items processed rapidly, when debouncer applied, then only ~10 notifications sent (not 100)
3. **AC-03:** Given debounce timer active, when new notification queued, then merged with pending (coalesced)
4. **AC-04:** Given notification queue > 100 items pending, when flush() called manually, then all sent immediately (< 500ms)
5. **AC-05:** Given debouncer config { maxNotificationsPerSec: 5, delayMs: 200 }, when applied, then respected
6. **AC-06:** Given debouncer idle, when notification sent, then immediately delivered (no artificial delay)
7. **AC-07:** Given debouncer shutdown requested, when close() called, then pending notifications flushed + no more accepted
8. **AC-08:** Given performance monitored, when debouncer active, then overhead < 10ms per notification
9. **AC-09:** Given notification payload accumulation, when memory limit approached, then older notifications dropped (FIFO)
10. **AC-10:** Given client listening to notifications, when debounced batch received, then processes all in batch (not individual)
11. **AC-11:** Given error in notification handler, when caught, then debouncer continues (not crashed)
12. **AC-12:** Given debouncer used throughout codebase, when audit logged, then "NOTIFY_DEBOUNCED: count=50, compressed_to=5" entries visible
13. **AC-13:** Given debouncer metrics exported, when monitored, then reports: total_notifications, debounced_count, avg_delay

**DoD:**

- ✅ All 13 AC passing
- ✅ Load test: 1000 notifications in bulk → verify compression ratio
- ✅ Memory profiling (pending queue doesn't leak)
- ✅ Code coverage: ≥ 85%

**Affected Files:**

- [NEW] `src/mcp/notifications/debouncer.ts` — Debouncer implementation
- [MODIFY] `src/mcp/tools/memory.ts` — Use debouncer for batch operations
- [NEW] `src/tests/unit/notification-debouncer.spec.ts`

**Implementation Hints:**

```typescript
// src/mcp/notifications/debouncer.ts
export class NotificationDebouncer {
  private pendingNotifications: any[] = [];
  private timer: NodeJS.Timeout | null = null;
  private readonly delayMs: number;
  private readonly maxPerSec: number;
  private lastFlushTime: number = 0;

  constructor(delayMs: number = 100, maxPerSec: number = 5) {
    this.delayMs = delayMs;
    this.maxPerSec = maxPerSec;
  }

  enqueue(notification: any): void {
    this.pendingNotifications.push(notification);

    // If idle, schedule flush
    if (!this.timer) {
      this.scheduleFlush();
    }
  }

  private scheduleFlush(): void {
    // Enforce rate limit
    const timeSinceLastFlush = Date.now() - this.lastFlushTime;
    const minIntervalMs = 1000 / this.maxPerSec;
    const delayNeeded = Math.max(0, minIntervalMs - timeSinceLastFlush);

    this.timer = setTimeout(() => this.flush(), Math.max(this.delayMs, delayNeeded));
  }

  flush(): void {
    if (this.pendingNotifications.length === 0) {
      this.timer = null;
      return;
    }

    const toNotify = this.pendingNotifications.splice(0);
    this.lastFlushTime = Date.now();

    // Send coalesced notification
    metrics.recordDebounced(toNotify.length, 1);
    this.emitNotification(toNotify);

    this.timer = null;

    // Schedule next flush if still pending
    if (this.pendingNotifications.length > 0) {
      this.scheduleFlush();
    }
  }

  private emitNotification(notifications: any[]): void {
    // Emit to subscribed clients
    eventEmitter.emit("notification_batch", { items: notifications, count: notifications.length });
  }

  close(): void {
    if (this.timer) clearTimeout(this.timer);
    this.flush(); // Flush pending
  }
}

// Usage in memory.ts
const debouncer = new NotificationDebouncer(100, 5);
await db.storeEpisodes(episodes, sessionId);
for (const ep of episodes) {
  debouncer.enqueue({ type: "episode_stored", episodeId: ep.id });
}
debouncer.flush(); // On completion
```

**Effort (realistic):**

- Debouncer algorithm: 3h
- Rate limiting logic: 2h
- Integration with notification system: 2h
- Tests + metrics: 4h
- **Total: 11h**

---

### ✨ TASK-14-11: E2E Transport Tests

**Goal:** Comprehensive end-to-end testing across both transports (stdio + HTTP) with cross-transport consistency validation.

**AC (16 total):**

1. **AC-01:** Given stdio transport active, when bootstrap_agent tool called, then response includes agent context (happy path)
2. **AC-02:** Given HTTP transport active, when bootstrap_agent tool called, then response identical to stdio response (cross-transport consistency)
3. **AC-03:** Given tool error in handler, when both transports used, then error response format identical
4. **AC-04:** Given large payload (1MB), when sent via HTTP transport, then transferred successfully + integrity verified
5. **AC-05:** Given connection timeout, when HTTP request idle > 10s, then server closes + client sees error (not hang)
6. **AC-06:** Given concurrent requests (100 simultaneous) via HTTP, when load applied, then all complete + response times SLA met
7. **AC-07:** Given RBAC filter active, when agent with restricted permissions calls tool via both transports, then same filtered results
8. **AC-08:** Given tool invocation sequence (10 chained calls), when executed via both transports, then final state identical
9. **AC-09:** Given transport switch mid-session (stdio → HTTP), when reconnected, then session state recovered (resumption)
10. **AC-10:** Given authentication headers in HTTP request, when RBAC applied, then permissions enforced (security validation)
11. **AC-11:** Given plugin system loaded, when using both transports, then all plugins accessible (no transport binding)
12. **AC-12:** Given notification debouncer active, when rapid bulk operations via HTTP, then debouncing effective (SLA met)
13. **AC-13:** Given resource template resolution, when accessed via both transports, then cached results consistent
14. **AC-14:** Given error logging enabled, when errors occur on both transports, then logs identical format (debugging consistency)
15. **AC-15:** Given performance benchmarked, when comparing transports, then HTTP overhead < 20% vs stdio
16. **AC-16:** Given all 5 enterprise patterns (sampling, debouncing, resource templates, RBAC, notifications), when tested E2E, then fully integrated

**DoD:**

- ✅ All 16 AC passing
- ✅ E2E test suite (50+ test cases covering all AC)
- ✅ Cross-transport consistency report
- ✅ Performance benchmarks documented
- ✅ Code coverage: ≥ 90% (E2E critical)

**Affected Files:**

- [NEW] `src/tests/e2e/epic-14-transports.spec.ts` — Full E2E suite
- [NEW] `src/tests/e2e/cross-transport-consistency.spec.ts` — Consistency validator
- [NEW] `src/tests/e2e/transport-performance.spec.ts` — Benchmarks

**Implementation Hints:**

```typescript
// src/tests/e2e/epic-14-transports.spec.ts
describe("EPIC-14 E2E: Transports", () => {
  describe("Stdio Transport", () => {
    test("AC-01: bootstrap_agent via stdio", async () => {
      const server = await createServer("stdio");
      const response = await server.invokeTool("bootstrap_agent", { agentId: uuid(), discoveryPhase: "discovery" });
      expect(response.agentContext).toBeDefined();
    });
  });

  describe("HTTP Transport", () => {
    test("AC-02: bootstrap_agent via HTTP matches stdio", async () => {
      const stdioServer = await createServer("stdio");
      const httpServer = await createServer("http", { port: 3000 });

      const stdioResp = await stdioServer.invokeTool("bootstrap_agent", args);
      const httpResp = await fetch("http://localhost:3000/tool", { method: "POST", body: JSON.stringify({ tool: "bootstrap_agent", ...args }) });

      expect(httpResp.data).toEqual(stdioResp);
    });
  });

  describe("Cross-Transport Consistency", () => {
    test("AC-08: Tool invocation sequence consistency", async () => {
      const stdiServer = await createServer("stdio");
      const httpServer = await createServer("http");

      const tools = ["bootstrap_agent", "request_context", "discovery_roles", ...];
      const stdioStates = [];
      const httpStates = [];

      for (const tool of tools) {
        stdioStates.push(await stdioServer.invoke(tool, args));
        httpStates.push(await httpServer.invoke(tool, args));
      }

      // Final state should be identical
      expect(stdioStates[stdioStates.length - 1]).toEqual(httpStates[httpStates.length - 1]);
    });
  });

  describe("Performance", () => {
    test("AC-15: HTTP overhead < 20%", async () => {
      const stdioTimes = await benchmarkTransport("stdio", 1000 iterations);
      const httpTimes = await benchmarkTransport("http", 1000 iterations);

      const overhead = (httpTimes.avg - stdioTimes.avg) / stdioTimes.avg;
      expect(overhead).toBeLessThan(0.2);
    });
  });
});
```

**Effort (realistic):**

- E2E test framework setup: 3h
- Cross-transport test cases: 8h
- Performance benchmarking: 4h
- Consistency validation logic: 3h
- Report generation: 2h
- **Total: 20h**

---

### ✨ TASK-14-12: Architecture Documentation

**Goal:** Document EPIC-14 architecture including transport abstraction, plugin system, resource templates, enterprise patterns.

**AC (12 total):**

1. **AC-01:** Given architecture document created, when examined, then includes:
   - Overview diagram (transport + plugin layer)
   - Transport abstraction design (stdio vs HTTP, factory pattern)
   - Plugin system architecture (loader, registry, module interface)

2. **AC-02:** Given resource template documentation, when read, then explains:
   - URI pattern matching algorithm
   - Resource caching strategy
   - RBAC enforcement in resource resolution

3. **AC-03:** Given enterprise patterns section, when reviewed, then covers:
   - Sampling & argument completion workflow
   - Notification debouncing algorithm
   - Performance SLAs for each pattern

4. **AC-04:** Given implementation guidance, when followed, then new developers can add:
   - New plugin module in < 30 min
   - New transport in < 2 hours
   - New resource template in < 1 hour

5. **AC-05:** Given troubleshooting section, when consulted, then covers:
   - Common transport issues (connection timeout, DNS rebinding)
   - Plugin loading failures (module not found, duplicate names)
   - Performance bottlenecks (slow resource resolution)

6. **AC-06:** Given documentation examples, when tried, then all runnable:
   - Code snippets use actual APIs
   - Paths correct and verified
   - Output samples generated from real runs

7. **AC-07:** Given architecture visualizations, when reviewed, then include:
   - ASCII diagrams for text format
   - Mermaid diagrams for flow (if supported)
   - Component interaction sequence

8. **AC-08:** Given API reference, when consulted, then documents all public interfaces:
   - ITransport, IToolModule, ResourceTemplateResolver
   - Method signatures with parameters + return types
   - Error codes + explanations

9. **AC-09:** Given security considerations, when read, then covers:
   - DNS rebinding protection rationale
   - RBAC enforcement points
   - Data privacy in resource templates

10. **AC-10:** Given performance tuning section, when followed, then provides:
    - Profiling instructions
    - Caching strategies
    - Batch operation optimization (debouncing)

11. **AC-11:** Given migration guide, when used, then explains:
    - Migrating legacy tools to plugin system
    - Transport selection decision tree
    - Deprecation timeline

12. **AC-12:** Given documentation complete, when reviewed by team, then:
    - No typos or broken links
    - All code examples working
    - Consistent terminology + style guide applied

**DoD:**

- ✅ All 12 AC satisfied
- ✅ Documentation review + sign-off
- ✅ Code examples verified runnable
- ✅ Diagrams readable + accurate
- ✅ No broken internal links

**Affected Files:**

- [NEW] `Docs/mcp-context-server/architecture/EPIC-14-transport-plugin-architecture.md` — Main architecture doc
- [NEW] `Docs/mcp-context-server/guides/plugin-developer-guide.md` — Plugin creation guide
- [NEW] `Docs/mcp-context-server/references/transport-api.md` — API reference
- [NEW] `Docs/mcp-context-server/troubleshooting/epic-14-diagnostics.md` — Troubleshooting

**Effort (realistic):**

- Architecture document: 3h
- Implementation guides: 2h
- API reference generation: 2h
- Code examples + verification: 3h
- Diagrams: 2h
- **Total: 12h**

---

## 📊 EPIC-14 Summary: Effort Realism & Sequencing

| Task | Title | Concrete AC | Est. Effort | Critical Path | Notes |
|:-----|:------|:-----------:|:-----------:|:-------------:|:------|
| T14-01 | Transport Abstraction | 15 | 15h | Day 1-2 | Foundation layer |
| T14-02 | HTTP Transport | 18 | 18h | Day 2-3 (blocked by T14-01) | Security focus |
| T14-03 | Plugin System | 20 | 22h | Day 1-2 (parallel T14-01) | Core infrastructure |
| T14-04 | Bootstrap Plugin | 16 | 16h | Day 3 (blocked by T14-03) | First plugin |
| T14-05 | Context & Discovery | 22 | 22h | Day 3-4 (blocked by T14-03) | RBAC integration |
| T14-06 | Memory Plugins | 18 | 23h | Day 3-4 (parallel T14-05) | Storage integration |
| T14-07 | Legacy Adapter | 12 | 10h | Day 4-5 (blocked by T14-03) | Backward compatibility |
| T14-08 | Resource Templates | 16 | 14h | Day 4-5 (blocked by T14-03) | Dynamic URIs |
| T14-09 | Sampling & Completion | 14 | 14h | Day 5 (blocked by T14-03) | LLM delegation |
| T14-10 | Notification Debouncing | 13 | 11h | Day 5 (blocked by T14-03) | Performance pattern |
| T14-11 | E2E Transport Tests | 16 | 20h | Day 6-7 (blocked by T14-02) | Cross-transport validation |
| T14-12 | Architecture Docs | 12 | 12h | Day 7 (blocked by T14-11) | Documentation |
| **TOTAL** | | **182 AC** | **197h** | **7-8 days** (resource-optimal) | Realistic for 3 devs |

**vs Original Estimate:**

- Original (generic): 12 tasks × 32h = 384h (wildly unrealistic)
- **Realistic (domain-specific): 197h across 7-8 days with 3 developers = ~66h per dev** ✅ Achievable

**Critical Path:** T14-01 (15h) → T14-02 (18h) → T14-11 (20h) → T14-12 (12h) = **65h serial**

- Parallelization: T14-03/04/05/06/07 can run while T14-02 in progress
- Wall-clock: 7-8 days (assuming 8h/day dev focus)

---

## 🚀 Developer Recommendations

1. **Start T14-01 + T14-03 in parallel** (both days 1-2) — Infrastructure foundation
2. **Dev1:** T14-01 (Transport abstraction) → T14-02 (HTTP transport)
3. **Dev2:** T14-03 (Plugin system) → T14-04/05/06 (Plugin modules)
4. **Dev3:** T14-07/08/09/10 (Enterprise patterns) + T14-11 (E2E tests)
5. **Tech Lead:** Review architecture decisions, gate T14-02 security, prepare T14-12

**Expected Quality:**

- ✅ 182 AC across tasks (all testable, specified)
- ✅ ≥ 85% code coverage per task
- ✅ Cross-transport consistency validated
- ✅ Enterprise patterns (sampling, debouncing) working
- ✅ Security review gates passed
- ✅ Performance SLAs met (< 250ms combined search, < 5s storage,< 500ms boot)

---

## Next Steps: Apply to TASK Files

I will now update all 12 TASK files in epic_14/tasks/ with these concrete AC, DoD, implementation hints, risks, and realistic effort estimates.

Ready to proceed? ✨
