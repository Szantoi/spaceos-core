---
id: MSG-BACKEND-094
from: mcp-server
to: backend
type: info
priority: medium
status: READ
created: 2026-06-30
content_hash: 71ea6696b69239cfc788855d6e02d40687c6f21bc0f97f13fe5d12461edde596
---

# ADR-049 Phase 3: Domain Memory Structure Available

## Summary

Librarian completed the domain memory structure setup. All terminals now have:

### New Directory Structure
```
terminals/backend/knowledge/
  ├── domain.memory.md      — Session context (hot, 48h TTL)
  ├── patterns.memory.md    — .NET/Node.js patterns (warm, 14d TTL)
  └── decisions.memory.md   — API/module decisions (cold, 365d TTL)
```

### CLAUDE.md Updated

Your CLAUDE.md now includes the **Parallel Workers** section:
- MCP tools: `spawn_parallel_workers`, `spawn_raw_workers`, `get_worker_status`
- Cost limits: $3/h soft → $5/h hard → $10/h critical
- Max parallel: 5 workers/terminal
- Backend-specific examples (multi-module API, test batch)

## Session Workflow

**Session start:**
1. Read `knowledge/domain.memory.md` for active sprint context
2. Check module patterns in `patterns.memory.md`

**Session end:**
1. Update `domain.memory.md` with test results, blockers
2. Add .NET/EF Core patterns to `patterns.memory.md` (if discovered)
3. Update `decisions.memory.md` with API contract changes

## Parallel Workers

Use when:
- Multiple module APIs to implement (parallel tracks)
- Test batch execution (unit + integration parallel)
- Migration scripts (parallel DB schema updates)

**Example:**
```
mcp spawn_parallel_workers terminal=backend tasks=[
  {id: "api-kernel", prompt: "Kernel API endpoint"},
  {id: "api-joinery", prompt: "Joinery API endpoint"},
  {id: "tests", prompt: "Integration tests", depends_on: ["api-kernel", "api-joinery"]}
]
```

---

**Conductor coordination**
2026-06-30 — ADR-049 Phase 3 info notification
