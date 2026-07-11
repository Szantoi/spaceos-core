---
id: workflow-backend_developer-multi-workspace
title: "Backend Developer Multi-Workspace Communication Protocol"
description: "Load this workflow ONLY in Multi-Workspace deployments. Handles Communication Hub message reading, processing, and sending responses for the Backend Developer role."
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-03-01
---

# Backend Developer Multi-Workspace Workflow

**Role**: Backend Developer
**Scope**: Load this file ONLY in Multi-Workspace deployment
**Purpose**: Reading, processing, and responding to Communication Hub messages

---

## When to use this workflow?

**Governed by Runbook:** The `backend_developer.runbook.md` "Multi-Workspace Detection" section determines when to load this file.

**Indicator**: If `docs/{project}/communication_hub/` folder exists → Multi-Workspace mode is active

---

## 1. Inbox Check Protocol

### 1.1 Startup - Check Pending Messages

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending messages (priority sorted)
Get-PendingMessages -Role "backend_developer" | Format-Table

# High priority only
Get-PendingMessages -Role "backend_developer" -HighPriorityOnly | Format-Table
```

**Manual check** (if PowerShell helper is not available):

1. Load: `docs/{project}/communication_hub/backend_developer_inbox.md`
2. Search: `status: ⏳ Pending` messages (link-based table)
3. **Priority sorting**: CRITICAL > HIGH > NORMAL > LOW
4. **FIFO within priority**: Oldest message first within the same priority level

---

## 2. Message Processing

### 2.1 Read message

```powershell
# Read specific message
Read-Message -MessageId "msg-005"
```

**Manual reading**:

1. Click the link in the "File" column of the inbox table
2. Load the message file: `messages/{date}/msg-{id}-{from}-to-{to}.md`
3. Check the frontmatter fields:
   - `from`: Who sent the message (usually orchestrator or tech_lead)
   - `priority`: critical | high | normal | low
   - `category`: Message type (task-assignment, bug-fix, task-revision, etc.)
   - `reply_to`: If this is a reply, the original message ID

### 2.2 Load Context Files

Load files referenced in the message body:

- Task assignment: Load task plan (`tasks/{TASK_ID}.md`), epic context, skills
- Bug fix: Load bug report, affected code files
- Task revision: Load original implementation, Tech Lead feedback message

### 2.3 Execute Task

Choose workflow based on the message category:

| Category | Workflow File | Task |
|:---------|:--------------|:-----|
| `task-assignment` | backend_developer.workflow.md | Task implementation (service layer, data layer, API integration) |
| `bug-fix` | backend_developer.workflow.md | Bug investigation, fix, regression test |
| `task-revision` | backend_developer.workflow.md | Address Tech Lead review feedback, update implementation-summary |

---

## 3. Response Creation

**CRITICAL: Template Completeness**

**ALWAYS use the FULL structure from Section 4 (Common Message Templates)**:

- **DO NOT shorten**: Every template section is required (Deliverables, Test Results, Implementation Notes, Next Steps, Files Changed, Context)
- **DO NOT simplify**: Detailed information (layer breakdown, code coverage %) is REQUIRED
- **Follow the PowerShell example**: The template PowerShell body (`@"..."`) contains the FULL structure
- **DO NOT write only a summary**: "Implementation complete" is NOT enough — detailed breakdown required

**Why this matters**: The Orchestrator/Tech Lead uses the detailed metrics to validate completion (e.g., Clean Architecture compliance, test quality).

---

### 3.1 Create Response Message

```powershell
New-Message -From "backend_developer" -To "orchestrator" `
  -Title "Task Implementation Complete - {TASK_ID}" `
  -Body "Backend implementation completed for {TASK_ID}. See full details below." `
  -Priority "normal" `
  -ReplyTo "msg-005" `
  -Category "task-implementation-complete"
```

**Manual response creation**:

1. **Create file**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-backend_developer-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: backend_developer
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
- [ ] {Deliverable 1}
- [ ] {Deliverable 2}

### Next Steps
{Who should do what next}

### Context
- **Original Request**: Link to [{original-msg-id}](../{date}/{original-msg-id}.md)
- **Task**: {EPIC_ROOT}/tasks/{TASK_ID}.md
```

### 3.2 Inbox Status Update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-005" -NewStatus "completed"
```

**Manual inbox update**:

1. Load: `docs/{project}/communication_hub/backend_developer_inbox.md`
2. Find the message in the "Pending Messages" table
3. Change the status column: `⏳ Pending` → `✅ Completed`

---

## 4. Common Message Templates

### 4.1 Task Implementation Complete (→ Orchestrator)

**Scenario**: Task implementation finished, handoff to QA or Tech Lead

**CRITICAL: ALL sections of Template 4.1 are REQUIRED**

Use the **full PowerShell body structure** — every section below is REQUIRED:

- ✅ **Deliverables**: Code changes (layer breakdown: Core/Entities, DomainServices, Infrastructure, API), Tests added (count + type), Implementation summary path
- ✅ **Test Results**: Build status, Unit tests (X/X format), Integration tests (Y/Y format), Code coverage (Z% + target)
- ✅ **Implementation Notes**: Deviations from plan, Technical decisions, Known issues/edge cases
- ✅ **Next Steps**: QA actions, Tech Lead actions
- ✅ **Files Changed**: File paths with brief description (separate section!)
- ✅ **Context**: Task Plan path, Implementation Summary path

**Why layer breakdown matters (Deliverables)**: Orchestrator/Tech Lead can verify Clean Architecture compliance (Core vs Infrastructure separation).

