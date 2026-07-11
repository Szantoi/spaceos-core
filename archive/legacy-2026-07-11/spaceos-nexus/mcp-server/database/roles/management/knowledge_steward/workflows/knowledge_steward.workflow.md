---
id: workflow-knowledge_steward
title: "Knowledge Steward Maintenance Workflow"
description: "Main maintenance workflow for the Knowledge Steward role. Covers epic archival, context optimization, registry updates, structural validation, and reporting."
type: workflow
scope: global
category: maintenance
last_updated: 2026-03-01
---

## Mission: Context Optimization and Structural Maintenance

**Role**: Knowledge Steward
**Goal**: Reduce token usage and maintain structural integrity of the `docs/{project}/epics/` folder without data loss.

---

### Cognitive Setup (Prompt Engineering)

Before starting any task, activate the appropriate **Prompt Engineering Patterns** from `prompt_engineering.knowledge.md`:

#### Core patterns for all tasks

1. **Persona Pattern**: You are the Chief Librarian — minimalist, structured, orderly.
2. **Fact Summary Pattern**: Replace every long text with a concise, factual list.
3. **Context Slicing**: Separate closed (Done) items from the working memory.

#### Task-specific patterns

- **ReACT Pattern**: For structural operations (Reasoning → Acting → Observation cycle)
- **Cognitive Verifier Pattern**: If unclear, generate clarifying questions
- **Fact Check Pattern**: During validation, strictly verify everything
- **Template Pattern**: When creating archive structures, use a template

---

### Maintenance Workflow

## 1. Epic Folder Audit (ReACT Pattern)

**Goal**: Check and maintain `{EPIC_ROOT}/` folders.

### Steps

#### 1.1 Epic Status Check

**Reasoning**: Which Epics and Tasks have been closed?

```markdown
1. Navigate to the `docs/joinerytech-flow/epics/` folder
2. Identify closed Epics (status: Done/Closed/Archived)
3. For each closed Epic, check Task statuses
```

**Acting**: Load the relevant files

- `docs/joinerytech-flow/epics/{EPIC_ID}/epic_plan.md` (Status field)
- `docs/joinerytech-flow/epics/{EPIC_ID}/tasks/{TASK_ID}.md` (Task statuses)
- `docs/joinerytech-flow/epics/{EPIC_ID}/*_report.md` (Implementation/QA reports)

**Observation**: What did you find?

- [ ] Epic closed? (Done)
- [ ] All Tasks closed? (Done)
- [ ] Implementation Report exists?
- [ ] QA Sign-off exists?
- [ ] Architect Sign-off exists?

---

#### 1.2 Critical Information Extraction (Fact Summary Pattern)

**Goal**: From each closed Epic/Task, save ONLY the information NECESSARY for future decision-making.

**Applying Fact Summary Pattern**:

Extract the following from each closed Task:

```markdown
## Context Summary: [TASK-ID] - [Brief Name]

**1. Changes Made (Delta):**
- `path/file.ext`: [Created/Modified/Deleted]
- ...

**2. Critical Decisions (ADR Lite):**
- [Decision]: [Rationale in 1 sentence]

**3. Remaining Risks / Tech Debt:**
- [Description]: [Why it was left this way?]

**4. Lessons Learned:**
- [New rule/pattern identified]
```

**Output**: Create a `summary.md` file in the Epic folder:

- Location: `docs/joinerytech-flow/epics/{EPIC_ID}/summary.md`
- Content: Fact Summary for each Task

---

## 2. Archival (Context Slicing)

**Goal**: Move detailed, no-longer-relevant documents to the `docs/archive/` folder in a structured format.

### 2.1 Archive Structure (Template Pattern)

Archive folder hierarchy:

```
docs/archive/
└── {project_name}/           # e.g. joinerytech-flow
    └── epics/
        └── {EPIC_ID}/        # e.g. EPIC-001_auth-system
            ├── tasks/
            │   ├── {TASK_ID}.md
            │   └── ...
            ├── reports/
            │   ├── implementation_report_{TASK_ID}.md
            │   ├── qa_signoff_{TASK_ID}.md
            │   └── ...
            ├── epic_plan.md
            ├── architect_signoff.md
            └── archived_at.txt    # Archive date
```

