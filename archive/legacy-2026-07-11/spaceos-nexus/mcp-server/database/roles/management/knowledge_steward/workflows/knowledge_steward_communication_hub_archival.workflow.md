---
id: workflow-knowledge_steward-communication-hub-archival
title: "Knowledge Steward Communication Hub Archival Workflow"
description: "Structured archival of Epic-related Communication Hub messages as a snapshot. Trigger after epic archival is complete in Multi-Workspace deployments."
type: workflow
scope: communication-hub-archival
category: archival
last_updated: 2026-03-01
---

# Communication Hub Archival Workflow

**Role**: Knowledge Steward (Chief Librarian)
**Scope**: Communication Hub message archival (WITHOUT Epic files)
**Purpose**: Structured snapshot archival of Epic-related Communication Hub messages

---

## When to use this workflow?

**Multi-Workspace Communication Hub trigger**:

- Category: `communication-hub-archival`
- Orchestrator message: Communication Hub Archival Request
- Context Files: `docs/{project}/communication_hub/messages/` (Epic-related messages)
- Prerequisites: Epic is confirmed closed

**Standalone trigger**:

- After Epic archival (Epic files already in archive/)
- Communication Hub active deployment (`docs/{project}/communication_hub/` folder exists)
- Epic-related message inventory ready

---

## Archival Conditions Check

**Reasoning**: Can the Communication Hub messages be archived?

**⚠️ CRITICAL Checklist**:

- [ ] Communication Hub deployment active: `docs/{project}/communication_hub/` exists
- [ ] Epic archived: `docs/archive/{project}/epics/{EPIC_ID}/` exists (Epic files archival complete)
- [ ] Epic-related messages identified: Message inventory prepared (Section 1)

**If Communication Hub is NOT active**: Workflow is NOT executable (skip archival)

**Action**:

- Multi-Workspace: Create Response Message (Template — No Communication Hub) → Orchestrator
- Standalone: Skip archival (NOT multi-workspace deployment)

---

## 1. Message Inventory (Message Discovery)

**Goal**: Identify messages belonging to the archived Epic.

### 1.1 PowerShell Automated Inventory

```powershell
# Search for messages referencing the Epic ID
$epicId = "{EPIC_ID}"  # e.g. "EPIC-008"
$project = "{project}"  # e.g. "joinerytech-flow"

$messagesPath = "docs/$project/communication_hub/messages/"

# Search messages mentioning Epic ID (frontmatter OR body)
$epicMessages = Get-ChildItem $messagesPath -Recurse -Filter "*.md" |
  Where-Object { (Select-String -Path $_.FullName -Pattern $epicId -Quiet) } |
  Select-Object -ExpandProperty FullName

Write-Host "Found $($epicMessages.Count) messages related to $epicId" -ForegroundColor Cyan

# Display message list (for inventory)
$epicMessages | ForEach-Object {
    $relativePath = $_ -replace [regex]::Escape((Get-Location).Path), '.'
    Write-Host "  - $relativePath"
}
```

---

### 1.2 Manual Inventory

**If PowerShell script is NOT available**:

1. Browse: `docs/{project}/communication_hub/messages/{date}/`
2. Search in frontmatter: `epic_id: {EPIC_ID}` or `related_epic: {EPIC_ID}`
3. Search in body: Epic ID reference (e.g. "EPIC-008", "Persistence Layer Epic")

**List the message types** (category field):

| Category | Description | Agents Involved |
|:---------|:------------|:----------------|
| `epic-planning-request` | Epic planning request | orchestrator → architect |
| `epic-planning-complete` | Epic planning completed | architect → orchestrator |
| `task-breakdown-request` | Task breakdown request | orchestrator → tech_lead |
| `task-breakdown-complete` | Task breakdown completed | tech_lead → orchestrator |
| `epic-closure-request` | Epic closure request | orchestrator → tech_lead |
| `epic-closure-complete` | Epic closure completed | tech_lead → orchestrator |
| `architect-signoff-request` | Architect approval request | orchestrator → architect |
| `architect-signoff-complete` | Architect approval | architect → orchestrator |
| `epic-archival-request` | Epic archival request | orchestrator → knowledge_steward |
| `epic-archival-complete` | Epic archival completed | knowledge_steward → orchestrator |
| `devils-advocate-critique` | Devils Advocate critique | devils_advocate → orchestrator/architect |

---

## Archive Communication Hub Structure

**Goal**: The archive Communication Hub hierarchy in standardized format.

