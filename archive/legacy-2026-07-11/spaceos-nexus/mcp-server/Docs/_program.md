---
id: program-joinerytech-mcpserver
title: "Program: JoineryTech MCP Server"
type: program_landing
scope: program
created: 2026-03-04
last_updated: 2026-03-04
---

# Program: JoineryTech MCP Server

## Mit tartalmaz ez a Program?

A **JoineryTech MCP Server** az összes JoineryTech AI agent egyetlen, autoritatív
kontextus- és projektmenedzsment-forrása. A program két önálló, párhuzamos sub-projektből áll:

---

## Sub-projektek

| Sub-Project | Mappa | Leírás | Felelős |
|:------------|:------|:-------|:--------|
| **mcp-context-server** | [mcp-context-server/](mcp-context-server/_readme.md) | Agent Context Server: SQLite backbone, RBAC, epizódikus RAG | backend_developer |
| **mcp-pm-engine** | [mcp-pm-engine/](mcp-pm-engine/_readme.md) | Project Management Engine: PM séma, MCP tool-ok, REST API | backend_developer |

---

## Program Hierarchia

```
Program: JoineryTech MCP Server
│
├── Project A: mcp-context-server
│   ├── M01 ✅  EPIC-01..08 (M01 lezárva / folyamatban)
│   ├── M02 🗓️  EPIC-09: SQLite schema
│   │          EPIC-10: bootstrap_agent
│   │          EPIC-11: RBAC → SQLite
│   │          EPIC-12: Episodic Memory (RAG)
│   └── M03 ⏳  EPIC-13: Legacy tool refactor
│
└── Project B: mcp-pm-engine
    ├── M03 ⏳  EPIC-14: PM schema + seeder
    │          EPIC-15: PM MCP tool-ok
    │          EPIC-16: REST API
    └── M04 ⏳  EPIC-17: Marketing domain demo
```

---

## Megosztott Függőségek

| Függőség | Érintett projektek | Leírás |
|:---------|:-------------------|:-------|
| `agent.db` SQLite (EPIC-09) | mcp-context-server → mcp-pm-engine | A PM Engine EPIC-14 az mcp-context-server EPIC-09 lezárása után startol |
| ChromaDB VectorStore | mcp-context-server (EPIC-12) | Az epizódikus RAG az mcp-context-server hatásköre |
| `better-sqlite3` npm package | mindkét sub-project | Közös SQLite driver |

---

## Kulcsdokumentumok

| Dokumentum | Cél |
|:-----------|:----|
| [goal.md](goal.md) | Stratégiai célok, alapelvek, sikerkritériumok |
| [Program_Vision_And_Strategy.md](Program_Vision_And_Strategy.md) | Teljes program vízió |
| [program-state.md](program-state.md) | Aggregált projektállapot dashboard |
| [MCP_Server_Architecture.md](MCP_Server_Architecture.md) | Architektúrális döntések |

---

## Megjegyzés a state.md fájlokról

A jelenlegi `state.md` kontextus-megőrző dokumentumok (Docs/state.md,
mcp-context-server/delivery/mcp-maintenance/state.md stb.) **ideiglenes** megoldások.

A PM Engine (EPIC-14–16, M03) lezárása után az állapot autoritatív forrása az `agent.db`
SQLite adatbázis lesz, amelyet a REST API-n keresztül lehet olvasni.
