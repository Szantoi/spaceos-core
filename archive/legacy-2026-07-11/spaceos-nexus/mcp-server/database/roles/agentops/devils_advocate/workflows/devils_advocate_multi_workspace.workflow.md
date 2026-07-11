---
id: devils-advocate-multi-workspace-workflow
title: "Devil's Advocate Multi-Workspace Communication Workflow"
type: workflow
scope: joinerytech-flow
category: multi-workspace
last_updated: 2026-02-17
---

# ?? Devil's Advocate Multi-Workspace Workflow

**Purpose**: Devil's Advocate Communication Hub protokoll (Message-based kommunikáció Epic/Task/ADR kritikai review-hoz)
**Multi-Workspace Pattern**: Communication Hub v2.0 támogatás

---

## ?? 1. Inbox Check & Message Processing

### 1.1 Inbox location

`docs/{project}/communication_hub/devils_advocate_inbox.md`

### 1.2 Pending message check

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Check pending messages
Get-PendingMessages -Role "devils_advocate"

# Output: List of pending messages (FIFO order by timestamp)
```

**Manuális inbox check**:

1. Töltsd be: `docs/{project}/communication_hub/devils_advocate_inbox.md`
2. Nézd meg a "Pending Messages" táblázatot
3. FIFO sorrendben dolgozd (legrégebbi timestamp elõször)

---

## ?? 2. Message Processing

### 2.1 Üzenet olvasása

```powershell
# Read specific message
Read-Message -MessageId "msg-015"
```

**Manuális olvasás**:

1. Kattints az inbox táblázat "File" oszlopában lévõ linkre
2. Töltsd be a message file-t: `messages/{date}/msg-{id}-{from}-to-devils_advocate.md`
3. Ellenõrizd a frontmatter mezõket:
   - `from`: Ki küldte az üzenetet (általában orchestrator/architect/tech_lead)
   - `priority`: critical | high | normal | low
   - `category`: Üzenet típusa (epic-planning-review, task-planning-review, adr-review)
   - `reply_to`: Ha ez egy válasz, az eredeti üzenet ID-ja

### 2.2 Context Files betöltése

Az üzenet body-ban hivatkozott fájlok betöltése:

- Epic planning review kérésnél: `{EPIC_ROOT}/plan.md`, ADR files
- Task planning review kérésnél: `{EPIC_ROOT}/tasks/{TASK_ID}.md`
- ADR review kérésnél: `docs/{project}/decisions/ADR-XXX-*.md`

### 2.3 Critique végrehajtása

Az üzenet category alapján válassz workflow-t:

| Category | Workflow File | Task |
|:---------|:--------------|:-----|
| `epic-planning-review` | [devils_advocate.workflow.md](devils_advocate.workflow.md) | Epic Plan kritikai elemzése (Standard enforcement, Risk identification) |
| `task-planning-review` | [devils_advocate.workflow.md](devils_advocate.workflow.md) | Task Plan kritikai elemzése (Edge case hunting, Security review) |
| `adr-review` | [devils_advocate.workflow.md](devils_advocate.workflow.md) | ADR kritikai elemzése (Alternative evaluation, Technical risk) |

---

## ?? 3. Response Creation

**?? KRITIKUS: Template Részletesség**

**MINDIG használd a Section 4 (Common Message Templates) TELJES struktúráját**:

- ? **NE rövidíts**: Minden template szakasz kötelezõ (Issues Breakdown, Risk Assessment, Recommendations, Next Steps)
- ? **NE egyszerûsíts**: Részletes információk (kritikus/high/medium/low breakdown) KÖTELEZÕEK
- ? **Kövesd a PowerShell példát**: A template PowerShell body (@"...") tartalmazza a TELJES struktúrát
- ? **NE csak összefoglalót írj**: "Review complete, approved" NEM elég - részletes issue breakdown szükséges

**Miért fontos**: Az Orchestrator/Architect a részletes információkból dolgozik (pl. Critical issues › blocker decision).

---

### 3.1 Válasz üzenet készítése

```powershell
New-Message -From "devils_advocate" -To "orchestrator" `
  -Title "Epic Planning Critique - {EPIC_ID} - {APPROVED/REJECTED}" `
  -Body "Epic planning review completed. Status: {APPROVED/REJECTED}. Critical issues: {count}. Risk level: {High/Medium/Low}." `
  -Priority "{high if REJECTED, normal if APPROVED}" `
  -ReplyTo "msg-015" `
  -Category "critique-report"
```