### Archive Folder Hierarchy

```
docs/archive/{project}/communication_hub/
├── epics/
│   └── {EPIC_ID}/                    # e.g. EPIC-008_persistence-layer
│       ├── messages/
│       │   ├── msg-001-orchestrator-to-architect.md
│       │   ├── msg-002-architect-to-orchestrator.md
│       │   ├── msg-003-orchestrator-to-tech_lead.md
│       │   ├── msg-004-tech_lead-to-orchestrator.md
│       │   ├── msg-005-orchestrator-to-knowledge_steward.md
│       │   ├── msg-006-devils_advocate-to-orchestrator.md
│       │   └── ...
│       ├── README.md                 # Message registry
│       └── archived_at.txt           # Archive date
```

**⚠️ IMPORTANT**:

- Epic file archival is handled separately (outside this workflow)
- Messages are a COPY operation (preserve originals in Communication Hub!)
- Archive = SNAPSHOT at Epic closure time

---

## Archival Protocol (Message Snapshot)

### Step 1: Create Archive Communication Hub Structure

```powershell
# 1. Create the archive communication hub structure
$arcCommHubPath = "docs/archive/$project/communication_hub/epics/$epicId"

New-Item -ItemType Directory -Path "$arcCommHubPath/messages/" -Force

# Example (joinerytech-flow project, EPIC-008):
# New-Item -ItemType Directory -Path "docs/archive/joinerytech-flow/communication_hub/epics/EPIC-008_persistence-layer/messages/" -Force
```

---

### Step 2: COPY Epic-Related Messages

**⚠️ CRITICAL**: COPY operation (DO NOT MOVE!) - preserve originals!

```powershell
# 2. Copy Epic-related messages (preserve originals)
$epicMessages | ForEach-Object {
    $targetPath = Join-Path "$arcCommHubPath/messages/" (Split-Path $_ -Leaf)
    Copy-Item $_ -Destination $targetPath -Force
}

Write-Host "Copied $($epicMessages.Count) messages to archive" -ForegroundColor Green
```

**Validation**: Verify originals are preserved

```powershell
# Check originals still exist
$originalExists = Test-Path $messagesPath
if (-not $originalExists) {
    Write-Host "CRITICAL ERROR: Original Communication Hub messages DELETED!" -ForegroundColor Red
    Write-Host "  Archive operation should COPY, NOT MOVE!" -ForegroundColor Yellow
}
```

---

### Step 3: Archive Communication Hub README.md Registry

**Goal**: Create Message Metadata table for the archived messages.

#### 3.1 Template Usage

**Template file**: archived_communication_hub_readme.template.md

```powershell
# 1. Copy the base template
$templatePath = "src/agent-system/database/roles/management/knowledge_steward/templates/archived_communication_hub_readme.template.md"
$readmePath = "$arcCommHubPath/README.md"

Copy-Item $templatePath $readmePath -Force

# 2. Placeholder replacement (automated)
$epicName = "{EPIC_NAME}"  # e.g. "Persistence Layer Implementation"
$archiveDate = Get-Date -Format 'yyyy-MM-dd'

(Get-Content $readmePath) `
  -replace '\{EPIC_ID\}', $epicId `
  -replace '\{EPIC_NAME\}', $epicName `
  -replace '\{YYYY-MM-DD\}', $archiveDate `
  -replace '\{project\}', $project |
  Set-Content $readmePath

# 3. Load the README.md and populate manually:
#    - Message Registry table (msg-XXX links + metadata)
#    - Message Statistics (count by category, agents involved)
#    - Search Keywords (key decisions, blockers resolved)

Write-Host "README.md template created: $readmePath" -ForegroundColor Cyan
Write-Host "MANUAL POPULATION NEEDED: Message Registry, Statistics, Keywords" -ForegroundColor Yellow
```

---

#### 3.2 README.md Template Structure

**Template components** (detailed description in template file):

**1. Message Registry Table**:

| msg-ID | Date | From | To | Title | Category | Status |
|:-------|:-----|:-----|:---|:------|:---------|:-------|
| [msg-001](messages/msg-001-orchestrator-to-architect.md) | 2026-02-10 | orchestrator | architect | Epic Planning Request - {EPIC_ID} | epic-planning-request | ✅ Complete |
| [msg-002](messages/msg-002-architect-to-orchestrator.md) | 2026-02-11 | architect | orchestrator | Epic Planning Complete - {EPIC_ID} | epic-planning-complete | ✅ Complete |
| ... | ... | ... | ... | ... | ... | ... |

