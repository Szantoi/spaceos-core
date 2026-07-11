---
id: template-archived-communication-hub-readme
title: "Archived Communication Hub README Template"
description: "Template for creating the message registry README in an archived Epic Communication Hub folder. Includes message table, statistics, keywords, and quality checklist."
type: template
scope: epic-archival
category: multi-workspace
last_updated: 2026-03-01
---

# 📬 Archived Communication Hub README Template

**PURPOSE**: Template for creating archived Communication Hub registry (`archived/{project}/communication_hub/epics/{EPIC_ID}/README.md`)

**WHEN TO USE**: During Epic archival (Phase 1), when Communication Hub deployment is active

**OUTPUT**: Comprehensive message registry + statistics az archivált Epic-hez kapcsolódó összes Communication Hub üzenetről

---

## ✏️ Template Structure

```markdown
---
id: archived-communication-hub-{EPIC_ID}
title: "Archived Communication Hub - {EPIC_ID}"
type: archive-registry
archived_at: {YYYY-MM-DD}
epic_id: {EPIC_ID}
epic_name: {EPIC_NAME}
---

# 📬 Archived Communication Hub - {EPIC_ID}

**Epic**: {EPIC_ID} - {EPIC_NAME}
**Archival Date**: {YYYY-MM-DD HH:MM:SS}
**Archiver**: Knowledge Steward

---

## 📨 Message Registry

| Message ID | Date | From | To | Title | Category | Status |
|:-----------|:-----|:-----|:---|:------|:---------|:-------|
| [msg-001](messages/msg-001-orchestrator-to-architect.md) | 2026-01-15 | orchestrator | architect | Epic Planning Request - {EPIC_ID} | epic-planning-request | ✅ Completed |
| [msg-002](messages/msg-002-architect-to-orchestrator.md) | 2026-01-16 | architect | orchestrator | Epic Planning Complete - {EPIC_ID} | epic-planning-complete | ✅ Completed |
| [msg-003](messages/msg-003-orchestrator-to-tech_lead.md) | 2026-01-17 | orchestrator | tech_lead | Task Breakdown Request - {EPIC_ID} | task-breakdown-request | ✅ Completed |
| [msg-004](messages/msg-004-tech_lead-to-orchestrator.md) | 2026-01-18 | tech_lead | orchestrator | Task Breakdown Complete - {EPIC_ID} | task-breakdown-complete | ✅ Completed |
| [msg-005](messages/msg-005-orchestrator-to-backend_dev.md) | 2026-01-20 | orchestrator | backend_dev | Task Assignment - TASK-{EPIC}-01 | task-assignment | ✅ Completed |
| [msg-006](messages/msg-006-backend_dev-to-orchestrator.md) | 2026-01-22 | backend_dev | orchestrator | Task Implementation Complete - TASK-{EPIC}-01 | task-implementation-complete | ✅ Completed |
| [msg-007](messages/msg-007-orchestrator-to-qa_tester.md) | 2026-01-23 | orchestrator | qa_tester | QA Request - TASK-{EPIC}-01 | qa-request | ✅ Completed |
| [msg-008](messages/msg-008-qa_tester-to-orchestrator.md) | 2026-01-24 | qa_tester | orchestrator | QA Testing Complete - TASK-{EPIC}-01 | qa-testing-complete | ✅ Completed |
| [msg-009](messages/msg-009-orchestrator-to-tech_lead.md) | 2026-02-10 | orchestrator | tech_lead | Epic Closure Request - {EPIC_ID} | epic-closure-request | ✅ Completed |
| [msg-010](messages/msg-010-tech_lead-to-orchestrator.md) | 2026-02-11 | tech_lead | orchestrator | Epic Closure Complete - {EPIC_ID} | epic-closure-complete | ✅ Completed |
| [msg-011](messages/msg-011-orchestrator-to-architect.md) | 2026-02-12 | orchestrator | architect | Architect Sign-off Request - {EPIC_ID} | architect-signoff-request | ✅ Completed |
| [msg-012](messages/msg-012-architect-to-orchestrator.md) | 2026-02-13 | architect | orchestrator | Architect Sign-off Complete - {EPIC_ID} | architect-signoff-complete | ✅ Completed |
| [msg-013](messages/msg-013-orchestrator-to-knowledge_steward.md) | 2026-02-17 | orchestrator | knowledge_steward | Epic Archival Request - {EPIC_ID} | epic-archival | ✅ Completed |

---

## 📊 Message Statistics

**Total Messages**: {N}

**Message Categories**:
- Epic Planning: {count} (Request + Complete)
- Task Breakdown: {count} (Request + Complete)
- Task Assignment: {count}
- Task Implementation: {count} (Complete messages)
- QA Testing: {count} (Request + Complete)
- Epic Closure: {count} (Request + Complete)
- Architect Sign-off: {count} (Request + Complete)
- Epic Archival: {count} (Request + Complete)

**Agents Involved**:
- Orchestrator: {count} messages ({sent} sent, {received} received)
- Architect: {count} messages ({sent} sent, {received} received)
- Tech Lead: {count} messages ({sent} sent, {received} received)
- Backend Developer: {count} messages ({sent} sent, {received} received)
- Frontend Developer: {count} messages ({sent} sent, {received} received)
- QA Tester: {count} messages ({sent} sent, {received} received)
- Knowledge Steward: {count} messages ({sent} sent, {received} received)

**Message Timeline**:
- First message: {YYYY-MM-DD} (Epic Planning Request)
- Last message: {YYYY-MM-DD} (Epic Archival Complete)
- Epic duration: {X} days

---

## 🔍 Search Keywords

**Epic Keywords**: {EPIC_ID}, {EPIC_NAME}, {domain}, {technology_stack}, {key_features}

**Message Types**: epic-planning, task-breakdown, task-assignment, task-implementation, qa-testing, epic-closure, architect-signoff, epic-archival

**Agents**: orchestrator, architect, tech_lead, backend_dev, frontend_dev, qa_tester, knowledge_steward

**Key Decisions**:
- {Decision 1 summary} - [msg-XXX](messages/msg-XXX.md)
- {Decision 2 summary} - [msg-XXX](messages/msg-XXX.md)
- {Decision 3 summary} - [msg-XXX](messages/msg-XXX.md)

**Blockers Resolved**:
- {Blocker 1 summary} - [msg-XXX](messages/msg-XXX.md)
- {Blocker 2 summary} - [msg-XXX](messages/msg-XXX.md)

---

## 📁 Archive Structure

```

