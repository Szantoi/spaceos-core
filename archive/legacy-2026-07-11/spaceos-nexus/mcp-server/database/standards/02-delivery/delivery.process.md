---
title: "Delivery Process Standard"
description: "Defines the 7-phase Epic lifecycle, role-based FSM state machine, messaging protocol, and dispatch playbook that governs all operational work in JoineryTech.Flow."
type: reference_guide
scope: global
date: 2026-02-24
last_updated: 2026-03-01
---

# Delivery Process Standard

This guide defines how the JoineryTech.Flow system operates in its execution phase — where validated concepts from `Plans/` meet implementation reality in the project root (`docs/<project>/`).

The system is built on a **role-based, asynchronous, multi-agent architecture** governed by strict finite state machines (FSM) and communication standards.

---

## Roles and the Underlying Architecture

Operational work is performed by distinct **roles**. A role is a logical "hat" — one or more agents may operate under it. Roles are defined in `src/agent-system/database/roles/`.

Every role folder follows the same logical structure for maximum modularity:

```text
src/agent-system/database/roles/<role_name>/
├── <role_name>.role.md       ← Role definition (Persona, responsibilities, goals)
├── <role_name>.runbook.md    ← Operating manual (which workflow to use in which context)
├── agents/                   ← Specific agent personalities filling this role
├── instructions/             ← Specific instructions for agents
├── skills/                   ← Skills (tools, scripts, knowledge bases for the work)
├── workflows/                ← Concrete, step-by-step executable processes (FSM or sequential)
├── templates/                ← Document templates produced by this role
└── messages/                 ← Message TEMPLATES (*.message.md — not actual dispatched messages!)
```

> **Important distinction:** The `src/agent-system/database/roles/<role>/messages/` folder contains **message templates** (`.message.md` files) describing the role's typical communication formats. **Actual dispatched messages** go to the project-root `messages/<role>/` folder (see Messaging v2.0 section).

### Role Grouping (Domain-Driven Structure)

Roles are separated in the filesystem by their **operational domain**, avoiding overlap:

#### 1. `management/` — Administration and System Direction

Operational and organisational roles above research and coding.

- **Orchestrator:** Does not write code. Responsible for context hygiene, `state.md` synchronisation, task dispatch, and thread management. Navigates the project through phases.
- **Knowledge Steward:** Responsible for structural maintenance, knowledge base updates, filesystem consistency, and archival.

#### 2. `discovery/` — Research and Design

Roles optimised for research, conceptualisation, and validation (Discovery Framework focus).

- **Architect:** System-level design, ADR authoring and approval.
- **Product Owner:** Strategic direction, backlog management, MVP definition.
  - **Discovery → Epic Execution Transition (Handoff Approval):** Exclusive owner. Decides whether to launch production development based on research results.
  - **Discovery Conclusion Notification:** When a Discovery phase (Integrator) or Fast Track ends, the PO must be notified of results and proposed next steps.
- **Tech Lead:** Senior engineer primarily handling reviews, planning, and blocker removal.
- **Universal Discovery Roles:** Targeted conceptual agents (Explorer, Framer, Experimenter, Integrator).

#### 3. `engineering/` — Implementation and Coding

Roles optimised for programming and implementation tasks (traditional SDLC focus).

- **Frontend Developer, Backend Developer, UI/UX Designer:** Implement the "contracts" defined in `tasks/`.
- **QA Tester:** Functional and integration (software) testing of delivered code.

#### 4. `agentops/` — Agent Operations

Roles optimised for LLM agent testing, tuning, and boundary stress-testing (Red Teaming).

- **Devils Advocate:** Critical analyser of plans and code ("red team").

---

## The Execution Engine: Runbooks and Workflows

Agents never "figure out" what to do. Every step is guided by documents:

- **Runbook (`<role>.runbook.md`):** The entry point. The Runbook tells the agent — based on context — which process to use. It defines what to read (triage), where the project stands (`state.md`), and which workflow to launch under which conditions.
- **Workflow (`workflows/`):** The concrete execution plan. For example, a code-review process or implementing a new API endpoint. Contains steps and automation hooks.

---

## FSM State Machine Navigation

Epic and Task lifecycle is governed by a strict FSM recorded in `state.md` (`fsm_state: "IN_DEV"`).

> **Canonical source for all state definitions, transitions, retry rules, and YAML schema:**
> → `docs/standards/02-delivery/epic.fsm-schema.md`

The Orchestrator advances state only when the current phase's Definition of Done is satisfied. On `max_retries` exceeded, the state automatically transitions to `ESCALATED`.

---

## Agent Communication and Handoff

Roles communicate via date-sharded atomic message files (one file per message, never append-to-shared-inbox).

