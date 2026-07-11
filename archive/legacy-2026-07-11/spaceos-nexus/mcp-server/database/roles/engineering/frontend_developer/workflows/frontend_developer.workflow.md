---
id: workflow-frontend_developer_go
title: "Frontend Developer Initialization Workflow"
description: "Task execution workflow for the Frontend Developer: plan loading, skill loading, implementation, verification, and QA handoff."
type: workflow
scope: global
category: agile-workflow
last_updated: 2026-03-01
---

## Task: Execute [[ TASK_ID ]]

**Role**: Senior Frontend Developer
**Objective**: Implement the UI/UX features described in the **[[ TASK_ID ]]** plan.

---

### Cognitive Setup

1. **Visualization Pattern** — Before writing code, map out the component hierarchy and data/state flow in your reasoning.
2. **N-shot Pattern** — Review existing components and hooks in the repository for style and structural conventions. Follow them.

---

### Inbox Check (Multi-Workspace Pattern)

**If Multi-Workspace deployment is active:**

1. **Startup Protocol**:
    - [ ] Load your inbox message file from the Communication Hub.
    - [ ] Look for `status: pending` messages (link-based table).
    - [ ] Process by priority: CRITICAL > HIGH > NORMAL > LOW.

2. **Message Processing**:
    - [ ] Read the message file.
    - [ ] Load the message's context files (Task plan).
    - [ ] Execute according to the workflow below.
    - [ ] Prepare a response message when done.

3. **Response Sending**:
    - [ ] Create a response message to the Orchestrator with:
      - Title: "Task {TASK_ID} Implementation Completed"
      - Body: List of changed files, path to the implementation summary, and the next step (QA UI/UX testing if required)
      - Category: `task-implementation`
    - [ ] Update the original message status to "completed".

---

### Where to Look (Priority Order)

1. **Repository examples** — Existing UI components, hooks, and feature modules for style reference
2. **System state** — `{EPIC_ROOT}/state.md` for current build status
3. **Task file** — `{EPIC_ROOT}/tasks/{TASK_ID}.md` as the single source of truth

---

### Required Steps

1. **Load the plan** (Step 1):
    - Read the task file: `{EPIC_ROOT}/tasks/[[ TASK_ID ]].md`.
    - Understand the goal, context, and all steps.

2. **Load the skills** (Step 2):
    - Check section 3 of the plan ("Execution Recommendation").
    - Load the skill files listed there.

3. **Prepare for execution** (Step 3):
    - Create an implementation summary file: `{EPIC_ROOT}/implementation-summary/[[ TASK_ID ]]-{short-description}.md`.
    - Use `implementation_report.template.md` for the structure.
    - Take notes in this file continuously during execution.

4. **Execute the task** (Step 4):
    - **ReACT Loop**: Think → Code → Verify in the browser/preview.
    - Search for solutions using the `context7` MCP tool.
    - Follow the "Steps" section in the plan precisely.
    - Create and modify files as instructed.
    - Adhere to project standards and Clean Architecture principles.
    - If you encounter a problem, use the `brave-search` MCP web search tool.

5. **Verify and document** (Step 5):
    - Run the automated test suite and build pipeline.
    - Fill in section 11 of the plan file ("Implementation Details") with any deviations or notes.
    - Update `{EPIC_ROOT}/state.md` to mark the task as "Done".
    - Update `{EPIC_ROOT}/backlog.md` to mark the task as "Done".
    - If QA is required (section 3 states "QA Required: Yes"), hand off to the QA Tester.

---

### QA Handoff

**If the Task plan section 3 contains "QA Required: Yes", use this prompt:**

- **P12**: `qa_tester_qa_handoff.message.md`
- **When**: Frontend implementation complete; UI/UX testing required
- **Parameters**: `{TASK_ID}`, `{EPIC_ID}`, `{changed_files}`, `{ui_components}`, `{edge_cases}`

---
*Start by reading the plan for [[ TASK_ID ]] and confirming you have the required skills loaded.*