**Manuális response készítés**:

1. **Fájl létrehozás**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-devils_advocate-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: devils_advocate
to: {recipient}
reply_to: {original-msg-id}
priority: high | normal
status: pending
category: critique-report
thread_id: {original-thread-id}
---

## Message Title

### Critique Status
{APPROVED | REJECTED | CONDITIONAL}

### Summary
{Rövid összefoglaló a critique eredményérõl}

### Critical Issues (??)
- {Issue 1}
- {Issue 2}

### High Priority Issues (??)
- {Issue 1}

### Recommendations
{Javaslatok a javításokhoz}

### Next Steps
{Ki mit csináljon ezután}

### Context
- **Original Request**: Link to [{original-msg-id}](../{date}/{original-msg-id}.md)
- **Reviewed Document**: {EPIC_ROOT}/plan.md
```

### 3.2 Inbox status update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-015" -NewStatus "completed"
```

**Manuális inbox update**:

1. Töltsd be: `docs/{project}/communication_hub/devils_advocate_inbox.md`
2. Keresd meg az üzenetet a "Pending Messages" táblázatban
3. Változtasd a status oszlopot: `? Pending` › `? Completed`

---

## ?? 4. Common Message Templates

### 4.1 Epic Planning Critique (› Orchestrator / Architect)

**Scenario**: Epic planning review befejezve

**?? KRITIKUS: Template 4.1 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Critique Status**: APPROVED/REJECTED/CONDITIONAL (explicit!)
- ? **Summary**: 2-3 sentence overview (Risk level, Overall assessment)
- ? **Critical Issues (??)**: Blocker lista (minden issue 1-2 sentence description + impact)
- ? **High Priority Issues (??)**: Warning lista (minden issue description + recommendation)
- ? **Medium/Low Issues**: Info lista (optional improvements)
- ? **Risk Assessment**: Technical risk, Business risk, Security risk (High/Medium/Low + explanation)
- ? **Recommendations**: Actionable steps (prioritized list)
- ? **Next Steps**: Orchestrator/Architect next actions (conditional on APPROVED/REJECTED)
- ? **Files**: Reviewed documents paths, Critique report path (ha külön file-ba írtad)

**Miért fontos Issues breakdown (Critical/High/Medium)**: Orchestrator döntést hoz blocker alapján (REJECTED › Epic planning revision).

**Miért fontos Risk Assessment**: Architect látja technical/business/security risks-et (mitigation planning).

**Miért fontos Recommendations prioritized**: Architect pontosan tudja mi a legfontosabb javítás.

