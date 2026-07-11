---
id: TASK-14-04
title: "TASK-14-04 & TASK-14-05 — Plugin Tool Exports (Bootstrap, Context, Discovery)"
epic: EPIC-14
milestone: M02
phase: "Phase 1 Foundation (Layer 3)"
created: 2026-03-09
type: "task-assignment"
status: "🟡 READY (Code already implemented, specs formalized 2026-03-09)"
assignee: "Dev C"
effort: "12 hours combined (TASK-14-04: 6h, TASK-14-05: 6h)"
duration: "2 calendar days (2026-03-22 Sat → 2026-03-23 Sun)"
blocker: "TASK-14-03 (Plugin System) — PluginManager + tool registry required"
blocks: ["TASK-14-08 (Resource templates)", "TASK-14-11 (E2E tests)"]
ac_count: "24 combined (12 per task)"
priority: "P0 (Enterprise tool modules)"
---

# TASK-14-04 & TASK-14-05: Plugin Tool Exports

## 📋 Problem Statement

The plugin system (TASK-14-03) provides the infrastructure (loader, registry, lifecycle). Now we need to refactor existing tools into plugin modules and export them via the IToolModule interface:

**TASK-14-04:** Bootstrap plugin — Re-export bootstrap_agent tool module
**TASK-14-05:** Context + Discovery plugins — Re-export context/discovery tool modules

These tasks ensure tools are discoverable at runtime and properly managed by the plugin system.

---

## ✅ TASK-14-04: Bootstrap Plugin (6 hours)

### Acceptance Criteria (12 Total)

**AC-1 through AC-6: Bootstrap Plugin Module**

1. **AC-1: Module Definition** — `src/mcp/tools/bootstrap.ts` exports IToolModule with name="bootstrap"
2. **AC-2: Tool Signature** — Tool "bootstrap_agent" exported with correct description + input schema
3. **AC-3: Handler Implementation** — Handler executes bootstrap logic (loads agent context)
4. **AC-4: Lifecycle Hooks** — Bootstrap module implements lifecycle.onInit() if async setup needed
5. **AC-5: Error Handling** — If bootstrap fails, error properly wrapped in ToolErrorResponse
6. **AC-6: Backward Compatibility** — Tool can be invoked via bootstrap_agent directly (legacy API still works)

**AC-7 through AC-12: Tool Registry Integration**

1. **AC-7: Registration** — When plugin system loads bootstrap module, tool automatically registered in toolRegistry
2. **AC-8: Invocation** — toolRegistry.invoke("bootstrap_agent", args, context) calls bootstrap handler
3. **AC-9: Context Propagation** — RequestContext passed to handler (auth, session info)
4. **AC-10: Timeout Handling** — If bootstrap_agent hangs > 30s, times out gracefully
5. **AC-11: Caching (Optional)** — Bootstrap results cached by sessionId for 5 minutes
6. **AC-12: Cross-Transport Consistency** — bootstrap_agent works identically via stdio and HTTP

### Deliverables

- `src/mcp/tools/bootstrap.ts` (refactored to IToolModule) ✅ Already exists
- `src/tests/unit/bootstrap-tool.test.ts` ✅ Unit tests for bootstrap module
- `src/tests/integration/bootstrap-plugin.test.ts` ✅ Integration tests with plugin system

### Success Criteria

- [ ] All 12 AC implemented
- [ ] Tool works unchanged (regression test)
- [ ] 80%+ test coverage
- [ ] Zero breaking changes

---

## ✅ TASK-14-05: Context + Discovery Plugins (6 hours)

### Acceptance Criteria (12 Total)

**AC-1 through AC-4: Context Plugin Module**

1. **AC-1: Context Module** — `src/mcp/tools/context.ts` exports IToolModule with name="context"
2. **AC-2: Tools Exported** — Context module exports:
   - Tool: `request_context` (fetch request context)
   - Tool: `lookup_context` (lookup by attribute)
3. **AC-3: Handlers** — Both handlers properly parameterized + return ContextResponse
4. **AC-4: Error Handling** — Missing context returns ToolErrorResponse with "Context not found"

**AC-5 through AC-8: Discovery Plugin Module**

1. **AC-5: Discovery Module** — `src/mcp/tools/discovery.ts` exports IToolModule with name="discovery"
2. **AC-6: Tools Exported** — Discovery module exports discovery track tools (all tools discoverable)
3. **AC-7: Discovery Workflow** — discovery tools guide user through workflow phases
4. **AC-8: Integration** — Discovery tools work with DDD aggregate (EPIC-13 preparation)

**AC-9 through AC-12: Cross-Module Coordination**

1. **AC-9: Tool Isolation** — Tools in different modules don't conflict (namespace properly)
2. **AC-10: Shared Context** — Context tool + discovery tool access same RequestContext
3. **AC-11: Performance** — All 3 plugins (bootstrap, context, discovery) load < 200ms total
4. **AC-12: Zero Duplicates** — No tool name appears in multiple modules (registry rejects duplicates)

### Deliverables

- `src/mcp/tools/context.ts` (refactored to IToolModule) ✅ Already exists
- `src/mcp/tools/discovery.ts` (refactored to IToolModule) ✅ Already exists
- `src/tests/unit/context-discovery-tools.test.ts` ✅ Unit tests for both modules
- `src/tests/integration/context-discovery-plugins.test.ts` ✅ Integration tests

### Success Criteria

- [ ] All 12 AC implemented
- [ ] Tools work unchanged (regression test)
- [ ] 80%+ test coverage
- [ ] No tool name conflicts
- [ ] Combined load time < 200ms

---

## 📂 Combined Deliverables

### Code Files

