---
id: workflow-orchestrator-multi-workspace
title: "Orchestrator Multi-Workspace Communication Protocol"
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-02-23
---

# ?? Orchestrator Multi-Workspace Workflow

**Szerepkör**: Orchestrator (Project Coordinator)
**Scope**: Csak Multi-Workspace deployment esetén töltsd be ezt a fájlt
**Célja**: Communication Hub üzenetek küldése és válaszok monitorozása

> ?? **v2.0 Messaging Convention (2026-02-23):**
> A `communication_hub/*_inbox.md` fájlokba való hozzáfûzés **elavult** (Race Condition kockázat).
> Az új konvenció: `messages/<cél-role>/<timestamp>_from-<feladó>_<tárgy>.md`
> Részletek: [messaging_v2_standard.md](../../core/messaging_v2_standard.md)

---

## ?? Mikor használd ezt a workflow-t?

**Runbook szabályozza:** A `orchestrator.runbook.md` "Multi-Workspace Detection" szekciója határozza meg, hogy mikor kell betölteni ezt a fájlt.

**Indicator**: Ha van `docs/{project}/communication_hub/` mappa › Multi-Workspace mod aktív

---

## ?? 1. Inbox Check Protocol (Optional - Orchestrator is Sender)

### 1.1 Response Monitoring - Ágens válaszok ellenõrzése

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending responses (messages sent by orchestrator, waiting for agent response)
Get-PendingMessages -Role "orchestrator" | Format-Table

# Check specific agent responses
Get-PendingMessages -Role "architect" | Where-Object { $_.Status -eq "completed" }
```

**Manuális ellenõrzés** (ha PowerShell helper nem elérhetõ):

1. Töltsd be: `docs/{project}/communication_hub/orchestrator_inbox.md`
2. Keress: Ágens response üzeneteket (from: architect/tech_lead/developer/qa/knowledge_steward)
3. Ellenõrizd a task végrehajtás státuszát

---

## ?? 2. Message Dispatching (Orchestrator Primary Role)

**?? KRITIKUS: Template Részletesség**

**MINDIG használd a Section 3 (Common Dispatch Message Templates) TELJES struktúráját**:

- ? **NE rövidíts**: Minden template szakasz kötelezõ (Epic Goal, Required Deliverables, Context Files, Success Criteria)
- ? **NE egyszerûsíts**: Részletes információk (file paths, specific deliverable content) KÖTELEZÕEK
- ? **Kövesd a PowerShell példát**: A template PowerShell body (@"...") tartalmazza a TELJES struktúrát
- ? **NE csak összefoglalót írj**: "Review Epic 08" NEM elég - részletes breakdown szükséges

**Miért fontos**: Az ágensek a részletes instrukcióból dolgoznak (pl. Required Deliverables › pontos artifact lista).

---

### Template Selection Guide

**Message category alapján válaszd ki a megfelelõ template-et (Section 3)**:

| Message Scenario           | Target Agent          | Template            | Template ID |
|----------------------------|-----------------------|---------------------|-------------|
| Epic Planning              | Architect             | Epic Planning Request | Template 3.1|
| Task Breakdown             | Tech Lead             | Task Breakdown Request| Template 3.2|
| Epic Closure               | Tech Lead             | Epic Closure Request  | Template 3.3|
| Architect Sign-off         | Architect             | Architect Sign-off Request | Template 3.4|
| Epic Archival              | Knowledge Steward     | Epic Archival Request | Template 3.5|
| Task Assignment            | Backend/Frontend Dev  | Task Assignment       | Template 3.6|
| Epic Planning Critique     | Devils Advocate       | Epic Planning Review Request | Template 3.7|
| Task Planning Critique     | Devils Advocate       | Task Planning Review Request | Template 3.8|
| ADR Critique               | Devils Advocate       | ADR Review Request    | Template 3.9|

**KRITIKUS: Használd a template Section 3-ból TELJES struktúráját**:

- ? **NE találd ki saját struktúrát**: Kövesd a PowerShell body (@"...") TELJES tartalmát
- ? **MINDEN szakasz kötelezõ**: Required Deliverables, Context Files, Success Criteria, Next Steps
- ? **Részletes breakdown**: File paths, specific deliverable content, acceptance criteria

**Anti-Pattern Example (ROSSZ)**:

```markdown
## Epic Archival Required

Archive Epic 07, 08, 11. Create archived/ folder, copy files, update state.md.

Deadline: 2026-02-17 EOD.
```

? **Miért ROSSZ**:

- Archive Policy hiányzik (Copy vagy Move? Knowledge Steward nem tudja!)
- Success Criteria checklist hiányzik (mi a pontos acceptance criteria?)
- Context Files hiányoznak (Epic folder paths?)
- Expected Context Reduction hiányzik (miért sürgõs?)

---

### 2.1 Üzenet készítése és küldése

```powershell
# Dispatch Epic Planning request to Architect
New-Message -From "orchestrator" -To "architect" `
  -Title "Epic Planning Request - {EPIC_ID}" `
  -Body @"
Epic planning required for {EPIC_ID}.

**Context**:
- Goal: {1-2 sentence description}
- Priority: {High/Normal/Low}
- Deadline: {YYYY-MM-DD}

**Required Deliverables**:
- Epic plan.md (goal, scope, DoD, constraints)
- ADR-XXX: {expected architectural decisions}
- dependency_map.md update

**Context Files**:
- Project Goal: docs/{project}/goal.md
- Related Epic-ek: {list Epic IDs with dependencies}

