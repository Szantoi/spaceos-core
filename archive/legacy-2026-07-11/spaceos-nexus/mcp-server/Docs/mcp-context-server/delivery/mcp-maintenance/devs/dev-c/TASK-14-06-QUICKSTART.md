---
id: TASK-14-06-QUICKSTART
title: "TASK-14-06 Day-1 Quickstart — Memory Tool Plugin Module"
type: developer-quickstart
owner: "Dev C (or TBD)"
duration: "8 hours"
created: 2026-03-11
---

# 🚀 TASK-14-06 Quickstart — Memory Tool Plugin Module

**Your mission:** Refactor existing memory tools into a decorator-based plugin module.

**Duration:** 8 hours (~1 full day)
**Files you'll touch:** `src/mcp/tools/memory.ts`, `src/tests/unit/memory-plugin.test.ts`
**Predecessor:** TASK-14-03, 14-04, 14-05 ✅ (all complete)
**No blockers** → Start immediately!

---

## Today's Checklist

- [ ] **08:00-08:30** — Read this quickstart + review TASK-14-06 spec (30 min)
- [ ] **08:30-09:15** — Review plugin system examples from TASK-14-03..05 (45 min)
- [ ] **09:15-11:00** — Implement MemoryPlugin with @Plugin decorator (1h45m)
- [ ] **11:00-12:00** — Implement save_episode, query_memory, search_memory tools (1h)
- [ ] **12:00-12:30** — Write unit tests (30 min)
- [ ] **12:30-13:00** — Integration test + final validation (30 min)
- [ ] **13:00+** — Post completion report! ✅

**Total: 8 hours** (with 30 min buffer)

---

## Key Links

| File | Purpose | Read Time |
|------|---------|-----------|
| `TASK-14-06-MEMORY-PLUGIN.md` | Full spec | 10 min |
| `src/mcp/tools/bootstrap.ts` | Plugin example | 15 min |
| `src/tests/unit/plugin-tools-integration.test.ts` | Test pattern | 10 min |

---

## Architecture Pattern

**Your plugin will look like:**

```typescript
// src/mcp/tools/memory.ts

@Plugin({
  id: "memory",
  name: "Memory Tool Plugin",
  version: "1.0.0",
  dependencies: ["bootstrap"],  // needs auth context
  critical: true
})
export class MemoryPlugin extends BasePlugin implements IToolModule {

  @Tool({
    name: "save_episode",
    description: "Save agent episode to episodic memory",
    schema: z.object({
      agent_id: z.string().uuid(),
      episode_data: z.object(...)
    })
  })
  async saveEpisode(args: {...}, context: McpContext) {
    // Your code here
  }

  @Tool({
    name: "query_memory",
    description: "Query past episodes",
    schema: z.object(...)
  })
  async queryMemory(args: {...}, context: McpContext) {
    // Your code here
  }

  // Add search_memory tool too
}
```

---

## Step-by-Step

### Step 1: Skeleton (15 min)

Create the plugin class with decorator:

```typescript
import { BasePlugin, Plugin, Tool } from "@joinerytech/mcp-plugin";
import { IToolModule, McpContext } from "@joinerytech/mcp-context";
import { z } from "zod";

@Plugin({
  id: "memory",
  name: "Memory Tool Plugin",
  version: "1.0.0",
  dependencies: ["bootstrap"],
  critical: true
})
export class MemoryPlugin extends BasePlugin implements IToolModule {
  // Placeholder for tools
}
```

### Step 2: save_episode Tool (2h)

Implement the first tool:

```typescript
@Tool({
  name: "save_episode",
  description: "Save discovery episode to memory",
  schema: z.object({
    agent_id: z.string().uuid(),
    episode_data: z.object({
      thought_process: z.string(),
      actions: z.array(z.string()),
      outcome: z.string(),
      reasoning: z.string()
    }),
    metadata: z.object({
      timestamp: z.number().optional(),
      tags: z.array(z.string()).optional()
    }).optional()
  })
})
async saveEpisode(
  args: { agent_id: string; episode_data: {...}; metadata?: {...} },
  context: McpContext
) {
  // 1. Validate RBAC (use context.requestContext for auth)
  // 2. Generate episode_id (uuidv4)
  // 3. Insert into SQLite (agent.db episodes table)
  // 4. Return { status: 'success', episode_id }

  return {
    status: 'success' as const,
    episode_id: generateUuid()
  };
}
```

### Step 3: query_memory & search_memory (1h30m)

Add two more tools using same pattern:

```typescript
@Tool({...})
async queryMemory(...) { /* similar to saveEpisode */ }

@Tool({...})
async searchMemory(...) { /* search by tags or keywords */ }
```

### Step 4: Unit Tests (1h)

Add to `src/tests/unit/memory-plugin.test.ts`:

```typescript
describe("MemoryPlugin", () => {
  it("should export valid PluginManifest", () => {
    const manifest = MemoryPlugin.getManifest();
    expect(manifest.id).toBe("memory");
    expect(manifest.critical).toBe(true);
  });

  it("should save episode", async () => {
    const result = await plugin.saveEpisode({
      agent_id: TEST_UUID,
      episode_data: { ... }
    }, mockContext);
    expect(result.status).toBe('success');
    expect(result.episode_id).toBeDefined();
  });

  // 2-3 more tests for query & search
});
```

---

## Testing Your Work

```bash
# Run memory plugin tests only
npm test -- --match "*memory-plugin*"

# Run full plugin test suite (verify no regressions)
npm test -- src/tests/unit/plugin-tools-integration.test.ts
```

**Expected:** ✅ All tests pass

---

## ✅ Completion Status (2026-03-15)

- **Task status:** ✅ _Completed_
- **Implementation:** MemoryPlugin is fully implemented with decorator-based metadata, tool registration, RBAC checks, and episode persistence helpers.
- **Tests:** Unit + integration test suites pass (memory-plugin.test.ts + memory-tools-integration.test.ts).

## What Was Delivered

- ✅ `src/mcp/tools/memory.ts` implements `MemoryPlugin` with the following MCP tools:
  - `save_episode`
  - `query_memory`
  - `search_memory`
  - `generate_episode_highlights`
  - `reflect_session`
  - `tag_episode_quality`
- ✅ RBAC validation (session_id required) and input validation (schema & manual checks)
- ✅ Plugin lifecycle hooks (`onInit`, `onDestroy`) are implemented and covered by tests
- ✅ Integration tests confirm tools can be invoked through the plugin handler map and behave consistently

---

## Gotchas & Tips

**Gotcha 1:** Forget @Tool decorator on a tool?
→ Tool won't be registered. Check PluginManager logs.

**Gotcha 2:** SQL query error in saveEpisode?
→ Wrap in try/catch, return `{ status: 'error', error: 'message' }`

**Gotcha 3:** RBAC context missing?
→ Every tool needs requestContext. Use `context.getRequestContext()` to get auth info.

**Tip:** Copy structure from `src/mcp/tools/bootstrap.ts` — it's a good template!

---

## Completion Sign-Off

When done:

1. All 6 AC from `TASK-14-06-MEMORY-PLUGIN.md` passing ✅
2. All tests green ✅
3. No regressions in existing tests ✅
4. Post completion report (see COMPLETION-REPORT-TEMPLATE.md)

**Questions?** Post in `coordinator/feedback/dev-c/` channel.
