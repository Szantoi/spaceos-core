---
id: workflow-knowledge_steward-multi-workspace
title: "Knowledge Steward Multi-Workspace Communication Protocol"
description: "Load this workflow ONLY in Multi-Workspace deployments. Handles Communication Hub message reading, processing, and sending responses for the Knowledge Steward role."
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-03-01
---

# Knowledge Steward Multi-Workspace Workflow

**Role**: Knowledge Steward (Chief Librarian)
**Scope**: Load this file ONLY in Multi-Workspace deployment
**Purpose**: Reading, processing, and responding to Communication Hub messages

---

## When to use this workflow?

**Governed by Runbook:** The `knowledge_steward.runbook.md` "Multi-Workspace Detection" section determines when to load this file.

**Indicator**: If `docs/{project}/communication_hub/` folder exists → Multi-Workspace mode is active

---

## 1. Inbox Check Protocol

### 1.1 Startup - Check Pending Messages

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending messages (priority sorted)
Get-PendingMessages -Role "knowledge_steward" | Format-Table

# Filter by critical priority (archival, context optimization)
Get-PendingMessages -Role "knowledge_steward" -HighPriorityOnly | Format-Table
```

**Manual check** (if PowerShell helper is not available):

1. Load: `docs/{project}/communication_hub/knowledge_steward_inbox.md`
2. Search: `status: ⏳ Pending` messages (link-based table)
3. **Priority sorting**: CRITICAL > HIGH > NORMAL > LOW
4. **FIFO within priority**: Oldest message first within the same priority level

---

## 2. Message Processing

### 2.1 Read message

```powershell
# Read specific message
Read-Message -MessageId "msg-008"
```

**Manual reading**:

1. Click the link in the "File" column of the inbox table
2. Load the message file: `messages/{date}/msg-{id}-{from}-to-{to}.md`
3. Check the frontmatter fields:
   - `from`: Who sent the message (usually orchestrator)
   - `priority`: critical | high | normal | low
   - `category`: Message type (epic-archival, calibration, context-optimization, etc.)
   - `reply_to`: If this is a reply, the original message ID

### 2.2 Load Context Files

Load files referenced in the message body:

- Epic archival request: `docs/{project}/epics/{EPIC}/` (entire folder)
- Calibration request: `epic_review.md` calibration section, `dod_rule.md`, standards files
- Context optimization request: Project-wide documentation inventory

### 2.3 Execute Task

Choose workflow based on the message category:

| Category | Workflow File | Task |
|:---------|:--------------|:-----|
| `communication-hub-archival` | knowledge_steward_communication_hub_archival.workflow.md | Communication Hub messages archival ONLY (Epic-related messages COPY to archive, README.md registry) |
| `context-optimization` | knowledge_steward_context_optimization.workflow.md | Token reduction (Epic audit, summary.md creation, Knowledge Map update, log purge, state.md pruning) |
| `calibration` | knowledge_steward_calibration.workflow.md | Calibration processing (dod_rule.md, standards update, template refinement) |
| `structure-maintenance` | knowledge_steward_structure_maintenance.workflow.md | Documentation structure validation, broken links fix |

---

## 3. Response Creation

**CRITICAL: Template Completeness**

**ALWAYS use the FULL structure from Section 4 (Common Message Templates)**:

- **DO NOT shorten**: Every template section is required (Deliverables, Context Reduction Metrics, Archive Policy, Next Steps)
- **DO NOT simplify**: Detailed information (token reduction %, file counts, archive structure) is REQUIRED
- **Follow the PowerShell example**: The template PowerShell body (`@"..."`) contains the FULL structure
- **DO NOT write only a summary**: "Archival complete" is NOT enough — detailed breakdown required

**Why this matters**: The Orchestrator validates archival success based on detailed metrics (e.g., Context Reduction → token budget management).

---

### 3.1 Create Response Message

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Epic Archival Complete - {EPIC_ID}" `
  -Body "Epic archival completed. Deliverables: archived/ folder, ARCHIVED.md markers, state.md updated. Context reduction: {X} tokens ({Y}%)" `
  -Priority "normal" `
  -ReplyTo "msg-008" `
  -Category "epic-archival-complete"
```

**Manual response creation**:

1. **Create file**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-knowledge_steward-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: knowledge_steward
to: {recipient}
reply_to: {original-msg-id}
priority: normal
status: pending
category: {category}
thread_id: {original-thread-id}
---

## Message Title

### Summary
{Brief summary of the executed maintenance task}

### Deliverables
- [ ] {Archival/Calibration/Optimization deliverable 1}
- [ ] {Deliverable 2}