**Next Steps**:
- Architect: Create Epic plan and ADR files
- Estimated duration: {X} days
"@ `
  -Priority "high" `
  -Category "epic-planning"
```

**Manuális message készítés**:

1. **Fájl létrehozás**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-orchestrator-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: orchestrator
to: {recipient}
priority: critical | high | normal | low
status: pending
category: {category}
thread_id: {thread-id if reply, else new UUID}
---

## Message Title

### Context
{Üzenet kontextusa, miért küldjük}

### Required Task
{Mit kell elvégezni}

### Deliverables
- [ ] {Deliverable 1}
- [ ] {Deliverable 2}

### Context Files
- {File path 1}
- {File path 2}

### Deadline
{YYYY-MM-DD} (if applicable)

### Next Steps
{Ki mit csináljon}
```

### 2.2 Inbox update (recipient inbox)

```powershell
# Automatically handled by New-Message helper
# Manual: Add message link to recipient inbox table
```

**Manuális inbox update**:

1. Töltsd be: `docs/{project}/communication_hub/{recipient}_inbox.md`
2. Adj hozzá új sort a "Pending Messages" táblázathoz:

```markdown
| msg-{id} | {timestamp} | Orchestrator | {priority} | {category} | ? Pending | [msg-{id}](../messages/{date}/msg-{id}-orchestrator-to-{recipient}.md) |
```

---

## ?? 3. Common Dispatch Message Templates

### 3.1 Epic Planning Request (› Architect)

**Scenario**: Új Epic planning indítása

**?? KRITIKUS: Template 3.1 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Epic Goal**: 1-2 sentence description (backlog-ból)
- ? **Context**: Priority, Target Milestone, Estimated complexity, Deadline
- ? **Dependencies**: Depends on (Epic IDs), Blocks (Epic IDs)
- ? **Required Deliverables**: Epic plan.md (content breakdown!), ADR-XXX (2-3 expected areas), dependency_map.md
- ? **Context Files to Load**: Project Goal path, Related Epic paths, Existing ADR paths
- ? **Next Steps**: Architect actions, Estimated duration, Orchestrator next action
- ? **Success Criteria**: [ ] Checklist (4 criteria: plan.md, ADR, dependency_map, Tech Lead ready)

**Miért fontos Required Deliverables content breakdown**: Architect pontosan tudja Epic plan.md tartalmi követelményeit (goal, scope, DoD, constraints, alternatives).

**Miért fontos Context Files paths**: Architect betölti context-et planning elõtt (Project Goal, Related Epics, ADRs).

**Miért fontos Success Criteria checklist**: Architect validálja completion-t ([ ] format explicit).

```powershell
New-Message -From "orchestrator" -To "architect" `
  -Title "Epic Planning Request - {EPIC_ID}: {EPIC_NAME}" `
  -Body @"
Epic planning required for {EPIC_ID}: {EPIC_NAME}.

**Epic Goal**: {1-2 sentence description from backlog}

**Context**:
- Priority: {High/Normal/Low}
- Target Milestone: {Milestone name}
- Estimated complexity: {Small/Medium/Large}
- Deadline: {YYYY-MM-DD}

**Dependencies**:
- Depends on: {Epic X, Epic Y} (if applicable)
- Blocks: {Epic Z} (if applicable)

**Required Deliverables**:
- Epic plan.md (goal, scope, DoD, constraints, alternatives analysis)
- ADR-XXX: {expected architectural decisions - list 2-3 areas}
- dependency_map.md update (Epic dependencies)

**Context Files to Load**:
- Project Goal: docs/{project}/goal.md
- Related Epic-ek: {list paths to related Epic plans}
- Existing ADRs: docs/{project}/decisions/ADR-*.md

**Next Steps**:
- Architect: Review context, create Epic plan and ADR files
- Architect: Update dependency_map.md with Epic dependencies
- Architect: Send completion message to Orchestrator
- Estimated duration: {2-3} days

**Success Criteria**:
- [ ] Epic plan.md created with clear DoD
- [ ] At least 1 ADR documented (architectural decision)
- [ ] dependency_map.md updated
- [ ] Tech Lead ready to start task breakdown
"@ `
  -Priority "high" `
  -Category "epic-planning"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
## Epic Planning Request

**Epic Goal**: Implement new feature X.

**Required Deliverables**:
- Epic plan
- ADRs
- Dependency map

**Next Steps**:
- Architect: Create Epic plan
```

? **Miért ROSSZ**:

- Context szakasz TELJESEN HIÁNYZIK (Priority? Target Milestone? Complexity? Deadline?)
- Dependencies szakasz TELJESEN HIÁNYZIK (Depends on? Blocks?)
- Required Deliverables: Epic plan.md content breakdown HIÁNYZIK (goal, scope, DoD, constraints, alternatives analysis)
- Required Deliverables: ADR-XXX expected areas HIÁNYZIK (2-3 architectural decision areas)
- Context Files to Load szakasz TELJESEN HIÁNYZIK (Project Goal path, Related Epic paths, ADR paths)
- Success Criteria szakasz TELJESEN HIÁNYZIK ([ ] checklist 4 kritériummal)
- Estimated duration HIÁNYZIK (Architect nem tudja mennyi idõ áll rendelkezésre)

### 3.2 Task Breakdown Request (› Tech Lead)

**Scenario**: Epic planning complete, task breakdown szükséges

