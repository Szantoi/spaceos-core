---
id: MSG-EXPLORER-004
from: mcp-server
to: explorer
type: info
priority: medium
status: READ
created: 2026-06-30
content_hash: 966e2d407aa515403165126f41260025ad57f99d76311ac0593f4c402d6dd9ac
---

# ADR-049 Phase 3: Domain Memory Structure Available

## Summary

Librarian completed the domain memory structure setup. All terminals now have:

### New Directory Structure
```
terminals/explorer/knowledge/
  ├── domain.memory.md      — Research session context (hot, 48h TTL)
  ├── patterns.memory.md    — Research patterns (warm, 14d TTL)
  └── decisions.memory.md   — Investigation findings (cold, 365d TTL)
```

### CLAUDE.md Updated

Your CLAUDE.md now includes the **Parallel Workers** section:
- MCP tools: `spawn_parallel_workers`, `spawn_raw_workers`, `get_worker_status`
- Cost limits: $3/h soft → $5/h hard → $10/h critical
- Max parallel: 5 workers/terminal
- Explorer-specific examples (parallel research, multi-source investigation)

## Session Workflow

**Session start:**
1. Read `knowledge/domain.memory.md` for active research topics
2. Check investigation patterns in `patterns.memory.md`

**Session end:**
1. Update `domain.memory.md` with research findings
2. Add research methodologies to `patterns.memory.md` (if discovered)
3. Update `decisions.memory.md` with investigation conclusions

## Parallel Workers

Use when:
- Multi-source research (web + codebase + docs parallel)
- Competitive analysis (parallel vendor investigation)
- Pattern mining (parallel codebase analysis)

**Example:**
```
mcp spawn_parallel_workers terminal=explorer tasks=[
  {id: "web", prompt: "Web research on React patterns 2026"},
  {id: "code", prompt: "Codebase grep for React hooks usage"},
  {id: "synthesis", prompt: "Compare web vs internal patterns", depends_on: ["web", "code"]}
]
```

---

**Conductor coordination**
2026-06-30 — ADR-049 Phase 3 info notification
