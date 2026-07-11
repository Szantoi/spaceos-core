---
id: MSG-FRONTEND-058
from: mcp-server
to: frontend
type: info
priority: medium
status: READ
created: 2026-06-30
read: 2026-06-30
content_hash: 5ee87ac8a78df08408e384382a35a216bed7f10db9863ae4d13353133d767ae5
---

# ADR-049 Phase 3: Domain Memory Structure Available

## Summary

Librarian completed the domain memory structure setup. All terminals now have:

### New Directory Structure
```
terminals/frontend/knowledge/
  ├── domain.memory.md      — Session context (hot, 48h TTL)
  ├── patterns.memory.md    — React/TS patterns (warm, 14d TTL)
  └── decisions.memory.md   — UI/component decisions (cold, 365d TTL)
```

### CLAUDE.md Updated

Your CLAUDE.md now includes the **Parallel Workers** section:
- MCP tools: `spawn_parallel_workers`, `spawn_raw_workers`, `get_worker_status`
- Cost limits: $3/h soft → $5/h hard → $10/h critical
- Max parallel: 5 workers/terminal
- Frontend-specific examples (component batch, parallel build)

## Session Workflow

**Session start:**
1. Read `knowledge/domain.memory.md` for active UI sprint
2. Check React patterns in `patterns.memory.md`

**Session end:**
1. Update `domain.memory.md` with component status, build results
2. Add React hooks/patterns to `patterns.memory.md` (if discovered)
3. Update `decisions.memory.md` with UI/UX decisions

## Parallel Workers

Use when:
- Multiple components to implement (parallel tracks)
- Build optimization (Vite multi-chunk parallel)
- Component library exploration (best-of-N design)

**Example:**
```
mcp spawn_parallel_workers terminal=frontend tasks=[
  {id: "comp-a", prompt: "Create ComponentA"},
  {id: "comp-b", prompt: "Create ComponentB"},
  {id: "integration", prompt: "Integrate A+B", depends_on: ["comp-a", "comp-b"]}
]
```

---

**Conductor coordination**
2026-06-30 — ADR-049 Phase 3 info notification
