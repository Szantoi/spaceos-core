---
id: workflow-frontend_developer-multi-workspace
title: "Frontend Developer Multi-Workspace Communication Protocol"
description: "Load this workflow ONLY in Multi-Workspace deployments. Handles Communication Hub message reading, processing, and sending responses for the Frontend Developer role."
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-03-01
---

# Frontend Developer Multi-Workspace Workflow

**Role**: Frontend Developer
**Scope**: Load this file ONLY in Multi-Workspace deployment
**Purpose**: Reading, processing, and responding to Communication Hub messages

---

## When to use this workflow?

**Governed by Runbook:** The `frontend_developer.runbook.md` "Multi-Workspace Detection" section determines when to load this file.

**Indicator**: If `docs/{project}/communication_hub/` folder exists → Multi-Workspace mode is active

---

## 1. Inbox Check Protocol

### 1.1 Startup - Check Pending Messages

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending messages (priority sorted)
Get-PendingMessages -Role "frontend_developer" | Format-Table

# Filter by high priority only
Get-PendingMessages -Role "frontend_developer" -HighPriorityOnly | Format-Table
```

**Manual check** (if PowerShell helper is not available):

1. Load: `docs/{project}/communication_hub/frontend_developer_inbox.md`
2. Search: `status: ⏳ Pending` messages (link-based table)
3. **Priority sorting**: CRITICAL > HIGH > NORMAL > LOW
4. **FIFO within priority**: Oldest message first within the same priority level

---

## 2. Message Processing

### 2.1 Read message

```powershell
# Read specific message
Read-Message -MessageId "msg-006"
```

**Manual reading**:

1. Click the link in the "File" column of the inbox table
2. Load the message file: `messages/{date}/msg-{id}-{from}-to-{to}.md`
3. Check the frontmatter fields:
   - `from`: Who sent the message (usually orchestrator/tech_lead)
   - `priority`: critical | high | normal | low
   - `category`: Message type (task-assignment, ui-bug-fix, component-refactoring, etc.)
   - `reply_to`: If this is a reply, the original message ID

### 2.2 Load Context Files

Load files referenced in the message body:

- Task assignment: `{EPIC_ROOT}/tasks/{TASK_ID}.md`
- UI bug fix: Screenshots, bug reports (if available)
- Component refactoring: ADR files, UI design specs

### 2.3 Execute Task

Choose workflow based on the message category:

| Category | Workflow File | Task |
|:---------|:--------------|:-----|
| `task-assignment` | frontend_developer.workflow.md | Task implementation (components, styles, tests, implementation-summary) |
| `ui-bug-fix` | frontend_developer.workflow.md | UI bug fixing (investigation, fix, visual regression test) |
| `component-refactoring` | frontend_developer.workflow.md | Component refactoring (cleanup, accessibility, performance) |
| `task-revision` | frontend_developer.workflow.md | Task revision based on Tech Lead feedback |

---

## 3. Response Creation

**CRITICAL: Template Completeness**

**ALWAYS use the FULL structure from Section 4 (Common Message Templates)**:

- **DO NOT shorten**: Every template section is required (Components, Styles, Tests, UI/UX Notes, Next Steps)
- **DO NOT simplify**: Detailed information (component structure, accessibility compliance) is REQUIRED
- **Follow the PowerShell example**: The template PowerShell body (`@"..."`) contains the FULL structure
- **DO NOT write only a summary**: "UI task complete" is NOT enough — detailed breakdown required

**Why this matters**: The Orchestrator/QA Tester uses the detailed information for validation (e.g., Accessibility → QA validation).

---

### 3.1 Create Response Message

```powershell
New-Message -From "frontend_developer" -To "orchestrator" `
  -Title "Task {TASK_ID} Implementation Complete" `
  -Body "Frontend implementation completed. Deliverables: {components list}, implementation-summary created. Next Steps: QA testing (if required)" `
  -Priority "normal" `
  -ReplyTo "msg-006" `
  -Category "task-implementation-complete"
```