```powershell
New-Message -From "devils_advocate" -To "orchestrator" `
  -Title "Epic Planning Critique - {EPIC_ID} - {APPROVED/REJECTED}" `
  -Body @"
Epic planning review completed for {EPIC_ID} ({EPIC_NAME}).

**Critique Status**: {APPROVED | REJECTED | CONDITIONAL}

**Summary**:
{2-3 sentence overview: Risk level assessed, Overall architectural soundness, Key concerns}

**Critical Issues (?? Blocker)** - {count}:
1. {Issue title}: {1-2 sentence description + impact}
   - Impact: {Breaks Clean Architecture / Security vulnerability / Data loss risk}
   - Recommendation: {Specific fix}
2. {Issue 2}...

**High Priority Issues (?? Warning)** - {count}:
1. {Issue title}: {description}
   - Impact: {Performance degradation / Maintainability concern}
   - Recommendation: {Specific improvement}

**Medium/Low Issues (?? Info)** - {count}:
1. {Optional improvement}: {description}

**Risk Assessment**:
- Technical risk: {High/Medium/Low} ({explanation: Single point of failure? Over-engineering?})
- Business risk: {High/Medium/Low} ({explanation: Scope creep? Unrealistic timeline?})
- Security risk: {High/Medium/Low} ({explanation: Input validation? Auth/authz gaps?})

**Recommendations** (prioritized):
1. {High priority}: {Actionable step - refactor X, add validation Y}
2. {Medium priority}: {Actionable step}
3. {Low priority}: {Optional improvement}

**Next Steps**:
- If APPROVED: Orchestrator: Proceed with Task Breakdown (Tech Lead dispatch)
- If REJECTED: Architect: Revise Epic plan addressing critical issues, Re-submit for review
- If CONDITIONAL: Architect: Address high priority issues before Task Breakdown

**Files**:
- Reviewed: {EPIC_ROOT}/plan.md
- ADRs reviewed: docs/{project}/decisions/ADR-XXX-{title}.md ({count} ADRs)
- Critique report: {EPIC_ROOT}/critique_report.md (if created)
"@ `
  -Priority "{high if REJECTED, normal if APPROVED}" `
  -ReplyTo "{original-msg-id}" `
  -Category "critique-report"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
## Epic Planning Critique

### Critique Status
APPROVED

### Summary
Epic plan looks good. No critical issues found.

### Next Steps
- Orchestrator: Proceed with Task Breakdown
```

? **Miért ROSSZ**:

- Critical Issues szakasz TELJESEN HIÁNYZIK (még ha 0 is, explicit "No critical issues" lista kellene!)
- High Priority Issues szakasz TELJESEN HIÁNYZIK (Medium/Low issues?)
- Risk Assessment szakasz TELJESEN HIÁNYZIK (Technical/Business/Security risk High/Medium/Low breakdown?)
- Recommendations szakasz TELJESEN HIÁNYZIK (még APPROVED esetén is lehet optional improvement!)
- Files szakasz TELJESEN HIÁNYZIK (Reviewed documents paths?)
- Summary túl rövid (Risk level? Overall assessment details?)
- "Looks good" NEM elég - részletes issue breakdown (még ha 0 critical) + Risk Assessment KÖTELEZÕ!

### 4.2 Task Planning Critique (› Tech Lead / Orchestrator)

**Scenario**: Task planning review befejezve

**?? KRITIKUS: Template 4.2 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - UGYANAZOK a szakaszok mint Template 4.1:

- ? Critique Status (APPROVED/REJECTED/CONDITIONAL)
- ? Summary (Risk level, Edge case coverage assessment)
- ? Critical Issues (??) (Blocker lista + impact + recommendation)
- ? High Priority Issues (??) (Warning lista)
- ? Medium/Low Issues (??) (Info lista)
- ? Risk Assessment (Technical/Business/Security)
- ? Recommendations (prioritized actionable steps)
- ? Next Steps (Tech Lead/Orchestrator actions conditional on status)
- ? Files (Task plan path, Critique report path)

```powershell
New-Message -From "devils_advocate" -To "tech_lead" `
  -Title "Task Planning Critique - {TASK_ID} - {APPROVED/REJECTED}" `
  -Body @"
Task planning review completed for {TASK_ID}.

**Critique Status**: {APPROVED | REJECTED | CONDITIONAL}

**Summary**:
{Edge case coverage assessed, Implementation steps clarity, DoD completeness}

**Critical Issues (?? Blocker)** - {count}:
1. {Issue}: {Missing error handling / Insufficient DoD / Security gap}
   - Impact: {Production incident risk / QA blocker}
   - Recommendation: {Add try-catch, Update DoD, Add input validation}

**High Priority Issues (?? Warning)** - {count}:
1. {Issue}: {Unclear implementation step / Missing dependency}
   - Recommendation: {Clarify step X, Document dependency on Epic Y}

**Medium/Low Issues (?? Info)** - {count}:
1. {Optional improvement}

**Risk Assessment**:
- Technical risk: {High/Medium/Low} ({Edge cases covered? Null safety?})
- Business risk: {High/Medium/Low} ({DoD sufficient for QA? Acceptance criteria clear?})
- Security risk: {High/Medium/Low} ({Input validation? SQL injection risk?})