> **Canonical source for message format, folder conventions, and protocol rules:**
> → `docs/standards/03-agent-system/agent.messaging.protocol.md`

Key distinction: `src/agent-system/database/roles/<role>/messages/` contains **templates**; `messages/<role>/` at project root contains **actual dispatched messages**.

> **Prohibited:** Chat-style continuous editing of shared files (`_inbox.md` append pattern is deprecated). Every handoff is a new, atomic `.md` message file. Detailed rules: `core/messaging_v2_standard.md`.

---

## Documentation and Implementation Evidence

In the operational phase, all completed work must be physically evidenced.

A Task cannot be officially considered closed — regardless of its status label — until the executing agent has produced an **Implementation Summary**.

**Process:**

1. The developer interprets the `tasks/TASK-XX-XX.md` contract.
2. The developer writes the code and applies changes.
3. The developer creates `docs/{project}/epics/{EPIC}/implementation-summary/{TASK_ID}-<slug>.md` using the role's `templates/` template.
4. The developer sends a message to the Orchestrator indicating task completion. The Orchestrator then updates `state.md` and advances the FSM.

---

## The Operational Workflow in Detail (Epic Lifecycle)

The atom of the operational phase is the **Epic**. Every Epic passes through 7 mandatory phases orchestrated by the Orchestrator. Phase transitions are recorded in `state.md`, and the Orchestrator advances only when the current phase's **Definition of Done** is satisfied.

**Trigger:** The process starts based on the Product Owner's strategic direction (backlog) and **explicit phase-transition approval (Discovery → Execution)**, with the Orchestrator's Pre-flight check.

### Process Overview

```
BACKLOG_READY
     │
     ▼  Orchestrator: git branch, context loading
 ┌─────────────────────────────────────────┐
 │  PHASE 0: Pre-flight Check              │
 │  Role: Orchestrator                     │
 │  Output: —                              │
 └────────────────┬────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────┐
 │  PHASE 1: Epic Planning                 │
 │  Role: Architect                        │
 │  FSM: → "Planning"                      │
 │  Output: epics/{EPIC}/plan.md           │
 │          decisions/ADR-XXX.md           │
 └────────────────┬────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────┐
 │  PHASE 2: Task Planning                 │
 │  Role: Tech Lead                        │
 │  FSM: → "In Progress"                   │
 │  Output: epics/{EPIC}/tasks/TASK-XX.md  │
 │          epics/{EPIC}/backlog.md        │
 └────────────────┬────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────────────────────┐
 │  PHASE 3: Implementation (per-Task loop)                │
 │  Role: Backend Developer / Frontend Developer           │
 │  FSM: Todo → IN_DEV → Ready for QA                     │
 │  Output: epics/{EPIC}/implementation-summary/           │
 │          TASK-XXX-{slug}.md                             │
 └────────────────┬────────────────────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────────────────────┐
 │  PHASE 4: QA Testing (per-Task loop)                    │
 │  Role: QA Tester                                        │
 │  FSM: QA_WAITING → QA_IN_PROGRESS → Done / Bug Fix     │
 │  Output: epics/{EPIC}/qa/TASK-XXX-{qa-comment}.md      │
 │  If REJECTED: → Bug Fix → return to Phase 3             │
 └────────────────┬────────────────────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────┐
 │  PHASE 5: Epic Review                   │
 │  Role: Tech Lead                        │
 │  FSM: → "Review"                        │
 │  Output: epics/{EPIC}/epic_review.md    │
 └────────────────┬────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────┐
 │  PHASE 6: Architect Sign-off            │
 │  Role: Architect                        │
 │  FSM: ARCHITECT_SIGNOFF                 │
 │  Output: epics/{EPIC}/architect_        │
 │          signoff.md                     │
 │  If REJECTED: → return to Planning      │
 └────────────────┬────────────────────────┘
                  ▼
 ┌─────────────────────────────────────────┐
 │  PHASE 7: Epic Closure                  │
 │  Role: Knowledge Steward + Prod. Owner  │
 │  FSM: → CLOSED_DONE                     │
 │  Output: archived/{EPIC}/              │
 │          product_backlog updated        │
 └─────────────────────────────────────────┘
```

---

### Phases in Detail

#### Phase 0 — Pre-flight Check (Orchestrator)

| Item | Detail |
|:-----|:-------|
| **Trigger** | User Epic execution request |
| **Responsible** | Orchestrator |
| **Workflow** | `orchestrator_epic_execution.workflow.md` |
| **Checks** | `goal.md` exists · `state.md` up to date · `dependency_map.md` consistent · No critical blocker |
| **Git** | `git checkout -b epic/{EPIC_ID}-{name}` branch creation |
| **Deployment decision** | Single-Workspace vs. Multi-Workspace (based on scope size) |
| **If missing** | PO / Stakeholder involvement — project cannot start without `goal.md` |