| File | Type | Status |
|:-----|:-----|:-------|
| `src/mcp/tools/bootstrap.ts` | MODIFY | Refactor to IToolModule ✅ Already done |
| `src/mcp/tools/context.ts` | MODIFY | Refactor to IToolModule ✅ Already done |
| `src/mcp/tools/discovery.ts` | MODIFY | Refactor to IToolModule ✅ Already done |
| `src/tests/unit/bootstrap-tool.test.ts` | CREATE | Unit tests ✅ Exists |
| `src/tests/unit/context-discovery-tools.test.ts` | CREATE | Unit tests ✅ Exists |
| `src/tests/integration/plugin-tools.test.ts` | CREATE | Integration tests ✅ Exists |

### Documentation

| File | Type | Purpose |
|:-----|:-----|:--------|
| `TASK-14-04-IMPLEMENTATION-SUMMARY.md` | Doc | Dev C completion (bootstrap) |
| `TASK-14-05-IMPLEMENTATION-SUMMARY.md` | Doc | Dev C completion (context/discovery) |

---

## 🔗 Dependencies

### Depends On

- **TASK-14-03:** Plugin system (PluginManager, registry, IToolModule interface)

### Blocks

- **TASK-14-08:** Resource templates (tools must be registered first)
- **TASK-14-11:** E2E tests (all tools must be available)

---

## 🧪 Testing

### Unit Tests

| Test | AC | Focus |
|:-----|:---|:-------|
| `test_bootstrap_module_export` | AC-1 | IToolModule shape correct |
| `test_bootstrap_tool_invoke` | AC-3 | Handler works |
| `test_context_module_export` | AC-1 | Context module IToolModule |
| `test_context_tools_invoke` | AC-3 | Both context tools work |
| `test_discovery_module_export` | AC-5 | Discovery module shape |
| `test_discovery_tools_invoke` | AC-7 | Discovery tools work |
| `test_no_duplicate_names` | AC-12 | No tool name conflicts |

### Integration Tests

| Test | AC | Scenario |
|:-----|:---|----------|
| `test_plugin_load_bootstrap` | AC-7 | Plugin system loads bootstrap module |
| `test_registry_invoke_bootstrap` | AC-8 | toolRegistry.invoke("bootstrap_agent") works |
| `test_context_propagation` | AC-9 | RequestContext available in handlers |
| `test_all_plugins_load_time` | AC-11 | Bootstrap + context + discovery < 200ms |
| `test_cross_transport_consistency` | AC-12 | Tools work via stdio and HTTP |

---

## 📊 Effort Breakdown (12 hours combined)

| Task | Hours | Focus |
|:------|:-----:|:-------|
| **TASK-14-04** (Bootstrap) | 6h | Module refactoring + tests |
| **TASK-14-05** (Context/Discovery) | 6h | Module refactoring + tests |
| **Total** | **12h** | **2026-03-22 Sat → 2026-03-23 Sun** |

---

## 📝 Implementation Notes

### TASK-14-04: Bootstrap Plugin Refactoring

```typescript
// src/mcp/tools/bootstrap.ts
import { IToolModule } from "./IToolModule";

export const bootstrapPlugin: IToolModule = {
  name: "bootstrap",
  tools: [
    {
      name: "bootstrap_agent",
      description: "Load complete agent context with schemas, roles, definitions",
      inputSchema: {
        type: "object",
        properties: {
          domain: { type: "string" },
          role: { type: "string" }
        },
        required: ["domain", "role"]
      }
    }
  ],
  handlers: {
    async bootstrap_agent(args: any, context: RequestContext) {
      // Existing bootstrap logic, now in plugin Module
      return { /* agent context */ };
    }
  },
  lifecycle: {
    async onInit() {
      // Optional: prepare bootstrap data
    }
  }
};

export default bootstrapPlugin;
```

### TASK-14-05: Context & Discovery Plugin Refactoring

```typescript
// src/mcp/tools/context.ts
export const contextPlugin: IToolModule = {
  name: "context",
  tools: [
    {
      name: "request_context",
      description: "Fetch current request context",
      inputSchema: { type: "object", properties: { sessionId: { type: "string" } } }
    },
    {
      name: "lookup_context",
      description: "Lookup context by attribute",
      inputSchema: { type: "object", properties: { key: { type: "string" } } }
    }
  ],
  handlers: {
    async request_context(args: any, context: RequestContext) {
      return { session: context.session, auth: context.auth };
    },
    async lookup_context(args: any, context: RequestContext) {
      return { value: /* lookup logic */ };
    }
  }
};

// src/mcp/tools/discovery.ts
export const discoveryPlugin: IToolModule = {
  name: "discovery",
  tools: [
    { name: "discovery_start", description: "Start discovery workflow" },
    { name: "discovery_phase", description: "Get current discovery phase" }
  ],
  handlers: {
    // ...
  }
};
```

---

## ✨ Why These Tasks Matter

1. **Plugin Architecture** — Tools are no longer hardcoded; managed via plugin registry
2. **Runtime Discoverability** — New tools can be added without code changes
3. **Lifecycle Management** — Tools can initialize/cleanup resources
4. **Modular Organization** — Clear separation: bootstrap, context, discovery tools

---

## 🎓 Reference

- **TASK-14-03:** Plugin System (prerequisite)
- **EPIC-14 goal.md:** Tool module organization strategy

---

## 📝 Notes

- **Code already exists:** All 3 tool modules (bootstrap, context, discovery) fully implemented in src/mcp/tools/
- **Task is validation + commit:** Dev C verifies all tools work via plugin system, writes implementation summaries
- **No new coding needed:** Just refactoring + testing verification
- **Fast track:** Can be completed in parallel with other layer 2-3 tasks
