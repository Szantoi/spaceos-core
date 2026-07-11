---
id: workflow-product-owner-multi-workspace
title: "Product Owner Multi-Workspace Communication Protocol"
description: "Load this workflow ONLY in Multi-Workspace deployments. Handles Communication Hub message reading, processing, and sending responses for the Product Owner role."
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-03-01
---

# Product Owner Multi-Workspace Workflow

**Role**: Product Owner
**Scope**: Load this file ONLY in Multi-Workspace deployment
**Purpose**: Reading, processing, and responding to Communication Hub messages

---

## When to use this workflow?

**Governed by Runbook:** The `product_owner.runbook.md` "Multi-Workspace Detection" section determines when to load this file.

**Indicator**: If `docs/{project}/communication_hub/` folder exists → Multi-Workspace mode is active

---

## 1. Inbox Check Protocol

### 1.1 Startup - Check Pending Messages

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending messages (priority sorted)
Get-PendingMessages -Role "product_owner" | Format-Table
```

**Manual check** (if PowerShell helper is not available):

1. Load: `docs/{project}/communication_hub/product_owner_inbox.md`
2. Search: `status: ⏳ Pending` messages (link-based table)
3. **Priority sorting**: CRITICAL > HIGH > NORMAL > LOW
4. **FIFO within priority**: Oldest message first within the same priority level

---

## 2. Message Processing

### 2.1 Read message

```powershell
# Read specific message
Read-Message -MessageId "msg-001"
```

**Manual reading**:

1. Click the link in the "File" column of the inbox table
2. Load the message file: `messages/{date}/msg-{id}-{from}-to-{to}.md`
3. Check the frontmatter fields:
   - `from`: Who sent the message (usually orchestrator or architect)
   - `priority`: critical | high | normal | low
   - `category`: Message type (strategic-review, health-report-request, epic-proposal-review, etc.)
   - `reply_to`: If this is a reply, the original message ID

### 2.2 Load Context Files

Load files referenced in the message body:

- Strategic review: `goal.md`, `state.md`, Epic reports
- Health report request: Active epic summaries, state.md
- Epic proposal review: `{EPIC_ROOT}/plan.md` or draft proposal

### 2.3 Execute Task

Choose action based on the message category:

| Category | Action | Task |
|:---------|:-------|:-----|
| `strategic-review` | Apply Value vs Effort Pattern | Evaluate business impact, update priorities |
| `health-report-request` | Apply Fact Summary Pattern | Summarize project health, produce health report |
| `epic-proposal-review` | Apply Value vs Effort Pattern | Evaluate epic proposal, approve or request changes |
| `directive-update` | Review and update | Revise strategic directive based on new information |

---

## 3. Response Creation

**CRITICAL: Template Completeness**

**ALWAYS use the FULL structure from Section 4 (Common Message Templates)**:

- **DO NOT shorten**: Every template section is required (Decision, Rationale, Strategic Impact, Next Steps)
- **Apply cognitive patterns**: Value vs Effort Pattern and Fact Summary Pattern where applicable
- **DO NOT write only a summary**: Detailed strategic reasoning is required

---

### 3.1 Create Response Message

1. **Create file**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-product_owner-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: product_owner
to: {recipient}
reply_to: {original-msg-id}
priority: normal
status: pending
category: {category}
thread_id: {original-thread-id}
---

## Message Title

### Summary
{Brief summary of the decision or action taken}

### Decision / Output
{Strategic directive, health report, or epic approval}

### Rationale
{Brief explanation of the decision reasoning}

### Next Steps
{Who should do what next}

### Context
- **Original Request**: Link to [{original-msg-id}](../{date}/{original-msg-id}.md)
```