### 2.2 Archival Protocol (ReACT Pattern)

**If an Epic:**

- ✅ Status: Done/Closed
- ✅ All Tasks closed (Done)
- ✅ QA Sign-off present
- ✅ Architect Sign-off present
- ✅ `summary.md` created

**Then:**

```powershell
# 1. Create the archive structure
New-Item -ItemType Directory -Path "docs/archive/joinerytech-flow/epics/{EPIC_ID}/tasks" -Force
New-Item -ItemType Directory -Path "docs/archive/joinerytech-flow/epics/{EPIC_ID}/reports" -Force

# 2. Move the files
Move-Item "docs/joinerytech-flow/epics/{EPIC_ID}/tasks/*.md" "docs/archive/joinerytech-flow/epics/{EPIC_ID}/tasks/"
Move-Item "docs/joinerytech-flow/epics/{EPIC_ID}/*_report.md" "docs/archive/joinerytech-flow/epics/{EPIC_ID}/reports/"
Move-Item "docs/joinerytech-flow/epics/{EPIC_ID}/epic_plan.md" "docs/archive/joinerytech-flow/epics/{EPIC_ID}/"
Move-Item "docs/joinerytech-flow/epics/{EPIC_ID}/*_signoff.md" "docs/archive/joinerytech-flow/epics/{EPIC_ID}/"

# 3. Archive timestamp
Get-Date -Format "yyyy-MM-dd HH:mm:ss" | Out-File "docs/archive/joinerytech-flow/epics/{EPIC_ID}/archived_at.txt"

# 4. Keep only summary.md in the working folder!
# Epic folder should look like: docs/joinerytech-flow/epics/{EPIC_ID}/summary.md
```

**⚠️ IMPORTANT**: Always verify that `summary.md` contains all critical decisions before archiving!

---

### 2.3 Communication Hub Message Archival (Multi-Workspace)

**Goal**: When archiving an Epic, structured archival of Communication Hub messages related to the Epic.

**When**: As part of Epic archival (after 2.2), BUT ONLY if Communication Hub is in active deployment.

#### 2.3.1 Message Inventory

**Identify messages related to the archived Epic**:

```powershell
# Search for messages referencing the Epic ID
Get-ChildItem "docs/{project}/communication_hub/messages/" -Recurse -Filter "*.md" |
  Select-String -Pattern "{EPIC_ID}" |
  Select-Object -Property Path -Unique
```

**Manual inventory**:

1. Browse: `docs/{project}/communication_hub/messages/{date}/`
2. Search in frontmatter: `epic_id: {EPIC_ID}` or Epic ID reference in body
3. List message types:
   - Epic Planning Request (orchestrator → architect)
   - Epic Planning Complete (architect → orchestrator)
   - Task Breakdown Request (orchestrator → tech_lead)
   - Task Breakdown Complete (tech_lead → orchestrator)
   - Epic Closure Request (orchestrator → tech_lead)
   - Epic Closure Complete (tech_lead → orchestrator)
   - Architect Sign-off Request (orchestrator → architect)
   - Architect Sign-off Complete (architect → orchestrator)
   - Epic Archival Request (orchestrator → knowledge_steward)
   - Epic Archival Complete (knowledge_steward → orchestrator)

#### 2.3.2 Archive Communication Hub Structure

**Template Pattern**: Create the archive communication hub hierarchy

```
docs/archive/{project}/communication_hub/
└── epics/
    └── {EPIC_ID}/                    # e.g. EPIC-008_persistence-layer
        ├── messages/
        │   ├── msg-001-orchestrator-to-architect.md
        │   ├── msg-002-architect-to-orchestrator.md
        │   ├── msg-003-orchestrator-to-tech_lead.md
        │   └── ...
        ├── README.md                 # Message registry
        └── archived_at.txt           # Archive date
```

#### 2.3.3 Archival Protocol