**?? KRITIKUS: Template 3.2 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Epic Context**: Epic Goal (brief summary), Epic Scope (1-2 sentence), Key ADRs (ADR IDs + titles)
- ? **Required Deliverables**: Task plans count estimate, backlog.md update, Task distribution (Backend/Frontend/Integration/QA)
- ? **Context Files to Load**: Epic Plan path, ADR Files paths (minden ADR!), Related Epic task plans paths
- ? **Task Plan Requirements**: Each task content (Goal, Context, Steps, Skills, DoD, QA), Dependency order, Estimated effort
- ? **Next Steps**: Tech Lead actions, Orchestrator next action, Estimated duration
- ? **Success Criteria**: [ ] Checklist (4 criteria: tasks created, backlog updated, dependencies documented, developers ready)

**Miért fontos Epic Context (Key ADRs)**: Tech Lead figyelembe veszi architectural constraints-t task breakdown során.

**Miért fontos Task Plan Requirements (Each task content)**: Tech Lead pontosan tudja task plan struktúrát (Goal, Context, Steps, Skills, DoD, QA).

**Miért fontos Success Criteria checklist**: Tech Lead validálja completion-t ([ ] format explicit).

```powershell
New-Message -From "orchestrator" -To "tech_lead" `
  -Title "Task Breakdown Request - {EPIC_ID}" `
  -Body @"
Task breakdown required for {EPIC_ID} (Epic planning completed by Architect).

**Epic Context**:
- Epic Goal: {brief summary from plan.md}
- Epic Scope: {1-2 sentence scope description}
- Key ADRs: {list ADR IDs with brief titles}

**Required Deliverables**:
- {Estimated N} task plans: docs/{project}/epics/{EPIC}/tasks/TASK-{EPIC}-*.md
- backlog.md updated (all tasks in 'Todo' status)
- Task distribution: Backend/Frontend/Integration/QA tasks

**Context Files to Load**:
- Epic Plan: docs/{project}/epics/{EPIC}/plan.md
- ADR Files: docs/{project}/decisions/ADR-XXX-*.md (referenced in plan)
- Related Epic task plans: {paths to similar Epic tasks for reference}

**Task Plan Requirements**:
- Each task: Goal, Context, Implementation Steps, Skills, DoD, QA scope
- Dependency order documented (which tasks block others)
- Estimated effort per task (hours/days)

**Next Steps**:
- Tech Lead: Create task breakdown (tasks/*.md files)
- Tech Lead: Update backlog.md with task status table
- Tech Lead: Send completion message to Orchestrator
- Orchestrator: Assign tasks to Backend/Frontend Developers
- Estimated duration: {1-2} days

**Success Criteria**:
- [ ] All tasks created with detailed plans
- [ ] backlog.md updated with task status
- [ ] Task dependencies documented
- [ ] Backend/Frontend developers ready to start implementation
"@ `
  -Priority "high" `
  -Category "task-breakdown"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
## Task Breakdown Request

**Epic**: Epic 10

**Required Deliverables**:
- Task plans
- Backlog update

**Next Steps**:
- Tech Lead: Create tasks
```

? **Miért ROSSZ**:

- Epic Context szakasz TELJESEN HIÁNYZIK (Epic Goal summary? Epic Scope? Key ADRs?)
- Required Deliverables: Task plans count estimate HIÁNYZIK (hány task várható?)
- Required Deliverables: Task distribution HIÁNYZIK (Backend/Frontend/Integration/QA breakdown)
- Context Files to Load szakasz TELJESEN HIÁNYZIK (Epic Plan path, ADR paths, Related task plans)
- Task Plan Requirements szakasz TELJESEN HIÁNYZIK (Each task content, Dependency order, Estimated effort)
- Success Criteria szakasz TELJESEN HIÁNYZIK ([ ] checklist 4 kritériummal)
- Estimated duration HIÁNYZIK

### 3.3 Epic Closure Request (› Tech Lead)

**Scenario**: Minden task complete, Epic closure szükséges

**?? KRITIKUS: Template 3.3 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Epic Status**: Tasks completed (N/N format), CI/CD status, QA status
- ? **Required Deliverables**: 3 files (tech_lead_signoff.md, qa_signoff.md, epic_review.md) - mindhárom content breakdown!
- ? **Context Files to Load**: Epic Plan path, Task Plans paths, Implementation Summaries paths, QA Reports paths
- ? **Closure Document Content**: tech_lead_signoff.md (4 sections), qa_signoff.md (3 sections), epic_review.md (content breakdown)
- ? **Calibration Instructions**: What to update (dod_rule, standards, templates explicit)
- ? **Next Steps**: Tech Lead actions, Orchestrator next action (Architect sign-off), Knowledge Steward action, Estimated duration
- ? **Success Criteria**: [ ] Checklist (4 criteria: 3 files created, Architect ready)

**Miért fontos Required Deliverables (3 files content breakdown)**: Tech Lead pontosan tudja minden file tartalmi követelményét.

**Miért fontos Calibration Instructions**: Tech Lead tudja epic_review.md Section 6 tartalmát (dod_rule, standards, templates explicit felsorolás).

**Miért fontos Success Criteria checklist**: Tech Lead validálja completion-t ([ ] format explicit, 3 file + Architect ready).