#### Phase 1 — Epic Planning (Architect)

| Item | Detail |
|:-----|:-------|
| **Trigger** | Orchestrator dispatch (`architect_epic_review.message.md`) |
| **Responsible** | Architect |
| **Workflow** | `architect.workflow.md` |
| **Input** | `goal.md`, `dependency_map.md`, existing ADRs |
| **Output** | `epics/{EPIC}/plan.md` · `decisions/ADR-XXX.md` |
| **DoD** | Min. 2 alternatives evaluated · ADR for critical decisions · Dependencies documented |
| **FSM** | state.md: Epic → `"Planning"` |
| **If missing** | Returned to Architect |

#### Phase 2 — Task Planning (Tech Lead)

| Item | Detail |
|:-----|:-------|
| **Trigger** | Orchestrator dispatch (`tech_lead_epic_planning.message.md`) |
| **Responsible** | Tech Lead |
| **Workflow** | `tech_lead.workflow.md` |
| **Input** | `epics/{EPIC}/plan.md` |
| **Output** | `epics/{EPIC}/tasks/TASK-XX.md` · `epics/{EPIC}/backlog.md` |
| **DoD** | Every task has role + skills + QA requirement · Dependencies defined · No circular dependency |
| **Optional** | Devils Advocate review (for high-risk Epics) |
| **FSM** | state.md: Epic → `"In Progress"`, all Tasks → `"Todo"` |

#### Phase 3 — Implementation Loop (Developer)

| Item | Detail |
|:-----|:-------|
| **Trigger** | Orchestrator per-Task dispatch (backend or frontend implementation message) |
| **Responsible** | Backend Developer or Frontend Developer (task-specific) |
| **Workflow** | `backend_developer.workflow.md` / `frontend_developer.workflow.md` |
| **Input** | `tasks/TASK-XX.md` (contract) |
| **Output** | `epics/{EPIC}/implementation-summary/TASK-XXX-{slug}.md` |
| **DoD** | Implementation Summary exists · Build green · Unit tests written |
| **FSM** | Task: `Todo` → `IN_DEV` → `Ready for QA` |
| **Loop** | Repeated for every `"Todo"` Task in dependency order |

#### Phase 4 — QA Testing Loop (QA Tester)

| Item | Detail |
|:-----|:-------|
| **Trigger** | Orchestrator per-Task dispatch (`qa_tester_testing.message.md`) |
| **Responsible** | QA Tester |
| **Workflow** | `qa_tester.workflow.md` |
| **Input** | `tasks/TASK-XX.md` + `implementation-summary/TASK-XXX-*.md` |
| **Output** | `epics/{EPIC}/qa/TASK-XXX-{qa-comment}.md` |
| **DoD** | QA Sign-off APPROVED · All tests green |
| **If REJECTED** | Blocker Register updated → Developer re-dispatch (bug fix) → back to QA |
| **3-Strike Rule** | 3× REJECTED → `ESCALATED` → Tech Lead intervention |
| **FSM** | Task: `QA_WAITING` → `QA_IN_PROGRESS` → `Done` (APPROVED) or `Blocked` (REJECTED) |

#### Phase 5 — Epic Review (Tech Lead)

| Item | Detail |
|:-----|:-------|
| **Trigger** | All Tasks `"Done"` — Orchestrator dispatch |
| **Responsible** | Tech Lead |
| **Workflow** | `tech_lead_closure.workflow.md` |
| **Input** | All `tasks/*.md` + `implementation-summary/*.md` + `qa/*.md` |
| **Output** | `epics/{EPIC}/epic_review.md` |
| **Content** | Task summary · Technical challenges · Lessons Learned · Calibration Instructions (global skill/template update proposals) · Metrics |
| **FSM** | Epic: `"In Progress"` → `"Review"` |

#### Phase 6 — Architect Sign-off (Architect)

| Item | Detail |
|:-----|:-------|
| **Trigger** | `epic_review.md` complete — Orchestrator dispatch |
| **Responsible** | Architect |
| **Workflow** | `architect_closure.workflow.md` |
| **Input** | `epic_review.md` + source code + `plan.md` |
| **Output** | `epics/{EPIC}/architect_signoff.md` |
| **Decision** | **APPROVED** → Phase 7 · **CONDITIONAL** → Minor fix → re-sign-off · **REJECTED** → Epic back to `"Planning"` |
| **Checks** | Clean Architecture layers · DDD principles · ADR compliance · Tech Debt level |
| **FSM** | Epic: `"Review"` → `"Done"` (APPROVED) or `"Planning"` (REJECTED) |

