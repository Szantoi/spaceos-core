---
id: epic-pm-engine-16
title: "Epic 16: PM REST API — Külső Alkalmazás Kiszolgálás"
type: epic
milestone: M03
project: pm-engine
project_id: mcp-pm-engine
status: planned
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-15
---

# 🌐 EPIC-16: PM REST API — Külső Alkalmazás Kiszolgálás

## Célkitűzés

HTTP REST API endpoint-okat nyújtani, amelyeken keresztül **egy külső alkalmazás**
(pl. dashboard, monitoring tool, Obsidian plugin) le tudja kérdezni a program és
projekt állapotát — anélkül, hogy a git repót vagy az MCP protokollt ismernie kellene.

Ez az endpoint teszi lehetővé, hogy a `state.md` fájlok teljesen elhagyhatóvá
váljanak az emberi megfigyelés számára.

---

## API Végpontok

### `GET /api/health`
```json
{ "status": "ok", "db": "connected", "version": "1.0.0" }
```

### `GET /api/program`
Visszaadja a teljes program hierarchiát (programok → projektek → milestone-ok → epic-ek).

```json
{
  "program": {
    "slug": "joinerytech-mcpserver",
    "title": "JoineryTech MCP Server",
    "projects": [
      {
        "slug": "mcp-context-server",
        "title": "Agent Context Server",
        "status": "active",
        "milestones": [
          {
            "slug": "M02",
            "title": "SQLite Backbone + Context Server",
            "status": "in_progress",
            "epics": [
              { "slug": "EPIC-09", "title": "...", "status": "in_progress", "assignee": "backend_developer" }
            ]
          }
        ]
      }
    ]
  }
}
```

### `GET /api/projects/:slug`
Egyetlen projekt részletei milestone + epic bontással.

### `GET /api/epics/:slug`
Egyetlen epic részletei task listával és státuszokkal.

### `GET /api/tasks?role=backend_developer&status=planned`
Szűrhető task lista — az Orchestrator dashboard-jához.

### `PATCH /api/tasks/:slug/status`
```json
{ "status": "completed", "changed_by": "external-dashboard" }
```
Task státusz frissítés (opcionális — ha a külső app is akar írni).

---

## Technikai Megvalósítás

Az MCP szerver már Express-t használ — az új route-ok egy `src/api/` modulban élnek.

```
src/api/
  pmRouter.ts     ← Express router: /api/* végpontok
  pmController.ts ← Handler függvények, PmService hívások

src/index.ts      ← app.use('/api', pmRouter) regisztrálás
```

### Autentikáció

- Első iteráció: **API key** (`X-Api-Key` header) — konfigurálható `.env`-ben.
- Ha nincs API key konfigurálva: csak read-only végpontok érhetők el (health, program, projects).
- PATCH végponthoz API key kötelező.

---

## Feladatok

| Típus | ID | Feladat | Becslés | Állapot |
|:-:|:---|:--------|:--------|:--------|
| `Arch` | T16-01 | API végpontok tervezése, OpenAPI spec draft | 0.5 nap | 🗓️ Planned |
| `Dev`  | T16-02 | `pmRouter.ts` + `pmController.ts` implementálása (GET végpontok) | 1.5 nap | 🗓️ Planned |
| `Dev`  | T16-03 | API key autentikáció middleware | 0.5 nap | 🗓️ Planned |
| `Dev`  | T16-04 | `PATCH /api/tasks/:slug/status` implementálása | 0.5 nap | 🗓️ Planned |
| `Dev`  | T16-05 | `src/index.ts` regisztrálás + CORS konfiguráció | 0.5 nap | 🗓️ Planned |
| `QA`   | T16-06 | E2E teszt: `GET /api/program` visszaadja a helyes struktúrát | 0.5 nap | 🗓️ Planned |
| `QA`   | T16-07 | Teszt: `state.md` fájlok törlése után az API ugyanúgy működik | 0.5 nap | 🗓️ Planned |

**Összesített becslés:** ~4.5 nap

---

## Definition of Done

- [ ] `GET /api/program` visszaadja a teljes hierarchiát JSON-ban.
- [ ] `GET /api/tasks?role=backend_developer` visszaadja a nyitott task-okat.
- [ ] API key autentikáció működik PATCH végpontnál.
- [ ] A `state.md` fájlok törlése után az API válaszai nem változnak.
- [ ] Playwright E2E teszt lefedi a főbb GET endpoint-okat.

---

## Blokkolók / Kockázatok

| Kockázat | Valószínűség | Mitigáció |
|:---------|:-------------|:----------|
| CORS konfiguráció külső app-hoz | Alacsony | `.env`-ből konfigurálható `CORS_ORIGIN` |
| PATCH végpont írási konfliktus az MCP `update_task_status`-sal | Alacsony | Mindkettő ugyanazt a `PmService.updateTaskStatus()` hívja — nincs duplikált logika |
