---
id: goal-epic-15
title: "EPIC-15 Goal: PM Query Tools (Read-Only Context-Server Layer)"
type: goal
epic: EPIC-15
sphere: mcp-context-server
milestone: M03
created: 2026-03-04
---

# EPIC-15 Goal: PM Query Tools (Read-Only Context-Server Layer)

## Executive Summary

Implement **read-only PM query MCP tools** in the context-server that expose project management state
(project status, task lists, workflow definitions) to agents without requiring direct access to the
`mcp-pm-engine` server. Queries are RBAC-filtered by agent domain and role.

**This realizes Vision Goal #4: Queryable PM state from agents.**

---

## Strategic Context (Vision Goals Addressed)

- **Goal #4:** Queryable PM state — Agents query task lists, project milestones, acceptance criteria
- **Goal #2 foundation:** Database-first — PM data sourced from agent.db (M03)
- **Goal #3 foundation:** Two-track support — Different query results for discovery vs. delivery agents

---

## Key Principles

1. **Read-only queries** — No task creation/updates in context-server; only reads
2. **RBAC-filtered** — Agent only sees tasks + projects in their domain
3. **Track-aware** — Discovery agents see discovery-track tasks; Delivery agents see delivery-track tasks
4. **Shallow copies** — Data may be synced from mcp-pm-engine DB to agent.db for read-only queries

---

## Success Criteria

### MCP Tools
- [ ] `get_project_state(project_id)` → {milestone, open_tasks_count, due_date, status}
- [ ] `list_my_team_tasks(domain?, track?)` → {tasks: [{id, title, status, assigned_to}]}
- [ ] `get_task_context(task_id)` → {task + acceptance_criteria + workflow + template}
- [ ] `search_tasks(query, filters?)` → {tasks: Task[]} (max 10, fuzzy search)
- [ ] All tools return standardized error responses (from EPIC-11)

### Data Source
- [ ] PM data stored in agent.db `projects`, `tasks`, `milestones` tables (seeded from mcp-pm-engine)
- [ ] Read latency < 100ms per query
- [ ] Data consistency: synced from mcp-pm-engine on schedule or event-driven

### RBAC Enforcement
- [ ] Domain filtering: agent only sees tasks in their domain
- [ ] Role filtering: admin can see all; regular roles limited
- [ ] Track filtering: discovery agents see discovery-track tasks only
- [ ] Query example: `SELECT * FROM tasks WHERE domain = ? AND track = ? AND assigned_to IN (...)`

### Integration Points
- [ ] Bootstrap (EPIC-10) includes agent's team task preview
- [ ] Middleware (EPIC-11) enforces RBAC on query results
- [ ] Session context (EPIC-11) determines domain + role for filtering

### Testing
- [ ] Unit test: query returns correct tasks for domain
- [ ] Unit test: RBAC filtering removes unauthorized tasks
- [ ] E2E test: `get_project_state()` for active project
- [ ] E2E test: `list_my_team_tasks()` filtered by domain
- [ ] E2E test: `search_tasks()` fuzzy search

---

## Deliverables

| Deliverable | Type | Location |
|:-----------|:-----|:---------|
| PM Query Tools Module | Code | `src/mcp/tools/pmQuery.ts` |
| PM DB Schema | Code | `src/metadata/initAgentDb.ts` (projects, tasks, milestones tables) |
| Query Helpers | Code | `src/metadata/queryPmDb.ts` |
| RBAC Query Builder | Code | `src/mcp/RbacFilter.ts` (extend) |
| Seeder: PM data sync | Code | `src/rag/seedPmData.ts` |
| E2E Test Suite | Tests | `src/tests/e2e/epic-15-pm-query.spec.ts` |
| PM Query Documentation | Docs | `Docs/mcp-context-server/tools/pm-query.md` |
| Implementation Summary | Report | `implementation-summary/EPIC-15-<date>.md` |

---

## Database Schema Additions