```powershell
New-Message -From "orchestrator" -To "tech_lead" `
  -Title "Epic Closure Request - {EPIC_ID}" `
  -Body @"
Epic closure required for {EPIC_ID} (all tasks completed).

**Epic Status**:
- Tasks completed: {N}/{N} (100%)
- CI/CD status: ? Passing
- QA status: {All tasks QA-approved / Pending QA sign-off}

**Required Deliverables**:
- tech_lead_signoff.md: Technical validation document
- qa_signoff.md: QA retrospective (if QA involved)
- epic_review.md: Learnings, best practices, calibration instructions

**Context Files to Load**:
- Epic Plan: docs/{project}/epics/{EPIC}/plan.md
- Task Plans: docs/{project}/epics/{EPIC}/tasks/*.md
- Implementation Summaries: docs/{project}/epics/{EPIC}/implementation-summary/*.md
- QA Reports: docs/{project}/epics/{EPIC}/qa-reports/*.md (if applicable)

**Closure Document Content**:
- **tech_lead_signoff.md**: Architecture review, implementation highlights, known issues, retrospective notes
- **qa_signoff.md**: Test results summary, task-level QA status, critical paths validated
- **epic_review.md**: Task completion review, learnings, best practices, calibration instructions

**Calibration Instructions** (in epic_review.md):
- What global skills to update (dod_rule.md, etc.)
- What standards to create/update
- What templates to refine

**Next Steps**:
- Tech Lead: Create closure documents (3 files)
- Tech Lead: Send completion message to Orchestrator
- Orchestrator: Request Architect sign-off
- Knowledge Steward: Process calibration instructions (after Architect sign-off)
- Estimated duration: {2} hours

**Success Criteria**:
- [ ] tech_lead_signoff.md created
- [ ] qa_signoff.md created (if QA involved)
- [ ] epic_review.md created with calibration instructions
- [ ] Architect ready to review for final sign-off
"@ `
  -Priority "high" `
  -Category "epic-closure"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
## Epic Closure Request

**Epic Status**: All tasks complete

**Required Deliverables**:
- tech_lead_signoff
- qa_signoff
- epic_review

**Next Steps**:
- Tech Lead: Create closure docs
```

? **Miért ROSSZ**:

- Epic Status: Tasks N/N format HIÁNYZIK, CI/CD status HIÁNYZIK, QA status HIÁNYZIK
- Required Deliverables: 3 files content breakdown TELJESEN HIÁNYZIK (tech_lead_signoff 4 sections, qa_signoff 3 sections, epic_review content)
- Context Files to Load szakasz TELJESEN HIÁNYZIK (Epic Plan, Task Plans, Implementation Summaries, QA Reports paths)
- Closure Document Content szakasz TELJESEN HIÁNYZIK (minden file tartalmi követelményei)
- Calibration Instructions szakasz TELJESEN HIÁNYZIK (dod_rule, standards, templates explicit lista)
- Success Criteria szakasz TELJESEN HIÁNYZIK ([ ] checklist 4 kritériummal)
- Estimated duration HIÁNYZIK

### 3.4 Architect Sign-off Request (› Architect)

**Scenario**: Tech Lead closure complete, Architect sign-off szükséges

**?? KRITIKUS: Template 3.4 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Epic Closure Status**: Tech Lead sign-off status, QA sign-off status, All tasks (N/N completed)
- ? **Required Deliverables**: architect_signoff.md (content breakdown: Clean Architecture compliance, ADR validation, Tech debt, Code quality, Calibration approval)
- ? **Context Files to Load**: Epic Plan path, Tech Lead Sign-off path, QA Sign-off path, Epic Review path, ADR Files paths (minden ADR!)
- ? **Architect Sign-off Content**: 5 sections (Clean Architecture, ADRs, Tech debt, Code quality, Calibration)
- ? **Next Steps**: Architect actions, Orchestrator next actions (Knowledge Steward archival/calibration), Estimated duration
- ? **Success Criteria**: [ ] Checklist (4 criteria: architect_signoff created, Clean Architecture validated, Tech debt assessed, Epic ready for archival)

**Miért fontos Required Deliverables content breakdown**: Architect pontosan tudja architect_signoff.md tartalmi követelményeit (5 sections).

**Miért fontos Architect Sign-off Content (5 sections)**: Architect tudja milyen validations szükségesek (Clean Architecture, ADRs, Tech debt, Code quality, Calibration).

**Miért fontos Success Criteria checklist**: Architect validálja completion-t ([ ] format explicit, 4 kritérium).

```powershell
New-Message -From "orchestrator" -To "architect" `
  -Title "Architect Sign-off Request - {EPIC_ID}" `
  -Body @"
Architect sign-off required for {EPIC_ID} (Tech Lead closure completed).

**Epic Closure Status**:
- Tech Lead sign-off: ? Complete
- QA sign-off: ? Complete
- All tasks: {N}/{N} completed

**Required Deliverables**:
- architect_signoff.md: Architecture validation document

**Context Files to Load**:
- Epic Plan: docs/{project}/epics/{EPIC}/plan.md
- Tech Lead Sign-off: docs/{project}/epics/{EPIC}/tech_lead_signoff.md
- QA Sign-off: docs/{project}/epics/{EPIC}/qa_signoff.md
- Epic Review: docs/{project}/epics/{EPIC}/epic_review.md
- ADR Files: docs/{project}/decisions/ADR-*.md (created during Epic)

**Architect Sign-off Content**:
- Clean Architecture compliance validation
- Architectural decisions review (ADR validation)
- Technical debt assessment
- Code quality review
- Calibration approval (global skill/standard updates)

**Next Steps**:
- Architect: Validate architecture compliance
- Architect: Create architect_signoff.md
- Architect: Send completion message to Orchestrator
- Orchestrator: Request Knowledge Steward for archival (Phase 1)
- Orchestrator: Request Knowledge Steward for calibration (Phase 3 - optional)
- Estimated duration: {1} hour

**Success Criteria**:
- [ ] architect_signoff.md created
- [ ] Clean Architecture compliance validated
- [ ] Technical debt assessed and documented
- [ ] Epic ready for archival
"@ `
  -Priority "normal" `
  -Category "epic-sign-off"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
## Architect Sign-off Request

**Epic**: Epic 08

**Required Deliverables**:
- architect_signoff.md

**Next Steps**:
- Architect: Create sign-off document
```

