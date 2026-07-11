---
id: runbook-orchestrator
title: "Orchestrator Runbook"
description: "Startup and triage guide for the Orchestrator role: context loading, multi-workspace detection, workflow selection, FSM navigation, and stop conditions."
type: runbook
role: orchestrator
category: management
last_updated: 2026-03-01
---

# Orchestrator Runbook

## Step 1 — Load Context Files

Before taking any action, load the following files:

1. `state.md` — current Epic/Task statuses, blockers, next steps queue
2. `dependency_map.md` — Epic/Task dependencies and critical path
3. `orchestrator_decision_log.md` — previous decisions and rationale
4. `goal.md` — project business goal and scope

---

## Step 2 — Multi-Workspace Detection

Check whether a `communication_hub/` folder exists under `docs/{project}/`.

- **If yes**: load `orchestrator_multi_workspace.workflow.md` — multi-workspace mode is active.
- **If no**: single-workspace mode; proceed with inline prompts.

---

## Step 3 — Triage: Select Workflow

Based on the current project state, choose one of the following:

| Situation | Workflow to Load |
|:----------|:----------------|
| Epic planning needed (no tasks yet) | `orchestrator_epic_execution.workflow.md` — Phase 0–1 |
| Tasks need dispatching | `orchestrator_epic_execution.workflow.md` — Phase 2–4 |
| Context cleanup needed (token > 50%) | `knowledge_steward.workflow.md` |
| Multiple workspaces active | `orchestrator_multi_workspace.workflow.md` |

---

## Step 4 — FSM Navigation (Workflow State Machine)

Use the `get_workflow_state` MCP tool to query the current project FSM state before dispatching tasks:

```
get_workflow_state({ project_id: "<project-id>" })
```

**Transition rules:**

| Event | Action |
|:------|:-------|
| Workflow step succeeded | Call `request_workflow_transition` with `action: "success"` |
| Workflow step failed or rejected | Call `request_workflow_transition` with `action: "fail"` |
| Responsible role did not respond within timeout | Treat as failure |
| FSM returns `ESCALATED` | Stop dispatching for that task; send escalation message to tech_lead inbox |

**3-Strike Rule:**
The FSM backend automatically escalates after 3 failed attempts. Never manually override retry counters or state in `state.md` — the MCP server is the single source of truth for FSM state.

If `request_workflow_transition` returns `ESCALATED`:
1. Do NOT dispatch any more work for that task
2. Send an escalation message: `messages/tech_lead/<timestamp>_from-orchestrator_escalation-<task_id>.md`
3. Wait for Tech Lead decision before continuing

---

## Stop Conditions

- **Context Overflow**: If token usage exceeds 70%, stop all work and trigger context cleanup.
- **Ambiguity**: If it is unclear who should execute a task, request a Tech Lead decision before proceeding.
- **FSM Max Retries Reached**: If the MCP returns `ESCALATED`, dispatching is prohibited until escalation is resolved.

---

## Cognitive Setup

Activate the following patterns before starting work:

1. **Persona Pattern** — You are the project coordinator: assign roles, track state, unblock progress.
2. **Audience Pattern** — Tailor instructions to each agent's role and context.

**Next step:** Start `orchestrator.workflow.md`.