```sql
CREATE TABLE projects (
  id TEXT PRIMARY KEY,
  name TEXT,
  domain TEXT,
  description TEXT,
  status TEXT, -- "planning" | "in_progress" | "closed"
  created_at TIMESTAMP
);

CREATE TABLE milestones (
  id TEXT PRIMARY KEY,
  project_id TEXT,
  name TEXT,
  description TEXT,
  status TEXT,
  due_date DATE,
  FOREIGN KEY (project_id) REFERENCES projects(id)
);

CREATE TABLE tasks (
  id TEXT PRIMARY KEY,
  project_id TEXT,
  milestone_id TEXT,
  domain TEXT,
  track TEXT, -- "discovery" | "delivery"
  title TEXT,
  description TEXT,
  acceptance_criteria TEXT, -- Full AC JSON
  status TEXT, -- "open" | "in_progress" | "done"
  assigned_to TEXT,
  created_at TIMESTAMP,
  updated_at TIMESTAMP,
  FOREIGN KEY (project_id) REFERENCES projects(id),
  FOREIGN KEY (milestone_id) REFERENCES milestones(id)
);

CREATE INDEX idx_tasks_domain_track ON tasks(domain, track);
CREATE INDEX idx_tasks_assigned_to ON tasks(assigned_to);
```

---

## Tool API

```typescript
// get_project_state
request: { project_id: string }
response: {
  project_id: string,
  name: string,
  milestone: { name: string, status: string, due_date: string },
  open_tasks_count: number,
  completed_tasks_count: number,
  status: "planning" | "in_progress" | "closed"
}

// list_my_team_tasks
request: { domain?: string, track?: "discovery" | "delivery", status?: string }
response: {
  tasks: [{
    id: string,
    title: string,
    domain: string,
    track: string,
    status: string,
    assigned_to: string,
    due_date: string
  }]
}

// get_task_context
request: { task_id: string }
response: {
  task: {...},
  acceptance_criteria: [{text: string, verified: bool}],
  workflow: {type: string, steps: Step[]},
  template: {category: string, content: string}
}

// search_tasks
request: { query: string, filters?: {domain, track, status}, limit?: 5..20 }
response: {
  tasks: [{...}],
  total_found: number
}
```

---

## Task Breakdown

- **TASK-15-01:** PM DB schema design (`projects`, `tasks`, `milestones`)
- **TASK-15-02:** PM seeder: sync from mcp-pm-engine
- **TASK-15-03:** `get_project_state()` implementation
- **TASK-15-04:** `list_my_team_tasks()` + RBAC filtering
- **TASK-15-05:** `get_task_context()` with workflow + template
- **TASK-15-06:** `search_tasks()` fuzzy search
- **TASK-15-07:** E2E test suite
- **TASK-15-08:** PM query tool documentation

---

## Blocks/Enablers

| Dependency | Status | Impact |
|:-----------|:-------|:--------|
| M02 Complete | 🔄 M02 → M03 | ✅ prerequisite (bootstrap, middleware ready) |
| EPIC-09 (SQLite) | ✅ M02 EPIC-09 | ✅ Base DB ready |
| EPIC-11 (middleware) | ✅ M02 EPIC-11 | ✅ RBAC enforcement available |
| mcp-pm-engine seeder | 🔄 M04 EPIC-19 | ⚠️ MVP: seed manually for M03 demo |

---

## Success Metrics

- ✅ EPIC-15 tasks complete + all tests green
- ✅ `get_project_state()` latency < 50ms
- ✅ `list_my_team_tasks()` latency < 100ms
- ✅ `search_tasks()` latency < 150ms
- ✅ Query results correctly RBAC-filtered 100%
- ✅ Agent only sees domain-relevant tasks
- ✅ M03 demo: agents query PM state, get relevant tasks

---

## Related Documentation

- `Docs/goal.md` § Vision Goal #4 — Queryable PM state
- EPIC-10 goal.md — Bootstrap includes task preview
- EPIC-11 goal.md — Middleware RBAC enforcement
- mcp-pm-engine (M04) — PM Engine schema design
