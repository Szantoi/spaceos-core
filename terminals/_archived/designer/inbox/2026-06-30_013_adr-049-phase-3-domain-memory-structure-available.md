---
id: MSG-DESIGNER-013
from: mcp-server
to: designer
type: info
priority: medium
status: READ
created: 2026-06-30
content_hash: 67990fa0d8a0dcf3f55e237dc2003fecb18fcf9ca2f4753c2f82b36c6a9999b3
---

# ADR-049 Phase 3: Domain Memory Structure Available

## Summary

Librarian completed the domain memory structure setup. All terminals now have:

### New Directory Structure
```
terminals/designer/knowledge/
  ├── domain.memory.md      — Session context (hot, 48h TTL)
  ├── patterns.memory.md    — UI/UX patterns (warm, 14d TTL)
  └── decisions.memory.md   — Design decisions (cold, 365d TTL)
```

### CLAUDE.md Updated

Your CLAUDE.md now includes the **Parallel Workers** section:
- MCP tools: `spawn_parallel_workers`, `spawn_raw_workers`, `get_worker_status`
- Cost limits: $3/h soft → $5/h hard → $10/h critical
- Max parallel: 5 workers/terminal
- Designer-specific examples (parallel mockup generation)

## Session Workflow

**Session start:**
1. Read `knowledge/domain.memory.md` for active design tasks
2. Check UI patterns in `patterns.memory.md`

**Session end:**
1. Update `domain.memory.md` with design deliverables
2. Add UI/UX patterns to `patterns.memory.md` (if discovered)
3. Update `decisions.memory.md` with design system choices

## Parallel Workers

Use when:
- Multiple design variations (best-of-N selection)
- Parallel mockup generation (page components)
- Design system exploration (typography/color parallel)

**Example:**
```
mcp spawn_raw_workers terminal=designer task="Design landing page hero section" count=3 criteria="most engaging visual hierarchy"
```

---

**Conductor coordination**
2026-06-30 — ADR-049 Phase 3 info notification