```powershell
# 1. Create the archive communication hub structure
New-Item -ItemType Directory -Path "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/messages/" -Force

# 2. COPY Epic-related messages (preserve originals)
Get-ChildItem "docs/{project}/communication_hub/messages/" -Recurse -Filter "*.md" |
  Where-Object { (Select-String -Path $_.FullName -Pattern "{EPIC_ID}" -Quiet) } |
  Copy-Item -Destination "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/messages/" -Force

# 3. Create Communication Hub README.md registry
$readmePath = "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md"
# Template: See section 2.3.4

# 4. Archive timestamp
Get-Date -Format "yyyy-MM-dd HH:mm:ss" | Out-File "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/archived_at.txt"
```

**⚠️ IMPORTANT**:

- **DO NOT DELETE** original messages from the Communication Hub (copy only!)
- Active Communication Hub messages are needed for future statistical analysis
- Archive = SNAPSHOT at Epic closure time only

#### 2.3.4 Archive Communication Hub README.md Template

**Template usage**: Use the dedicated template for the registry of archived Communication Hub messages.

**Template file**: archived_communication_hub_readme.template.md

**Quick usage**:

```powershell
# 1. Copy the base template
Copy-Item "src/agent-system/database/roles/management/knowledge_steward/templates/archived_communication_hub_readme.template.md" `
  "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md"

# 2. Placeholder replacement (automated)
(Get-Content "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md") `
  -replace '\{EPIC_ID\}', 'EPIC-008' `
  -replace '\{EPIC_NAME\}', 'Persistence Layer Implementation' `
  -replace '\{YYYY-MM-DD\}', (Get-Date -Format 'yyyy-MM-dd') `
  -replace '\{project\}', 'joinerytech-flow' |
  Set-Content "docs/archive/{project}/communication_hub/epics/{EPIC_ID}/README.md"

# 3. Load the README.md and populate manually:
#    - Message Registry table (msg-XXX links + metadata)
#    - Message Statistics (count by category, agents involved)
#    - Search Keywords (key decisions, blockers resolved)
```

**Template structure** (detailed description in the template file):

- **Message Registry table**: Chronological message list (msg-ID, date, from, to, title, category, status)
- **Message Statistics**: Total messages, category breakdown, agent involvement, timeline
- **Search Keywords**: Epic keywords, message types, agents, key decisions, blockers resolved
- **Archive Structure**: Folder tree documentation
- **Related Archives**: Epic archive, epic summary, active Epic cross-reference

**Template Quality Checklist** (detailed in template file):

- [ ] Message Registry complete (all Epic-related messages listed)
- [ ] Links valid (all message links correct)
- [ ] Statistics accurate (counts calculated)
- [ ] Search keywords comprehensive (Epic keywords + key decisions)
- [ ] Archive structure documented (folder tree)
- [ ] Related archives linked (Epic archive, summary)
- [ ] Timestamp present (archived_at.txt)

---

## 3. Registry & Documentation Update (Fact Check Pattern)

### 3.1 Knowledge Map Update

**Fact Check Pattern**: Strictly verify!

- [ ] Does `src/agent-system/database/standards/core/knowledge_map.md` contain a reference to the archived Epic?
- [ ] Is the `summary.md` listed in the registry?
- [ ] Are there no broken links to archived files?

**If new skills or templates were created during the Epic**:

- Add them to `knowledge_map.md` in the appropriate category
- Update the statistics in skill_structure_management.knowledge.md

### 3.2 Link Integrity (ReACT Pattern)

**Acting**: Search for references to archived files

```powershell
# Search for references in project documentation
Select-String -Path "docs/joinerytech-flow/**/*.md" -Pattern "epics/{EPIC_ID}" -Recurse
```

**Observation**: Are there active references to the archived Epic?

**Reasoning**: If so:

- **Option A**: Update the reference to `docs/archive/...` path
- **Option B**: Replace the reference with `summary.md` (preferred)

---

## 4. Structural Validation (Cognitive Verifier + Fact Check Pattern)

### 4.1 Documentation Quality Gate

Check documentation quality for the archived Epic:

**Cognitive Verifier Pattern**: If unclear, ask!

- [ ] Does `summary.md` contain enough context for future searches?
- [ ] Are archived folder names unambiguous and searchable?
- [ ] Does every archived Epic have an `archived_at.txt`?

**Fact Check Pattern**: Strict verification

- [ ] Are all critical decisions included in the summary?
- [ ] Is tech debt documented?
- [ ] Were lessons learned added to the appropriate skills?

### 4.2 Empty Folder Cleanup

If an Epic folder contains only `summary.md`:

```powershell
# Check if it is truly empty (only summary.md present)
Get-ChildItem "docs/joinerytech-flow/epics/{EPIC_ID}/" -Recurse

