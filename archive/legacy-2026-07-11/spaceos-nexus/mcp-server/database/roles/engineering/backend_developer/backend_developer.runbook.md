---
id: runbook-backend-developer
title: "Backend Developer Runbook"
description: "Startup guide for the Backend Developer role: task context loading, multi-workspace detection, cognitive setup, and next step pointer."
type: runbook
role: backend_developer
category: engineering
last_updated: 2026-03-01
---

# Backend Developer Runbook

## Three-Step Start

Before writing any code, complete these three steps in order:

1. **Understand** — Read the task file (`{EPIC_ROOT}/tasks/{TASK_ID}.md`) in full. Focus on the goal, constraints, and section 3 (Execution Recommendation).
2. **Plan** — Identify the required skills listed in section 3. Load those skill files before starting.
3. **Verify** — At the end of every task, run the automated test suite and build pipeline. Only mark a task as Done when all checks pass.

---

## Context References

Load these when needed:

- `state.md` — current build and task status for the Epic
- `task.template.md` — task file structure reference
- `implementation_report.template.md` — implementation summary structure
- `testing_strategy.knowledge.md` — testing patterns and coverage guidelines

---

## Multi-Workspace Detection

Check whether a `communication_hub/` folder exists under `docs/{project}/`.

- **If yes**: load `backend_developer_multi_workspace.workflow.md` — read your inbox before starting work.
- **If no**: single-workspace mode; execute the task directly.

---

## Cognitive Setup

Activate the following patterns at the start of every task:

1. **N-shot Pattern** — Before implementing, review existing code in the repository to understand established style and patterns. Follow them.
2. **Fact Summary Pattern** — At the end of the task, produce a concise summary of what was changed and why. Record it in the implementation summary file.

---

## Where to Look (Priority Order)

1. **Repository examples** — Existing implementation files for style and structure reference
2. **System state** — `{EPIC_ROOT}/state.md` for current build status
3. **Task file** — `{EPIC_ROOT}/tasks/{TASK_ID}.md` as the single source of truth

---

**Next step:** Load `backend_developer.workflow.md` and execute `[[ TASK_ID ]]`.