**Recommendations** (prioritized):
1. {Critical fix}: {Add error handling for X scenario}
2. {High priority}: {Clarify implementation step Y}
3. {Optional}: {Refactor Z for better maintainability}

**Next Steps**:
- If APPROVED: Orchestrator: Assign task to Backend/Frontend Developer
- If REJECTED: Tech Lead: Revise task plan addressing critical issues, Re-submit for review
- If CONDITIONAL: Tech Lead: Address high priority issues before assignment

**Files**:
- Reviewed: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Critique report: {EPIC_ROOT}/tasks/{TASK_ID}_critique.md (if created)
"@ `
  -Priority "{high if REJECTED, normal if APPROVED}" `
  -ReplyTo "{original-msg-id}" `
  -Category "critique-report"
```

### 4.3 ADR Review Critique (› Architect / Orchestrator)

**Scenario**: ADR (Architectural Decision Record) review befejezve

**?? KRITIKUS: Template 4.3 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúráját** - UGYANAZOK a szakaszok mint Template 4.1:

- ? Critique Status (APPROVED/REJECTED/CONDITIONAL)
- ? Summary (Alternative evaluation, Trade-offs clarity)
- ? Critical Issues (??) (Blocker lista: Missing alternative, Violation of standards)
- ? High Priority Issues (??) (Warning: Insufficient trade-off analysis)
- ? Medium/Low Issues (??) (Info: Documentation improvement)
- ? Risk Assessment (Technical/Business/Security)
- ? Recommendations (prioritized: Re-evaluate alternatives, Document trade-offs)
- ? Next Steps (Architect/Orchestrator actions)
- ? Files (ADR path, Critique report path)

**Miért fontos Alternative evaluation (ADR review)**: Devils Advocate kihívja "Miért nem X-et választottuk?" kérdést.

**Miért fontos Trade-offs clarity**: Architect látja milyen alternatívák maradtak ki (bias check).

```powershell
New-Message -From "devils_advocate" -To "architect" `
  -Title "ADR Review Critique - ADR-{NUMBER} - {APPROVED/REJECTED}" `
  -Body @"
ADR review completed for ADR-{NUMBER} ({Decision Title}).

**Critique Status**: {APPROVED | REJECTED | CONDITIONAL}

**Summary**:
{Alternative evaluation completeness, Trade-offs clarity, Standards compliance}

**Critical Issues (?? Blocker)** - {count}:
1. {Missing alternative X}: {Why was alternative X not considered?}
   - Impact: {Sub-optimal decision / Violates Clean Architecture}
   - Recommendation: {Evaluate alternative X, Document why rejected}
2. {Trade-off not documented}: {Performance vs Maintainability trade-off missing}
   - Recommendation: {Add trade-off section to ADR}

**High Priority Issues (?? Warning)** - {count}:
1. {Insufficient justification}: {Why this pattern over Y?}
   - Recommendation: {Strengthen justification with benchmarks/references}

**Medium/Low Issues (?? Info)** - {count}:
1. {Documentation improvement}: {Add code example to ADR}

**Risk Assessment**:
- Technical risk: {High/Medium/Low} ({Long-term maintainability? Scalability?})
- Business risk: {High/Medium/Low} ({Migration cost? Lock-in risk?})
- Security risk: {High/Medium/Low} ({Encryption weakness? Auth pattern gaps?})

**Recommendations** (prioritized):
1. {Critical}: {Evaluate alternative X, document rejection rationale}
2. {High priority}: {Add trade-off section (Performance vs Maintainability)}
3. {Optional}: {Add code example for clarity}

**Next Steps**:
- If APPROVED: Orchestrator: Proceed with Epic planning using this ADR
- If REJECTED: Architect: Revise ADR addressing critical issues (alternatives, trade-offs), Re-submit for review
- If CONDITIONAL: Architect: Address high priority issues (strengthen justification) before approval