? **Miért ROSSZ**:

- Epic Closure Status szakasz TELJESEN HIÁNYZIK (Tech Lead sign-off? QA sign-off? All tasks N/N?)
- Required Deliverables: architect_signoff.md content breakdown TELJESEN HIÁNYZIK (5 sections: Clean Architecture, ADRs, Tech debt, Code quality, Calibration)
- Context Files to Load szakasz TELJESEN HIÁNYZIK (Epic Plan, Tech Lead Sign-off, QA Sign-off, Epic Review, ADR paths)
- Architect Sign-off Content szakasz TELJESEN HIÁNYZIK (5 sections és tartalmi követelmények)
- Success Criteria szakasz TELJESEN HIÁNYZIK ([ ] checklist 4 kritériummal)
- Estimated duration HIÁNYZIK

### 3.5 Epic Archival Request (› Knowledge Steward)

**Scenario**: Epic closure complete, archival szükséges (context reduction)

**?? KRITIKUS: Template 3.5 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúrát** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Priority**: ?? CRITICAL jelölés (context reduction blocker)
- ? **Epic-ek to Archive**: Táblázat (Epic ID, Name, Closed date, file_count, size)
- ? **Expected Context Reduction**: Token reduction, Active context before/after, Files to archive
- ? **Required Actions**: 5-step protocol (detailed steps)
- ? **Archive Policy**: Copy (NOT move) - KRITIKUS szakasz!
- ? **Context Files**: Epic folder paths, state.md path
- ? **Deadline**: YYYY-MM-DD EOD (same day archival)
- ? **Next Steps**: Knowledge Steward actions, Orchestrator validation, Estimated duration
- ? **Success Criteria**: [ ] Checklist format (5 criteria)

**Miért fontos Archive Policy**: Knowledge Steward pontosan tudja Copy NOT move (originals megmaradnak historical reference-hez).

**Miért fontos Success Criteria**: Explicit acceptance criteria checklist (Knowledge Steward minden pontot validál).

```powershell
New-Message -From "orchestrator" -To "knowledge_steward" `
  -Title "Epic Archival Request - {EPIC_IDs} (Phase 1 - CRITICAL)" `
  -Body @"
Epic archival required for {EPIC_IDs} (Epic closure completed, context reduction needed).

**Priority**: ?? CRITICAL (context reduction blocker)

**Epic-ek to Archive**:
- Epic {ID1}: {Name} (Closed: {date}, {file_count} files, {size} KB)
- Epic {ID2}: {Name} (Closed: {date}, {file_count} files, {size} KB)
- Epic {ID3}: {Name} (Closed: {date}, {file_count} files, {size} KB)

**Expected Context Reduction**:
- Token reduction: ~{X} tokens ({Y}% of active context)
- Active context before: {A} KB › After: {B} KB
- Files to archive: {N} files (~{M} KB)

**Required Actions**:
1. Create archived/ folder structure (docs/{project}/archived/)
2. Copy Epic folders to archived/ (preserve originals)
3. Create archived/README.md (registry table: Epic ID, Closure Date, Archive Date)
4. Add ARCHIVED.md markers to original Epic folders
5. Update state.md (Epic State Map: "?? Archival Pending" › "? Archived {date}")

**Archive Policy** (KÖTELEZÕ szakasz):
- Copy (NOT move) Epic-ek to archived/
- Originals remain in epics/ for historical reference
- Read-only access to archived Epic-ek

**Context Files**:
- Epic folders: docs/{project}/epics/epic_{ID}/
- State.md: docs/{project}/state.md

**Deadline**: {YYYY-MM-DD} EOD (CRITICAL - same day archival)

**Next Steps**:
- Knowledge Steward: Execute archival protocol (5 steps)
- Knowledge Steward: Send completion message with metrics
- Orchestrator: Validate state.md Epic State Map update
- Estimated duration: {1.5} hours

**Success Criteria** (KÖTELEZÕ szakasz):
- [ ] archived/ folder created with Epic copies
- [ ] archived/README.md registry created
- [ ] ARCHIVED.md markers added to original Epic folders
- [ ] state.md Epic State Map updated
- [ ] Context reduction: {X} tokens achieved
"@ `
  -Priority "critical" `
  -Category "epic-archival" `
  -DeadlineDate (Get-Date).ToString("yyyy-MM-dd")
```

### 3.6 Task Assignment (› Backend/Frontend Developer)

**Scenario**: Task breakdown complete, developer assignment szükséges

```powershell
New-Message -From "orchestrator" -To "backend_developer" `
  -Title "Task Assignment - {TASK_ID}" `
  -Body @"
Task assignment for {TASK_ID}: {Task Name}.

**Task Context**:
- Epic: {EPIC_ID} - {Epic Name}
- Priority: {High/Normal/Low}
- Estimated effort: {X} hours/days
- Dependencies: {TASK-XX blocks this / This blocks TASK-YY}

**Required Deliverables**:
- Code implementation (files listed in task plan section 4)
- Tests ({unit/integration} tests as specified in task plan section 8)
- Implementation summary: docs/{project}/epics/{EPIC}/implementation-summary/{TASK_ID}-*.md

