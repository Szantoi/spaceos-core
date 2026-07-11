---
title: "Project Folder Structure Standard"
description: "Defines the Epic-First hierarchy for structuring and tracking development projects. Program → Project → Milestone → Epic → Task."
description: "Defines the Epic-First hierarchy for structuring and tracking development projects in JoineryTech.Flow, covering Program, Project, Milestone, Epic, and Task levels."
type: reference_guide
scope: global
date: 2026-02-23
last_updated: 2026-03-01
---

# Project Folder Structure Standard

This guide defines how to structure and track a development project in JoineryTech.Flow. The complete hierarchy:

**Epic-First Archetype (Atom) → [Project / Milestone / Program] (Containers)**

In JoineryTech.Flow, the **Epic** is the atomic unit. Every other level (Project, Milestone, Program) is simply a container — used only when complexity justifies it.

---

## When to Use Which Level?

| Level | When justified (Trigger) | Example |
|:------|:------------------------|:--------|
| **Epic** (Atom) | **Always.** This is the fundamental execution unit. | `epic-01: Auth` |
| **Milestone** | If >6 Epics exist, OR a production shipping phase (e.g. v1.0, v2.0). | `milestone-01: MVP` |
| **Sub-Milestone** | If a Milestone contains >6 Epics, or parallel tracks run (e.g. UI vs API). | `milestone-01a: API` |
| **Project** | If a new business goal (`goal.md`) and separate tracking (`state.md`) are needed, OR tasks require parallel independent agents. | `docs/joinerytech-flow/agent-system-v2/` |
| **Program** | If 2+ projects need a shared `program-state.md` dashboard. | `docs/joinery-cloud/` |

> **Lean Rule:** If the task is simple, do not create a Milestone folder! An Epic folder can live directly under `docs/<project>/epics/`.

---

## Program Level: Grouping Multiple Projects

A Program level is created when **two or more project folders** belong to the same development wave and require a shared status dashboard.

```text
docs/
├── agent-system-v2/               ← Program folder
│   ├── _program.md                ← Program Landing Page
│   └── program-state.md           ← Cross-project aggregated dashboard
├── core-v2-upgrade/               ← Sub-Project A (full internal structure)
└── role-protocol/                 ← Sub-Project B (full internal structure)
```

### `_program.md` (Program Landing Page)

- What is this Program? Why do these projects belong together?
- Which projects (`docs/<project>/`) are included?
- List of shared dependencies and blockers

### `program-state.md` (Cross-project Dashboard)

Aggregated status table showing each sub-project's current milestone and status:

| Sub-Project | Active Milestone | Status | Responsible |
|:------------|:----------------|:-------|:------------|
| `core-v2-upgrade` | M01: State Integrity | In Progress | Tech Lead |
| `role-protocol` | M02: Role Refinement | Planned | Architect |

---

## Project Level: The Independent Scope Level

Every independent project lives under `docs/<project-name>/`.

**When to split a Program into multiple Projects?**

1. **Parallelisability:** If two agents must work simultaneously on completely different parts of the codebase without blocking each other (separate `state.md`).
2. **Independent success criteria:** If the success of "A" is not a technical prerequisite for "B" (separate `goal.md`).
3. **Different domains:** E.g. a technical refactoring (`core-v2`) and a new feature (`role-protocol`) belong to the same program but are logically separate.

```text
docs/my-awesome-feature/
├── _readme.md      # Project "Landing Page" overview
├── goal.md         # The "Why" (Objective, Scope, Success Criteria)
├── state.md        # Global Dashboard (Milestone/Epic map)
├── epics/          # DIRECT EPICS (Lean model)
│   ├── epic-01/
│   └── epic-02/
└── milestones/     # SEPARATED PHASES (Enterprise model)
    ├── milestone-01/
    └── milestone-02/
```

### `_readme.md` (Landing Page)

Short summary naming the project, listing key files, describing basic rules, and referencing research/plan documents.

### `goal.md` (Goal Definition)

Describes the business and technical objectives. Defines project scope (**In Scope** / **Out of Scope**) and lists measurable **Success Criteria**.

### `state.md` (Global Dashboard)

The most important reference point for the Orchestrator and Tech Lead. Must contain:

- `Project Overview` table (Total Milestones, Total Epics, Active Epic).
- `Milestone Map` table.
- `Epic State Map` table (ID, Title, State, Responsible).

If this project belongs to a Program, `state.md` includes the Program reference:

```yaml
program: agent-system-v2
program_state: docs/joinerytech-flow/agent-system-v2/program-state.md
```

---

## Milestone Level: Physical Separation

The project lifecycle is divided into Milestones. A Milestone ends when all its Epics are complete and the system has received a testable increment.

```text
milestones/
├── milestone_01/
│   ├── plan.md              # What is this Milestone? What must be achieved?
│   ├── epic_01/
│   └── epic_02/
├── milestone_02/
│   └── ...
```

### `milestone_X/plan.md`

Defines the precise goal, focus areas, and "done" condition for the Milestone. Lists the Epics under it.

### Sub-Milestone (Optional)

If a Milestone is too complex, it can be broken into Sub-Milestones. Notation: `milestone_01a`, `milestone_01b`.