#### Phase 7 — Epic Closure (Knowledge Steward + Product Owner)

| Item | Detail |
|:-----|:-------|
| **Trigger** | Architect Sign-off APPROVED |
| **Responsible 7.1** | Knowledge Steward |
| **Workflow** | `knowledge_steward_calibration.workflow.md` |
| **Output 7.1** | `archived/{EPIC}/` (full Epic copy) · Global skill/template/standards updates · `knowledge_map.md` update |
| **Responsible 7.2** | Product Owner |
| **Workflow** | `product_owner.workflow.md` Section A (Epic Closure Review) |
| **Output 7.2** | Business evaluation · Goal alignment check · Next Epic proposals · `product_backlog.md` update |
| **FSM** | Epic: `CLOSED_DONE` |

---

## Document Artifact Map

During the Epic lifecycle the following documents are created (in the **Epic root directory** at `docs/{project}/epics/{EPIC}/` or `docs/{project}/milestones/{M}/epics/{EPIC}/`):

```text
epics/{EPIC_ID}/
├── plan.md                          ← Phase 1: Architect (epic_plan.template.md)
├── backlog.md                       ← Phase 2: Tech Lead
├── tasks/
│   ├── TASK-01.md                   ← Phase 2: Tech Lead (task.template.md)
│   ├── TASK-02.md
│   └── ...
├── implementation-summary/
│   ├── TASK-01-{slug}.md            ← Phase 3: Developer (implementation_report.template.md)
│   └── ...
├── qa/
│   ├── TASK-01-{qa-comment}.md      ← Phase 4: QA Tester (qa_signoff.template.md)
│   └── ...
├── epic_review.md                   ← Phase 5: Tech Lead (epic_review.template.md)
└── architect_signoff.md             ← Phase 6: Architect (architect_signoff.template.md)
```

---

## Prompt Template — Role Dispatch Table

Message templates used in each phase (full Playbook reference: `core/workflow_diagram.md`):

| Phase | Sender | Target Role | Template | ID |
|:------|:-------|:------------|:---------|:---|
| 1 — Epic Planning | Orchestrator | Architect | `orchestrator/messages/architect_epic_review.message.md` | P5 |
| 2 — Task Planning | Orchestrator | Tech Lead | `orchestrator/messages/tech_lead_epic_planning.message.md` | P1 |
| 3 — Backend impl. | Orchestrator | Backend Developer | `orchestrator/messages/backend_developer_task_implementation.message.md` | P2 |
| 3 — Frontend impl. | Orchestrator | Frontend Developer | `orchestrator/messages/frontend_developer_task_implementation.message.md` | P3 |
| 4 — QA Testing | Orchestrator | QA Tester | `orchestrator/messages/qa_tester_testing.message.md` | P4 |
| 4 — Bug fix (BE) | QA Tester | Backend Developer | `qa_tester/messages/backend_developer_bug_fix.message.md` | P13 |
| 4 — Bug fix (FE) | QA Tester | Frontend Developer | `qa_tester/messages/frontend_developer_bug_fix.message.md` | P14 |
| 4 — QA Signoff | QA Tester | Tech Lead | `qa_tester/messages/tech_lead_qa_signoff_handoff.message.md` | P15 |
| 5 — Epic Review | Orchestrator | Tech Lead | *(manual Mega-Prompt)* | — |
| 6 — Sign-off | Orchestrator | Architect | `orchestrator/messages/architect_epic_review.message.md` | P5 |
| 7.1 — Closure | Orchestrator | Knowledge Steward | *(manual Mega-Prompt)* | — |
| 7.2 — PO Review | Orchestrator | Product Owner | *(auto-trigger Section A)* | — |

---

## Summary Map

```text
       [Product Owner]
              │
      (Backlog, Goals)
              ▼
       [Orchestrator] ────────────────┐
        (Dispatching)                 │ (FSM State, Retries)
              │                       │
              ▼                       ▼
    [Planners / Tech Lead] ◄────► [state.md]
        (Planning, Fix)               ▲
              │                       │
              ▼                       │
   [Executors (Dev, UI/UX)] ──────────┘
    (Coding, Impl. Summary)           │
              │                       │
              ▼                       │
       [Quality (QA)] ────────────────┘
   (Testing, Approval)
              │
              ▼
      [Knowledge Steward]
(Archival, System Maintenance)
```

**Every agent moves based on these principles:**

1. Receives the signal (Inbox Message).
2. Checks its **Runbook** to determine what to do.
3. Reads the context (`state.md`, `instructions/`, `skills/`).
4. Executes the work based on the **Workflow**.
5. Documents the result using **Templates** (Implementation Summary).
6. Forwards control (sends a **Message** to the next role or the Orchestrator).