**Context Files to Load**:
- Task Plan: docs/{project}/epics/{EPIC}/tasks/{TASK_ID}.md
- Epic Plan: docs/{project}/epics/{EPIC}/plan.md
- ADR Files: {list ADRs relevant to this task}
- Skill Files: {list skills from task plan section 3}

**Implementation Guidelines**:
- Follow Clean Architecture principles (Core/Infrastructure/API layers)
- Match coding style of existing codebase (N-shot pattern)
- Write tests for all new functionality (target: 80%+ coverage)
- Document implementation in implementation-summary

**Next Steps**:
- Developer: Implement task according to plan
- Developer: Run build and tests (validate passing)
- Developer: Create implementation-summary
- Developer: Send completion message to Orchestrator
- QA: Test implementation (if QA required in task plan section 3)
- Estimated duration: {X} hours/days

**Success Criteria**:
- [ ] Code implemented according to task plan
- [ ] Build passing ?
- [ ] Tests passing ({unit/integration}) ?
- [ ] Implementation summary created
- [ ] Ready for QA (if required) or Tech Lead review
"@ `
  -Priority "normal" `
  -Category "task-assignment"
```

---

### 3.7 Epic Planning Review Request (› Devils Advocate)

**Scenario**: Epic planning befejezve (Architect), kritikai review szükséges BEFORE Task Breakdown

**?? KRITIKUS: Template 3.7 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Epic Context**: Epic Goal, Scope summary, Priority, Complexity
- ? **Required Deliverables**: critique_report.md (??/??/?? severity breakdown), Critique status (APPROVED/REJECTED/CONDITIONAL)
- ? **Context Files to Load**: Epic plan path, ADR paths (MINDEN ADR explicit!), Epic dependency_map path
- ? **Critique Focus Areas**: 7 areas (Standards enforcement, Architecture compliance, Risk identification, Dependency analysis, Scope completeness, DoD clarity, Alternative approaches)
- ? **Next Steps**: Devils Advocate critique execution, Orchestrator decision (APPROVED › Task Breakdown, REJECTED › Architect revision)
- ? **Success Criteria**: [ ] checklist 5 kritérium

**Miért fontos Critique Focus Areas (7 areas)**: Devils Advocate pontosan tudja mit vizsgáljon (NEM generic "review it").

**Miért fontos Context Files (ADR paths explicit)**: Devils Advocate betöltés context-hez (ADR compliance check).

**Miért fontos Success Criteria checklist**: Devils Advocate látja completion criteria (critique completeness validáció).

```powershell
New-Message -From "orchestrator" -To "devils_advocate" `
  -Title "Epic Planning Review Request - {EPIC_ID}" `
  -Body @"
Epic planning review required for {EPIC_ID} ({Epic Name}) BEFORE Task Breakdown.

**Epic Context**:
- Epic Goal: {brief 1-2 sentence summary}
- Scope: {brief scope description - features count, complexity level}
- Priority: {High/Normal/Low}
- Complexity: {High/Medium/Low}
- Milestone: {Milestone name}

**Required Deliverables**:
- Critique report: docs/{project}/epics/{EPIC}/critique_report.md (?? Critical/?? High/?? Medium severity breakdown)
- Critique status determination: APPROVED | REJECTED | CONDITIONAL
- Critical issues (??) count (blocker-ek, amelyek REJECTED-ot indokolnak)
- Recommendations (actionable steps, prioritized)

**Context Files to Load**:
- Epic Plan: docs/{project}/epics/{EPIC}/plan.md
- ADR Files: {list ALL ADRs referenced in Epic plan - docs/{project}/decisions/ADR-XXX-*.md}
- Dependency Map: docs/{project}/epics/{EPIC}/dependency_map.md
- Project Standards: docs/{project}/standards/ (Clean Architecture, DDD, SOLID compliance check)

**Critique Focus Areas** (7 areas - MINDEN area KÖTELEZÕ vizsgálat):
1. **Standards Enforcement**: Epic plan follows project standards? (Clean Architecture DoD compliance, DDD patterns used correctly)
2. **Architecture Compliance**: Solutions fit Clean Architecture layers? (Core/Infrastructure/API dependency rules)
3. **Risk Identification**: Technical risks identified? (Single point of failure, Performance bottlenecks, Security gaps)
4. **Dependency Analysis**: Dependencies realistic? (Circular dependencies, Missing dependencies, Blocking risks)
5. **Scope Completeness**: Epic scope well-defined? (Missing features, Ambiguous requirements)
6. **DoD Clarity**: Epic DoD clear and testable? (Vague acceptance criteria, Missing test cases)
7. **Alternative Approaches**: Better solutions exist? (Over-engineering, Under-engineering, Simpler patterns available)

**Next Steps**:
- Devils Advocate: Execute devils_advocate.workflow.md (Plan Review Section)
- Devils Advocate: Create critique_report.md (??/??/?? severity breakdown)
- Devils Advocate: Send critique completion message to Orchestrator (Template 4.1)
- Orchestrator Decision:
  - If APPROVED: Proceed with Task Breakdown (Tech Lead dispatch - Template 3.2)
  - If REJECTED: Architect revision required (address critical issues ??)
  - If CONDITIONAL: Architect address high priority issues (??) before Task Breakdown
- Estimated duration: 3-4 hours

**Success Criteria**:
- [ ] Epic plan reviewed against 7 focus areas
- [ ] Critical issues (??) identified (if any)
- [ ] Risk assessment completed (Technical/Business/Security - High/Medium/Low breakdown)
- [ ] Recommendations provided (actionable steps, prioritized)
- [ ] Critique status determined (APPROVED/REJECTED/CONDITIONAL)

**SLA**: 4 hours (Epic planning kritikus path - Task Breakdown blocker)
"@ `
  -Priority "high" `
  -Category "epic-planning-review"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
Epic planning review for EPIC-07. Check if it's good. Send feedback.
```