archived/{project}/communication_hub/epics/{EPIC_ID}/
├── README.md                             # This file
├── archived_at.txt                       # Timestamp
└── messages/
    ├── msg-001-orchestrator-to-architect.md
    ├── msg-002-architect-to-orchestrator.md
    ├── msg-003-orchestrator-to-tech_lead.md
    └── ... ({N} messages total)

```

---

## 🔗 Related Archives

- **Epic Archive**: [archived/{project}/epics/{EPIC_ID}/](../../epics/{EPIC_ID}/)
- **Epic Summary**: [epic_summary.md](../../epics/{EPIC_ID}/summary.md)
- **Active Epic** (read-only): [docs/{project}/epics/{EPIC_ID}/](../../../../{project}/epics/{EPIC_ID}/)

---

_Archived by Knowledge Steward on {YYYY-MM-DD HH:MM:SS}_
```

---

## 📋 Template Usage Guide

### 1. Collect Message Data

**PowerShell Helper**:

```powershell
# Find all messages related to Epic
$epicId = "EPIC-008"
$messages = Get-ChildItem "docs/{project}/communication_hub/messages/" -Recurse -Filter "*.md" |
  Where-Object { (Select-String -Path $_.FullName -Pattern $epicId -Quiet) } |
  Sort-Object LastWriteTime

# Count messages by category
$messages | ForEach-Object {
  $content = Get-Content $_.FullName -Raw
  if ($content -match "category:\s*(.+)") {
    [PSCustomObject]@{
      Category = $matches[1].Trim()
      File = $_.Name
    }
} | Group-Object Category | Select-Object Count, Name
```

### 2. Populate Message Registry Table

**For each message**:

1. Extract frontmatter: `id`, `timestamp`, `from`, `to`, `category`, `status`
2. Extract title from first `##` heading
3. Create table row:

   ```markdown
   | [msg-XXX](messages/msg-XXX-{from}-to-{to}.md) | {date} | {from} | {to} | {title} | {category} | ✅ {status} |
   ```

### 3. Calculate Statistics

**Message count by category**:

- Count messages matching `category: epic-planning-request`, `category: epic-planning-complete`, etc.

**Agent involvement**:

- Count messages where `from: {agent}` (sent)
- Count messages where `to: {agent}` (received)

**Timeline**:

- First message: Earliest `timestamp` from message frontmatter
- Last message: Latest `timestamp` from message frontmatter
- Duration: Days between first and last message

### 4. Extract Search Keywords

**From Epic plan.md**:

- Epic name, domain, technology stack, key features

**From messages**:

- Key decisions (search for "Decision:", "ADR-", "Architecture:")
- Blockers resolved (search for "Blocker:", "Issue:", "Problem:")

### 5. Create Archive README.md

**File Location**: `docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md`

**Command**:

```powershell
# Copy template
Copy-Item "src/agent-system/database/roles/management/knowledge_steward/templates/archived_communication_hub_readme.template.md" `
  "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md"

# Replace placeholders
(Get-Content "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md") `
  -replace '\{EPIC_ID\}', 'EPIC-008' `
  -replace '\{EPIC_NAME\}', 'Persistence Layer Implementation' `
  -replace '\{YYYY-MM-DD\}', (Get-Date -Format 'yyyy-MM-dd') |
  Set-Content "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md"
```

---

## ✅ Quality Checklist

Before finalizing archived Communication Hub README.md:

- [ ] **Message Registry complete**: All Epic-related messages listed (chronological order)
- [ ] **Links valid**: All message links point to correct `messages/{msg-id}.md` files
- [ ] **Statistics accurate**: Message counts, agent involvement, timeline calculated correctly
- [ ] **Search keywords comprehensive**: Epic keywords, message types, agents, key decisions documented
- [ ] **Archive structure documented**: Folder tree matches actual archive structure
- [ ] **Related archives linked**: Epic archive, epic summary, active Epic cross-referenced
- [ ] **Timestamp present**: `archived_at.txt` file created with archival timestamp

---

*Template maintained by Knowledge Steward. Last updated: 2026-02-17*