**2. Message Statistics**:

```markdown
**Total Messages**: {X}

**Category Breakdown**:
- Epic Planning: {Y} messages
- Task Breakdown: {Z} messages
- Epic Closure: {A} messages
- Sign-offs: {B} messages
- Devils Advocate Critiques: {C} messages

**Agents Involved**:
- orchestrator: {D} messages
- architect: {E} messages
- tech_lead: {F} messages
- knowledge_steward: {G} messages
- devils_advocate: {H} messages (if any)

**Timeline**:
- First Message: {YYYY-MM-DD}
- Last Message: {YYYY-MM-DD}
- Duration: {X} days
```

**3. Search Keywords**:

```markdown
**Epic Keywords**: Persistence Layer, Repository Pattern, ...

**Message Types**: Epic Planning, Task Breakdown, Architect Sign-off, Devils Advocate ADR Critique

**Agents**: orchestrator, architect, tech_lead, knowledge_steward, devils_advocate

**Key Decisions**:
- Repository Pattern adoption (msg-012, architect → orchestrator)
- ...

**Blockers Resolved**:
- Connection String management (msg-020, tech_lead → orchestrator)
- ...
```

**4. Archive Structure**: Folder tree documentation

**5. Related Archives**: Epic archive cross-reference

---

#### 3.3 README.md Template Quality Checklist

**⚠️ CRITICAL Checklist** (detailed in template file):

- [ ] Message Registry complete (all Epic-related messages listed)
- [ ] Links valid (all message links correct — relative paths!)
- [ ] Statistics accurate (counts calculated from message inventory)
- [ ] Search keywords comprehensive (Epic keywords + key decisions)
- [ ] Archive structure documented (folder tree)
- [ ] Related archives linked (Epic archive, epic summary)
- [ ] Timestamp present (archived_at.txt)

---

### Step 4: Archive Timestamp

```powershell
# 4. Archive timestamp
Get-Date -Format "yyyy-MM-dd HH:mm:ss" | Out-File "$arcCommHubPath/archived_at.txt"
```

---

## Archival Validation

**⚠️ CRITICAL Checklist**:

- [ ] Archive Communication Hub folder exists: `docs/archive/{project}/communication_hub/epics/{EPIC_ID}/`
- [ ] Messages archived: `messages/*.md` (count matches inventory)
- [ ] README.md exists: `docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md`
- [ ] README.md populated: Message Registry, Statistics, Keywords complete
- [ ] Timestamp exists: `archived_at.txt`
- [ ] **Originals PRESERVED**: `docs/{project}/communication_hub/messages/` (originals STILL exist!)

**Validation Script**:

```powershell
# Validation
$validationErrors = @()

# Archive folder check
if (-not (Test-Path "$arcCommHubPath")) {
    $validationErrors += "Archive Communication Hub folder NOT created: $arcCommHubPath"
}

# Messages count check
$archivedMessages = (Get-ChildItem "$arcCommHubPath/messages" -Filter "*.md" -ErrorAction SilentlyContinue).Count
$expectedCount = $epicMessages.Count

if ($archivedMessages -ne $expectedCount) {
    $validationErrors += "Messages count mismatch (expected: $expectedCount, archived: $archivedMessages)"
}

# README.md check
if (-not (Test-Path "$arcCommHubPath/README.md")) {
    $validationErrors += "README.md NOT created"
}

# Timestamp check
if (-not (Test-Path "$arcCommHubPath/archived_at.txt")) {
    $validationErrors += "archived_at.txt NOT created"
}

# CRITICAL: Originals preserved check
$originalMessagesExist = Test-Path $messagesPath
if (-not $originalMessagesExist) {
    $validationErrors += "CRITICAL: Original Communication Hub messages DELETED! (should be COPIED, not MOVED)"
}

if ($validationErrors.Count -gt 0) {
    Write-Host "VALIDATION ERRORS:" -ForegroundColor Red
    $validationErrors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
} else {
    Write-Host "Communication Hub archival validation PASSED" -ForegroundColor Green
}
```

---

## Archival Metrics (Multi-Workspace Response)

**If part of Multi-Workspace Communication Hub workflow**:

### Response Message Template

**Category**: communication-hub-archival-complete

**Required Deliverables**:

