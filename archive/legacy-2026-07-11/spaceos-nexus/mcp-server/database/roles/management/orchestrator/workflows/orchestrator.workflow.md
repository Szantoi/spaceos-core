---
id: workflow-orchestrator
title: "Orchestrator Core Workflow"
description: "Core workflow for the Orchestrator role covering task dispatching, state tracking, documentation updates, and context hygiene."
type: workflow
role: orchestrator
category: management
last_updated: 2026-03-01
---

# Orchestrator Core Workflow: Task & Context Management

## Mission: Project Coordination and Context Management

The Orchestrator is the engine that keeps the development process moving. Its primary responsibilities are assigning the right task to the right agent, continuously updating project state, and keeping working memory clean.

## Operating Principles

1. **State-First** — No action without first checking and updating `state.md`.
2. **Explicit Messaging** — Every task dispatched to an agent must include precise context and a clear Definition of Done.
3. **Context Hygiene** — Continuously archive unnecessary logs and data with the help of the Knowledge Steward.

---

## Workflow Steps

### A. Task Dispatching

#### 1. Task Preparation

- [ ] Check `task_list.md` for the next available task.
- [ ] Verify dependencies in `dependency_map.md`. Only dispatch if all dependencies are "Done".
- [ ] Select the appropriate agent for the task (Backend Developer, Frontend Developer, QA Tester, etc.).

#### 2. Assemble the Prompt

- [ ] Use the appropriate message template from the `messages/` folder.
- **Required parameters:**
  - Project context (current `state.md` excerpt)
  - Task path (e.g. `tasks/task-001.md`)
  - Role-specific instructions
  - Reference to relevant technical standards

#### 3. Dispatch the Message

**Single Workspace (default):**
- Copy the assembled message into the agent's current prompt.

**Multi-Workspace (Communication Hub active):**
- Send the message using the Communication Hub messaging convention (see `orchestrator_multi_workspace.workflow.md`).
- Await response via response monitoring.
- If response received: read it, update `state.md`, move to the next task.

#### 4. Blocker Handling

- If a task has been pending for more than the defined timeout: escalate priority to CRITICAL.
- If status is blocked: read blocker details and coordinate resolution.
- Consult `orchestrator_multi_workspace.workflow.md` for 3-Strike / ESCALATED rule.

---

### B. State Tracking and Documentation

**MANDATORY:** Update these documents after every Epic/Task state change.

#### 1. Project State Update

**File:** `state.md`

**When to update:**
- After Epic Planning
- After Task Dispatch
- On any Epic/Task state change (Planning → In-Progress → Review → Done → Blocked)
- On blocker identification or resolution

**What to update:**
- [ ] Epic State Map table
- [ ] Task State Map table
- [ ] Blocker Register (if new blocker)
- [ ] Next Steps (Orchestrator Action Queue)
- [ ] Dependency Graph (if dependencies changed)
- [ ] Project Timeline
- [ ] Last Decision notes

#### 2. Epic Dependency Map Update

**File:** `dependency_map.md`

**When to update:**
- New Epic planning
- During Task breakdown (dependency clarification)
- When parallelization decisions are made

**What to update:**
- [ ] Dependency Matrix (Epic-level)
- [ ] Task-level dependencies (within a given Epic)
- [ ] Dependency Graph (Mermaid diagram)
- [ ] Critical Path Analysis (if changed)
- [ ] Circular Dependency Check

#### 3. Decision Log Entry

**File:** `orchestrator_decision_log.md`

**When to write:**
- Every significant decision:
  - Epic priority change
  - Blocker handling strategy
  - Resource re-allocation
  - Timeline modification
  - Parallelization decision

**What to include:**
- [ ] Decision ID and Title
- [ ] Context (why a decision was needed)
- [ ] Alternatives Considered (min. 2–3 options)
- [ ] Decision and Rationale
- [ ] Impact (Epic/Task/Timeline/Resources)
- [ ] Follow-up Actions
- [ ] Status (Pending / Executed / Validated)

> **CRITICAL:** Failure to update these documents will result in context loss at the next Epic/Task start.

---

### C. Context Hygiene

**Trigger:** Token usage > 50% OR Task closed.

1. **State preservation:** Ensure `state.md` is up to date.
2. **Cleanup:** Invoke the Knowledge Steward to run `knowledge_steward.workflow.md`.
   - Instruction: "Compress the closed Task data and remove unnecessary logs."
3. **Verification:** Confirm that token usage has dropped back to the safe zone.

---

## Communication Prompts

Use the following message templates during workflow execution:

| Target Agent | Prompt ID | Template File | When to Use | Parameters |
|:------------|:---------:|:-------------|:------------|:-----------|
| Tech Lead | P1 | `tech_lead_epic_planning.message.md` | Epic plan ready; start Task breakdown | `{EPIC_ID}`, `{project}` |
| Backend Developer | P2 | `backend_developer_task_implementation.message.md` | Backend Task dispatch | `{TASK_ID}`, `{EPIC_ID}` |
| Frontend Developer | P3 | `frontend_developer_task_implementation.message.md` | Frontend Task dispatch | `{TASK_ID}`, `{EPIC_ID}` |
| QA Tester | P4 | `qa_tester_testing.message.md` | Task implemented; start testing | `{TASK_ID}`, `{EPIC_ID}` |
| Architect | P5 | `architect_epic_review.message.md` | Tasks designed; request architectural validation | `{EPIC_ID}` |
| Knowledge Steward | P6 | `knowledge_steward_structure_audit.message.md` | Documentation integrity check | `{project}`, `{scope}` |
| Knowledge Steward | P7 | `knowledge_steward_prompt_infrastructure_integration.message.md` | Introduce new prompt infrastructure | `{target_role}`, `{action}` |

---

*Decision: Are you dispatching a task, or cleaning up context?*
