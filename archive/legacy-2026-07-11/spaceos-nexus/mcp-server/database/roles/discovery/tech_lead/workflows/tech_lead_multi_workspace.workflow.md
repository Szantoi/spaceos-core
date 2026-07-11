---
id: workflow-tech_lead-multi-workspace
title: "Tech Lead Multi-Workspace Communication Protocol"
description: "Load this workflow ONLY in Multi-Workspace deployments. Handles Communication Hub message reading, processing, and sending responses for the Tech Lead role."
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-03-01
---

# Tech Lead Multi-Workspace Workflow

**Role**: Technical Lead
**Scope**: Load this file ONLY in Multi-Workspace deployment
**Purpose**: Reading, processing, and responding to Communication Hub messages

---

## When to use this workflow?

**Governed by Runbook:** The `tech_lead.runbook.md` "Multi-Workspace Detection" section determines when to load this file.

**Indicator**: If `docs/{project}/communication_hub/` folder exists → Multi-Workspace mode is active

---

## 1. Inbox Check Protocol

### 1.1 Startup - Check Pending Messages

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending messages
Get-PendingMessages -Role "tech_lead" | Format-Table

# Filter by priority
Get-PendingMessages -Role "tech_lead" -HighPriorityOnly | Format-Table
```

**Manual check** (if PowerShell helper is not available):

1. Load: `docs/{project}/communication_hub/tech_lead_inbox.md`
2. Search: `status: ⏳ Pending` messages (link-based table)
3. **FIFO processing**: Oldest timestamp first (sort by Timestamp ASC)
4. **Priority override**: CRITICAL/HIGH messages promoted to front

---

## 2. Message Processing

### 2.1 Read message

```powershell
# Read specific message
Read-Message -MessageId "msg-002"
```

**Manual reading**:

1. Click the link in the "File" column of the inbox table
2. Load the message file: `messages/{date}/msg-{id}-{from}-to-{to}.md`
3. Check the frontmatter fields:
   - `from`: Who sent the message
   - `priority`: critical | high | normal | low
   - `category`: Message type (task-breakdown, epic-closure, etc.)
   - `reply_to`: If this is a reply, the original message ID

### 2.2 Load Context Files

Load files referenced in the message body:

- Task breakdown: `{EPIC_ROOT}/plan.md`, ADR files
- Epic closure: `{EPIC_ROOT}/tasks/*.md`, implementation summaries
- Retrospective: `{EPIC_ROOT}/plan.md`, `summary.md`, `learnings.md`

### 2.3 Execute Task

Choose workflow based on the message category:

| Category | Workflow File | Task |
|:---------|:--------------|:-----|
| `task-breakdown` | tech_lead.workflow.md | Task planning (tasks/{TASK_ID}.md creation, backlog.md update) |
| `epic-closure` | tech_lead_closure.workflow.md | Epic review (epic_review.md, tech_lead_signoff.md, qa_signoff.md creation) |
| `task-review` | tech_lead.workflow.md | Task implementation review, feedback to developer |
| `calibration` | tech_lead_closure.workflow.md | Calibration recommendations (dod_rule.md, standards update) |

---

## 3. Response Creation

**CRITICAL: Template Completeness**

**ALWAYS use the FULL structure from Section 4 (Common Message Templates)**:

- **DO NOT shorten**: Every template section is required (Deliverables, Validation Summary, Calibration Recommendations, Next Steps, Files)
- **DO NOT simplify**: Detailed information (task counts, file paths, metrics) is REQUIRED
- **Follow the PowerShell example**: The template PowerShell body (`@"..."`) contains the FULL structure
- **DO NOT write only a summary**: "Epic closure complete" is NOT enough — detailed breakdown required

**Why this matters**: The Orchestrator/next agent uses the detailed information (e.g., Calibration Recommendations → Knowledge Steward processing).

---

### 3.1 Create Response Message

```powershell
New-Message -From "tech_lead" -To "orchestrator" `
  -Title "Task Breakdown Completed - {EPIC_ID}" `
  -Body "Task breakdown completed. Deliverables: {N} tasks created, backlog.md updated. Next Steps: Developer task assignment" `
  -Priority "normal" `
  -ReplyTo "msg-002" `
  -Category "task-breakdown-complete"
```

**Manual response creation**:

1. **Create file**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-tech_lead-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: tech_lead
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
- **Epic**: {EPIC_ROOT}/
```

### 3.2 Inbox Status Update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-002" -NewStatus "completed"
```

**Manual inbox update**:

1. Load: `docs/{project}/communication_hub/tech_lead_inbox.md`
2. Find the message in the "Pending Messages" table
3. Change the status column: `⏳ Pending` → `✅ Completed`

---

## 4. Common Message Templates

### 4.1 Task Breakdown Complete (→ Orchestrator)

**Scenario**: Epic task breakdown finished — tasks ready for developer assignment

**CRITICAL: ALL sections of Template 4.1 are REQUIRED**

Use the **full PowerShell body structure** — every section below is REQUIRED:

- ✅ **Deliverables**: Task files created (N tasks, file paths), backlog.md updated, dependencies mapped
- ✅ **Task Summary**: Task list with ID, title, estimated effort, and dependency
- ✅ **Implementation Notes**: Architectural decisions, risk flags, technical constraints
- ✅ **Next Steps**: Orchestrator actions, Developer assignment order
- ✅ **Files Created/Modified**: File paths with brief description
- ✅ **Context**: Epic Plan path, ADR references

```powershell
New-Message -From "tech_lead" -To "orchestrator" `
  -Title "Task Breakdown Completed - {EPIC_ID}" `
  -Body @"
Task breakdown completed for {EPIC_ID}.

**Deliverables**:
- Tasks created: {N} task files
  - {TASK_ID_1}: {Task Title 1}
  - {TASK_ID_2}: {Task Title 2}
  - {TASK_ID_3}: {Task Title 3}
- backlog.md updated with task priority order
- Dependencies mapped (execution order defined)

**Task Summary**:
| Task ID | Title | Effort | Dependencies |
|:--------|:------|:-------|:-------------|
| {TASK_ID_1} | {Title} | {S/M/L} | None |
| {TASK_ID_2} | {Title} | {S/M/L} | {TASK_ID_1} |

**Implementation Notes**:
- {Key architectural decision}
- {Risk flag or technical constraint}
- {Suggested implementation order}

**Next Steps**:
- Orchestrator: Assign tasks to developers (follow dependency order)
- Developer: Load task plan and epic context before starting

**Files Created/Modified**:
- Created: {EPIC_ROOT}/tasks/{TASK_ID_1}.md
- Created: {EPIC_ROOT}/tasks/{TASK_ID_2}.md
- Updated: {EPIC_ROOT}/backlog.md

**Context**:
- Epic Plan: {EPIC_ROOT}/plan.md
- ADR References: {EPIC_ROOT}/adr/
"@ `
  -Priority "normal" `
  -ReplyTo "{original-msg-id}" `
  -Category "task-breakdown-complete"
```

### 4.2 Epic Closure Complete (→ Orchestrator)

**Scenario**: Epic review and closure finished — all tasks done, sign-offs ready

```powershell
New-Message -From "tech_lead" -To "orchestrator" `
  -Title "Epic Closure Complete - {EPIC_ID}" `
  -Body @"
Epic closure completed for {EPIC_ID}.

**Deliverables**:
- Epic review: {EPIC_ROOT}/epic_review.md
- Tech Lead sign-off: {EPIC_ROOT}/tech_lead_signoff.md
- QA sign-off: {EPIC_ROOT}/qa_signoff.md (if applicable)

**Validation Summary**:
- Tasks completed: {N}/{N}
- Build status: ✅ Passing
- Test coverage: {X}% (target: 80%+)
- DoD checklist: ✅ All items met

**Calibration Recommendations** (for Knowledge Steward):
- {New pattern/skill identified}
- {Standards update suggested}
- {Template improvement proposed}

**Next Steps**:
- Orchestrator: Architect sign-off request
- Knowledge Steward: Process calibration recommendations (epic_review.md Section 6)

**Files Created/Modified**:
- Created: {EPIC_ROOT}/epic_review.md
- Created: {EPIC_ROOT}/tech_lead_signoff.md
- Updated: {EPIC_ROOT}/backlog.md (all tasks marked Done)

**Context**:
- Epic Plan: {EPIC_ROOT}/plan.md
- Tasks: {EPIC_ROOT}/tasks/
"@ `
  -Priority "normal" `
  -ReplyTo "{original-msg-id}" `
  -Category "epic-closure-complete"
```

### 4.3 Task Review Feedback (→ Backend/Frontend Developer)

**Scenario**: Code review complete — feedback sent to developer for revisions

```powershell
New-Message -From "tech_lead" -To "backend_developer" `
  -Title "Task Review Feedback - {TASK_ID}" `
  -Body @"
Task {TASK_ID} review completed. Revision required before acceptance.

**Review Summary**:
- Overall assessment: {Needs Revision / Minor Issues / Accepted with Notes}

**Required Changes**:
1. {Specific change required 1}
2. {Specific change required 2}

**Suggestions** (optional, not blocking):
- {Improvement suggestion 1}
- {Improvement suggestion 2}

**Quality Issues Found**:
- Test coverage: {X}% (below 80% target)
- Missing: {specific test scenarios}
- Code quality: {specific issue}

**Acceptance Criteria** (what must be done before re-review):
- [ ] {Must-fix item 1}
- [ ] {Must-fix item 2}

**Next Steps**:
- Developer: Address required changes and update implementation-summary
- Tech Lead: Re-review after developer re-submission

**Context**:
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Implementation Summary: {EPIC_ROOT}/implementation-summary/{TASK_ID}-*.md
"@ `
  -Priority "high" `
  -ReplyTo "{original-msg-id}" `
  -Category "task-review-feedback"
```

---

## 5. Workflow Integration Points

### 5.1 Task Breakdown (Orchestrator→Tech Lead)

**Incoming Message**: `task-breakdown` (from Orchestrator)

**Actions**:

1. Load: Epic plan (`{EPIC_ROOT}/plan.md`), ADR files, skills
2. Execute: tech_lead.workflow.md
3. Deliverables: Task files, updated backlog.md
4. Response: [Template 4.1](#41-task-breakdown-complete--orchestrator)

### 5.2 Epic Closure (Orchestrator→Tech Lead)

**Incoming Message**: `epic-closure` (from Orchestrator after all tasks completed)

**Actions**:

1. Load: All task files, implementation summaries, QA reports
2. Execute: tech_lead_closure.workflow.md
3. Deliverables: epic_review.md, tech_lead_signoff.md, qa_signoff.md
4. Response: [Template 4.2](#42-epic-closure-complete--orchestrator)

### 5.3 Task Review (Developer→Tech Lead)

**Incoming Message**: `task-implementation-complete` (from Developer)

**Actions**:

1. Load: Implementation summary, changed files, test results
2. Execute: Code review against DoD and task plan
3. Deliverables: Approved or feedback with revision requests
4. Response: [Template 4.3](#43-task-review-feedback--backendfrontend-developer) OR approval message

---

## PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Check inbox (priority sorted)
Get-PendingMessages -Role "tech_lead" | Format-Table

# High priority only
Get-PendingMessages -Role "tech_lead" -HighPriorityOnly | Format-Table

# Read message
Read-Message -MessageId "msg-002"

# Send response
New-Message -From "tech_lead" -To "orchestrator" `
  -Title "Task Breakdown Completed" `
  -Body "..." `
  -Priority "normal" `
  -ReplyTo "msg-002" `
  -Category "task-breakdown-complete"

# Update status
Update-MessageStatus -MessageId "msg-002" -NewStatus "completed"

# Get statistics
Get-MessageStatistics
```

---

**Note**: If Multi-Workspace mode is NOT active, do NOT load this file. The standard `tech_lead.workflow.md` is sufficient in single-workspace mode.