- [ ] Archive Communication Hub path: `docs/archive/{project}/communication_hub/epics/{EPIC_ID}/`
- [ ] Messages archived count: `{X}` messages (category breakdown)
- [ ] README.md registry created: Message Registry, Statistics, Keywords complete
- [ ] Originals preserved: `docs/{project}/communication_hub/messages/` (COPY operation confirmed)
- [ ] Timestamp: `{YYYY-MM-DD HH:mm:ss}`

### PowerShell Response Message Example

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Communication Hub Archival Complete - $epicId" `
  -Category "communication-hub-archival-complete" `
  -Body @"
Communication Hub archival completed successfully.

**Deliverables:**
- Archive Communication Hub folder: docs/archive/$project/communication_hub/epics/$epicId/
- Messages archived: $archivedMessages messages
  - Epic Planning: $epicPlanningCount
  - Task Breakdown: $taskBreakdownCount
  - Epic Closure: $epicClosureCount
  - Sign-offs: $signoffsCount
  - Devils Advocate Critiques: $daCount
- README.md registry: Message Registry ($archivedMessages messages), Statistics, Keywords
- Originals preserved: docs/$project/communication_hub/messages/ (COPY operation - originals STILL exist)
- Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

**Next Steps:**
- Epic closure notification: Notify Orchestrator (Epic + Communication Hub archival complete)
- Archive cross-reference: Link Epic archive to Communication Hub archive in README.md
"@
```

---

## Troubleshooting

### Problem: Originals accidentally deleted

**Symptom**: `docs/{project}/communication_hub/messages/` NOT found after archival

**Solution**:

```powershell
# CRITICAL ERROR: Archive operation used MOVE instead of COPY
Write-Host "CRITICAL ERROR: Original Communication Hub messages DELETED!" -ForegroundColor Red

# Restore originals from archive (copy back)
$archivedMessages = Get-ChildItem "$arcCommHubPath/messages" -Filter "*.md"

$archivedMessages | ForEach-Object {
    $filename = $_.Name
    Write-Host "Manual restoration needed: $_" -ForegroundColor Yellow
}

Write-Host "MANUAL FIX REQUIRED: Restore originals from archive to correct date folders" -ForegroundColor Red
```

**Prevention**: ALWAYS use COPY operation (NOT MOVE!)

---

### Problem: README.md NOT populated (template placeholders only)

**Symptom**: README.md contains `{EPIC_ID}`, `{Message count}` placeholders

**Solution**:

```powershell
Write-Host "README.md template NOT populated - manual completion needed" -ForegroundColor Yellow
Write-Host "  1. Load README.md: $readmePath"
Write-Host "  2. Populate Message Registry table (msg-XXX links + metadata)"
Write-Host "  3. Calculate Statistics (category counts, agents involved)"
Write-Host "  4. Add Search Keywords (Epic keywords, key decisions, blockers)"
```

---

## Workflow Completion Checklist

**Communication Hub Archival is complete when:**

- [ ] Archive Communication Hub folder exists and is structured (`messages/`, README.md, archived_at.txt)
- [ ] All Epic-related messages archived (count matches inventory)
- [ ] README.md registry POPULATED (Message Registry, Statistics, Keywords complete)
- [ ] Originals PRESERVED (`docs/{project}/communication_hub/messages/` originals STILL exist!)
- [ ] archived_at.txt exists (timestamp)
- [ ] Validation script PASSED
- [ ] Response Message sent (if Multi-Workspace)

---

## Related Workflows

| Workflow | Purpose | When to Use |
|:---------|:--------|:------------|
| knowledge_steward_context_optimization.workflow.md | Epic Audit + Summary generation | Before Epic archival (summary.md creation) |
| knowledge_steward_multi_workspace.workflow.md | Multi-Workspace Communication Hub protocol | Multi-Workspace deployment (category routing) |

---

## Knowledge Base

**Communication Hub Archival Philosophy**:

- **Snapshot NOT Delete**: Archive = snapshot at Epic closure time (preserve originals!)
- **Separate Concerns**: Epic files ≠ Communication Hub messages (separate workflows!)
- **Message Registry**: README.md comprehensive metadata (category, agents, timeline, key decisions)
- **Cross-reference**: Epic archive ↔ Communication Hub archive (Related Archives links)

**Archive Policy**:

- Communication Hub messages: COPY (preserve originals!) — this workflow
- Originals preservation: Active Communication Hub needed for future statistical analysis

**Multi-Workspace Integration**:

- Category: `communication-hub-archival` (separate from `epic-archival`)
- Prerequisites: Epic is confirmed closed
- Response Message: Communication Hub Archival Complete → Orchestrator
