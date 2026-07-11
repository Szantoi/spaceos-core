# Legacy Tools Migration Guide

This document helps developers transition from the old, file-based MCP tools to the
new decorator-based plugin system introduced in **EPIC-14, Phase 1**.

Legacy tools are still available via the `LegacyPlugin` until **v2.0** (estimated
2026‑06‑01).  Each legacy tool logs a deprecation warning when invoked and
forwards the call to its modern equivalent.

## Mapping of Legacy → Modern Tools

| Legacy Name         | Modern Equivalent     | Comments |
|---------------------|-----------------------|----------|
| `search_knowledge`  | `search_knowledge_base` | Identical parameters; only name changed |
| `brainstorm`        | `brainstorm`          | Tool kept with same signature; wrapper adds warning |

_Note:_ if additional legacy names are discovered, add them to
`src/mcp/tools/legacy.ts` and update this table.

## Code Example

### Before (pre‑EPIC‑14)
```ts
// old/tools.ts
export async function search_knowledge(query: string) {
  // manual SQL or FTS query
}
```

### After (decorator plugin)
```ts
import { Plugin, Tool } from '../plugins/PluginDecorators';

@Plugin({ id: 'legacy', name: 'Legacy Tools Adapter', deprecated: true })
export class LegacyPlugin {
  @Tool({ name: 'search_knowledge', description: 'Deprecated alias' })
  async searchKnowledge(args: { query: string }, ctx: McpContext) {
    console.warn('[DEPRECATED] use search_knowledge_base instead');
    return this.context.pluginManager.invokeTool('search_knowledge_base', args, ctx);
  }
}
```

## Running the Legacy Loader

The `LegacyPlugin` is automatically loaded by `mcpServer.createMcpServerRouter()`
if the compiled `src/mcp/tools/legacy.ts` file exists.  No additional
configuration is required.

## Removal Timeline

- **v1.x** – legacy plugin enabled, deprecation warnings logged.
- **2026‑06‑01** – scheduled removal of legacy code in version 2.0.  After that
  date, clients must have migrated to the modern tool names.

## Migration Checklist

1. Search your codebase for legacy tool names (e.g. `search_knowledge`).
2. Replace calls with modern names: `search_knowledge_base` etc.
3. Run full test suite; ensure no deprecation warnings remain.
4. Update any documentation or workflows referencing old names.

For assistance, contact the `@joinerytech/engineering` team or open a GitHub issue
labeled `epic-14 legacy`.
