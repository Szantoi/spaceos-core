---
id: epic-pm-engine-14
title: "Epic 14: PM Schema Design & State.md Seeder"
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
depends_on: EPIC-09
---

# 🗄️ EPIC-14: PM Schema Design & State.md Seeder

## Célkitűzés

Az `agent.db`-t kibővíteni a **projekt-menedzsment hierarchia táblákkal**
(`programs`, `projects`, `milestones`, `epics`, `tasks`, `task_status`),
és elkészíteni a seedert, amely a meglévő `Docs/**/state.md` fájlokból
tölti fel az adatbázist — backward-compatible indulás.

**Blokkol:** EPIC-15 és EPIC-16 (mindkettő függ ettől a sémától).

---

## SQLite Schema Kiterjesztés

```sql
CREATE TABLE IF NOT EXISTS programs (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    slug        TEXT NOT NULL UNIQUE,   -- pl. 'joinerytech-mcpserver'
    title       TEXT NOT NULL,
    status      TEXT NOT NULL DEFAULT 'active',
    created_at  TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS projects (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    program_id  INTEGER NOT NULL REFERENCES programs(id),
    slug        TEXT NOT NULL UNIQUE,   -- pl. 'mcp-context-server'
    title       TEXT NOT NULL,
    status      TEXT NOT NULL DEFAULT 'active',
    created_at  TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS milestones (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    project_id  INTEGER NOT NULL REFERENCES projects(id),
    slug        TEXT NOT NULL,          -- pl. 'M02'
    title       TEXT NOT NULL,
    status      TEXT NOT NULL DEFAULT 'planned',
    -- 'planned' | 'in_progress' | 'completed'
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(project_id, slug)
);

CREATE TABLE IF NOT EXISTS epics (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    milestone_id INTEGER NOT NULL REFERENCES milestones(id),
    slug         TEXT NOT NULL,         -- pl. 'EPIC-09'
    title        TEXT NOT NULL,
    status       TEXT NOT NULL DEFAULT 'backlog',
    -- 'backlog' | 'planned' | 'in_progress' | 'completed' | 'blocked'
    assignee     TEXT,
    priority     TEXT DEFAULT 'P1',
    depends_on   TEXT,                  -- JSON array of epic slugs
    created_at   TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(milestone_id, slug)
);

CREATE TABLE IF NOT EXISTS tasks (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    epic_id     INTEGER NOT NULL REFERENCES epics(id),
    slug        TEXT NOT NULL,          -- pl. 'T09-01'
    title       TEXT NOT NULL,
    type        TEXT,                   -- 'Dev' | 'QA' | 'Arch' | 'Docs'
    estimate    TEXT,                   -- pl. '1 nap'
    status      TEXT NOT NULL DEFAULT 'planned',
    -- 'planned' | 'in_progress' | 'completed' | 'blocked'
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(epic_id, slug)
);

CREATE TABLE IF NOT EXISTS task_status_log (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    task_id     INTEGER NOT NULL REFERENCES tasks(id),
    old_status  TEXT,
    new_status  TEXT NOT NULL,
    changed_by  TEXT,                   -- agent session_id vagy 'seeder'
    changed_at  TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Indexek
CREATE INDEX IF NOT EXISTS idx_projects_program ON projects(program_id);
CREATE INDEX IF NOT EXISTS idx_milestones_project ON milestones(project_id);
CREATE INDEX IF NOT EXISTS idx_epics_milestone ON epics(milestone_id);
CREATE INDEX IF NOT EXISTS idx_epics_status ON epics(status);
CREATE INDEX IF NOT EXISTS idx_tasks_epic ON tasks(epic_id);
CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks(status);
```

---

## State.md Seeder

### Fájl: `scripts/seed-pm-db.ts`

**Feladata:**

1. Végigjárja a `Docs/**/state.md` fájlokat.
2. Minden fájlból kinyeri (YAML frontmatter + Markdown táblák):
   - `program`, `project_id`, milestone map, epic state map (ID, title, state, felelős)
3. `INSERT OR REPLACE` szemantikával feltölti a PM táblákat.
4. Az epic-ekhez tartozó task-okat az `epic_XX/state.md` fájlokból olvassa (Feladatok tábla).
5. Befejezéskor statisztikát nyomtat.

### Seed adatok (jelenlegi Docs alapján)

```
programs:   1  (joinerytech-mcpserver)
projects:   2  (mcp-context-server, mcp-pm-engine)
milestones: 4  (M01, M02, M03 / PROJ-A + M03, M04 / PROJ-B)
epics:      10 (EPIC-01..17, a relevánsak)
tasks:      ~35
```

---

## Feladatok

| Típus | ID | Feladat | Becslés | Állapot |
|:-:|:---|:--------|:--------|:--------|
| `Arch` | T14-01 | PM séma véglegesítése, táblák és indexek tervezése | 0.5 nap | 🗓️ Planned |
| `Dev`  | T14-02 | PM táblák hozzáadása `AgentDb.ts`-hez (init, CRUD metódusok) | 1 nap | 🗓️ Planned |
| `Dev`  | T14-03 | `seed-pm-db.ts` szkript: state.md fájlok parse-olása + DB feltöltés | 1.5 nap | 🗓️ Planned |
| `Dev`  | T14-04 | `seed-pm-db.ts` integrálása a fő `seed-agent-db.ts`-be (egy parancs) | 0.5 nap | 🗓️ Planned |
| `QA`   | T14-05 | Unit teszt: seeder idempotencia — kétszer futtatva nem duplikál | 0.5 nap | 🗓️ Planned |

**Összesített becslés:** ~4 nap

---

## Definition of Done

- [ ] PM táblák léteznek `agent.db`-ben seeder futtatása után.
- [ ] A jelenlegi Docs-beli összes epic megjelenik az `epics` táblában helyes státusszal.
- [ ] `AgentDb.getProjectState(project_slug)` visszaad egy strukturált objektumot az összes epichez.
- [ ] Seeder kétszer futtatva nem duplikál rekordot.
- [ ] `npm run seed` egyetlen parancs lefuttatja mind a context server, mind a PM seedert.

---

## Blokkolók / Kockázatok

| Kockázat | Valószínűség | Mitigáció |
|:---------|:-------------|:----------|
| `state.md` frontmatter formátum nem egységes minden fájlban | Közepes | Seeder logoljon warning-ot, ne álljon le hibás fájlnál |
| Task adatok nem teljesen kinyerhetők Markdown táblából | Közepes | Csak epic-szintű adatot kötelezővé tenni; task-okat opcionálisra |
