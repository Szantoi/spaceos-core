---
id: goal-epic-14
title: "EPIC-14 Goal: Modern MCP Transports & Tool Plugin Architecture"
type: goal
epic: EPIC-14
sphere: mcp-context-server
milestone: M02
created: 2026-03-04
---

# EPIC-14 Goal: Modern MCP Transports & Tool Plugin Architecture

## Executive Summary

**Modernize the MCP server architecture** to support multiple transports (stdio + HTTP) and organize tools
into a plugin system. Move from monolithic tool registration to modular, discoverable tools.
Implement enterprise MCP patterns: resource templates, argument completion sampling, notification debouncing.

**This ensures the server is scalable and follows modern MCP best practices.**

---

## Strategic Context (Vision Goals Addressed)

- **Goal #2:** Database-first runtime — Server no longer depends on hardcoded tool registry
- **Broader goal:** Production readiness — Modern patterns enable future scaling (multi-agent, load balancing)

---

## Key Principles

1. **Multiple transports** — Support stdio (CLI, embedded) + HTTP (remote agents)
2. **Plugin-based tools** — Tools organized into modules; discoverable at runtime
3. **Resource templates** — Dynamic URIs (resource://role/{domain}/{role}) instead of hardcoded endpoints
4. **LLM delegation** — Complex tools can ask LLM for argument clarification
5. **Bulk operation efficiency** — Debounced notifications for seeder + batch operations

---

## Success Criteria

### Transport Abstraction
- [ ] Configurable via `MCP_TRANSPORT` env var (stdio | http)
- [ ] StdioServerTransport: default, works with CLI
- [ ] StreamableHTTPServerTransport: opt-in, works with remote agents
- [ ] Health check endpoint for HTTP transport
- [ ] Graceful shutdown for both transports
- [ ] No breaking changes to existing tool API

### Tool Plugin System
- [ ] Tool modules organized: `src/mcp/tools/*.ts`
  - [ ] bootstrap.ts (bootstrap_agent)
  - [ ] context.ts (request_context, lookup_context)
  - [ ] discovery.ts (discovery tools)
  - [ ] memory.ts (episodic memory tools)
  - [ ] legacy.ts (backward-compat file-based tools)
  - [ ] admin.ts (internal tools)
- [ ] Plugin loader: dynamically loads + registers modules
- [ ] Tool registry: centralized Map<tool_name, handler>
- [ ] Module exports: consistent interface (IToolModule)

### Resource Templates
- [ ] Implement ResourceTemplate pattern for dynamic URIs
- [ ] Resource URIs supported:
  - [ ] `resource://role/{domain}/{role}` → role definition
  - [ ] `resource://workflow/{type}` → workflow definition
  - [ ] `resource://template/{category}` → template
  - [ ] `resource://discovery/{phase}` → discovery workflow
  - [ ] `resource://task/{task_id}` → task context
- [ ] Resource listing: server.listResources()
- [ ] No file paths in resource URIs

### Sampling & Argument Completion
- [ ] Sampling support: complex tools can delegate to LLM
- [ ] Example: `request_context()` with many optional filters
- [ ] LLM clarification when arguments ambiguous
- [ ] Error response includes "needs_clarification" hint

### Notification Debouncing
- [ ] Debounce wrapper for bulk operations
- [ ] Configuration: max 1 notification per 100ms
- [ ] Example: seeder loads 50 roles → 1 notification, not 50
- [ ] Flush() method to force immediate notification

### Testing & Validation
- [ ] Unit test: tool plugin loading
- [ ] Unit test: both transports work
- [ ] Unit test: resource template URI resolution
- [ ] Unit test: sampling + clarification
- [ ] Unit test: notification debouncing
- [ ] E2E test: tool call via stdio transport
- [ ] E2E test: tool call via HTTP transport
- [ ] E2E test: cross-transport consistency

---

## Deliverables

| Deliverable | Type | Location |
|:-----------|:-----|:---------|
| Transport Abstraction | Code | `src/mcp/transport/index.ts` |
| HTTP Transport Setup | Code | `src/mcp/transport/httpTransport.ts` |
| Tool Plugin System | Code | `src/mcp/tools/IToolModule.ts`, `src/mcp/tools/loader.ts` |
| Tool Modules (refactored) | Code | `src/mcp/tools/bootstrap.ts`, `context.ts`, `discovery.ts`, `memory.ts`, `legacy.ts` |
| Resource Template Support | Code | `src/mcp/resources/resourceTemplates.ts` |
| Sampling Delegation | Code | `src/mcp/sampling/samplingUtil.ts` |
| Notification Debouncing | Code | `src/mcp/notifications/debouncer.ts` |
| E2E Test Suite | Tests | `src/tests/e2e/epic-14-modern-mcp.spec.ts` |
| Architecture Documentation | Docs | `Docs/mcp-context-server/architecture/transport-plugin-system.md` |
| Implementation Summary | Report | `implementation-summary/EPIC-14-<date>.md` |

---

## Tool Plugin Module Interface

```typescript
// src/mcp/tools/IToolModule.ts
export interface IToolModule {
  name: string;
  tools: MCP.Tool[];
  handlers: {
    [toolName: string]: (args: any, context: RequestContext) => Promise<ToolResponse>;
  };
}

// Example: src/mcp/tools/bootstrap.ts
export const bootstrapToolModule: IToolModule = {
  name: "bootstrap",
  tools: [
    {
      name: "bootstrap_agent",
      description: "Load complete agent context",
      inputSchema: BOOTSTRAP_INPUT_SCHEMA,
    }
  ],
  handlers: {
    async bootstrap_agent(args, context) {
      // Implementation
    }
  }
};

// Loader: src/mcp/tools/loader.ts
export async function loadToolModules(dir: string): Promise<IToolModule[]> {
  const modules = [];
  const files = fs.readdirSync(dir).filter(f => f.endsWith(".ts"));
  for (const file of files) {
    const module = await import(path.join(dir, file));
    modules.push(module.default || module[Object.keys(module)[0]]);
  }
  return modules;
}
```

---

## Resource Template Example

```typescript
// Resource URI template pattern
server.registerResource(
  new ResourceTemplate("resource://role/{domain}/{role}", "Role definition"),
  async (uri: string, context: RequestContext) => {
    const [domain, role] = uri.split("/").slice(2);
    const roleData = await db.getRole(domain, role);
    return {
      contents: [{ uri, mimeType: "text/markdown", text: roleData.markdown }],
      isComplete: true
    };
  }
);

// Usage: client requests resource://role/engineering/backend_developer
```

---

## Transport Configuration

```typescript
// Environment variable configuration
const transportType = process.env.MCP_TRANSPORT || "stdio"; // "stdio" | "http"
const httpPort = process.env.MCP_PORT || 3000;

let transport;
if (transportType === "http") {
  transport = new StreamableHTTPServerTransport({
    port: httpPort,
    cors: true,
    enableDnsRebindingProtection: true,
  });
} else {
  transport = new StdioServerTransport();
}

server.connect(transport);
```

---

## Task Breakdown

- **TASK-14-01:** Transport abstraction + env configuration
- **TASK-14-02:** HTTP StreamableHTTPServerTransport setup + health check
- **TASK-14-03:** Tool plugin system interface + loader
- **TASK-14-04:** Refactor bootstrap tool into plugin module
- **TASK-14-05:** Refactor context + discovery tools into plugins
- **TASK-14-06:** Refactor memory tools into plugin
- **TASK-14-07:** Legacy tools backward-compat wrapper
- **TASK-14-08:** Resource template support
- **TASK-14-09:** Sampling + argument completion
- **TASK-14-10:** Notification debouncing
- **TASK-14-11:** E2E test: both transports
- **TASK-14-12:** Architecture documentation

---

## Blocks/Enablers

| Dependency | Status | Impact |
|:-----------|:-------|:--------|
| EPIC-09 (SQLite) | 🔄 M02 EPIC-09 | ✅ Tool queries use agent.db |
| EPIC-10 (bootstrap) | 🔄 M02 EPIC-10 | ✅ Refactored into plugin |
| EPIC-11 (context) | 🔄 M02 EPIC-11 | ✅ Refactored into plugin |
| EPIC-12 (memory) | 🔄 M02 EPIC-12 | ✅ Refactored into plugin |
| EPIC-13 (discovery) | 🔄 M02 EPIC-13 | ✅ Refactored into plugin |

---

## Design Decisions

| Decision | Rationale |
|:---------|:----------|
| Plugin-based tools | Scalability: new tools added without monolith refactor |
| Resource templates | Dynamic URIs eliminate file-path parameters |
| HTTP transport optional | No breaking change to CLI users; remote agents gain option |
| Debounced notifications | Bulk operations (seeder) don't flood clients with noise |
| Sampling support | LLM helps with complex argument disambiguation |

---

## Modern MCP Best Practices Applied

- ✅ StreamableHTTPServerTransport for remote agents (vs. legacy SSE)
- ✅ Resource templates for discoverable endpoints
- ✅ Sampling for LLM-assisted argument completion
- ✅ Notification debouncing for bulk operations
- ✅ Structured error responses (MCP protocol)
- ✅ Tool metadata (title, description, schema)

---

## Success Metrics

- ✅ EPIC-14 tasks complete + all tests green
- ✅ `MCP_TRANSPORT=stdio` (default) works
- ✅ `MCP_TRANSPORT=http` works with remote agents
- ✅ All existing tools accessible via both transports
- ✅ Tool plugin system: 5 modules successfully loaded + registered
- ✅ Resource templates: URI patterns resolve correctly
- ✅ Sampling: ambiguous arguments clarified by LLM
- ✅ Debouncing: seeder notification = 1, not N
- ✅ E2E test latency < 100ms per tool call (both transports)

---

## Related Documentation

- [MCP SDK Documentation](https://modelcontextprotocol.io/)
- StreamableHTTPServerTransport: [MCP Spec](https://spec.modelcontextprotocol.io/)
- Resource Templates: [MCP Examples](https://github.com/modelcontextprotocol/servers)
- Sampling: [MCP Sampling Pattern](https://modelcontextprotocol.io/docs/concepts/sampling)
- `src/mcp/` current structure (to be modularized)
