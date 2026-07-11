---
id: mcp-pm-engine-scope
title: "Sub-Project: MCP Project Management Engine"
type: scope-root
project: mcp-pm-engine
track: both
created: 2026-03-04
---

# 🔶 MCP Project Management Engine

**Felelősség:** Az Orchestrator tudja, mi van folyamatban — és képes feladatot kiosztani.
Emberi megfigyelőknek API-on keresztül kiszolgált dashboard, nem `.md` fájlok.

Egyetlen `get_project_state` hívás → az összes epic/task állapot lekérdezéssel,
nem fájlolvasással.

## Struktúra

```
mcp-pm-engine/
├── _readme.md     ← ez a fájl
├── delivery/      ← PM Engine implementáció (M03-tól)
└── discovery/     ← PM státusz-kezelés megközelítés vizsgálata
```

## Kulcs MCP eszközök (tervezett, M03)

`get_project_state` · `get_next_tasks` · `update_task_status` · `list_epics` · `list_tasks`

## Státusz

🗓️ **Tervezett** — Discovery fázis M02 lezárása után indul.

> A `state.md` fájlok ideiglenesen megmaradnak kontextus-megőrzés céljából.
> A PM Engine lezárása után a `state.md`-k emberi olvasású tükrökké válnak —
> az állapot autoritatív forrása kizárólag az SQLite DB lesz.
