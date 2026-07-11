---
id: TASK-14-03-API-REFERENCE
title: "TASK-14-03 Plugin System — API Reference & Developer Guide"
epic: EPIC-14
task: TASK-14-03
created: 2026-03-10
type: "api-reference-and-guide"
---

# Plugin System: API Reference & Developer Guide

**Audience:** Backend developers adding new plugins or tools to the MCP server
**Status:** ✅ Complete (40/40 tests passing)
**Last Updated:** 2026-03-10

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Core APIs](#core-apis)
3. [Plugin Declaration](#plugin-declaration)
4. [Tool Declaration](#tool-declaration)
5. [Lifecycle Hooks](#lifecycle-hooks)
6. [Dependency Management](#dependency-management)
7. [Error Handling](#error-handling)
8. [Best Practices](#best-practices)
9. [Examples](#examples)
10. [Troubleshooting](#troubleshooting)

---

## Quick Start

### 1. Create a New Plugin

```typescript
// src/mcp/tools/my-plugin.ts

import { BasePlugin, Plugin, Tool } from '../../plugins/PluginDecorators';
import { IToolModule, ToolHandler, McpContext } from '../../plugins/PluginTypes';

@Plugin({
  id: 'my-plugin',
  name: 'My Custom Plugin',
  version: '1.0.0',
  dependencies: ['bootstrap'], // Load after bootstrap plugin
  critical: false, // Non-critical: failure doesn't block startup
})
export class MyPlugin extends BasePlugin implements IToolModule {
  name = 'my-plugin';
  handlers: Map<string, ToolHandler> = new Map();

  constructor() {
    super();
    // Register tool handlers
    this.handlers.set('my_first_tool', this.my_first_tool.bind(this));
    this.handlers.set('my_second_tool', this.my_second_tool.bind(this));
  }

  @Tool({
    name: 'my_first_tool',
    description: 'A sample tool',
    inputSchema: {
      type: 'object',
      properties: {
        input_param: { type: 'string', description: 'A parameter' },
      },
      required: ['input_param'],
    },
  })
  async my_first_tool(input: any, context: Partial<McpContext>): Promise<any> {
    const { input_param } = input;
    return { status: 'success', message: `You said: ${input_param}` };
  }

  @Tool({
    name: 'my_second_tool',
    description: 'Another tool',
    inputSchema: { type: 'object', properties: {} },
  })
  async my_second_tool(input: any, context: Partial<McpContext>): Promise<any> {
    return { status: 'success', message: 'Second tool executed' };
  }

  // Lifecycle hooks
  async onInit(context: any): Promise<void> {
    console.log('[MyPlugin] Initializing...');
  }

  async onDestroy(): Promise<void> {
    console.log('[MyPlugin] Shutting down.');
  }
}
```

### 2. Register Plugin with PluginManager

The plugin is automatically registered when BootstrapPlugin loads it. No manual registration needed!

```typescript
// In index.ts or your bootstrap code
const pluginManager = await createPluginManager(agentDb, sessionManager, rbacFilter, workflowTracker, guardrail);
// MyPlugin automatically loaded if listed in BootstrapPlugin's dependency chain
```

---

## Core APIs

### PluginManager: Orchestration API

**Location:** `src/plugins/PluginManager.ts`

#### `loadPlugin(id: string, options?: PluginLoadOptions): Promise<Plugin>`

Load a single plugin by ID.

```typescript
const plugin = await pluginManager.loadPlugin('bootstrap', { critical: true });
```

**Parameters:**

- `id` (string): The plugin ID (e.g., "bootstrap", "context-discovery")
- `options` (optional):
  - `critical` (boolean): If true, failure throws error; if false, error is caught & logged
  - `timeout` (number): Max milliseconds to wait for load (default: 30000)

**Returns:** Promise resolving to loaded Plugin

**Throws:**

- `CircularDependencyError` if plugin dependencies form a cycle
- `MissingDependencyError` if a required dependency isn't available
- `PluginLoadError` if `critical: true` and plugin initialization fails

---

#### `unloadPlugin(id: string): Promise<void>`

Unload a plugin and clean up resources.

```typescript
await pluginManager.unloadPlugin('my-plugin');
```

**Parameters:**

- `id` (string): Plugin ID to unload

**Effects:**

- Calls plugin's `onDestroy()` lifecycle hook
- Removes plugin from registry
- Removes plugin's tools from tool registry

---

#### `getPluginStatus(id: string): PluginHealthReport`

Get the current status of a plugin.

```typescript
const status = pluginManager.getPluginStatus('bootstrap');
// { id: 'bootstrap', status: 'LOADED', loadTime: 45, error: null }
```

**Returns:**

```typescript
{
  id: string;
  status: 'NOT_LOADED' | 'LOADING' | 'LOADED' | 'FAILED';
  loadTime?: number;    // ms taken to load
  error?: Error | null; // If status === 'FAILED'
}
```

---

#### `registry: ReadonlyMap<string, Plugin>`

Access the plugin registry (read-only).

```typescript
for (const [id, plugin] of pluginManager.registry.entries()) {
  console.log(`Plugin ${id} loaded, tools:`, Array.from(plugin.handlers.keys()));
}
```

**Note:** Registry is immutable; use `loadPlugin()` and `unloadPlugin()` to modify.

---

#### `invokeTool(toolName: string, args: any, context?: Partial<McpContext>): Promise<any>`

Invoke a tool by name.

```typescript
const result = await pluginManager.invokeTool('bootstrap_agent', { agentId: 'agent-123' }, {
  session_id: 'session-456',
  track: 'standard',
});
```

**Parameters:**

- `toolName` (string): The tool name (e.g., "bootstrap_agent", "request_context")
- `args` (any): Input arguments matching tool's `inputSchema`
- `context` (optional): McpContext with session_id, track, auth info, etc.

**Returns:** Promise resolving to tool's return value

**Throws:**

- `ToolNotFoundError` if tool not found in registry
- `ValidationError` if args don't match inputSchema
- Tool-specific errors from the handler

---

### Plugin Dependency Resolver API

**Location:** `src/plugins/PluginDependencyResolver.ts`

#### `resolveDependencies(manifests: PluginManifest[]): PluginManifest[]`

Resolve plugin load order respecting dependencies.

```typescript
const manifests = [bootstrapManifest, contextManifest, discoveryManifest];
const sorted = dependencyResolver.resolveDependencies(manifests);
// Result: [bootstrap] then [context, discovery] (in declaration order)
```

**Parameters:**

- `manifests` (PluginManifest[]): Array of plugin manifests

**Returns:** Sorted array in topological order (ready to load sequentially)

**Throws:**

- `CircularDependencyError` if cycle detected (includes cycle path)
- `MissingDependencyError` if a dependency doesn't exist

---

#### `hasCycle(id: string, manifests: Map, visited?, stack?): { hasCycle: boolean, path?: string }`

Check if a plugin and its dependencies have a circular dependency.

```typescript
const check = dependencyResolver.hasCycle('bootstrap', manifestMap);
if (check.hasCycle) {
  console.error(`Cycle detected: ${check.path}`);
  // Example path: "bootstrap → context → discovery → bootstrap"
}
```

**Parameters:**

- `id` (string): Starting plugin ID
- `manifests` (Map): Map of all plugin manifests
- `visited` (Set): Visited nodes (internal, don't pass)
- `stack` (string[]): Current path (internal, don't pass)

**Returns:**

```typescript
{
  hasCycle: boolean;
  path?: string;  // Human-readable path if hasCycle === true
}
```

---

## Plugin Declaration

### @Plugin Decorator

**Location:** `src/plugins/PluginDecorators.ts`

Declare a plugin class's metadata.

```typescript
@Plugin({
  id: 'my-plugin',
  name: 'My Plugin',
  version: '1.0.0',
  dependencies: ['bootstrap', 'some-other-plugin'],
  critical: false,
  description: 'Optional: What this plugin does',
  author: 'Optional: Your name',
})
export class MyPlugin extends BasePlugin implements IToolModule {
  // ... implementation
}
```

**Properties:**

| Property | Type | Required | Description |
|:---------|:-----|:---------|:-----------|
| `id` | string | ✅ | Unique plugin identifier (kebab-case recommended) |
| `name` | string | ✅ | Display name for UI/logs |
| `version` | string | ✅ | Semantic version (e.g., "1.0.0") |
| `dependencies` | string[] | ✅ | List of plugin IDs this plugin depends on (empty if none) |
| `critical` | boolean | ✅ | If true, plugin failure blocks startup; if false, error is logged but startup continues |
| `description` | string | ❌ | Optional description of plugin purpose |
| `author` | string | ❌ | Optional author name |

---

### PluginManifest Interface

**Location:** `src/plugins/PluginTypes.ts`

```typescript
export interface PluginManifest {
  id: string;
  name: string;
  version: string;
  dependencies: string[];
  critical: boolean;
  description?: string;
  author?: string;
}
```

Extracted from `@Plugin` decorator at runtime.

---

## Tool Declaration

### @Tool Decorator

**Location:** `src/plugins/PluginDecorators.ts`

Declare a tool within a plugin.

```typescript
@Tool({
  name: 'my_tool',
  description: 'Does something useful',
  inputSchema: {
    type: 'object',
    properties: {
      userId: { type: 'string', description: 'User ID' },
      action: { type: 'string', enum: ['create', 'update', 'delete'] },
    },
    required: ['userId', 'action'],
  },
})
async my_tool(input: any, context: Partial<McpContext>): Promise<any> {
  const { userId, action } = input;
  // ... implementation
  return { status: 'success', message: '...' };
}
```

**Properties:**

| Property | Type | Required | Description |
|:---------|:-----|:---------|:-----------|
| `name` | string | ✅ | Tool name (unique across all plugins, snake_case recommended) |
| `description` | string | ✅ | Human-readable description of what tool does |
| `inputSchema` | JSONSchema | ✅ | JSON Schema describing input parameters |

**inputSchema Example:**

```json
{
  "type": "object",
  "properties": {
    "param1": { "type": "string", "description": "..." },
    "param2": { "type": "number", "description": "...", "minimum": 0 },
    "param3": { "type": "array", "items": { "type": "string" }, "description": "..." }
  },
  "required": ["param1", "param2"]
}
```

---

### IToolModule Interface

**Location:** `src/plugins/PluginTypes.ts`

```typescript
export interface IToolModule {
  name: string;                           // Plugin name
  handlers: Map<string, ToolHandler>;     // Tool handlers indexed by tool name
}

export type ToolHandler = (input: any, context: Partial<McpContext>) => Promise<any>;
```

All plugins must implement `IToolModule`.

---

## Lifecycle Hooks

### Plugin Lifecycle Phases

```
1. NOT_LOADED (initial state)
   ↓
2. LOADING (during loadPlugin call)
   ↓
3A. LOADED (onInit succeeded)
    → Plugin available in registry
    → Tools callable via PluginManager.invokeTool()

OR

3B. FAILED (onInit threw error)
    → Error logged or thrown (depending on critical flag)
    → Plugin NOT in registry
    → Tools NOT callable
```

---

### onInit Lifecycle Hook

Called when plugin is loading, before registration.

```typescript
async onInit(context: any): Promise<void> {
  console.log('[MyPlugin] Initializing...');
  // Validate configuration
  // Initialize connections
  // Load cached data
  // Perform auth checks
}
```

**Called:** During `PluginManager.loadPlugin(id)`

**If throws:**

- If `critical: true` → error propagates, startup fails
- If `critical: false` → error logged, plugin marked FAILED, startup continues

**Best Practice:** Fail fast with clear error message.

```typescript
async onInit(context: any): Promise<void> {
  if (!process.env.DATABASE_URL) {
    throw new Error('DATABASE_URL environment variable not set');
  }
  // ... continue initialization
}
```

---

### onDestroy Lifecycle Hook

Called when plugin is unloaded, for cleanup.

```typescript
async onDestroy(): Promise<void> {
  console.log('[MyPlugin] Shutting down.');
  // Close database connections
  // Cancel pending operations
  // Clear in-memory caches
  // Flush logs
}
```

**Called:** During `PluginManager.unloadPlugin(id)` or server shutdown

**Best Practice:** Always use `try/catch` — never throw from onDestroy.

```typescript
async onDestroy(): Promise<void> {
  try {
    await this.db.close();
  } catch (error) {
    console.error('[MyPlugin] Error during shutdown:', error);
    // Don't rethrow; ensure cleanup completes
  }
}
```

---

### onError Lifecycle Hook (Optional)

Called when a tool handler throws an error.

```typescript
async onError(toolName: string, error: Error, context: Partial<McpContext>): Promise<void> {
  console.error(`[MyPlugin] Tool "${toolName}" failed:`, error.message);
  // Optional: Send to monitoring/logging service
  // Optional: Update plugin state
}
```

**Called:** When tool handler throws (before error propagates to caller)

**Note:** Not required; define only if you need custom error handling.

---

## Dependency Management

### Declaring Dependencies

```typescript
@Plugin({
  id: 'my-plugin',
  dependencies: ['bootstrap', 'context'],  // Load bootstrap first, then context, then my-plugin
  critical: false,
})
export class MyPlugin extends BasePlugin implements IToolModule {
  // ...
}
```

**Load Order:**

```
1. bootstrap
2. context
3. my-plugin (after all dependencies)
```

If any dependency fails and is marked `critical: true`, the entire plugin load fails.

---

### Optional Dependencies (Future)

**Status:** Not yet implemented (candidate for EPIC-15)

Currently, all dependencies are **required**. Future versions may support optional dependencies:

```typescript
@Plugin({
  id: 'my-plugin',
  dependencies: ['bootstrap'],           // Required
  optionalDependencies: ['analytics'],   // Optional (ignored if missing)
})
```

---

### Circular Dependency Detection

Circular dependencies are **detected and rejected at startup**.

```typescript
// DON'T create this:
@Plugin({ id: 'a', dependencies: ['b'] })
@Plugin({ id: 'b', dependencies: ['c'] })
@Plugin({ id: 'c', dependencies: ['a'] })  // Error: circular!

// Error message:
// CircularDependencyError: Circular dependency detected: a → b → c → a
```

**Detection:** DFS algorithm runs during `PluginManager.loadPlugin()` for the first plugin, detecting all cycles before loading starts.

---

## Error Handling

### Common Errors

#### CircularDependencyError

Thrown when plugin dependencies form a cycle.

```typescript
try {
  await pm.loadPlugin('my-plugin');
} catch (error) {
  if (error instanceof CircularDependencyError) {
    console.error(`Circular dependency: ${error.path}`);
    // Fix: Remove or change dependency declarations
  }
}
```

---

#### MissingDependencyError

Thrown when a dependency doesn't exist.

```typescript
// In plugin manifest:
dependencies: ['bootstrap', 'non-existent-plugin']

// Error:
// MissingDependencyError: Cannot find dependency 'non-existent-plugin'
```

**Fix:** Check plugin ID names in dependencies array.

---

#### ToolNotFoundError

Thrown when invoking a tool that doesn't exist.

```typescript
try {
  await pm.invokeTool('non_existent_tool', {});
} catch (error) {
  if (error instanceof ToolNotFoundError) {
    console.error(`Tool '${error.toolName}' not found`);
    console.log('Available tools:', pm.getAvailableTools());
  }
}
```

---

#### ValidationError

Thrown when tool input doesn't match inputSchema.

```typescript
try {
  await pm.invokeTool('my_tool', { wrong_param: 'value' });  // missing required 'userId'
} catch (error) {
  if (error instanceof ValidationError) {
    console.error(`Validation failed: ${error.message}`);
    console.log('Expected:', error.schema);
  }
}
```

---

### Error Handling Best Practices

**In Tool Handlers:**

```typescript
@Tool({ name: 'my_tool', ... })
async my_tool(input: any, context: Partial<McpContext>): Promise<any> {
  try {
    // Validate input
    if (!input.userId) {
      throw new Error('userId is required');
    }

    // Perform work
    const result = await someAsyncOperation();

    return { status: 'success', data: result };
  } catch (error: any) {
    // Log error with context but don't expose sensitive details
    console.error(`[MyPlugin.my_tool] Error:`, error.message);

    // Return error response (don't throw from handler)
    return {
      status: 'error',
      error: error.message,
      code: 'TOOL_FAILED'
    };
  }
}
```

**In Plugin onInit:**

```typescript
async onInit(context: any): Promise<void> {
  try {
    // Initialize with clear error messages
    const config = this.loadConfig();
    if (!config.apiKey) {
      throw new Error('API key not configured (set MY_PLUGIN_API_KEY env var)');
    }
  } catch (error: any) {
    // Fail fast with helpful error message
    throw error;  // Propagates based on critical flag
  }
}
```

---

## Best Practices

### 1. Use Snake_case for Tool Names

```typescript
@Tool({ name: 'my_tool', ... })    // ✅ Good
@Tool({ name: 'myTool', ... })     // ❌ Avoid camelCase

@Plugin({ id: 'my-plugin', ... })  // ✅ Good
@Plugin({ id: 'myPlugin', ... })   // ❌ Avoid camelCase
```

---

### 2. Clear Tool Descriptions

```typescript
// ✅ Good: Clear what the tool does, preconditions, side effects
@Tool({
  name: 'create_user',
  description: 'Create a new user account with email and password. Returns user ID. Throws if email already exists.',
  inputSchema: { /* ... */ }
})

// ❌ Poor: Vague
@Tool({
  name: 'do_something',
  description: 'Does stuff',
  inputSchema: { /* ... */ }
})
```

---

### 3. Comprehensive inputSchema

```typescript
// ✅ Good: All properties documented with types
@Tool({
  name: 'update_user',
  description: '...',
  inputSchema: {
    type: 'object',
    properties: {
      userId: { type: 'string', description: 'UUID of user to update' },
      email: { type: 'string', format: 'email', description: 'New email address' },
      age: { type: 'integer', minimum: 0, maximum: 150, description: 'User age' },
    },
    required: ['userId']  // Only userId required, others optional
  }
})

// ❌ Poor: No schema
@Tool({
  name: 'update_user',
  description: '...',
  inputSchema: { type: 'object' }  // Too vague
})
```

---

### 4. Idempotent Tool Handlers

```typescript
// ✅ Good: Multiple calls with same input produce same result
@Tool({ name: 'get_user', ... })
async get_user(input: any, context: Partial<McpContext>): Promise<any> {
  const user = await this.db.getUserById(input.userId);
  return user;  // Same result every call
}

// ❌ Poor: Side effects or state changes
@Tool({ name: 'get_user', ... })
async get_user(input: any, context: Partial<McpContext>): Promise<any> {
  const user = await this.db.getUserById(input.userId);
  await this.db.incrementAccessCount(input.userId);  // ❌ Side effect
  return user;
}

// ✅ Better: If you need side effects, use a different tool
@Tool({ name: 'record_user_access', ... })
async record_user_access(input: any, context: Partial<McpContext>): Promise<any> {
  await this.db.incrementAccessCount(input.userId);
  return { status: 'logged' };
}
```

---

### 5. Minimal Dependencies

```typescript
// ✅ Good: Only depends on bootstrap (auth)
@Plugin({
  id: 'my-plugin',
  dependencies: ['bootstrap'],
  critical: false,
})

// ❌ Poor: Too many dependencies
@Plugin({
  id: 'my-plugin',
  dependencies: ['bootstrap', 'context', 'discovery', 'analytics', 'logging'],
  critical: true,  // Plus critical causes cascade failures
})
```

---

### 6. Fast Initialization

```typescript
// ✅ Good: onInit completes in <100ms
async onInit(context: any): Promise<void> {
  // Validate config
  // Start background tasks (don't await)
  setImmediate(() => this.warmUpCache());  // Don't block
}

// ❌ Poor: Long initialization time
async onInit(context: any): Promise<void> {
  // Load 1GB dataset from disk
  this.largeDataset = await fs.promises.readFile('huge-file.json');  // Blocks startup
}
```

---

## Examples

### Example 1: Simple Tool Plugin

```typescript
import { BasePlugin, Plugin, Tool } from '../../plugins/PluginDecorators';
import { IToolModule, ToolHandler, McpContext } from '../../plugins/PluginTypes';

@Plugin({
  id: 'calculator',
  name: 'Calculator Plugin',
  version: '1.0.0',
  dependencies: ['bootstrap'],
  critical: false,
  description: 'Basic arithmetic operations',
})
export class CalculatorPlugin extends BasePlugin implements IToolModule {
  name = 'calculator';
  handlers: Map<string, ToolHandler> = new Map();

  constructor() {
    super();
    this.handlers.set('add', this.add.bind(this));
    this.handlers.set('subtract', this.subtract.bind(this));
  }

  @Tool({
    name: 'add',
    description: 'Add two numbers',
    inputSchema: {
      type: 'object',
      properties: {
        a: { type: 'number', description: 'First number' },
        b: { type: 'number', description: 'Second number' },
      },
      required: ['a', 'b'],
    },
  })
  async add(input: any, context: Partial<McpContext>): Promise<any> {
    return { result: input.a + input.b };
  }

  @Tool({
    name: 'subtract',
    description: 'Subtract two numbers',
    inputSchema: {
      type: 'object',
      properties: {
        a: { type: 'number' },
        b: { type: 'number' },
      },
      required: ['a', 'b'],
    },
  })
  async subtract(input: any, context: Partial<McpContext>): Promise<any> {
    return { result: input.a - input.b };
  }

  async onInit(): Promise<void> {
    console.log('[Calculator] Ready to perform calculations');
  }
}
```

---

### Example 2: Plugin with Database Connection

```typescript
@Plugin({
  id: 'user-service',
  name: 'User Service Plugin',
  version: '1.0.0',
  dependencies: ['bootstrap'],
  critical: true,  // Critical: user service must work
})
export class UserServicePlugin extends BasePlugin implements IToolModule {
  name = 'user-service';
  handlers: Map<string, ToolHandler> = new Map();
  private db: Database | null = null;

  constructor() {
    super();
    this.handlers.set('get_user', this.get_user.bind(this));
    this.handlers.set('create_user', this.create_user.bind(this));
  }

  async onInit(): Promise<void> {
    try {
      const dbUrl = process.env.USER_DB_URL;
      if (!dbUrl) {
        throw new Error('USER_DB_URL not set');
      }
      this.db = await Database.connect(dbUrl);
      console.log('[UserService] Connected to database');
    } catch (error: any) {
      throw new Error(`Failed to initialize UserService: ${error.message}`);
    }
  }

  @Tool({
    name: 'get_user',
    description: 'Get user by ID',
    inputSchema: {
      type: 'object',
      properties: {
        userId: { type: 'string' },
      },
      required: ['userId'],
    },
  })
  async get_user(input: any, context: Partial<McpContext>): Promise<any> {
    const user = await this.db!.query('SELECT * FROM users WHERE id = ?', [input.userId]);
    return user || { error: 'User not found' };
  }

  @Tool({
    name: 'create_user',
    description: 'Create a new user',
    inputSchema: {
      type: 'object',
      properties: {
        email: { type: 'string' },
        name: { type: 'string' },
      },
      required: ['email', 'name'],
    },
  })
  async create_user(input: any, context: Partial<McpContext>): Promise<any> {
    const result = await this.db!.query(
      'INSERT INTO users (email, name) VALUES (?, ?)',
      [input.email, input.name]
    );
    return { userId: result.id, email: input.email };
  }

  async onDestroy(): Promise<void> {
    try {
      if (this.db) {
        await this.db.close();
        console.log('[UserService] Database connection closed');
      }
    } catch (error) {
      console.error('[UserService] Error closing database:', error);
    }
  }
}
```

---

## Troubleshooting

### Problem: Plugin fails to load with "CircularDependencyError"

**Cause:** Plugin dependencies form a cycle (A→B→C→A).

**Solution:**

1. Examine error message for cycle path
2. Break the cycle by removing or changing a dependency
3. Load plugins in correct order:

```typescript
// ❌ This will fail:
@Plugin({ id: 'a', dependencies: ['b'] })
@Plugin({ id: 'b', dependencies: ['a'] })

// ✅ Fix: Remove circular dependency
@Plugin({ id: 'a', dependencies: [] })
@Plugin({ id: 'b', dependencies: ['a'] })  // b depends on a, not vice versa
```

---

### Problem: Tool call fails with "ToolNotFoundError"

**Cause:** Tool not registered or plugin not loaded.

**Solution:**

1. Check tool name spelling (case-sensitive, snake_case)
2. Verify plugin is marked as `critical: false` if it's failing silently
3. Check plugin dependencies are correct

```typescript
// Debug: List available tools
const allTools = [];
for (const plugin of pm.registry.values()) {
  allTools.push(...plugin.handlers.keys());
}
console.log('Available tools:', allTools);
```

---

### Problem: Plugin initialization hangs

**Cause:** Blocking operation in `onInit()`.

**Solution:** Move long-running tasks to background or lazy-load.

```typescript
// ❌ Hangs startup
async onInit(): Promise<void> {
  await this.loadLargeDataset();  // Blocks startup
}

// ✅ Better
async onInit(): Promise<void> {
  // Start in background, don't block
  this.loadLargeDataset().catch(err => {
    console.error('Failed to load dataset:', err);
  });
}
```

---

### Problem: Tool error is not caught

**Cause:** Tool handler throws unhandled error.

**Solution:** Wrap tool logic in try/catch:

```typescript
@Tool({ name: 'my_tool', ... })
async my_tool(input: any, context: Partial<McpContext>): Promise<any> {
  try {
    const result = await someOperation();
    return { status: 'success', data: result };
  } catch (error: any) {
    // Log error
    console.error('[my_tool] Error:', error.message);

    // Return error response (don't throw)
    return {
      status: 'error',
      error: error.message,
      code: 'OPERATION_FAILED'
    };
  }
}
```

---

### Problem: Plugin memory not released on unload

**Cause:** `onDestroy()` not cleaning up resources.

**Solution:** Implement `onDestroy()` with cleanup:

```typescript
async onDestroy(): Promise<void> {
  // Close connections
  if (this.db) await this.db.close();

  // Clear caches
  this.cache.clear();

  // Cancel pending operations
  for (const promise of this.pendingOps) {
    await promise;  // Wait for completion or timeout
  }
  this.pendingOps.clear();
}
```

---

## API Reference Quick Links

- [PluginManager API](#pluginmanager-orchestration-api)
- [DependencyResolver API](#plugin-dependency-resolver-api)
- [@Plugin Decorator](#plugin-decorator)
- [@Tool Decorator](#tool-decorator)
- [Lifecycle Hooks](#lifecycle-hooks)
- [Error Handling](#error-handling)

---

**Last Updated:** 2026-03-10
**Version:** 1.0.0
**Status:** ✅ Complete

For questions or issues, refer to [TASK-14-03 Implementation Summary](./TASK-14-03-plugin-system-architecture.md) or [ADR-EPIC14-03](./ADR-EPIC14-03-plugin-system-architecture.md).