### Metrics
- Token reduction: {X} tokens ({Y}%)
- Files archived/optimized: {count}

### Next Steps
{Who should do what next}

### Context
- **Original Request**: Link to [{original-msg-id}](../{date}/{original-msg-id}.md)
- **Epic/Project**: docs/{project}/epics/{EPIC}/
```

### 3.2 Inbox Status Update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-008" -NewStatus "completed"
```

**Manual inbox update**:

1. Load: `docs/{project}/communication_hub/knowledge_steward_inbox.md`
2. Find the message in the "Pending Messages" table
3. Change the status column: `⏳ Pending` → `✅ Completed`

---

## 4. Common Message Templates

### 4.1 Epic Archival Complete (→ Orchestrator)

**Scenario**: Epic archival (Phase 1) completed

**CRITICAL: ALL sections of Template 4.1 are REQUIRED**

Use the **full PowerShell body structure** — every section below is REQUIRED:

- ✅ **Deliverables**: Epic folders (EACH Epic: file_count + size KB!), archived/README.md, ARCHIVED.md markers, state.md update, **Communication Hub messages** (EACH Epic: N messages count!)
- ✅ **Context Reduction Metrics**: Token reduction, Active context before/after, Files archived (Epic files + CH messages), Total archived
- ✅ **Archive Policy**: Originals preserved (read-only), Archived copies (long-term), No data loss
- ✅ **Next Steps**: Orchestrator validation, Project team access
- ✅ **Files Modified**: Created files (archived/README.md, ARCHIVED.md paths), Updated files (state.md)
- ✅ **Context**: Original Request (msg-ID), Validation Report (path)

**Why Deliverables detail matters (file_count + KB)**: Orchestrator validates archival completeness and sees exact token savings.

**Why Communication Hub messages count matters**: Orchestrator sees Multi-Workspace archival scope (Section 2.3 capability validation).

**Why Archive Policy matters**: Orchestrator/Project team knows originals were preserved (read-only historical reference).

**Why Files Modified matters**: Orchestrator validates artifact creation based on explicit file path list.

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Epic Archival Complete - Phase 1" `
  -Body @"
Epic archival completed for Epic 07, 08, 11 (Phase 1 - CRITICAL priority task).

**Deliverables**:
- archived/ folder created: docs/{project}/archived/
- Epic folders copied:
  - Epic 07: docs/{project}/archived/epic_07/ ({file_count} files, {size} KB)
  - Epic 08: docs/{project}/archived/epic_08/ ({file_count} files, {size} KB)
  - Epic 11: docs/{project}/archived/epic_11/ ({file_count} files, {size} KB)
- archived/README.md created (registry table with closure/archive dates)
- ARCHIVED.md markers added to original Epic folders (3 files)
- state.md updated (Epic State Map: "⏳ Archival Pending" → "✅ Archived 2026-02-17")
- **Communication Hub messages archived** (Multi-Workspace):
  - Epic 07: {N} messages archived to docs/{project}/archived/communication_hub/epics/epic_07/
  - Epic 08: {M} messages archived to docs/{project}/archived/communication_hub/epics/epic_08/
  - Epic 11: {K} messages archived to docs/{project}/archived/communication_hub/epics/epic_11/
  - Archive registry: docs/{project}/archived/communication_hub/epics/{EPIC_ID}/README.md

**Context Reduction Metrics**:
- Token reduction: ~33,000 tokens (27% of active context)
- Active context before: 123 KB → After: 90 KB
- Files archived: 33 Epic files (~110 KB) + {X} Communication Hub messages (~{Y} KB)
- **Total archived**: {33 + X} files (~{110 + Y} KB)

**Archive Policy**:
- Originals preserved in epics/ for historical reference (read-only)
- Archived copies in archived/ for long-term storage
- No data loss, full Epic documentation retained

**Next Steps**:
- Orchestrator: Validate state.md Epic State Map update
- Project team: Access archived Epics via archived/README.md registry

**Files Modified**:
- Created: docs/{project}/archived/README.md
- Created: docs/{project}/epics/epic_07/ARCHIVED.md (+ epic_08, epic_11)
- Updated: docs/{project}/state.md (Epic State Map, Change Log)

**Context**:
- Original Request: msg-002 (Orchestrator Phase 1 Archival dispatch)
- Validation Report: docs/{project}/reports/EPIC_CLOSURE_VALIDATION_REPORT.md
"@ `
  -Priority "high" `
  -ReplyTo "msg-002" `
  -Category "epic-archival-complete"
