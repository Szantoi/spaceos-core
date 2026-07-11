---
id: DEV-C-EPIC-14-ASSIGNMENT
title: "Dev C — EPIC-14 Task Assignment (Plugin System + Dependency Resolution)"
created: 2026-03-08
status: "🟡 PENDING_TECH_LEAD_DECISION"
type: dev-assignment
developer: Dev C
epic: EPIC-14
phase: Phase 1 (Foundation)
---

# Dev C — EPIC-14 Task Assignment

**Duration:** 26 hours (base 22h + QA improvement 4h)
**AC Count:** 24 (base 20 + 4 QA improvement)
**Status:** ✅ **READY FOR IMMEDIATE START**
**Dependency Note:** Requires TASK-14-01 (Dev A) transport baseline first

---

## 📋 Your Task: TASK-14-03 — Plugin System + Dynamic Module Loading

### What You're Building

A modern plugin architecture that supports dynamic module loading with:

- Dependency resolution (plugin A can depend on plugin B)
- Lifecycle hooks (onInit, onDestroy, onError)
- Error recovery (optional plugins fail gracefully)
- Type-safe plugin definitions via TypeScript decorators

### Current Spec

**TASK-14-03.md base AC:** 20 acceptance criteria (plugin loader design)

### NEW AC from Online Research (P1 HIGH PRIORITY)

**Issue #2 — Plugin Dependency Resolution & Lifecycle:**

Online research identified that plugin systems need dependency resolution to prevent circular dependencies and lifecycle hooks for proper initialization/cleanup.

**NEW AC-31 through AC-35: Plugin Dependencies & Lifecycle**

```
NEW AC-31: Plugin Dependency Declaration
Given: Plugin manifest defines {name: "memory", dependencies: ["bootstrap", "context"]}
When: loadPlugin("memory") called
Then: bootstrap + context loaded first (dependencies satisfied)

NEW AC-32: Circular Dependency Detection
Given: Plugin A depends on B, Plugin B depends on A
When: Attempt to load either plugin
Then: CircularDependencyError thrown (not silent failure)

NEW AC-33: Plugin Lifecycle Hooks
Given: Plugin implements {lifecycle: {onInit, onDestroy, onError}}
When: pluginManager.loadPlugin()
Then: lifecycle.onInit() called before registration
      lifecycle.onDestroy() called during unload

NEW AC-34: Optional Plugin Error Recovery
Given: Optional plugin fails during load (e.g., DB migration fails)
When: Server continues starting
Then: Plugin marked in failedPlugins list, server proceeds

NEW AC-35: Plugin Health Status
Given: Some plugins loaded, some failed
When: getPluginStatus() called
Then: Returns {healthy: ["bootstrap", "context"], failed: ["memory"]}
```

**Your Change:** Build PluginManager with dependency resolver + lifecycle hooks

**Effort:** +4h (total: 26h)

---

## 🔴 **P1 HIGH PRIORITY: Plugin reliability critical for modular architecture**

Without proper dependency resolution:

- 🟠 Circular dependencies cause infinite loops or crashes
- 🟠 Optional plugin failures crash entire server
- 🟠 No way to know which plugins are active vs dead
- 🟠 Difficult to extend system with new plugins

---

## 🎯 Implementation Checklist

### Phase 1: Study & Design (3-4 hours)

