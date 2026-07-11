---
id: epic-mcp-maintenance-15
title: "Epic 15: PM Query Tools (Read-Only Context-Server Layer)"
type: epic
milestone: M03
project: mcp-maintenance
project_id: mcp-context-server
status: DONE
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "DONE"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-10
---

# 📊 EPIC-15: PM Query Tools (Read-Only Context-Server Layer)

## Célkitűzés

Implementálni a **read-only PM query MCP tool-okat** a context-server szinten, amelyek agyenteknek
lehetővé teszik a projektmanagement state lekérdezését anélkül, hogy közvetlenül az `mcp-pm-engine`
szerverrel kommunikálnának. A contextual relevancia alapján (agent role, domain) szűrés és
autentikáció a context-server biztosítja.

**Megjegyzés:** Az M03-ra vonatkozó write-operations (`update_task_status`, `add_task` stb.)
az mcp-pm-engine szerverhez tartoznak (PROJ-B). Ez az EPIC csak read-only query-kat tartalmaz.

---

## Kontextus és Motiváció

Az EPIC-10 `bootstrap_agent()` alapján az agent tudja a saját domain-jét és role-ját. M03-ban
az agent képesnek kell lennie arra, hogy:

1. **Lekérdezze a projektstátuszt**: "Mi az aktuális project state (milestone, open tasks)?"
2. **Lekérdezze a következő feladatokat**: "Mi az én domain-omban a soron következő task?"
3. **Lekérdezze a workflow-okat**: "Milyen task-template-ek érhetők el a jelenlegi szakaszban?"

A context-server összeveti az agent identitásával (RBAC) és csak releváns PM state-t adja vissza.
Ha az agent Discovery track-ben dolgozik és Discovery track task-ok nincsenek, az üres listát kap.

---

## Érintett MCP Tool-ok (New)

| Tool | Input | Output | Notes |
|:-----|:------|:--------|:------|
| `get_project_state(project_id)` | {project_id: string} | {milestone, open_tasks_count, due_date} | Read-only query |
| `list_my_team_tasks(domain?, track?)` | {domain?: string, track?: "discovery"{}"delivery"} | {tasks: [{id, title, status, assigned_to}]} | Filtered by agent domain+role |
| `get_task_context(task_id)` | {task_id: string} | {task, acceptance_criteria, workflow, template} | Full context for task |
| `search_tasks(query, filters?)` | {query: string, filters?: {domain, track, status}} | {tasks: Task[]} (max 10) | Semantic search on task titles+descriptions |

---

## Sikerkritérium (Definition of Done)

- [x] `get_project_state(project_id)` MCP tool implementálva (`pm-query` plugin + legacy module).
- [x] `list_my_team_tasks()` tool: agent domain scope + RBAC filtering (admin bypass).
- [x] `get_task_context(task_id)` tool: detailed task context lekérdezés + domain RBAC check.
- [x] `search_tasks()` tool: query + filters (domain/track/status) + limit.
- [x] PM data forrása: `agent.db` read-only query builders (`AgentDb.getProjectState`, `listPmTasks`, `getTaskContext`).
- [x] RBAC validáció: non-admin agent csak saját domain task-okat ér el.
- [x] Error handling: `ErrorResponses` standardizált válaszok.
- [x] E2E/Integration teszt implementálva és futtatva: `pm-query-tools.integration.test.ts` (`list_my_team_tasks → get_task_context → search_tasks → get_project_state`)
- [x] Zero write-operations ebben az EPIC-ben (read-only)
- [x] Dokumentáció: query tool usage guide (`usage-guide.md`)

## Nem Scope (EPIC-15-ben)

- Write-operations (update_task_status, add_task) — mcp-pm-engine felelőssége
- PM Engine Server (PROJ-B) — external projekt
- Real-time task notifications — M05 auditing feladata

---

## Függőségek

| Függőség | Állapot | Hatás |
|:---------|:--------|:------|
| EPIC-10: bootstrap_agent() | ✅ M02 | Agent identity available |
| EPIC-09: SQLite schema | ✅ M02 | PM data in agent.db |
| EPIC-11: Request context middleware | ✅ M02 | RBAC validation available |

---

## Task Breakdown (TODO)

- [x] TASK-15-01: PM query tool schemas + acceptance criteria
- [x] TASK-15-02: `get_project_state()` implementation
- [x] TASK-15-03: `list_my_team_tasks()` + RBAC filtering
- [x] TASK-15-04: `get_task_context()` with workflow + template
- [x] TASK-15-05: `search_tasks()` fuzzy search
- [x] TASK-15-06: Integration test suite implementálva és verifikálva

---

## Aktuális Implementáció (2026-03-12)

- `src/mcp/tools/pm-query.ts`: új `PmQueryPlugin` + `createPmQueryToolModule()`
- `src/mcp/AgentDb.ts`: új PM read-only metódusok (`getProjectState`, `listPmTasks`, `getTaskContext`)
- `src/mcp/mcpServer.ts`: plugin manifest + load + legacy module regisztráció
- `src/tests/unit/pm-query-plugin.test.ts`: PM plugin unit testek
- `src/tests/unit/AgentDb.pm-query.test.ts`: PM AgentDb query unit testek
- `src/tests/integration/pm-query-tools.integration.test.ts`: MCP HTTP integration flow teszt

---

## Kapcsolódó Dokumentáció

- Vision Goal #4: "Queryable PM state from agents"
- database/standards/02-delivery/delivery.process.md — PM terminology
- EPIC-10 bootstrap_agent() — identity foundation
