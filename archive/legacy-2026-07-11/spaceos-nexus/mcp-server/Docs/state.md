---
id: state-mcpserver
title: "State: McpServer — Aggregated Dashboard (DEPRECATED)"
type: aggregated-state
scope: mcpserver
track: both
updated: 2026-03-04
status: deprecated
note: "DEPRECATED. Az uj standard-kompatibilis program-szintu dashboard: Docs/program-state.md"
superseded_by: Docs/program-state.md
---

# ⚠️ DEPRECATED — Lásd: [program-state.md](program-state.md)

> Ez a fájl **elavult**. A `database/standards/00-foundation/project.folder-structure.md`
> standard szerint a program-szintű dashboard neve `program-state.md`.
>
> **→ Aktuális dashboard: [program-state.md](program-state.md)**

---

## Discovery Track

| Téma | Fázis | Állapot | Eredmény |
|:-----|:------|:--------|:---------|
| [mcp-integration](discovery/mcp-integration/dwi-state.md) | 04 Test-and-Learn | ✅ Lezárva | JSON handoff + hierarchy validálva |
| [mcp-rbac](discovery/mcp-rbac/dwi-state.md) | 04 Test-and-Learn | ✅ Lezárva | RBAC constraints kísérlet validálva |

---

## Delivery Track

### mcp-context-server — mcp-maintenance

| Milestone | Epic | Title | Állapot |
|:----------|:-----|:------|:--------|
| M01 | EPIC-01 | RBAC schema update & root cleanup | ✅ Completed |
| M01 | EPIC-02 | Dead Code Elimination & Static Analysis | 🚧 In Progress |
| M01 | EPIC-08 | MCP Write Layer — Artifact Submit & Session Control | 🏗️ In Dev |
| M02 | EPIC-09 | SQLite Schema Design & Database Seeder | 🗓️ Planned |
| M02 | EPIC-10 | `bootstrap_agent` Tool — Zero-Path Agent Identification | 🗓️ Planned |
| M02 | EPIC-11 | RBAC Migration: YAML → SQLite | 🗓️ Planned |

📄 Részletek: [mcp-context-server/delivery/mcp-maintenance/state.md](mcp-context-server/delivery/mcp-maintenance/state.md)

### mcp-context-server — mcp-rbac

| Milestone | Epic | Title | Állapot |
|:----------|:-----|:------|:--------|
| M01 | EPIC-01 | RbacFilter service implementálása | ✅ Completed |
| M01 | EPIC-02 | MCP kliens cache-elés tesztelése | ✅ Completed |

> FSM: `CLOSED_DONE` — Teszt-üzem lezárva, visszabontható.

📄 Részletek: [mcp-context-server/delivery/mcp-rbac/state.md](mcp-context-server/delivery/mcp-rbac/state.md)

---

## Összefoglalás

| Sub-project | Epics összesen | Kész | Folyamatban | Tervezett |
|:------------|:---------------|:-----|:------------|:----------|
| mcp-context-server/mcp-maintenance | 6 | 1 | 2 | 3 |
| mcp-context-server/mcp-rbac | 2 | 2 | 0 | 0 |
| mcp-pm-engine | 0 | 0 | 0 | 0 — M03-tól |