### 3.2 Inbox Status Update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-001" -NewStatus "completed"
```

**Manual inbox update**:

1. Load: `docs/{project}/communication_hub/product_owner_inbox.md`
2. Find the message in the "Pending Messages" table
3. Change the status column: `⏳ Pending` → `✅ Completed`

---

## 4. Common Message Templates

### PO-001: Strategic Directive Update (→ Orchestrator)

Use to communicate updated strategic priorities to the Orchestrator.

**Template file**: orchestrator_strategic_directive.message.md

```powershell
New-Message -From "product_owner" -To "orchestrator" `
  -Title "Strategic Directive Update - {DATE}" `
  -Body @"
Strategic directive updated based on {trigger/context}.

**Updated Priorities**:
1. {Priority 1 — Epic/Goal with rationale}
2. {Priority 2 — Epic/Goal with rationale}
3. {Priority 3 — Epic/Goal with rationale}

**Business Rationale**:
{Value vs Effort analysis summary}

**Changes from Previous Directive**:
- {What changed and why}

**Next Steps**:
- Orchestrator: Align active work with updated priorities
- Architect: Review if architectural changes are needed

**Context**:
- Goal document: goal.md
- Project state: state.md
"@ `
  -Priority "high" `
  -ReplyTo "{original-msg-id}" `
  -Category "strategic-directive-update"
```

### PO-002: Epic Proposal (→ Architect)

Use to initiate a new Epic proposal to the Architect.

**Template file**: architect_epic_proposal.message.md

```powershell
New-Message -From "product_owner" -To "architect" `
  -Title "Epic Proposal - {EPIC_TITLE}" `
  -Body @"
New Epic proposal for review and planning.

**Epic Title**: {EPIC_TITLE}
**Business Value**: {Brief description of expected business impact}
**Priority**: {High / Medium / Low}

**Problem Statement**:
{What problem does this Epic solve?}

**Expected Outcomes**:
- {Outcome 1}
- {Outcome 2}

**Acceptance Criteria** (high-level):
- {Criterion 1}
- {Criterion 2}

**Effort Estimate** (rough):
{Small / Medium / Large / Unknown}

**Dependencies**:
{Any known dependencies on other Epics or external factors}

**Next Steps**:
- Architect: Review proposal and create Epic plan
- Product Owner: Review and approve Epic plan

**Context**:
- Goal document: goal.md
- Related Epics: {list if any}
"@ `
  -Priority "normal" `
  -Category "epic-proposal"
```

---

## 5. Workflow Integration Points

### 5.1 Strategic Review Request (Orchestrator→Product Owner)

**Incoming Message**: `strategic-review` (from Orchestrator)

**Actions**:

1. Load: goal.md, state.md, active and completed Epic summaries
2. Apply: Value vs Effort Pattern, Fact Summary Pattern
3. Deliverables: Updated strategic directive or health report
4. Response: [Template PO-001](#po-001-strategic-directive-update--orchestrator)

### 5.2 Epic Proposal Initiation (Product Owner→Architect)

**Scenario**: Product Owner identifies need for a new Epic

**Actions**:

1. Prepare: Epic concept definition, business rationale
2. Send: [Template PO-002](#po-002-epic-proposal--architect)
3. Follow-up: Review Architect's Epic plan and approve

---

## PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Check inbox (priority sorted)
Get-PendingMessages -Role "product_owner" | Format-Table

# Read message
Read-Message -MessageId "msg-001"

# Send response
New-Message -From "product_owner" -To "orchestrator" `
  -Title "Strategic Directive Update" `
  -Body "..." `
  -Priority "high" `
  -ReplyTo "msg-001" `
  -Category "strategic-directive-update"

# Update status
Update-MessageStatus -MessageId "msg-001" -NewStatus "completed"

# Get statistics
Get-MessageStatistics
```

---

**Completion Steps**:

1. Update the `state.md` file with the results of the above steps.
2. Report completion of work to the User.
3. **STOP**

---

**Note**: If Multi-Workspace mode is NOT active, do NOT load this file. The standard `product_owner.workflow.md` is sufficient in single-workspace mode.