? **Miért ROSSZ**:

- Epic Context TELJESEN HIÁNYZIK (Goal, Scope, Priority, Complexity?)
- Critique Focus Areas TELJESEN HIÁNYZIK (7 areas - mit vizsgáljon Devils Advocate?)
- Context Files to Load HIÁNYZIK (ADR paths explicit lista KÖTELEZÕ!)
- Success Criteria checklist HIÁNYZIK ([ ] formátum - completion validáció)
- Next Steps breakdown HIÁNYZIK (mi történik APPROVED/REJECTED/CONDITIONAL esetén?)
- "Check if it's good" NEM elég - explicit Critique Focus 7 areas KÖTELEZÕ

---

### 3.8 Task Planning Review Request (› Devils Advocate)

**Scenario**: Task planning befejezve (Tech Lead), kritikai review szükséges BEFORE Task Assignment

**?? KRITIKUS: Template 3.8 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - UGYANAZOK a szakaszok mint Template 3.7:

- ? Task Context (Task Goal, Epic, Implementation approach, Priority, Estimated effort)
- ? Required Deliverables (critique_report.md, Critique status)
- ? Context Files to Load (Task plan, Epic plan, ADR paths)
- ? Critique Focus Areas (7 areas: Edge case handling, Error recovery, Input validation, Clean Architecture compliance, Over/Under-engineering, DoD completeness, Security risks)
- ? Next Steps (Devils Advocate execution, Orchestrator decision)
- ? Success Criteria ([ ] checklist 5 kritérium)

```powershell
New-Message -From "orchestrator" -To "devils_advocate" `
  -Title "Task Planning Review Request - {TASK_ID}" `
  -Body @"
Task planning review required for {TASK_ID} ({Task Name}) BEFORE Task Assignment.

**Task Context**:
- Task Goal: {brief 1-2 sentence summary}
- Epic: {EPIC_ID} - {Epic Name}
- Implementation approach: {brief description - Backend/Frontend/Infrastructure/API layer}
- Priority: {High/Normal/Low}
- Estimated effort: {X} hours/days
- Dependencies: {TASK-XX blocks this / This blocks TASK-YY}

**Required Deliverables**:
- Critique report: docs/{project}/epics/{EPIC}/tasks/{TASK_ID}_critique.md (??/??/?? severity breakdown)
- Critique status determination: APPROVED | REJECTED | CONDITIONAL
- Critical issues (??) count (blocker-ek)
- Recommendations (actionable steps, prioritized)

**Context Files to Load**:
- Task Plan: docs/{project}/epics/{EPIC}/tasks/{TASK_ID}.md
- Epic Plan: docs/{project}/epics/{EPIC}/plan.md
- ADR Files: {list ADRs relevant to this task}
- Project Standards: docs/{project}/standards/

**Critique Focus Areas** (7 areas - MINDEN area KÖTELEZÕ vizsgálat):
1. **Edge Case Handling**: Edge cases covered? (Null values, Empty arrays, Boundary conditions)
2. **Error Recovery**: Error handling sufficient? (Try-catch blocks, Retry logic, Graceful degradation)
3. **Input Validation**: Input validation adequate? (SQL injection, XSS prevention, Type safety)
4. **Clean Architecture Compliance**: Implementation follows Clean Architecture? (Core/Infrastructure/API dependency rules)
5. **Over/Under-engineering**: Complexity appropriate? (Too complex for simple task? Too simple for complex requirements?)
6. **DoD Completeness**: Task DoD clear and testable? (Acceptance criteria, Test scenarios)
7. **Security Risks**: Security gaps identified? (Auth/authz, Encryption, Sensitive data handling)

**Next Steps**:
- Devils Advocate: Execute devils_advocate.workflow.md (Edge Case Hunting Section)
- Devils Advocate: Create critique_report.md (??/??/?? severity breakdown)
- Devils Advocate: Send critique completion message to Tech Lead/Orchestrator (Template 4.2)
- Orchestrator Decision:
  - If APPROVED: Proceed with Task Assignment (Backend/Frontend Developer dispatch - Template 3.6)
  - If REJECTED: Tech Lead revision required (address critical issues ??)
  - If CONDITIONAL: Tech Lead address high priority issues (??) before assignment
- Estimated duration: 2 hours

**Success Criteria**:
- [ ] Task plan reviewed against 7 focus areas
- [ ] Edge cases identified (if missing)
- [ ] Security risks assessed (Input validation, Auth/authz)
- [ ] Recommendations provided (prioritized actionable steps)
- [ ] Critique status determined (APPROVED/REJECTED/CONDITIONAL)

**SLA**: 2 hours (Task planning kritikus path - Developer assignment blocker)
"@ `
  -Priority "high" `
  -Category "task-planning-review"
```

---

### 3.9 ADR Review Request (› Devils Advocate)

**Scenario**: ADR draft befejezve (Architect), kritikai review szükséges BEFORE ADR Approval

**?? KRITIKUS: Template 3.9 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - UGYANAZOK a szakaszok mint Template 3.7:

