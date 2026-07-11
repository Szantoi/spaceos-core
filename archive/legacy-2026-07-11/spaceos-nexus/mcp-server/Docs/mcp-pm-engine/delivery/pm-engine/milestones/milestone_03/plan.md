---
id: plan-pm-engine-m03
title: "Milestone 03: PM Engine + API Kiszolgálás"
type: milestone
project: pm-engine
project_id: mcp-pm-engine
status: planned
depends_on: M02
created: 2026-03-04
---

# 🏁 M03 (PROJ-B): PM Engine + API Kiszolgálás

## Kontextus

M02 után az agent kontextus-kiszolgálás teljes egészében DB-alapú. A következő lépés
a **projekt-menedzsment állapot** gépiesítése: az Orchestrator ne olvassa a `state.md`
fájlokat — helyette MCP tool-on kérdezze le a PM adatbázisból.

M03 végén egy külső alkalmazás is képes lesz az összes projekt- és task-állapotot
REST API-n keresztül lekérni — a `state.md` fájlok emberi kényelmi másolattá válnak,
nem elsődleges forrássá.

## Cél

- PM hierarchia (`programs → projects → milestones → epics → tasks`) SQLite-ban él.
- Az Orchestrator egyetlen `get_project_state` hívással látja az összes epic státuszát.
- Külső app REST API-n figyeli a projekt állapotát — nem kell a git repót olvasnia.
- A `state.md` fájlok elhagyhatóvá válnak.

## Epicek

| ID | Cím | Prioritás | Állapot |
|:---|:----|:----------|:--------|
| EPIC-14 | PM Schema Design & State.md Seeder | P0 — Alapkövetelmény | 🗓️ Planned |
| EPIC-15 | PM MCP Tool-ok | P0 — Alapkövetelmény | 🗓️ Planned |
| EPIC-16 | PM REST API — külső alkalmazás kiszolgálás | P1 — Fontos | 🗓️ Planned |

## Sikerkritérium

- [ ] `get_project_state()` visszaadja az összes epic státuszát DB-lekérdezéssel.
- [ ] `get_next_tasks(role)` visszaadja a nyitott, prioritizált task-okat.
- [ ] `update_task_status(task_id, status)` DB-be ír.
- [ ] `GET /api/projects` HTTP endpoint elérhető és visszaadja a program hierarchiát.
- [ ] Az Orchestrator agent egyetlen `state.md` fájlt sem olvas a feladatosztáshoz.
- [ ] A `state.md` fájlok törlése után a szerver ugyanúgy működik.

## Nem scope (M03-ban)

- Multi-domain demonstráció (M04 feladata).
- Frontend UI (Out of Scope).
