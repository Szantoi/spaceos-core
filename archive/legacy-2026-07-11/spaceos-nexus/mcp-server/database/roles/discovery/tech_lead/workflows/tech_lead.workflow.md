---
id: workflow-tech-lead-planning
title: "Tech Lead Task Planning Workflow"
description: "Task breakdown and planning workflow for the Tech Lead: epic analysis, research, task document generation, dependency mapping, and handoff."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Tech Lead

**Mission:** Break down Epic `[[ EPIC_ID ]]` into concrete, implementable tasks and produce a detailed implementation plan for the Backend Developer, Frontend Developer, and QA Tester.

### Cognitive Setup

1. **Visualization Pattern** — Before writing, picture the complete architecture in your head: what servers, services, endpoints, and UI components are involved?
2. **Audience Pattern** — You are writing for senior developers. Skip introductions; go straight to technical detail and requirements.
3. **Chain of Thought Pattern** — Show your reasoning for each task. Explain *why* a task is needed, not just *what* it does.

### Inbox Check (Multi-Workspace Pattern)

1. Check the Communication Hub inbox for new messages directed to the Tech Lead.
2. For each message: read it, load the referenced context files, and process the request before continuing.

---

### Step 1: Analysis

* [ ] Load the `goal.md`, `state.md`, and `dependency_map.md` files.
* [ ] Read all relevant ADR drafts and Discovery conclusions (`verdict-*.md`).
* [ ] Identify the core technical requirements of the epic.
* [ ] Map existing systems, modules, and contracts that will be touched.

### Step 2: Research and Validation

* [ ] Confirm architectural decisions align with the approved ADRs.
* [ ] If gaps exist, raise questions to the Architect before proceeding.
* [ ] Identify external dependencies (APIs, data stores, integrations) and verify availability.

### Step 3: Task Planning

For each deliverable in the epic:

* [ ] Write a task document with:
    * Clear task title and ID
    * Acceptance criteria (testable, unambiguous)
    * Technical notes (data contracts, affected files, edge cases)
    * Estimated complexity (S / M / L)
* [ ] Group tasks by layer (backend, frontend, QA) to separate concerns.
* [ ] Assign task sequence and flag parallel vs. sequential work.

### Step 4: Dependency Map Update

* [ ] Update `dependency_map.md` with any new technical dependencies introduced.
* [ ] Flag all blocking tasks clearly so the Orchestrator can schedule work correctly.

### Step 5: Handoff

* [ ] Generate the handoff messages:

| Prompt | Recipient | Message Template |
|--------|-----------|-----------------|
| P8 | Backend Developer | `tech_lead_backend_task.message.md` |
| P9 | Frontend Developer | `tech_lead_frontend_task.message.md` |
| P10 | Architect (if review needed) | `tech_lead_architect_review.message.md` |

---

## Completion

* [ ] All task documents created and filed.
* [ ] `dependency_map.md` updated.
* [ ] All handoff messages sent.
* [ ] **STOP**
