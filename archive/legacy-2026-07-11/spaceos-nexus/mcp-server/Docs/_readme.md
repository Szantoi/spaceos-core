---
id: mcpserver-scope
title: "McpServer — Documentation Root"
type: scope-root
track: both
parent_program: joinerytech-flow
created: 2026-03-04
last_updated: 2026-03-04
---

# 🛠️ McpServer — Dokumentációs Gyökér

Ez a mappa tartalmazza az összes dokumentációt, amely a **JoineryTech MCP Server** komponenshez kapcsolódik — a Discovery (Problem Space) és a Delivery (Solution Space) sávokat egyaránt.

---

## Struktúra

```
Docs/
├── _readme.md                       ← ez a fájl (régi landing — lásd _program.md)
├── _program.md                      ← Program Landing Page (standard szerint)
├── program-state.md                 ← Aggregált cross-project dashboard (standard szerint)
├── goal.md                          ← program-szintű célkitűzés és alapelvek
├── state.md                         ← DEPRECATED: lásd program-state.md
├── Program_Vision_And_Strategy.md   ← stratégiai irány, két alrendszer, roadmap
├── MCP_Server_Architecture.md       ← architektúrális döntések (RAG vs. full doc)
│
├── mcp-context-server/              ← Alrendszer A: Agent Context Server
│   ├── _readme.md
│   ├── delivery/
│   │   ├── mcp-maintenance/         ← SQLite backbone, RBAC, hygiene (M01–M02)
│   │   └── mcp-rbac/                ← RBAC implementáció (lezárva)
│   └── discovery/
│       ├── mcp-integration/         ← MCP integráció vizsgálata (lezárva)
│       └── mcp-rbac/                ← RBAC scope validálása (lezárva)
│
└── mcp-pm-engine/                   ← Alrendszer B: Project Management Engine
    ├── _readme.md
    ├── delivery/                    ← (M03-tól)
    └── discovery/                   ← (M02 lezárása után indul)
```

> **Megjegyzés a `state.md` fájlokról:** ezek **ideiglenes** kontextus-megőrző dokumentumok.
> A PM Engine (M03) lezárása után az állapot autoritatív forrása az SQLite DB lesz —
> a `state.md` fájlok elhagyhatók. Emberi megfigyelés: külső app + API.

---

## Discovery Témák

| Téma | Állapot | Leírás |
|:-----|:--------|:-------|
| [mcp-integration](discovery/mcp-integration/dwi-state.md) | 04 Test-and-Learn | MCP tool handoff integráció vizsgálata |
| [mcp-rbac](discovery/mcp-rbac/dwi-state.md) | 04 Test-and-Learn | RBAC scope és megközelítés validálása |

---

## Delivery Sub-projektek

| Sub-project | Állapot | Leírás |
|:------------|:--------|:-------|
| [mcp-maintenance](delivery/mcp-maintenance/_readme.md) | 🏗️ Active | RBAC konzisztencia, szerver higiénia, teszt lefedettség |
| [mcp-rbac](delivery/mcp-rbac/state.md) | 🏗️ Active | RBAC filter, role context, jogosultságok kezelése |

---

## Kapcsolódó Információk

- **MCP Server kód:** `JoineryTech.McpServer` repository
- **Program szint:** [joinerytech-flow program](../\_program.md)
- **Aggregált program állapot:** [program-state.md](../program-state.md)
