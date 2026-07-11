---
id: MSG-ARCHITECT-031
from: mcp-server
to: architect
type: info
priority: medium
status: READ
created: 2026-06-30
content_hash: 4280a6abb36f39a04d15e457d68fe03a2264b8f9fcb49c03071a6d96e49c0c5e
---

# ADR-049 Phase 3: Domain Memory Structure Available

## Summary

Librarian completed the domain memory structure setup. All terminals now have:

### New Directory Structure
```
terminals/architect/knowledge/
  ├── domain.memory.md      — Session context (hot, 48h TTL)
  ├── patterns.memory.md    — Recurring patterns (warm, 14d TTL)
  └── decisions.memory.md   — Architectural decisions (cold, 365d TTL)
```

### CLAUDE.md Updated

Your CLAUDE.md now includes the **Parallel Workers** section:
- MCP tools: `spawn_parallel_workers`, `spawn_raw_workers`, `get_worker_status`
- Cost limits: $3/h soft → $5/h hard → $10/h critical
- Max parallel: 5 workers/terminal
- Architecture-specific examples included

## Session Workflow

**Session start:**
1. Read `knowledge/domain.memory.md` for current context
2. Check active decisions in `decisions.memory.md`

**Session end:**
1. Update `domain.memory.md` with session summary
2. Add new patterns to `patterns.memory.md` (if discovered)
3. Update `decisions.memory.md` with architectural decisions

## Parallel Workers

Use when:
- Multiple ADR analyses needed (best-of-N)
- Cross-module architecture review (parallel validation)
- Performance-critical design exploration

**Example:**
```
mcp spawn_parallel_workers terminal=architect tasks=[
  {id: "adr-a", prompt: "Design pattern A pros/cons"},
  {id: "adr-b", prompt: "Design pattern B pros/cons"},
  {id: "compare", prompt: "Compare A vs B", depends_on: ["adr-a", "adr-b"]}
]
```

---

**Conductor coordination**
2026-06-30 — ADR-049 Phase 3 info notification