```

**Anti-Pattern Example (WRONG)**:

```markdown
## Epic Archival Complete

### 1. Structure & Copy
- [x] Archive directory confirmed
- [x] EPIC-07 copied to archived/epics/epic_07/

### 2. Context Reduction
- Reduction: 27%
- Tokens: ~33,000
```

❌ **Why this is WRONG**:

- Deliverables: File count + KB MISSING for each Epic (format: 20 files, 27 KB required!)
- Deliverables: Communication Hub message count MISSING (5 messages for Epic 07, etc.)
- Archive Policy section COMPLETELY MISSING (Originals preserved? No data loss?)
- Files Modified section COMPLETELY MISSING (Created/Updated file paths explicit list)
- Context section MISSING in body (Original Request msg-002, Validation Report path)
- Only checkbox list → detailed metrics breakdown (file count, KB, message count REQUIRED!)

### 4.2 Calibration Processing Complete (→ Orchestrator / Tech Lead)

**Scenario**: Calibration instructions processed (based on epic_review.md)

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Calibration Processing Complete - {EPIC_ID}" `
  -Body @"
Calibration processing completed for Epic {EPIC_ID}.

**Deliverables**:

**Global Skills Updated**:
1. docs/roles/core/dod_rule.md:
   - Added "Persistence Epic Requirements" section (5-point checklist)
   - Migration validation criteria documented

**Standards Created**:
1. docs/{project}/standards/database_migration_standard.md:
   - Migration naming convention defined
   - Migration workflow documented (6 steps)
   - Database policy established (.gitignore, local DB handling)

**Templates Refined**:
1. docs/roles/tech_lead/templates/epic_review.template.md:
   - Added "Calibration Instructions" section (for Knowledge Steward processing)
   - Subsections: Global Skills to Update, Standards to Create, Templates to Refine

**Calibration Source**: docs/{project}/epics/{EPIC}/epic_review.md (Section 6: Calibration Instructions)

**Knowledge Base Impact**:
- New global skill rules: 1 (Persistence Epic DoD)
- New standards: 1 (Database Migration Standard)
- Template improvements: 1 (Epic Review Template)

**Next Steps**:
- Orchestrator: Validate calibration changes in skill/standard files
- Tech Lead: Review updated dod_rule.md for future Epic planning
- Architect: Review database_migration_standard.md for architecture compliance

**Files Modified**:
- Updated: docs/roles/core/dod_rule.md
- Created: docs/{project}/standards/database_migration_standard.md
- Updated: docs/roles/tech_lead/templates/epic_review.template.md

**Context**:
- Original Request: {calibration-msg-id} (Orchestrator/Tech Lead calibration dispatch)
- Epic Review: docs/{project}/epics/{EPIC}/epic_review.md
"@ `
  -Priority "normal" `
  -ReplyTo "{calibration-msg-id}" `
  -Category "calibration-complete"
```

### 4.3 Context Optimization Complete (→ Orchestrator)

**Scenario**: Project-wide context optimization completed

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Context Optimization Complete - {PROJECT}" `
  -Body @"
Context optimization completed for {PROJECT}.

**Optimization Actions**:

**File Consolidation**:
- Consolidated {N} redundant files into {M} consolidated files
- Examples:
  - Multiple decision_log fragments → Single decision_log.md
  - Scattered learnings → Consolidated learnings.md per Epic

**Token Reduction**:
- Before: {X} tokens ({Y} KB active context)
- After: {A} tokens ({B} KB active context)
- Reduction: {C} tokens ({D}% decrease)

**Structure Improvements**:
- Fixed {N} broken cross-references
- Standardized {M} file naming inconsistencies
- Updated {K} outdated frontmatter metadata

**Archive Recommendations**:
- Suggested archival: {list Epic IDs or documentation folders}
- Estimated additional token reduction: {X} tokens ({Y}%)

**Documentation Health Score**:
- Before: {X}/100
- After: {Y}/100
- Improvement: +{Z} points

**Next Steps**:
- Orchestrator: Review optimization changes for validation
- Project team: Access optimized documentation structure
- (Optional) Archive additional completed Epics as recommended

**Files Modified**:
{List of consolidated/modified files}

**Context**:
- Original Request: {optimization-msg-id} (Orchestrator context optimization dispatch)
- Optimization report: docs/{project}/reports/context_optimization_report.md
"@ `
  -Priority "normal" `
  -ReplyTo "{optimization-msg-id}" `
  -Category "context-optimization-complete"
