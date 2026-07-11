---
id: TASK-14-07-QUICKSTART
title: "TASK-14-07 Day-1 Quickstart — Legacy Tools Backward Compatibility"
type: developer-quickstart
owner: "Dev E (or TBD)"
duration: "6 hours"
created: 2026-03-11
---

# 🚀 TASK-14-07 Quickstart — Legacy Tools Backward Compatibility

**Your mission:** Wrap any existing file-based or monolithic tools into the modern plugin system without breaking changes.

**Duration:** 6 hours (~2 days)
**Files you'll touch:** `src/mcp/tools/legacy.ts`, `src/tests/unit/legacy-tools.test.ts`
**Predecessor:** TASK-14-03..06 ✅ (all complete)
**No blockers** → Start after TASK-14-06!

---

## Today's Checklist

- [ ] **09:00-09:30** — Understand legacy tools audit (30 min)
- [ ] **09:30-11:00** — Create LegacyPlugin wrapper class (1h30m)
- [ ] **11:00-12:00** — Wrap each legacy tool with @Tool decorator (1h)
- [ ] **12:00-12:30** — Write deprecation metadata + warnings (30 min)
- [ ] **12:30-13:00** — Unit tests + backward compatibility verification (30 min)
- [ ] **13:00+** — Post completion report! ✅

**Total: 6 hours**

---

## What is "Legacy"?

In this project, legacy tools might be:
- Old file-based tools in `src/mcp/tools/old-*.ts`
- Monolithic tools.ts file (if it exists)
- Standalone tool functions without decorators
- Tools with no versioning or metadata

**Your job:** Classify, wrap, and provide a migration path.

---

## Architecture Pattern

```typescript
// src/mcp/tools/legacy.ts

@Plugin({
  id: "legacy",
  name: "Legacy Tools (Deprecated)",
  version: "1.0.0",
  dependencies: [],
  deprecated: true,
  deprecation_reason: "Use modern plugin system instead",
  migration_path: "See LEGACY-MIGRATION-GUIDE.md"
})
export class LegacyPlugin extends BasePlugin implements IToolModule {

  @Tool({
    name: "old_tool_name",
    description: "⚠️ DEPRECATED — Use new_tool_name instead",
    schema: z.object({...}),
    deprecated: true,
    replaces: "new_tool_name"  // optional: pointer to replacement
  })
  async oldToolName(args: {...}, context: McpContext) {
    console.warn("DeprecationWarning: oldToolName is deprecated");
    // Call implementation (either legacy code path or wrapper)
    return { status: 'success' };
  }
}
```

---

## Step-by-Step

### Step 1: Audit Existing Tools (1h)

Find all legacy tools:

```bash
# Search for tools NOT in modern structure
find src/mcp/tools -name "*.ts" | xargs grep -l "@Tool" | wc -l
find src/mcp/tools -name "*.ts" | xargs grep -L "@Tool" | head -5
```

Document findings:
- Tool name
- Location
- Parameters
- Return type
- Current usage (if tracked)

### Step 2: Create LegacyPlugin Class (1h30m)

```typescript
import { BasePlugin, Plugin, Tool } from "@joinerytech/mcp-plugin";

@Plugin({
  id: "legacy",
  name: "Legacy Tools (Deprecated)",
  version: "1.0.0",
  dependencies: [],
  deprecated: true
})
export class LegacyPlugin extends BasePlugin implements IToolModule {
  // Wrapped tools go here
}
```

### Step 3: Wrap Each Legacy Tool (1h30m)

For each legacy tool, create a @Tool wrapper:

```typescript
@Tool({
  name: "old_tool",
  description: "⚠️ DEPRECATED since 2026-03",
  schema: z.object({...}),  // Keep same schema as original
  deprecated: true
})
async oldTool(args: any, context: McpContext) {
  console.warn("DeprecationWarning: oldTool is deprecated. Use new_tool instead.");

  // Option 1: Call legacy implementation directly
  return legacyImpl.call(args);

  // Option 2: Translate to new tool
  // return newTool.call(translateArgs(args));
}
```

### Step 4: Migration Metadata (30 min)

Create `LEGACY-MIGRATION-GUIDE.md`:

```markdown
# Legacy Tools Migration Guide

## Tool: old_tool
- **Status:** ⚠️ DEPRECATED since 2026-03-11
- **Replacement:** Use `new_tool` instead
- **Migration Steps:**
  1. Update tool name in scripts: `old_tool` → `new_tool`
  2. Update parameters: [map old args to new]
  3. Update response parsing: [explain any output changes]

## Tool: old_tool_2
- Similar structure...
```

### Step 5: Unit Tests (30m)

```typescript
describe("LegacyPlugin", () => {
  it("should wrap legacy tool with deprecation warning", async () => {
    const result = await plugin.oldTool({ ... }, mockContext);
    expect(result.status).toBe('success');
    // Verify deprecation warning logged
  });

  it("should maintain backward compatibility", async () => {
    // Old code path still works
    const oldResult = await legacyFunc({...});
    const wrappedResult = await plugin.oldTool({...}, mockContext);
    // Results should be equivalent
  });
});
```

---

## Testing Your Work

```bash
# Run legacy tools tests
npm test -- --match "*legacy-tools*"

# Verify no breaking changes in existing tests
npm test -- src/tests/unit/
```

**Expected:** ✅ All tests pass, deprecation warnings logged

---

## Deprecation Best Practices

- ✅ DO log deprecation warnings
- ✅ DO provide migration path in documentation
- ✅ DO keep backward compatibility (don't break existing clients)
- ❌ DON'T remove old tools immediately
- ❌ DON'T silently redirect new code path (warn first)

**Timeline:** Keep legacy tools for 1-2 releases, then mark "critical"

---

## Gotchas & Tips

**Gotcha 1:** Forget @Tool decorator?
→ Old test might still work, but tool won't be registered. Test via PluginManager!

**Gotcha 2:** Response format changed?
→ Keep old format for backward compatibility. Update docs with "migration to new format".

**Tip:** If unsure about migration, post in feedback/ and wait for guidance.

---

## Completion Sign-Off

When done:

1. All 4 AC from `TASK-14-07-LEGACY-TOOLS.md` passing ✅
2. All tests green ✅
3. No breaking changes verified ✅
4. LEGACY-MIGRATION-GUIDE.md written ✅
5. Post completion report!