**Manual response creation**:

1. **Create file**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-frontend_developer-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: frontend_developer
to: {recipient}
reply_to: {original-msg-id}
priority: normal
status: pending
category: {category}
thread_id: {original-thread-id}
---

## Message Title

### Summary
{Brief summary of the executed task}

### Deliverables
- [ ] {Components created/modified}
- [ ] {Tests added}
- [ ] {Implementation summary created}

### Next Steps
{Who should do what next}

### Context
- **Original Request**: Link to [{original-msg-id}](../{date}/{original-msg-id}.md)
- **Task**: {EPIC_ROOT}/tasks/{TASK_ID}.md
```

### 3.2 Inbox Status Update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-006" -NewStatus "completed"
```

**Manual inbox update**:

1. Load: `docs/{project}/communication_hub/frontend_developer_inbox.md`
2. Find the message in the "Pending Messages" table
3. Change the status column: `⏳ Pending` → `✅ Completed`

---

## 4. Common Message Templates

### 4.1 Task Implementation Complete (→ Orchestrator)

**Scenario**: UI task implementation finished, handoff to QA or Tech Lead

**CRITICAL: ALL sections of Template 4.1 are REQUIRED**

Use the **full PowerShell body structure** — every section below is REQUIRED:

- ✅ **Deliverables**: Components created/modified (with file paths), Styles added, Tests added (count + type), Implementation summary path
- ✅ **UI/UX Notes**: Accessibility compliance, responsive design, cross-browser compatibility
- ✅ **Test Results**: Build status, Unit tests (X/X format), Visual regression tests
- ✅ **Implementation Notes**: Deviations from design spec, Technical decisions, Known issues
- ✅ **Next Steps**: QA actions, Tech Lead actions
- ✅ **Files Changed**: File paths with brief description (separate section!)
- ✅ **Context**: Task Plan path, Implementation Summary path

```powershell
New-Message -From "frontend_developer" -To "orchestrator" `
  -Title "Task Implementation Complete - {TASK_ID}" `
  -Body @"
Frontend implementation completed for {TASK_ID}.

**Deliverables**:
- Components created/modified: {count} files
  - New components: {list files}
  - Modified components: {list files}
  - Styles: {list files}
- Tests added: {count} tests ({unit/component/e2e})
- Implementation summary: {EPIC_ROOT}/implementation-summary/{TASK_ID}-*.md

**UI/UX Notes**:
- Accessibility: {WCAG compliance level, ARIA labels added}
- Responsive: {breakpoints tested}
- Browser compatibility: {browsers tested}

**Test Results**:
- Build status: ✅ Passing
- Unit tests: {X}/{X} passing
- Component tests: {Y}/{Y} passing

**Implementation Notes**:
- {Any deviations from original design spec}
- {Technical decisions made during implementation}
- {Known issues or edge cases}

**Next Steps**:
- QA: Run acceptance tests (if QA required in task plan section 3)
- Tech Lead: Review implementation (if review requested)

**Files Changed**:
{List of file paths with brief description}

**Context**:
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Implementation Summary: {EPIC_ROOT}/implementation-summary/{TASK_ID}-*.md
"@ `
  -Priority "normal" `
  -ReplyTo "{original-msg-id}" `
  -Category "task-implementation-complete"
```

### 4.2 UI Bug Fix Complete (→ Orchestrator / Tech Lead)

**Scenario**: UI bug fix completed

```powershell
New-Message -From "frontend_developer" -To "orchestrator" `
  -Title "UI Bug Fix Complete - {BUG_ID}" `
  -Body @"
UI bug fix completed for {BUG_ID}.

**Bug Summary**: {1-2 sentence description of original issue}

**Root Cause**: {Technical explanation of what caused the bug}

**Fix Applied**:
- {Description of fix}
- Files modified: {list files}