```text
milestone_01/
├── plan.md                      ← Main Milestone plan (lists sub-milestones)
├── sub_milestone_01a/           ← Sub-Milestone A
│   ├── sub-plan.md              ← What does this phase solve?
│   ├── epic_01/
│   └── epic_02/
└── sub_milestone_01b/           ← Sub-Milestone B (can be delivered in parallel)
    ├── sub-plan.md
    └── epic_03/
```

> Sub-Milestones are only justified if phases **can be delivered independently**, or the Milestone's Epic count exceeds 6.

---

## Epic Level: The Logical Module Level

At this level the project is broken into sub-units. Every Epic has its own folder and `state.md`.

```text
milestone_01/
├── epic_01/
│   ├── state.md                      # Epic dashboard (Task list!)
│   ├── tasks/                        # Task files
│   │   ├── TASK-01-01.md
│   │   ├── TASK-01-02.md
│   │   └── ...
│   └── implementation-summary/       # Implementation summaries (REQUIRED)
│       ├── TASK-01-01-<slug>.md
│       ├── TASK-01-02-<slug>.md
│       └── ...
```

### `epic_X/state.md`

Describes the internal structure of the Epic:

1. **Objective:** What does this module add to the system?
2. **Current tasks:** Markdown table of tasks.
3. **FSM Tracking (v1.0, optional):** If the Epic follows an active FSM workflow, the front-matter includes:

```yaml
# FSM tracking fields — per fsm_schema.md (v1.0)
fsm_workflow_id: "agile-epic-lifecycle-v1"  # which FSM definition is active
fsm_state: "QA_IN_PROGRESS"                 # currently active state
fsm_retry_count: 1                          # how many times the current state has been retried
```

**Available FSM state values:** `BACKLOG_READY`, `IN_DEV`, `CODE_REVIEW`, `QA_WAITING`, `QA_IN_PROGRESS`, `ARCHITECT_SIGNOFF`, `WAITING_FOR_INPUT`, `ESCALATED`, `CLOSED_DONE`, `CLOSED_BLOCKED`

> When `fsm_retry_count` reaches the `max_retries` value for the current state, the Orchestrator **must set the Epic to `ESCALATED`** and halt dispatch.

Example task table:

| Type | ID | Task | Status | Description |
|:----:|:---|:-----|:-------|:------------|
| `Logic` | T1-01 | Create login logic | Planned | Main backend login implementation. |

If the Epic depends on work in another project folder, this must be noted in `state.md` front-matter:

```yaml
related_projects:
  - role-protocol/milestones/milestone_02/
```

### `epic_X/implementation-summary/` (Required)

After every closed Task, the executing agent **must create** a summary file in this folder.

**Naming convention:** `TASK-{EPIC}-{NUM}-{short-slug}.md`
Example: `TASK-01-02-state-dependency-consistency.md`

**Required content:**

```yaml
---
id: impl-task-01-02
title: "Implementation Summary: <Task title>"
task: TASK-01-02
status: completed
created: 2026-02-22
author: <agent role>
---
```

- **What was done** (technical details, decisions)
- **List of modified/created files**
- **Acceptance criteria testing** (checkboxes ticked)

> **Why is this required?** The Orchestrator and Tech Lead verify task completion from these files — without reading the code itself. Without them, the task list is just words — without evidence.

---

## Task Level: The Smallest Executable Unit (Agent Level)

Every task is a physical `.md` file in the `tasks/` folder, named `TASK-{EPIC_NUM}-{TASK_NUM}.md`.

### `TASK-XY-XY.md` Structure

Every Task file is a "Contract" between the requester (Tech Lead / PO) and the Agent.

1. **Front-matter YAML (Required):**

   ```yaml
   ---
   id: task-01-01
   title: "Appropriate title reference"
   type: task
   epic: EPIC-XX
   scope: project-name
   status: pending | in_progress | completed
   priority: P0 | P1 | P2
   role: backend_developer  # Who this is for
   created: 2026-02-22
   # FSM tracking — optional if the Epic defines an fsm_workflow_id:
   fsm_state: "IN_DEV"       # which FSM state this task is in now
   fsm_retry_count: 0        # how many times it has been rejected
   ---
   ```

2. **# TASK-XX-XX: Main title**
3. **## Description:** What exactly is the task? Why does it need to be done? Is anything blocking it?
4. **## Acceptance Criteria:** Checkbox list.
   - [ ] The `path/file.ps1` file is created.
   - [ ] Exits with error code (Exit 1) if the file is not found.
5. **(Optional) ## Technical implementation suggestion:** Description, code snippet, or pseudocode.

---

## Summary & Lifecycle

How does the process flow?

1. Determine whether a **Program** level is needed (do multiple projects belong together?).
2. Create the **Project** under `docs/<project>/`.
3. If Program level exists: update `program-state.md` with the new project.
4. Split into **Milestones** in `goal.md`.
5. If a Milestone is too complex: break into **Sub-Milestones**.
6. Set up the current Milestone with a `plan.md`.
7. Create **Epic** folders and `epic_XX/state.md` files.
8. Write concrete `TASK-XX-XX.md` files.
9. The agent completes the task, then **must create** `implementation-summary/TASK-XX-XX-<slug>.md`.
10. Update status to `completed` in Project State and Epic State tables.

> **Golden Rule:** No closed task without an implementation summary. If `status: completed` appears in `tasks/` but no matching file exists in `implementation-summary/`, the task is not actually closed.