# If only summary.md is present — that is correct. That is the goal!
```

**DO NOT DELETE** the Epic folder — `summary.md` must remain there!

---

## 5. Log & State Cleanup

### 5.1 Log Purge

- Delete logs older than 30 days from `src/agent-system/database/standards/core/logs/`
- Exception: Production incident logs (keep for 1 year)

### 5.2 State Update

If there is a `state.md` in the project root:

- Remove closed Epic/Task statuses
- Keep only active work

---

## 6. Reporting (Fact Summary + Audience Pattern)

### 6.1 Metrics Report (Audience: Orchestrator)

**Fact Summary Pattern**: Concise, measurable report

```markdown
## Knowledge Steward - Maintenance Report

**Date:** 2026-02-15
**Epic:** {EPIC_ID} - {Name}

### Actions Executed:
- {N} Tasks archived
- {M} Reports archived
- Summary.md created ({X} KB)
- Registry updated
- Communication Hub messages archived ({K} messages) - **If Multi-Workspace active**

### Token Savings:
- **Before:** ~{Y} tokens (full Epic content)
- **After:** ~{Z} tokens (summary only)
- **Savings:** ~{Y-Z} tokens ({percentage}%)

### Structural Changes:
- Archive folder: `docs/archive/joinerytech-flow/epics/{EPIC_ID}/`
- Active folder: `docs/joinerytech-flow/epics/{EPIC_ID}/summary.md`
- **Communication Hub Archive**: `docs/archive/joinerytech-flow/communication_hub/epics/{EPIC_ID}/` - **If Multi-Workspace active**

### Next Steps:
- [ ] Architect review (if new pattern/skill merge needed)
- [ ] Orchestrator: Update Epic status in tracking system
```

---

## Workflow Checklist

Verify at the end of the complete workflow:

### Context Hygiene

- [ ] Has context size decreased?
- [ ] No "dead" information in working folders?
- [ ] Only summary.md remains in the Epic folder?

### Archive Integrity

- [ ] Archive folder structure is correct?
- [ ] All critical files present in archive?
- [ ] `archived_at.txt` timestamp present?

### Documentation Quality

- [ ] Does summary.md contain all critical decisions?
- [ ] knowledge_map.md updated?
- [ ] No broken links?

### Structural Compliance

- [ ] Archive folder hierarchy follows the Template Pattern?
- [ ] File names consistent?
- [ ] No duplicate files?

---

## Universal Communication Prompt

The Knowledge Steward communicates with all agents using a **universal documentation request prompt**. This prompt is **parameterizable in 4 types**:

### Prompt to use

- **P18**: documentation_request.message.md
- **Types** (parameter: `{request_type}`):
  1. **Epic Archival** (`EPIC_ARCHIVE`) — archival request to Architect after Epic closure
  2. **Task Documentation** (`TASK_DOC`) — documentation request to Developer after Task implementation
  3. **Context Cleanup** (`CONTEXT_CLEANUP`) — context cleanup request to Orchestrator
  4. **Skill Calibration** (`SKILL_CALIBRATION`) — skill update proposal request to Tech Lead/Architect

### Parameters

- `{request_type}`: See above (EPIC_ARCHIVE, TASK_DOC, CONTEXT_CLEANUP, SKILL_CALIBRATION)
- `{target_agent}`: Target agent (Orchestrator, Tech Lead, Backend Dev, Frontend Dev, QA, Architect)
- `{EPIC_ID}` or `{TASK_ID}`: Relevant identifier
- `{project}`: Project name
- `{scope}`: Scope description (optional)

---

## Workflow Triggers

When to start this workflow:

1. **After Epic closure**: After Architect Sign-off is received
2. **Token saturation**: If context usage > 50%
3. **Regular maintenance**: Once per week (Fridays)
4. **Orchestrator request**: If explicit request is received

---

*Start with Epic status check (step 1.1) and follow the ReACT Pattern for every operation: Reasoning → Acting → Observation.*