**Visual Verification**:
- Screenshots: {before/after if applicable}
- Tested on: {browsers/devices}

**Test Results**:
- Build status: ✅ Passing
- Visual regression test: ✅ Passing
- Related tests: {N}/{N} passing

**Next Steps**:
- QA: Validate bug fix in acceptance tests
- Tech Lead: Review fix (if high priority bug)

**Files Changed**:
{List of file paths}

**Context**:
- Bug Report: {link to bug report if exists}
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
"@ `
  -Priority "high" `
  -Category "ui-bug-fix-complete"
```

### 4.3 Task Blocked - Need Clarification (→ Tech Lead / Orchestrator)

**Scenario**: Blocking issue encountered during UI task implementation

```powershell
New-Message -From "frontend_developer" -To "tech_lead" `
  -Title "Task Blocked - Need Clarification - {TASK_ID}" `
  -Body @"
Task {TASK_ID} implementation blocked. Need clarification on UI/UX or technical details.

**Blocker Description**:
{Detailed description of what is blocking progress}

**Questions**:
1. {Specific question 1}
2. {Specific question 2}

**Current Progress**:
- Completed: {what has been implemented so far}
- Blocked: {what cannot proceed without clarification}

**Proposed Solutions** (if applicable):
- Option A: {description, pros/cons}
- Option B: {description, pros/cons}

**Impact**:
- Time impact: {estimated delay}
- Dependency impact: {tasks that depend on this}

**Next Steps**:
- Tech Lead: Provide clarification or design decision
- Developer: Resume implementation after clarification

**Context**:
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Design spec: {link to design spec if applicable}
"@ `
  -Priority "high" `
  -Category "task-blocked"
```

---

## 5. Workflow Integration Points

### 5.1 Task Assignment (Orchestrator→Frontend Developer)

**Incoming Message**: `task-assignment` (from Orchestrator)

**Actions**:

1. Load: Task plan (`tasks/{TASK_ID}.md`), Epic context, UI/UX specs, skills
2. Execute: frontend_developer.workflow.md
3. Deliverables: Component files, styles, tests, implementation-summary
4. Response: [Template 4.1](#41-task-implementation-complete--orchestrator)

### 5.2 Bug Fix Assignment (Tech Lead→Frontend Developer)

**Incoming Message**: `ui-bug-fix` (from Tech Lead/Orchestrator)

**Actions**:

1. Load: Bug report, related task plan, affected component files
2. Execute: Investigation → Fix → Visual regression test
3. Deliverables: Fixed component, regression test
4. Response: [Template 4.2](#42-ui-bug-fix-complete--orchestrator--tech-lead)

### 5.3 Task Revision (Tech Lead→Frontend Developer)

**Incoming Message**: `task-review-feedback` (from Tech Lead)

**Actions**:

1. Load: Original implementation, feedback message
2. Execute: Address revision requests, update implementation-summary
3. Deliverables: Revised components, updated implementation-summary
4. Response: [Template 4.1](#41-task-implementation-complete--orchestrator) (with revision note)

---

## PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Check inbox (priority sorted)
Get-PendingMessages -Role "frontend_developer" | Format-Table

# High priority only
Get-PendingMessages -Role "frontend_developer" -HighPriorityOnly | Format-Table

# Read message
Read-Message -MessageId "msg-006"

# Send response
New-Message -From "frontend_developer" -To "orchestrator" `
  -Title "Task Implementation Complete" `
  -Body "..." `
  -Priority "normal" `
  -ReplyTo "msg-006" `
  -Category "task-implementation-complete"

# Update status
Update-MessageStatus -MessageId "msg-006" -NewStatus "completed"

# Get statistics
Get-MessageStatistics
```

---

**Note**: If Multi-Workspace mode is NOT active, do NOT load this file. The standard `frontend_developer.workflow.md` is sufficient in single-workspace mode.
