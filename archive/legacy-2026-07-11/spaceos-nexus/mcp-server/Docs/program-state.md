---
id: program-state-mcpserver
title: "Program State: JoineryTech MCP Server"
type: program_state
scope: program
track: both
updated: 2026-03-04
note: "Ideiglenes aggregalt dashboard. M03 utan az agent.db SQLite DB es a REST API valtja fel."
---

# Program State: JoineryTech MCP Server

> **Megjegyzés:** Ez az aggregált állapot-dashboard a PM Engine (EPIC-14–16) lezárásáig
> aktív. Utána az autoritatív forrás az `agent.db` + REST API lesz.

---

## Cross-Project Overview

| Sub-Project | Aktív Milestone | Aktív Epic | Állapot | Felelős |
|:------------|:----------------|:-----------|:--------|:--------|
| `mcp-context-server` | M02 | EPIC-09: SQLite schema | 🗓️ BACKLOG_READY | backend_developer |
| `mcp-pm-engine` | M03 | EPIC-14: PM séma | 🗓️ BACKLOG_READY | backend_developer |

---

## mcp-context-server — Milestone Térkép

| Milestone | Focus | Állapot | Epics |
|:----------|:------|:--------|:------|
| M01 | RBAC, Write Layer, Static Analysis | 🚧 Folyamatban | EPIC-01..08 |
| M02 | SQLite backbone, bootstrap_agent, RAG | 🗓️ Tervezett | EPIC-09, 10, 11, 12 |
| M03 | Legacy tool refactor | ⏳ Jövő | EPIC-13 |

### M02 Epic Állapot

| ID | Title | FSM State | Prioritás |
|:---|:------|:----------|:----------|
| EPIC-09 | SQLite Schema Design & Database Seeder | BACKLOG_READY | P0 |
| EPIC-10 | bootstrap_agent Tool | BACKLOG_READY | P0 (depends: EPIC-09) |
| EPIC-11 | RBAC Migration: YAML → SQLite | BACKLOG_READY | P1 (depends: EPIC-09) |
| EPIC-12 | Episodic Memory Layer (RAG) | BACKLOG_READY | P1 |

📄 Részletek: [mcp-context-server/delivery/mcp-maintenance/state.md](mcp-context-server/delivery/mcp-maintenance/state.md)

---

## mcp-pm-engine — Milestone Térkép

| Milestone | Focus | Állapot | Epics |
|:----------|:------|:--------|:------|
| M03 | PM séma, MCP tool-ok, REST API | 🗓️ Tervezett | EPIC-14, 15, 16 |
| M04 | Domain demo, integráció | ⏳ Jövő | EPIC-17 |

### M03 Epic Állapot

| ID | Title | FSM State | Prioritás |
|:---|:------|:----------|:----------|
| EPIC-14 | PM Schema Design & State.md Seeder | BACKLOG_READY | P0 (depends: EPIC-09) |
| EPIC-15 | PM MCP Tools (get_project_state stb.) | BACKLOG_READY | P1 (depends: EPIC-14) |
| EPIC-16 | REST API & External Interface | BACKLOG_READY | P1 (depends: EPIC-15) |

📄 Részletek: [mcp-pm-engine/delivery/pm-engine/state.md](mcp-pm-engine/delivery/pm-engine/state.md)

---

## Discovery Track Összefoglalás

| Téma | Fázis | Állapot | Eredmény |
|:-----|:------|:--------|:---------|
| mcp-integration | 04 Test-and-Learn | ✅ Lezárva | JSON handoff validálva |
| mcp-rbac | 04 Test-and-Learn | ✅ Lezárva | RBAC constraints validálva |

---

## Globális Állapot Összefoglaló

| Sub-project | Összes Epic | Kész | Folyamatban | BACKLOG_READY |
|:------------|:------------|:-----|:------------|:--------------|
| mcp-context-server | 13 | ~6 | 2 (EPIC-02, EPIC-08) | EPIC-09..13 |
| mcp-pm-engine | 7 | 0 | 0 | EPIC-14..17, 18..20 |
| **Összesen** | **20** | **~6** | **2** | **9+** |
