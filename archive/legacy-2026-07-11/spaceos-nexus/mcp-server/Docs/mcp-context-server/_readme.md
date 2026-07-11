---
id: mcp-context-server-scope
title: "Sub-Project: MCP Context Server"
type: scope-root
project: mcp-context-server
track: both
created: 2026-03-04
---

# 🔷 MCP Context Server

**Felelősség:** Az agent tudja, ki ő — és megkapja mindazt, ami a munkájához kell.

Egyetlen `bootstrap_agent` hívás → teljes kontextus payload: identity, role, runbook,
allowed tools, workflow, template, session_id.

## Struktúra

```
mcp-context-server/
├── _readme.md               ← ez a fájl
├── delivery/
│   ├── mcp-maintenance/     ← SQLite backbone, RBAC, hygiene (M01–M02)
│   └── mcp-rbac/            ← RBAC scope és implementáció (lezárva)
└── discovery/
    ├── mcp-integration/     ← MCP tool handoff integráció vizsgálata (lezárva)
    └── mcp-rbac/            ← RBAC megközelítés validálása (lezárva)
```

## Kulcs MCP eszközök

`bootstrap_agent` · `get_role` · `get_workflow` · `get_template` · `get_core` ·
`search_knowledge` · `submit_artifact` · `get_workflow_state`
