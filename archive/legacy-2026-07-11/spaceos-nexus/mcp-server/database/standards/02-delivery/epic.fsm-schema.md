---
id: fsm-schema-v1
title: "FSM Schema Standard — Finite State Machine Workflow Convention"
description: "Defines the mandatory Finite State Machine schema that every agent workflow must follow, eliminating Infinite Ping-Pong and 3-Strike Blindness problems through deterministic state transitions and automatic escalation."
type: standard
scope: global
category: workflow-governance
version: 1.0
created: 2026-02-23
last_updated: 2026-02-23
---

# FSM Schema Standard — Finite State Machine Workflow Convention

> **Goal:** Eliminate "Infinite Ping-Pong" and "3-Strike Blindness" problems in workflows. Every workflow must terminate deterministically — either with a successful close or human escalation.

---

## 1. Why FSM?

The previous markdown-based, free-text workflow files allowed agents to decide the next step according to their own logic. This caused two typical problems:

| Problem | Description |
|:---------|:-------|
| **Infinite Ping-Pong** | A → B → A → B loop, where two agents keep handing the task back to each other |
| **3-Strike Blindness** | The same code fails QA 3 times in a row, but the orchestrator does not escalate |
| **Context Drift** | Persona switch (e.g. QA → Devil's Advocate) loses the original state |

The FSM (Finite State Machine) solution:

- Every state has an explicit name and pre-defined accepted transitions.
- When `max_retries` is reached, the task automatically moves to `ESCALATED` state.
- The currently active state is tracked in `state.md`.

---

## 2. FSM State Definition Format

### YAML Template

```yaml
# FSM State Block — embeddable in Workflow .md files
fsm:
  schema_version: "1.0"
  workflow_id: "<workflow_unique_id>"
  initial_state: "<state_name>"
  states:
    <STATE_NAME>:
      label: "<human-readable description>"
      role: "<responsible role>"
      inputs:
        - "<required input data / artifact>"
      outputs:
        - "<expected output data / artifact>"
      timeout_hours: <number>          # how long before escalation
      max_retries: <number>            # how many times this state can be re-entered
      on_success: "<target state>"
      on_fail: "<target state>"
      on_timeout: "<target state>"     # optional, default: ESCALATED
      on_max_retries: "ESCALATED"      # always ESCALATED — not configurable
    ESCALATED:
      label: "Human intervention required"
      role: tech_lead
      inputs:
        - "escalation_report.md (what went wrong, how many times)"
      outputs:
        - "decision: retry, redesign, or close-as-wontfix"
      timeout_hours: 48
      max_retries: 1
      on_success: "<return state>"
      on_fail: "CLOSED_BLOCKED"
    CLOSED_DONE:
      label: "Successfully closed"
      role: orchestrator
      terminal: true
    CLOSED_BLOCKED:
      label: "Closed without resolution — documented"
      role: orchestrator
      terminal: true
```

### Required Fields

| Field | Required | Description |
|:-----|:--------:|:-------|
| `workflow_id` | ✅ | Unique identifier (e.g. `agile-epic-lifecycle-v1`) |
| `initial_state` | ✅ | Which state the workflow starts from |
| `states.<name>.label` | ✅ | Human-readable description |
| `states.<name>.role` | ✅ | Which agent role is responsible for this state |
| `states.<name>.on_success` | ✅ | Next state on successful completion |
| `states.<name>.on_fail` | ✅ | Next state on failure |
| `states.<name>.max_retries` | ✅ | Max times this state can be re-entered (3-Strike rule) |
| `terminal: true` | ✅ | CLOSED_DONE and CLOSED_BLOCKED must be terminal states |

---

## 3. Standard State Names (reserved)

The following state names are globally reserved — every workflow must use the same semantics:

| State name | Semantics |
|:-----------|:-----------|
| `BACKLOG_READY` | Task planned, all prerequisites met, ready for execution |
| `IN_DEV` | Active development in progress |
| `CODE_REVIEW` | Pull Request open, review in progress |
| `QA_WAITING` | Handed off to QA, but not yet assigned |
| `QA_IN_PROGRESS` | QA actively testing |
| `ARCHITECT_SIGNOFF` | Waiting for Architect or Tech Lead approval (for sensitive / cross-cutting changes) |
| `IN_REVIEW` | A review role is actively evaluating |
| `WAITING_FOR_INPUT` | Waiting for an external decision / human confirmation |
| `ESCALATED` | Human (tech_lead/po) intervention required |
| `CLOSED_DONE` | Successfully closed, all acceptance criteria satisfied |
| `CLOSED_BLOCKED` | Closed without resolution — documented blocker |

> **Note:** `ARCHITECT_SIGNOFF` and `BACKLOG_READY` are not mandatory in every workflow — only include them when the process requires it. `ESCALATED`, `CLOSED_DONE`, and `CLOSED_BLOCKED` are mandatory in every workflow.

---

## 4. Placing the FSM Block in Workflow Files

The FSM block must be placed at the end of every workflow `.md` file, separated from the descriptive text:

```markdown
---

## FSM Definition

```yaml
fsm:
  schema_version: "1.0"
  workflow_id: "..."
  ...
```

```

---

## 5. Retry Rules — The "3-Strike Rule"

```

First failure (retry_count: 1):
  → falls back to the on_fail target state
  → Orchestrator notifies the responsible role of the failure

Second failure (retry_count: 2):
  → falls back to the on_fail target state
  → Orchestrator sends a WARNING message to tech_lead

Third failure (retry_count: 3 = max_retries):
  → AUTOMATICALLY → moves to ESCALATED state
  → tech_lead intervention mandatory
  → Orchestrator STOPS dispatch for this task

```

The default value of `max_retries` (if not specified): **3**.

---

## 6. Tracking in state.md

`state.md` files must include the active FSM state:

```yaml
# state.md front-matter addition
fsm_state: "QA_IN_PROGRESS"      # current state
fsm_retry_count: 1               # how many times it has been returned
fsm_workflow_id: "agile-epic-lifecycle-v1"
```

---

## 7. Example — Agile Epic Lifecycle FSM

```yaml
fsm:
  schema_version: "1.0"
  workflow_id: "agile-epic-lifecycle-v1"
  initial_state: IN_DEV
  states:
    IN_DEV:
      label: "Active development"
      role: backend_developer
      inputs: ["task_assignment.md"]
      outputs: ["implementation + unit tests"]
      timeout_hours: 72
      max_retries: 3
      on_success: CODE_REVIEW
      on_fail: IN_DEV
    CODE_REVIEW:
      label: "Pull Request review"
      role: tech_lead
      inputs: ["pull_request_url"]
      outputs: ["review_decision: approved | changes_requested"]
      timeout_hours: 24
      max_retries: 3
      on_success: QA_WAITING
      on_fail: IN_DEV
    QA_WAITING:
      label: "Waiting for QA"
      role: orchestrator
      inputs: ["implementation + unit tests (approved PR)"]
      outputs: ["qa_tester assignment"]
      timeout_hours: 8
      max_retries: 1
      on_success: QA_IN_PROGRESS
      on_fail: ESCALATED
    QA_IN_PROGRESS:
      label: "QA testing in progress"
      role: qa_tester
      inputs: ["test_plan.md"]
      outputs: ["qa_report.md (pass | fail)"]
      timeout_hours: 48
      max_retries: 3
      on_success: CLOSED_DONE
      on_fail: IN_DEV
    ESCALATED:
      label: "Human intervention required"
      role: tech_lead
      inputs: ["escalation_report.md"]
      outputs: ["decision: retry | redesign | close-as-wontfix"]
      timeout_hours: 48
      max_retries: 1
      on_success: IN_DEV
      on_fail: CLOSED_BLOCKED
    CLOSED_DONE:
      label: "Epic successfully closed"
      role: orchestrator
      terminal: true
    CLOSED_BLOCKED:
      label: "Epic closed without resolution"
      role: orchestrator
      terminal: true
```

---

## 8. Related Documents

- `orchestrator.runbook.md` — FSM navigation logic for the Orchestrator
- `standards/core/runbook.md` — Global reference list
- `roles/management/orchestrator/workflows/` — Workflow files extended with FSM