**Files**:
- Reviewed: docs/{project}/decisions/ADR-{NUMBER}-{title}.md
- Critique report: docs/{project}/decisions/ADR-{NUMBER}_critique.md (if created)
"@ `
  -Priority "{high if REJECTED, normal if APPROVED}" `
  -ReplyTo "{original-msg-id}" `
  -Category "critique-report"
```

---

## ?? 5. Workflow Integration Points

### 5.1 Epic Planning Phase (Orchestrator›Devils Advocate)

**Incoming Message**: `epic-planning-review` (from Orchestrator)

**Actions**:

1. Töltsd be: Epic plan.md, ADR files
2. Végrehajtás: [devils_advocate.workflow.md](devils_advocate.workflow.md) (Plan Review Section)
3. Deliverables: Critique report (APPROVED/REJECTED + issues)
4. Response: [Template 4.1](#41-epic-planning-critique--orchestrator--architect)

### 5.2 Task Planning Phase (Orchestrator›Devils Advocate)

**Incoming Message**: `task-planning-review` (from Orchestrator/Tech Lead)

**Actions**:

1. Töltsd be: Task plan.md
2. Végrehajtás: [devils_advocate.workflow.md](devils_advocate.workflow.md) (Edge Case Hunting Section)
3. Deliverables: Critique report (APPROVED/REJECTED + issues)
4. Response: [Template 4.2](#42-task-planning-critique--tech-lead--orchestrator)

### 5.3 ADR Review Phase (Architect›Devils Advocate)

**Incoming Message**: `adr-review` (from Architect)

**Actions**:

1. Töltsd be: ADR file
2. Végrehajtás: [devils_advocate.workflow.md](devils_advocate.workflow.md) (Alternative Evaluation Section)
3. Deliverables: Critique report (APPROVED/REJECTED + issues)
4. Response: [Template 4.3](#43-adr-review-critique--architect--orchestrator)

---

## ?? 6. Metrics & Reporting

### 6.1 Critique Report Summary

```powershell
# Generate monthly critique summary
$month = "2026-02"
Get-ChildItem "communication_hub/messages/$month/*devils_advocate*.md" |
  Where-Object { (Select-String -Path $_ -Pattern "category: critique-report" -Quiet) } |
  ForEach-Object {
    $status = (Select-String -Path $_ -Pattern "Critique Status: (\w+)").Matches.Groups[1].Value
    $critical = (Select-String -Path $_ -Pattern "Critical Issues.*- (\d+)" -AllMatches).Matches.Count
    [PSCustomObject]@{ File=$_.Name; Status=$status; CriticalIssues=$critical }
  } | Format-Table
```

**Metrics to track**:

- Total critiques: {N}
- APPROVED: {X} ({Y}%)
- REJECTED: {A} ({B}%)
- CONDITIONAL: {C} ({D}%)
- Critical issues found: {E}
- High priority issues found: {F}

---

## ?? Best Practices

### Devils Advocate Mindset

1. **"Assume it will fail"**: Úgy tekints a tervre, mintha az biztosan hibás lenne (kezdõ állás)
2. **Evidence-based critique**: Csak technikai érvekkel és standardokkal kritizálj (Clean Architecture, SOLID, DDD hivatkozások)
3. **Constructive feedback**: REJECTED esetén is adj konkrét javítási javaslatokat (actionable recommendations)
4. **Intellectual honesty**: Ha a terv jó, azt is ismerd el (APPROVED legitim outcome, ne keress mesterséges hibát)

### Template Usage

- **MINDIG használd a PowerShell body (@"...") TELJES struktúráját**
- **Critical Issues (??)**: Blocker-ek, amelyek REJECTED-ot indokolnak
- **High Priority Issues (??)**: Warning-ok, amelyek CONDITIONAL-t indokolnak
- **Risk Assessment**: MINDEN esetben (Technical/Business/Security) - még APPROVED esetén is (Low/Low/Low OK)
- **Recommendations**: MINDEN esetben (még APPROVED esetén is lehet optional improvement)

---

*Ez a workflow biztosítja a Devils Advocate Multi-Workspace integr

ációját.*