**Why code coverage matters (Test Results)**: QA/Tech Lead can evaluate test quality (80%+ target).

**Why Implementation Notes matter**: Tech Lead sees technical decisions (ADR needed? Standards update?).

**Why Files Changed is a separate section**: Orchestrator validates completion based on the file path list.

```powershell
New-Message -From "backend_developer" -To "orchestrator" `
  -Title "Task Implementation Complete - {TASK_ID}" `
  -Body @"
Backend implementation completed for {TASK_ID}.

**Deliverables**:
- Code changes: {count} files modified
  - Core/Entities: {list files}
  - Core/DomainServices: {list files}
  - Infrastructure: {list files}
  - API/Controllers: {list files}
- Tests added: {count} tests ({unit/integration})
- Implementation summary: {EPIC_ROOT}/implementation-summary/{TASK_ID}-*.md

**Test Results**:
- Build status: ✅ Passing
- Unit tests: {X}/{X} passing
- Integration tests: {Y}/{Y} passing
- Code coverage: {Z}% (target: 80%+)

**Implementation Notes**:
- {Any deviations from original plan}
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

**Anti-Pattern Example (WRONG)**:

```markdown
## Task Implementation Complete

### Deliverables
- [x] ProjectsController.cs: Updated
- [x] WorkTasksController.cs: Updated
- [x] Implementation summary created

### Test Results
- Build: ✅ Passing
- Tests: ✅ Passing

### Next Steps
- QA: Test endpoints
```

❌ **Why this is WRONG**:

- Deliverables: Layer breakdown MISSING (Core/Infrastructure/API separation)
- Test Results: Unit/Integration breakdown MISSING, Code coverage % MISSING
- Implementation Notes section COMPLETELY MISSING (Technical decisions? Deviations?)
- Files Changed section COMPLETELY MISSING (separate section required!)
- Only checkbox list → detailed breakdown needed

### 4.2 Bug Fix Complete (→ Orchestrator / Tech Lead)

**Scenario**: Bug fix completed

```powershell
New-Message -From "backend_developer" -To "orchestrator" `
  -Title "Bug Fix Complete - {BUG_ID}" `
  -Body @"
Bug fix completed for {BUG_ID}.

**Bug Summary**: {1-2 sentence description of original issue}

**Root Cause**: {Technical explanation of what caused the bug}

**Fix Applied**:
- {Description of fix}
- Files modified: {list files}

**Regression Prevention**:
- Regression test added: {test name}
- Code coverage increased: {X}% → {Y}%

**Test Results**:
- Build status: ✅ Passing
- Regression test: ✅ Passing
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
  -Category "bug-fix-complete"
```

### 4.3 Task Blocked - Need Clarification (→ Tech Lead / Orchestrator)

**Scenario**: Blocking issue encountered during task implementation

```powershell
New-Message -From "backend_developer" -To "tech_lead" `
  -Title "Task Blocked - Need Clarification - {TASK_ID}" `
  -Body @"
Task {TASK_ID} implementation blocked. Need clarification on technical details.

**Blocker Description**:
{Detailed description of what is blocking progress}

**Questions**:
1. {Specific question 1}
2. {Specific question 2}
3. {Specific question 3}

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
- Tech Lead: Provide clarification or architectural decision
- Developer: Resume implementation after clarification

**Context**:
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Current implementation: {branch name / PR link if applicable}
"@ `
  -Priority "high" `
  -Category "task-blocked"
```

---

## 5. Workflow Integration Points

### 5.1 Task Assignment (Orchestrator→Backend Developer)

**Incoming Message**: `task-assignment` (from Orchestrator)

**Actions**:

1. Load: Task plan (`tasks/{TASK_ID}.md`), Epic context, skills
2. Execute: backend_developer.workflow.md
3. Deliverables: Code files, tests, implementation-summary
4. Response: [Template 4.1](#41-task-implementation-complete--orchestrator)

### 5.2 Bug Fix Assignment (Tech Lead→Backend Developer)

**Incoming Message**: `bug-fix` (from Tech Lead/Orchestrator)

**Actions**:

1. Load: Bug report, related task plan, affected code
2. Execute: Investigation → Fix → Regression test
3. Deliverables: Fixed code, regression test
4. Response: [Template 4.2](#42-bug-fix-complete--orchestrator--tech-lead)

### 5.3 Task Revision (Tech Lead→Backend Developer)

**Incoming Message**: `task-review-feedback` (from Tech Lead)

**Actions**:

1. Load: Original implementation, feedback message
2. Execute: Address revision requests, update implementation-summary
3. Deliverables: Revised code, updated implementation-summary
4. Response: [Template 4.1](#41-task-implementation-complete--orchestrator) (with revision note)

---

## PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Check inbox (priority sorted)
Get-PendingMessages -Role "backend_developer" | Format-Table

# High priority only
Get-PendingMessages -Role "backend_developer" -HighPriorityOnly | Format-Table

# Read message
Read-Message -MessageId "msg-005"

# Send response
New-Message -From "backend_developer" -To "orchestrator" `
  -Title "Task Implementation Complete" `
  -Body "..." `
  -Priority "normal" `
  -ReplyTo "msg-005" `
  -Category "task-implementation-complete"

# Update status
Update-MessageStatus -MessageId "msg-005" -NewStatus "completed"

# Get statistics
Get-MessageStatistics
```

---

**Note**: If Multi-Workspace mode is NOT active, do NOT load this file. The standard `backend_developer.workflow.md` is sufficient in single-workspace mode.
