---
id: epic-15-usage-guide
title: "EPIC-15 PM Query Tools - Usage Guide"
type: guide
epic: EPIC-15
status: active
updated: 2026-03-12
---

## EPIC-15 PM Query Tools - Usage Guide

## Purpose

These tools provide read-only project management context from the MCP context-server layer.
They are intended for agents that need current project/task visibility without direct PM engine access.

## Read-only Contract

All EPIC-15 tools are read-only.

- No task creation
- No status update
- No write-side mutations
- No workflow transition commands

If write operations are needed, use the PM Engine server (out of scope for EPIC-15).

## Tools

### 1) get_project_state

Returns project-level summary state.

Input:

```json
{
  "project_id": "PROJ-1"
}
```

Output shape:

```json
{
  "success": true,
  "data": {
    "project_id": "PROJ-1",
    "milestone": "EPIC-1",
    "open_tasks_count": 3,
    "due_date": "2026-04-01"
  }
}
```

### 2) list_my_team_tasks

Lists tasks filtered by domain/track/status.
For non-admin roles, domain is constrained to the caller domain.

Input:

```json
{
  "track": "delivery",
  "status": "Open",
  "limit": 20
}
```

Output shape:

```json
{
  "success": true,
  "data": {
    "tasks": [
      {
        "id": "TASK-42",
        "title": "Implement PM query tools",
        "status": "Open",
        "assigned_to": "backend_dev",
        "domain": "engineering",
        "track": "delivery"
      }
    ],
    "total": 1,
    "filters": {
      "domain": "engineering",
      "track": "delivery",
      "status": "Open"
    }
  }
}
```

### 3) get_task_context

Returns detailed context for a task.
Includes descriptive context needed for implementation/evaluation.

Input:

```json
{
  "task_id": "TASK-42"
}
```

Output shape:

```json
{
  "success": true,
  "data": {
    "id": "TASK-42",
    "title": "Implement PM query tools",
    "status": "Open",
    "assigned_to": "backend_dev",
    "domain": "engineering",
    "track": "delivery",
    "description": "...",
    "acceptance_criteria": "...",
    "workflow": "delivery_workflow",
    "template": "implementation_summary"
  }
}
```

### 4) search_tasks

Fuzzy task search by query + optional filters.
Default result cap is 10 unless `limit` is provided.

Input:

```json
{
  "query": "cache",
  "filters": {
    "status": "Open",
    "track": "delivery"
  },
  "limit": 10
}
```

Output shape:

```json
{
  "success": true,
  "data": {
    "tasks": [],
    "total": 0,
    "query": "cache",
    "filters": {
      "domain": "engineering",
      "track": "delivery",
      "status": "Open"
    }
  }
}
```

## RBAC and Domain Scoping

- Non-admin roles: restricted to own domain
- Admin/architect roles: cross-domain queries allowed
- Cross-domain access violations return forbidden responses

## Error Conventions

The tools return standardized error payloads via `ErrorResponses`.
Common cases:

- `notFound`: missing project/task or PM schema unavailable
- `forbidden`: cross-domain access for non-admin role
- validation errors for malformed input payloads

## Validation Checklist

- Tool methods exist in `src/mcp/tools/pm-query.ts`
- DB read models exist in `src/mcp/AgentDb.ts`
- Router wiring exists in `src/mcp/mcpServer.ts`
- Unit tests:
  - `src/tests/unit/pm-query-plugin.test.ts`
  - `src/tests/unit/AgentDb.pm-query.test.ts`
- Integration test:
  - `src/tests/integration/pm-query-tools.integration.test.ts`