```

### 4.4 Structure Validation Complete (→ Orchestrator)

**Scenario**: Documentation structure validation and maintenance completed

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Structure Validation Complete - {PROJECT}" `
  -Body @"
Documentation structure validation completed for {PROJECT}.

**Validation Results**:

**Broken Links Fixed**: {N}
- Epic cross-references: {count} fixed
- ADR references: {count} fixed
- Task dependency links: {count} fixed

**Missing Mandatory Files Created**: {M}
- Epic {ID}: Missing plan.md → Created template
- Epic {ID}: Missing backlog.md → Created template

**Frontmatter Validation**:
- Files checked: {count}
- Invalid frontmatter fixed: {count}
- Missing required fields added: {count}

**Naming Convention Issues Fixed**: {K}
- Task files renamed: {count} (TASK-{EPIC}-{N} format enforced)
- Epic folders normalized: {count} (epic_{N} format enforced)

**Structure Compliance Score**:
- Before: {X}/100
- After: {Y}/100
- Improvement: +{Z} points

**Remaining Issues** (if any):
- [ ] {Issue 1}: {Description, recommended action}
- [ ] {Issue 2}: {Description, recommended action}

**Next Steps**:
- Orchestrator: Review validation report for remaining issues
- Project team: Follow updated structure guidelines
- (Optional) Schedule periodic structure validation (monthly)

**Files Modified**:
{List of files with broken links fixed, frontmatter updated, etc.}

**Context**:
- Original Request: {validation-msg-id} (Orchestrator structure validation dispatch)
- Validation report: docs/{project}/reports/structure_validation_report.md
"@ `
  -Priority "normal" `
  -ReplyTo "{validation-msg-id}" `
  -Category "structure-validation-complete"
```

---

## 5. Workflow Integration Points

### 5.1 Epic Archival (Orchestrator→Knowledge Steward)

**Incoming Message**: `epic-archival` (from Orchestrator - Phase 1 CRITICAL)

**Actions**:

1. Load: Epic folders to archive
2. Execute:
   - knowledge_steward_communication_hub_archival.workflow.md — Epic-related messages archival (**Multi-Workspace only**)
3. Deliverables:
   - archived/ folder (Epic files)
   - archived/communication_hub/ folder (Epic-related messages) — **Multi-Workspace only**
   - ARCHIVED.md markers
   - state.md update
4. Response: [Template 4.1](#41-epic-archival-complete--orchestrator) (includes Communication Hub archival metrics)

### 5.2 Calibration Processing (Orchestrator/Tech Lead→Knowledge Steward)

**Incoming Message**: `calibration` (from Orchestrator/Tech Lead after Epic closure)

**Actions**:

1. Load: epic_review.md calibration section
2. Execute: knowledge_steward_calibration.workflow.md
3. Deliverables: dod_rule.md update, new standards, template refinements
4. Response: [Template 4.2](#42-calibration-processing-complete--orchestrator--tech-lead)

### 5.3 Context Optimization (Orchestrator→Knowledge Steward)

**Incoming Message**: `context-optimization` (from Orchestrator - periodic maintenance)

**Actions**:

1. Load: Project-wide documentation inventory
2. Execute: knowledge_steward_context_optimization.workflow.md
3. Deliverables: Optimized documentation structure, context_optimization_report.md
4. Response: [Template 4.3](#43-context-optimization-complete--orchestrator)

### 5.4 Structure Validation (Orchestrator→Knowledge Steward)

**Incoming Message**: `structure-maintenance` (from Orchestrator - periodic audit)

**Actions**:

1. Load: Project documentation structure
2. Execute: knowledge_steward_structure_maintenance.workflow.md
3. Deliverables: Broken links fixed, missing files created, structure_validation_report.md
4. Response: [Template 4.4](#44-structure-validation-complete--orchestrator)

---

## PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Check inbox (priority sorted - CRITICAL archival tasks first)
Get-PendingMessages -Role "knowledge_steward" | Format-Table

# Critical priority only (archival, context blocker removal)
Get-PendingMessages -Role "knowledge_steward" -HighPriorityOnly | Format-Table

# Read message
Read-Message -MessageId "msg-002"

# Send response
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Epic Archival Complete" `
  -Body "..." `
  -Priority "high" `
  -ReplyTo "msg-002" `
  -Category "epic-archival-complete"

# Update status
Update-MessageStatus -MessageId "msg-002" -NewStatus "completed"

# Get statistics
Get-MessageStatistics
```

---

**Note**: If Multi-Workspace mode is NOT active, do NOT load this file. The standard knowledge steward workflow files are sufficient in single-workspace mode.