- ? ADR Context (Decision title, Decision area, Impacted layers, Alternatives considered count)
- ? Required Deliverables (critique_report.md, Critique status)
- ? Context Files to Load (ADR file, Related ADRs, Project standards)
- ? Critique Focus Areas (7 areas: Alternative evaluation, Trade-offs clarity, Standards compliance, Long-term maintainability, Migration cost analysis, Lock-in risk, Security implications)
- ? Next Steps (Devils Advocate execution, Architect/Orchestrator decision)
- ? Success Criteria ([ ] checklist 5 kritérium)

```powershell
New-Message -From "orchestrator" -To "devils_advocate" `
  -Title "ADR Review Request - ADR-{NUMBER}" `
  -Body @"
ADR review required for ADR-{NUMBER} ({Decision Title}) BEFORE ADR Approval.

**ADR Context**:
- Decision Title: {ADR title}
- Decision Area: {Architecture/Database/Security/Performance/Infrastructure}
- Impacted Layers: {Core/Infrastructure/API}
- Alternatives Considered: {count} alternatives documented
- Status: {Proposed/Draft}

**Required Deliverables**:
- Critique report: docs/{project}/decisions/ADR-{NUMBER}_critique.md (??/??/?? severity breakdown)
- Critique status determination: APPROVED | REJECTED | CONDITIONAL
- Critical issues (??) count (missing alternatives, insufficient justification)
- Recommendations (alternative suggestions, trade-off documentation)

**Context Files to Load**:
- ADR File: docs/{project}/decisions/ADR-{NUMBER}-{title}.md
- Related ADRs: {list ADRs referenced in this ADR}
- Project Standards: docs/{project}/standards/architecture_standards.md

**Critique Focus Areas** (7 areas - MINDEN area KÖTELEZÕ vizsgálat):
1. **Alternative Evaluation**: Alternatives sufficiently explored? (Why not Pattern X? Why chosen over Y?)
2. **Trade-offs Clarity**: Trade-offs documented? (Performance vs Maintainability, Cost vs Flexibility)
3. **Standards Compliance**: Decision follows project standards? (Clean Architecture, DDD, SOLID principles)
4. **Long-term Maintainability**: Maintainability considered? (Future evolution, Refactoring difficulty)
5. **Migration Cost Analysis**: Migration cost realistic? (Effort estimation, Breaking changes impact)
6. **Lock-in Risk**: Vendor/technology lock-in assessed? (Exit strategy, Alternative availability)
7. **Security Implications**: Security impact analyzed? (New attack surface, Encryption weaknesses)

**Next Steps**:
- Devils Advocate: Execute devils_advocate.workflow.md (Alternative Evaluation Section)
- Devils Advocate: Create critique_report.md (??/??/?? severity breakdown)
- Devils Advocate: Send critique completion message to Architect/Orchestrator (Template 4.3)
- Architect/Orchestrator Decision:
  - If APPROVED: ADR status › Accepted (use in Epic planning)
  - If REJECTED: Architect revision required (re-evaluate alternatives, document trade-offs)
  - If CONDITIONAL: Architect strengthen justification, add trade-off section before approval
- Estimated duration: 3 hours

**Success Criteria**:
- [ ] ADR reviewed against 7 focus areas
- [ ] Alternatives evaluation completeness assessed
- [ ] Trade-offs clarity checked (Performance/Maintainability/Cost/Flexibility)
- [ ] Recommendations provided (alternative suggestions if applicable)
- [ ] Critique status determined (APPROVED/REJECTED/CONDITIONAL)

**SLA**: 3 hours (ADR approval kritikus path - Epic planning blocker)
"@ `
  -Priority "high" `
  -Category "adr-review"
```

---

## ?? 4. Response Monitoring Workflow

### 4.1 Agent Response Check

**Orchestrator-ként periodikus ellenõrzés**:

```powershell
# Check all agent inboxes for completed responses
$agents = @("architect", "tech_lead", "backend_developer", "frontend_developer", "qa_tester", "knowledge_steward")

foreach ($agent in $agents) {
    $responses = Get-PendingMessages -Role $agent | Where-Object {
        $_.From -eq $agent -and $_.To -eq "orchestrator" -and $_.Status -eq "pending"
    }

    if ($responses.Count -gt 0) {
        Write-Host "Agent $agent has $($responses.Count) pending responses:"
        $responses | Format-Table MessageId, Title, Priority, Date
    }
}
```

### 4.2 Task Completion Validation

Agent response üzenet érkezésekor:

1. Olvasd be a response message-t
2. Ellenõrizd a deliverables checkboxeket (all checked?)
3. Validáld a context files létezését (path check)
4. Frissítsd a state.md vagy backlog.md státuszt
5. Indítsd a következõ workflow fázist (pl. Task complete › QA assignment)

---

## ??? PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Send message to agent
New-Message -From "orchestrator" -To "architect" `
  -Title "Epic Planning Request" `
  -Body "..." `
  -Priority "high" `
  -Category "epic-planning"

# Check agent responses
Get-PendingMessages -Role "architect" | Format-Table
Get-PendingMessages -Role "tech_lead" | Where-Object { $_.Status -eq "completed" }

# Read agent response
Read-Message -MessageId "msg-101"

# Update orchestrator inbox (for tracking)
Update-MessageStatus -MessageId "msg-001" -NewStatus "completed"

# Get statistics (all agents)
Get-MessageStatistics
```

---

**Note**: Orchestrator primary role: Message dispatching és workflow coordination. Inbox check optional (response monitoring céljából).