- [ ] Read [EPIC-14/state.md](../../../../milestones/milestone_02/epic_14/state.md)
- [ ] Read TASK-14-03.md (full specification)
- [ ] Read EPIC-14-ONLINE-RESEARCH-REVIEW.md (Finding #1 — plugin architecture)
- [ ] Review npm plugin system examples (webpack, rollup, babel)
- [ ] Create design diagram: PluginManager → PluginDependencyResolver → PluginRegistry

### Phase 2: Implementation (18-20 hours)

#### 2.1 Plugin Interface & Decorator (3-4h)

```typescript
// src/plugins/IPlugin.ts

export enum PluginStatus {
  NOT_LOADED = "NOT_LOADED",
  LOADING = "LOADING",
  LOADED = "LOADED",
  FAILED = "FAILED",
  UNLOADING = "UNLOADING"
}

export interface IToolModule {
  name: string;
  version: string;
  // NEW: dependencies array
  dependencies?: string[];
  // NEW: lifecycle hooks
  lifecycle?: {
    onInit?: () => Promise<void>;
    onDestroy?: () => Promise<void>;
    onError?: (error: Error) => void;
  };
  tools: Map<string, ToolHandler>;
}

export interface PluginManifest {
  name: string;
  version: string;
  entry: string;              // Path to plugin module
  dependencies?: string[];
  critical?: boolean;        // If true, fail if plugin fails to load
}

// Decorator pattern for plugin metadata
export function Plugin(config: { name: string; version: string; dependencies?: string[] }) {
  return function<T extends { new(...args: any[]): {} }>(constructor: T) {
    (constructor as any)._pluginConfig = config;
    return constructor;
  };
}

export function Tool(config: { name: string; description: string }) {
  return function(target: any, propertyKey: string, descriptor: PropertyDescriptor) {
    if (!target._tools) target._tools = [];
    target._tools.push({ name: config.name, method: propertyKey, description: config.description });
    return descriptor;
  };
}

// Example Plugin:
@Plugin({ name: "bootstrap", version: "1.0.0" })
class BootstrapPlugin {
  @Tool({ name: "createSession", description: "Create MCP session" })
  createSession(config: any) {
    return { sessionId: "..." };
  }
}
```

**AC-01 through AC-08 verification:**

- [ ] AC-01: IToolModule interface defined
- [ ] AC-02: PluginStatus enum tracks lifecycle
- [ ] AC-03: Plugin decorator for metadata
- [ ] AC-04: Tool decorator for method tagging
- [ ] AC-05: PluginManifest schema defined
- [ ] AC-06: Version string validated (semver)
- [ ] AC-07: Tool names unique per plugin
- [ ] AC-08: Dependency names formatted correctly (lowercase)

---

#### 2.2 Plugin Dependency Resolver (5-6h) — NEW QA IMPROVEMENT

```typescript
// src/plugins/PluginDependencyResolver.ts

interface DependencyGraph {
  [pluginName: string]: string[];  // name -> dependencies
}

class CircularDependencyError extends Error {
  constructor(cycle: string[]) {
    super(`Circular dependency detected: ${cycle.join(" -> ")}`);
  }
}

class PluginDependencyResolver {
  private graph: DependencyGraph = {};
  private visited = new Set<string>();
  private recursionStack = new Set<string>();

  // AC-31: Resolve dependencies
  async resolveDependencies(pluginName: string, manifests: Map<string, PluginManifest>): Promise<string[]> {
    const manifest = manifests.get(pluginName);
    if (!manifest) throw new Error(`Unknown plugin: ${pluginName}`);

    // AC-32: Detect circular dependencies
    this.detectCycles(pluginName, manifests);

    // Return load order (dependencies first)
    return this.getLoadOrder(pluginName, manifests);
  }

  // AC-32: Circular dependency detection
  private detectCycles(pluginName: string, manifests: Map<string, PluginManifest>, path: string[] = []): void {
    if (this.recursionStack.has(pluginName)) {
      // Cycle detected!
      const cycleStart = path.indexOf(pluginName);
      const cycle = [...path.slice(cycleStart), pluginName];
      throw new CircularDependencyError(cycle);
    }

    if (this.visited.has(pluginName)) return;

    this.recursionStack.add(pluginName);

    const manifest = manifests.get(pluginName);
    if (manifest?.dependencies) {
      for (const dep of manifest.dependencies) {
        this.detectCycles(dep, manifests, [...path, pluginName]);
      }
    }

    this.recursionStack.delete(pluginName);
    this.visited.add(pluginName);
  }

  // Get topological sort (load order)
  private getLoadOrder(pluginName: string, manifests: Map<string, PluginManifest>): string[] {
    const order: string[] = [];
    const visited = new Set<string>();

    const visit = (name: string) => {
      if (visited.has(name)) return;
      visited.add(name);

      const manifest = manifests.get(name);
      if (manifest?.dependencies) {
        for (const dep of manifest.dependencies) {
          visit(dep);
        }
      }

      order.push(name);
    };

    visit(pluginName);
    return order;
  }
}

// Usage example:
const resolver = new PluginDependencyResolver();
try {
  const loadOrder = await resolver.resolveDependencies("memory", pluginManifests);
  // Load in order: loadOrder[0], loadOrder[1], ...
} catch (error) {
  if (error instanceof CircularDependencyError) {
    console.error("Cannot load plugin: " + error.message);
  }
}
```

**AC-31/AC-32 verification (NEW):**

- [ ] **AC-31:** Dependencies resolved in correct order (dependencies first)
- [ ] **AC-32:** Circular dependency detected and thrown (not infinite loop)

---

#### 2.3 Plugin Lifecycle Manager (5-6h) — NEW QA IMPROVEMENT

```typescript
// src/plugins/PluginManager.ts

class PluginManager {
  private pluginRegistry = new Map<string, IToolModule>();
  private pluginStatus = new Map<string, PluginStatus>();
  private loadedPlugins = new Set<string>();
  private failedPlugins = new Map<string, Error>();
  private resolver = new PluginDependencyResolver();

  // AC-33: Lifecycle hooks
  async loadPlugin(pluginName: string, isCritical: boolean = false): Promise<void> {
    try {
      this.pluginStatus.set(pluginName, PluginStatus.LOADING);

      // Resolve dependencies
      const manifest = await this.loadManifest(pluginName);
      const dependencies = await this.resolver.resolveDependencies(pluginName, this.manifests);

      // Load dependencies first
      for (const dep of dependencies.slice(0, -1)) {  // All except self
        if (!this.loadedPlugins.has(dep)) {
          await this.loadPlugin(dep, true);  // Dependencies are "critical"
        }
      }

      // Import module
      const moduleModule = await import(`./plugins/${pluginName}.ts`);
      const pluginClass = moduleModule.default;
      const instance = new pluginClass();

      // Call onInit lifecycle hook (AC-33)
      if (instance.lifecycle?.onInit) {
        console.log(`[PLUGIN] Calling onInit for ${pluginName}`);
        await instance.lifecycle.onInit();
      }

      // Register tools
      this.pluginRegistry.set(pluginName, instance);
      this.loadedPlugins.add(pluginName);
      this.pluginStatus.set(pluginName, PluginStatus.LOADED);
      console.log(`[PLUGIN] Loaded: ${pluginName}`);

    } catch (error) {
      this.pluginStatus.set(pluginName, PluginStatus.FAILED);
      this.failedPlugins.set(pluginName, error as Error);

      // AC-34: Optional plugin fails gracefully
      if (isCritical) {
        throw new Error(`Critical plugin failed: ${pluginName}`, { cause: error });
      } else {
        console.warn(`[PLUGIN] Optional plugin failed: ${pluginName}`, error);
      }
    }
  }

  // AC-33: onDestroy lifecycle hook
  async unloadPlugin(pluginName: string): Promise<void> {
    const plugin = this.pluginRegistry.get(pluginName);
    if (!plugin) return;

    this.pluginStatus.set(pluginName, PluginStatus.UNLOADING);

    if (plugin.lifecycle?.onDestroy) {
      console.log(`[PLUGIN] Calling onDestroy for ${pluginName}`);
      await plugin.lifecycle.onDestroy();
    }

    this.pluginRegistry.delete(pluginName);
    this.loadedPlugins.delete(pluginName);
    this.pluginStatus.set(pluginName, PluginStatus.NOT_LOADED);
    console.log(`[PLUGIN] Unloaded: ${pluginName}`);
  }

  // AC-35: Plugin health status
  getPluginStatus(): { healthy: string[]; failed: string[]; status: Map<string, PluginStatus> } {
    return {
      healthy: Array.from(this.loadedPlugins),
      failed: Array.from(this.failedPlugins.keys()),
      status: this.pluginStatus
    };
  }

  // Tool dispatcher
  async invokeTool(toolName: string, args: any): Promise<any> {
    for (const [pluginName, plugin] of this.pluginRegistry) {
      if (plugin.tools.has(toolName)) {
        const handler = plugin.tools.get(toolName)!;
        return handler(args);
      }
    }
    throw new Error(`Unknown tool: ${toolName}`);
  }

  // Error callback (called by plugin lifecycle)
  onPluginError(pluginName: string, error: Error): void {
    if (this.pluginRegistry.get(pluginName)?.lifecycle?.onError) {
      this.pluginRegistry.get(pluginName)!.lifecycle!.onError!(error);
    }
  }
}

// Usage:
const manager = new PluginManager();

// Load critical plugin (fails if error)
await manager.loadPlugin("bootstrap", true);

// Load optional plugin (fails silently)
await manager.loadPlugin("memory", false);

// Check status
const status = manager.getPluginStatus();
console.log(status);
// Output: { healthy: ['bootstrap', 'discovery'], failed: ['memory'], ... }

// Invoke tool
const result = await manager.invokeTool("getRoles", {});
```

**AC-33/AC-34/AC-35 verification (NEW):**

- [ ] **AC-33:** onInit called during load, onDestroy called during unload
- [ ] **AC-34:** Optional plugin fails → logged warning, server continues
- [ ] **AC-35:** getPluginStatus() returns correct healthy/failed lists

---

#### 2.4 Plugin Registry & Tool Routing (3-4h)

```typescript
// src/plugins/PluginRegistry.ts

export class PluginRegistry {
  private plugins = new Map<string, IToolModule>();
  private toolToPlugin = new Map<string, string>();  // tool name -> plugin name

  registerPlugin(plugin: IToolModule): void {
    this.plugins.set(plugin.name, plugin);

    // Index all tools
    for (const toolName of plugin.tools.keys()) {
      if (this.toolToPlugin.has(toolName)) {
        throw new Error(`Tool ${toolName} already registered in plugin ${this.toolToPlugin.get(toolName)}`);
      }
      this.toolToPlugin.set(toolName, plugin.name);
    }
  }

  getTool(toolName: string): [IToolModule, ToolHandler] | null {
    const pluginName = this.toolToPlugin.get(toolName);
    if (!pluginName) return null;

    const plugin = this.plugins.get(pluginName);
    if (!plugin) return null;

    const handler = plugin.tools.get(toolName);
    if (!handler) return null;

    return [plugin, handler];
  }

  listTools(): Array<{ toolName: string; pluginName: string; description?: string }> {
    const tools = [];
    for (const [toolName, pluginName] of this.toolToPlugin) {
      tools.push({ toolName, pluginName });
    }
    return tools;
  }
}
```

**AC-09 through AC-20 verification:**

- [ ] AC-09: Tool registration enforces uniqueness
- [ ] AC-10: getTool() returns correct plugin + handler
- [ ] AC-11: listTools() returns all registered tools
- [ ] AC-12-20: (baseline AC for plugin system features)

---

### Phase 3: Testing (4-6 hours)

**Unit Tests (10 test cases — UT-09-18 from EPIC-14-QA-TEST-STRATEGY.md):**

```typescript
// src/tests/unit/plugins.resolver.test.ts

describe("PluginDependencyResolver", () => {
  test("UT-09: Load single plugin", async () => {
    const manager = new PluginManager();
    await manager.loadPlugin("bootstrap");

    const status = manager.getPluginStatus();
    expect(status.healthy).toContain("bootstrap");
  });

  test("UT-10: Plugin dependency met (bootstrap → memory)", async () => {
    const manager = new PluginManager();
    const manifests = new Map([
      ["bootstrap", { name: "bootstrap", version: "1.0.0" }],
      ["memory", { name: "memory", version: "1.0.0", dependencies: ["bootstrap"] }]
    ]);

    const resolver = new PluginDependencyResolver();
    const order = await resolver.resolveDependencies("memory", manifests);

    // bootstrap must come before memory
    expect(order.indexOf("bootstrap")).toBeLessThan(order.indexOf("memory"));
  });

  test("UT-11: Circular dependency detected", async () => {
    const manifests = new Map([
      ["a", { name: "a", version: "1.0.0", dependencies: ["b"] }],
      ["b", { name: "b", version: "1.0.0", dependencies: ["a"] }]
    ]);

    const resolver = new PluginDependencyResolver();

    expect(() => {
      resolver.resolveDependencies("a", manifests);
    }).toThrow(CircularDependencyError);
  });

  test("UT-12: Missing dependency throws error", async () => {
    const manifests = new Map([
      ["memory", { name: "memory", version: "1.0.0", dependencies: ["undefined"] }]
    ]);

    const resolver = new PluginDependencyResolver();
    expect(() => resolver.resolveDependencies("memory", manifests)).toThrow();
  });

  test("UT-13: Plugin versioning", async () => {
    const manager = new PluginManager();
    await manager.loadPlugin("bootstrap@1.0.0");

    const plugin = manager.getPlugin("bootstrap");
    expect(plugin.version).toBe("1.0.0");
  });

  test("UT-14: Version conflict throws error", async () => {
    const manager = new PluginManager();

    // Try to load @3.0 when @2.0 already loaded
    expect(async () => {
      await manager.loadPlugin("bootstrap@2.0.0");
      await manager.loadPlugin("bootstrap@3.0.0");
    }).rejects.toThrow(VersionConflictError);
  });

  test("UT-15: Plugin onInit hook called", async () => {
    const manager = new PluginManager();
    const initSpy = jest.fn();

    // Mock plugin with onInit
    const mockPlugin = {
      name: "test",
      lifecycle: { onInit: initSpy }
    };

    await manager.loadPlugin("test");
    expect(initSpy).toHaveBeenCalled();
  });

  test("UT-16: Plugin onDestroy hook called", async () => {
    const manager = new PluginManager();
    const destroySpy = jest.fn();

    // Mock plugin with onDestroy
    const mockPlugin = {
      name: "test",
      lifecycle: { onDestroy: destroySpy }
    };

    await manager.loadPlugin("test");
    await manager.unloadPlugin("test");
    expect(destroySpy).toHaveBeenCalled();
  });

  test("UT-17: Plugin registry tracks tools", async () => {
    const manager = new PluginManager();
    const tools = manager.listAllTools();

    expect(tools).toContainEqual({
      toolName: "getRoles",
      pluginName: "bootstrap"
    });
  });

  test("UT-18: Plugin health status after failures", async () => {
    const manager = new PluginManager();

    await manager.loadPlugin("bootstrap", true);
    await manager.loadPlugin("memory", false);  // Optional, fail

    const status = manager.getPluginStatus();
    expect(status.healthy).toContain("bootstrap");
    expect(status.failed).toContain("memory");
  });
});
```

**Checklist:**

- [ ] 10 unit tests pass (UT-09-18)
- [ ] Dependency resolution working
- [ ] Circular dependency detection working
- [ ] Lifecycle hooks called correctly
- [ ] Plugin health status accurate

---

### Phase 4: Documentation (2-3 hours)

**Deliverables:**

- [ ] Update TASK-14-03.md with 24 AC (base 20 + QA improvement 4)
- [ ] Create src/plugins/PLUGIN-DEVELOPMENT.md (guide)
- [ ] Create PLUGIN-ARCHITECTURE.md (design docs)

**PLUGIN-DEVELOPMENT.md example:**

```markdown
# Plugin Development Guide

## Creating a New Plugin

### 1. Define Plugin Class

```typescript
import { Plugin, Tool, IToolModule } from "@mcp/plugins";

@Plugin({
  name: "my-plugin",
  version: "1.0.0",
  dependencies: ["bootstrap"]  // Must load after bootstrap
})
export class MyPlugin implements IToolModule {
  name = "my-plugin";
  version = "1.0.0";
  tools = new Map();

  lifecycle = {
    async onInit() {
      console.log("Initializing my-plugin...");
      // Setup database connections, etc.
    },
    async onDestroy() {
      console.log("Shutting down my-plugin...");
      // Cleanup resources
    },
    onError(error: Error) {
      console.error("Plugin error:", error);
    }
  };

  @Tool({ name: "myTool", description: "Does something cool" })
  myTool(args: any) {
    return { result: "..." };
  }
}
```

### 2. Register Plugin

```typescript
const manager = new PluginManager();
await manager.loadPlugin("my-plugin", false);  // Optional plugin

// Invoke tool
const result = await manager.invokeTool("myTool", { arg: "value" });
```

### 3. Handle Errors

```typescript
const status = manager.getPluginStatus();
console.log(`Healthy: ${status.healthy.join(", ")}`);
console.log(`Failed: ${status.failed.join(", ")}`);
```

## Dependency Declaration

Plugins can declare dependencies. They'll be loaded in order:

```typescript
@Plugin({
  name: "advanced-plugin",
  dependencies: ["bootstrap", "context", "memory"]  // Load in this order
})
```

Circular dependencies are detected and prevented:

```typescript
// This will throw CircularDependencyError
// A → B → C → A (no good)
```

## Lifecycle Hooks

onInit: Called when plugin loads (connection setup, DB migration)
onDestroy: Called when plugin unloads (resource cleanup)
onError: Called when plugin encounters errors (logging, reporting)

```

---

## ✅ Acceptance Criteria (24 Total)

### Base AC (20):
- [ ] AC-01 through AC-20 (baseline plugin system features)

### QA Improvement AC (4 — P1 HIGH PRIORITY):
- [ ] **AC-31 (NEW):** Dependencies resolved in correct load order
- [ ] **AC-32 (NEW):** Circular dependencies detected and thrown (no infinite loops)
- [ ] **AC-33 (NEW):** Plugin lifecycle hooks (onInit, onDestroy) called correctly
- [ ] **AC-34 (NEW):** Optional plugin failures don't crash server

**Plus implicit AC-35:**
- [ ] **AC-35 (NEW):** getPluginStatus() returns accurate healthy/failed lists

**Total:** 24 AC → verification checklist above

---

## 📚 Resources

**Online Research:**
- EPIC-14-ONLINE-RESEARCH-REVIEW.md (Finding #1 — plugin architecture with dynamic imports)
- EPIC-14-QA-TEST-STRATEGY.md (UT-09-18, IT-07-10)

**npm References:**
- webpack plugin system (reference)
- rollup plugin ecosystem (design pattern)
- babel plugin architecture (decorator pattern)

**Related Tasks:**
- Depends on: TASK-14-01 (Dev A) — Transport error codes (for error handling)
- Blocks: TASK-14-04-07 (plugin module implementations)

---

## 🔗 QA Validation

**QA Test Matrix:**
- UT-09 through UT-18: Plugin loading, dependencies, lifecycle
- IT-07-10: Plugin integration tests
- E2E-06-09: Full plugin workflow + cross-plugin communication

**QA Sign-Off Criteria:**
- All 10 unit tests passing
- All 24 AC verified
- No circular dependency crashes
- Plugin health status accurate
- Lifecycle hooks called in correct order

**Expected Pass Rate:** 100% (10/10 unit tests)

---

## 📅 Timeline

| Day | Task | Hours | Status |
|:---:|:----:|:-----:|:------:|
| **Day 11** (3/19) | Study + Design + Setup | 4h | ⏳ Awaiting start |
| **Day 12** (3/20) | Implement Resolver + Manager | 11h | ⏳ Awaiting start |
| **Day 13** (3/21) | Lifecycle hooks + Tests + Docs | 11h | ⏳ Awaiting start |

**Total:** 26 hours (base 22h + QA improvement 4h)

---

## 🚀 Next Steps (After Tech Lead Decision)

1. **2026-03-14 EOD:** Tech Lead decides (Option A = all improvements)
2. **2026-03-19 09:00:** Dev C kickoff meeting
3. **2026-03-19 10:00:** Dev C starts TASK-14-03 (parallel with Dev A/B)
4. **2026-03-21 18:00:** Dev C submits PR (24 AC verified, 10 unit tests green)
5. **2026-03-22:** Dev C begins TASK-14-06 (Memory plugins, 23h) or peer review

---

**Assignment:** DEV-C-EPIC-14-ASSIGNMENT
**Status:** 🟡 **PENDING_TECH_LEAD_GO/NO-GO** (Option A: All improvements)
**Critical Priority:** P1 (Plugin reliability)
**Prepared by:** QA Tester Agent
**Date:** 2026-03-08

