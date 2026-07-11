---
id: epic-pm-engine-15
title: "Epic 15: PM MCP Tool-ok — get_project_state, get_next_tasks, update_task_status"
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
depends_on: EPIC-14
---

# 🛠️ EPIC-15: PM MCP Tool-ok

## Célkitűzés

Implementálni a három alapvető PM MCP tool-t, amelyekkel az Orchestrator agent
egyetlen `state.md` fájl olvasása nélkül képes lekérdezni és frissíteni a
program állapotát.

---

## Három MCP Tool

### `get_project_state`

```typescript
// Bemenet
{
  project?: string,    // slug, pl. 'mcp-context-server' — elhagyva: összes projekt
  milestone?: string   // slug, pl. 'M02' — elhagyva: összes milestone
}

// Kimenet
{
  program: string,
  projects: Array<{
    slug: string,
    title: string,
    status: string,
    milestones: Array<{
      slug: string,
      title: string,
      status: string,
      epics: Array<{
        slug: string,
        title: string,
        status: string,
        assignee: string,
        task_count: number,
        open_tasks: number
      }>
    }>
  }>
}
```

### `get_next_tasks`

```typescript
// Bemenet
{
  role: string,        // pl. 'backend_developer'
  epic_id?: string,    // szűkítés epic-re, pl. 'EPIC-09'
  limit?: number       // default: 5
}

// Kimenet
{
  tasks: Array<{
    slug: string,
    title: string,
    type: string,
    estimate: string,
    epic_slug: string,
    epic_title: string,
    priority: string
  }>
}
```

### `update_task_status`

```typescript
// Bemenet
{
  task_slug: string,          // pl. 'T09-01'
  status: 'in_progress' | 'completed' | 'blocked',
  session_id?: string         // ki változtatta (audit log)
}

// Kimenet
{
  updated: true,
  task_slug: string,
  new_status: string,
  epic_slug: string
}
```

---

## Service Architektúra

```
src/mcp/
  mcpServer.ts     ← MÓDOSÍTOTT: 3 új tool regisztrálva

src/metadata/
  PmService.ts     ← ÚJ: get_project_state, get_next_tasks, update_task_status logika
```

A `PmService` az `AgentDb` példányon keresztül dolgozik — nincs közvetlen SQL a tool handler-ekben.

```typescript
// PmService.ts
export class PmService {
  constructor(private db: AgentDb) {}

  getProjectState(project?: string, milestone?: string): ProjectStatePayload { ... }
  getNextTasks(role: string, epicId?: string, limit?: number): TaskListPayload { ... }
  updateTaskStatus(taskSlug: string, status: string, sessionId?: string): UpdateResult { ... }
}
```

---

## RBAC Engedélyek

| Tool | Szükséges engedély | Default |
|:-----|:-------------------|:--------|
| `get_project_state` | `pm_read` | Minden agent-role kap |
| `get_next_tasks` | `pm_read` | Minden agent-role kap |
| `update_task_status` | `pm_write` | Csak `backend_developer`, `management`, `orchestrator` |

Az RBAC sémák (`*.schema.yaml`) frissítése szükséges az új engedélyekkel.

---

## Feladatok

| Típus | ID | Feladat | Becslés | Állapot |
|:-:|:---|:--------|:--------|:--------|
| `Dev`  | T15-01 | `PmService.ts` implementálása: `getProjectState()`, `getNextTasks()`, `updateTaskStatus()` | 1.5 nap | 🗓️ Planned |
| `Dev`  | T15-02 | `AgentDb.ts` kiegészítése PM lekérdezési metódusokkal (JOIN-ok, state-szűrés) | 1 nap | 🗓️ Planned |
| `Dev`  | T15-03 | Tool-ok regisztrálása `mcpServer.ts`-ben; input schema validáció (zod) | 0.5 nap | 🗓️ Planned |
| `Dev`  | T15-04 | RBAC schema.yaml-ok frissítése: `pm_read`, `pm_write` engedélyek | 0.5 nap | 🗓️ Planned |
| `QA`   | T15-05 | Unit teszt: `getProjectState()` mock DB-vel | 0.5 nap | 🗓️ Planned |
| `QA`   | T15-06 | E2E teszt: Orchestrator scenario — `get_next_tasks` → `update_task_status` flow | 1 nap | 🗓️ Planned |

**Összesített becslés:** ~5 nap

---

## Definition of Done

- [ ] `get_project_state()` MCP hívás visszaadja a teljes program hierarchiát DB-lekérdezéssel.
- [ ] `get_next_tasks("backend_developer")` visszaadja a nyitott task-okat prioritás szerint.
- [ ] `update_task_status("T09-01", "completed")` DB-be ír és `task_status_log`-ba naplóz.
- [ ] RBAC: `pm_write` nélküli role `update_task_status`-ra `PERMISSION_DENIED`-t kap.
- [ ] E2E teszt: Orchestrator flow zöld.

---

## Blokkolók / Kockázatok

| Kockázat | Valószínűség | Mitigáció |
|:---------|:-------------|:----------|
| EPIC-14 csúszik | Közepes | Mockolható `AgentDb` — fejlesztés párhuzamosítható |
| `get_next_tasks` role-alapú prioritizálás logikája komplex | Alacsony | Első iteráció: FIFO + `depends_on` blokkolt epic szűrés |
