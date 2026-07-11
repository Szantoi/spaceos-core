---
id: workflow-qa_tester-multi-workspace
title: "QA Tester Multi-Workspace Communication Protocol"
type: workflow
scope: multi-workspace
category: communication
last_updated: 2026-02-17
---

# ?? QA Tester Multi-Workspace Workflow

**Szerepkör**: Senior QA & Security Engineer
**Scope**: Csak Multi-Workspace deployment esetén töltsd be ezt a fájlt
**Célja**: Communication Hub üzenetek olvasása, feldolgozása és válaszok küldése

---

## ?? Mikor használd ezt a workflow-t?

**Runbook szabályozza:** A `qa_tester.runbook.md` "Multi-Workspace Detection" szekciója határozza meg, hogy mikor kell betölteni ezt a fájlt.

**Indicator**: Ha van `docs/{project}/communication_hub/` mappa › Multi-Workspace mod aktív

---

## ?? 1. Inbox Check Protocol

### 1.1 Startup - Pending üzenetek ellenõrzése

```powershell
# Load helper module
Import-Module .\scripts\communication-hub-helper.ps1

# Get pending messages (priority sorted)
Get-PendingMessages -Role "qa_tester" | Format-Table

# Filter by high priority only (CRITICAL bugs, security issues)
Get-PendingMessages -Role "qa_tester" -HighPriorityOnly | Format-Table
```

**Manuális ellenõrzés** (ha PowerShell helper nem elérhetõ):

1. Töltsd be: `docs/{project}/communication_hub/qa_tester_inbox.md`
2. Keress: `status: ? Pending` üzeneteket (link-based táblázat)
3. **Priority sorting**: CRITICAL > HIGH > NORMAL > LOW
4. **FIFO within priority**: Azonos prioritáson belül legrégebbi elõször

---

## ?? 2. Message Processing

### 2.1 Üzenet olvasása

```powershell
# Read specific message
Read-Message -MessageId "msg-007"
```

**Manuális olvasás**:

1. Kattints az inbox táblázat "File" oszlopában lévó linkre
2. Töltsd be a message file-t: `messages/{date}/msg-{id}-{from}-to-{to}.md`
3. Ellenõrizd a frontmatter mezõket:
   - `from`: Ki küldte az üzenetet (általában orchestrator/tech_lead/developer)
   - `priority`: critical | high | normal | low
   - `category`: Üzenet típusa (qa-request, security-review, epic-qa-signoff, etc.)
   - `reply_to`: Ha ez egy válasz, az eredeti üzenet ID-ja

### 2.2 Context Files betöltése

Az üzenet body-ban hivatkozott fájlok betöltése:

- QA request kérésnél: `{EPIC_ROOT}/tasks/{TASK_ID}.md`, implementation-summary
- Epic QA sign-off kérésnél: All task plans, implementation summaries, tech_lead_signoff
- Security review kérésnél: ADR files, authentication/authorization related code

### 2.3 Task végrehajtása

Az üzenet category alapján válassz workflow-t:

| Category | Workflow File | Task |
|:---------|:--------------|:-----|
| `qa-request` | [qa_tester.workflow.md](qa_tester.workflow.md) | Task-level QA testing (acceptance tests, edge cases, qa_report) |
| `epic-qa-signoff` | [qa_tester.workflow.md](qa_tester.workflow.md) | Epic-level QA sign-off (qa_signoff.md creation, retrospective testing) |
| `security-review` | [qa_tester.workflow.md](qa_tester.workflow.md) | Security audit (vulnerability scan, penetration testing, security report) |
| `regression-testing` | [qa_tester.workflow.md](qa_tester.workflow.md) | Regression test suite execution after bug fixes |

---

## ?? 3. Response Creation

**?? KRITIKUS: Template Részletesség**

**MINDIG használd a Section 4 (Common Message Templates) TELJES struktúráját**:

- ? **NE rövidíts**: Minden template szakasz kötelezõ (Test Results by Category, Bug List by Priority, Critical Paths, Next Steps)
- ? **NE egyszerûsíts**: Részletes információk (test counts, steps to reproduce bugs) KÖTELEZÕEK
- ? **Kövesd a PowerShell példát**: A template PowerShell body (@"...") tartalmazza a TELJES struktúrát
- ? **NE csak összefoglalót írj**: "QA passed" NEM elég - részletes breakdown szükséges

**Miért fontos**: Az Orchestrator/Developer a részletes információkból dolgozik (pl. Bug List › Developer fix prioritization).

---

### 3.1 Válasz üzenet készítése

```powershell
New-Message -From "qa_tester" -To "orchestrator" `
  -Title "QA Testing Complete - {TASK_ID}" `
  -Body "QA testing completed. Deliverables: qa_report.md, {N} bugs found. Next Steps: Developer bug fixing (if bugs > 0) or Task closure" `
  -Priority "normal" `
  -ReplyTo "msg-007" `
  -Category "qa-testing-complete"
```

**Manuális response készítés**:

1. **Fájl létrehozás**: `docs/{project}/communication_hub/messages/{date}/msg-{new-id}-qa_tester-to-{recipient}.md`

2. **Frontmatter template**:

```markdown
---
id: msg-{new-id}
timestamp: {ISO8601 timestamp}
from: qa_tester
to: {recipient}
reply_to: {original-msg-id}
priority: normal
status: pending
category: {category}
thread_id: {original-thread-id}
---

## Message Title

### Summary
{Rövid összefoglaló a QA eredményekrõl}

### Test Results
- [ ] {Test suite 1 results}
- [ ] {Test suite 2 results}

### Bugs Found
{Count and severity of bugs}

### Next Steps
{Ki mit csináljon ezután}

### Context
- **Original Request**: Link to [{original-msg-id}](../{date}/{original-msg-id}.md)
- **Task/Epic**: {EPIC_ROOT}/tasks/{TASK_ID}.md
```

### 3.2 Inbox status update

```powershell
# Update original message status
Update-MessageStatus -MessageId "msg-007" -NewStatus "completed"
```

**Manuális inbox update**:

1. Töltsd be: `docs/{project}/communication_hub/qa_tester_inbox.md`
2. Keresd meg az üzenetet a "Pending Messages" táblázatban
3. Változtasd a status oszlopot: `? Pending` › `? Completed`

---

## ?? 4. Common Message Templates

### 4.1 QA Testing Complete - No Issues (› Orchestrator)

**Scenario**: Task-level QA testing befejezve, bugs nem találtak

**?? KRITIKUS: Template 4.1 MINDEN szakasza KÖTELEZÕ**

**Használd a teljes PowerShell body struktúrát** - a következõ szakaszok mindegyike KÖTELEZÕ:

- ? **Test Summary**: Acceptance tests (X/X format), Edge case tests (Y/Y format), Regression tests (Z/Z format), Security tests (N/N format)
- ? **Test Coverage**: Functional coverage %, Code coverage %, Critical paths validated status
- ? **Test Execution Details**: Happy path, Error handling, Boundary conditions, Performance (response time)
- ? **QA Report**: Path to detailed QA report file
- ? **Next Steps**: Orchestrator actions, Tech Lead actions
- ? **Context**: Task Plan path, Implementation Summary path

**Miért fontos Test Summary breakdown**: Orchestrator látja test category-k részletes eredményét (Acceptance vs Security vs Regression).

**Miért fontos Test Coverage metrics**: Tech Lead értékeli quality-t (Functional + Code coverage %).

**Miért fontos Test Execution Details**: Orchestrator validálja DoD compliance-t (Happy path, Edge cases, Performance threshold).

```powershell
New-Message -From "qa_tester" -To "orchestrator" `
  -Title "QA Testing Complete - {TASK_ID} - All Tests Passed" `
  -Body @"
QA testing completed for {TASK_ID}. All tests passed, no bugs found.

**Test Summary**:
- Acceptance tests: {X}/{X} passing ?
- Edge case tests: {Y}/{Y} passing ?
- Regression tests: {Z}/{Z} passing ?
- Security tests: {N}/{N} passing ?

**Test Coverage**:
- Functional coverage: {X}%
- Code coverage: {Y}% (backend/frontend)
- Critical paths validated: ?

**Test Execution Details**:
- Happy path scenarios: ? All validated
- Error handling: ? Proper error messages, no crashes
- Boundary conditions: ? Null, empty, max values handled
- Performance: ? Response time < {X}ms (threshold: {Y}ms)

**QA Report**: {EPIC_ROOT}/qa-reports/{TASK_ID}-qa_report.md

**Next Steps**:
- Orchestrator: Mark task as QA-approved, proceed to closure
- Tech Lead: Review for Epic closure (if last task)

**Context**:
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Implementation Summary: {EPIC_ROOT}/implementation-summary/{TASK_ID}-*.md
"@ `
  -Priority "normal" `
  -ReplyTo "{original-msg-id}" `
  -Category "qa-testing-complete"
```

**Anti-Pattern Example (ROSSZ)**:

```markdown
## QA Testing Complete

### Test Summary
- All tests: ? Passing

### Next Steps
- Orchestrator: Task approved
```

? **Miért ROSSZ**:

- Test Summary: Acceptance/Edge case/Regression/Security breakdown HIÁNYZIK (csak "All tests")
- Test Coverage szakasz TELJESEN HIÁNYZIK (Functional %, Code %, Critical paths?)
- Test Execution Details szakasz TELJESEN HIÁNYZIK (Happy path? Error handling? Boundary? Performance?)
- QA Report path HIÁNYZIK
- Context HIÁNYZIK (Task Plan, Implementation Summary paths)
- "All tests passing" NEM elég – részletes category breakdown szükséges

### 4.2 QA Testing Complete - Bugs Found (› Orchestrator / Developer)

**Scenario**: Task-level QA testing befejezve, bugs találtak

```powershell
New-Message -From "qa_tester" -To "orchestrator" `
  -Title "QA Testing Complete - {TASK_ID} - {N} Bugs Found" `
  -Body @"
QA testing completed for {TASK_ID}. {N} bugs found, require developer fixing.

**Test Summary**:
- Acceptance tests: {X}/{Y} passing ??
- Edge case tests: {A}/{B} passing ??
- Regression tests: {C}/{C} passing ?
- Security tests: {D}/{D} passing ?

**Bugs Found** ({N} total):

**Critical (Priority 1)** - {count}:
1. {Bug description} - {Severity: Critical/High/Medium/Low}
   - Steps to reproduce: {brief steps}
   - Expected: {expected behavior}
   - Actual: {actual behavior}

**High (Priority 2)** - {count}:
1. {Bug description}
   - {brief details}

**Medium (Priority 3)** - {count}:
1. {Bug description}

**QA Report**: {EPIC_ROOT}/qa-reports/{TASK_ID}-qa_report.md
(Detailed bug descriptions, screenshots, reproduction steps in report)

**Next Steps**:
- Developer: Fix bugs (priority order: Critical › High › Medium)
- QA: Retest after bug fixes
- Orchestrator: Track bug fixing progress

**Context**:
- Task Plan: {EPIC_ROOT}/tasks/{TASK_ID}.md
- Implementation Summary: {EPIC_ROOT}/implementation-summary/{TASK_ID}-*.md
"@ `
  -Priority "high" `
  -ReplyTo "{original-msg-id}" `
  -Category "qa-testing-complete-bugs-found"
```

### 4.3 Epic QA Sign-off Complete (› Orchestrator / Tech Lead)

**Scenario**: Epic-level QA retrospective sign-off befejezve

```powershell
New-Message -From "qa_tester" -To "orchestrator" `
  -Title "Epic QA Sign-off Complete - {EPIC_ID}" `
  -Body @"
Epic QA sign-off completed for {EPIC_ID}.

**Epic-Level Test Results**:
- Total tasks tested: {N}
- Tasks passed QA: {X}/{N} ({percentage}%)
- Integration tests: {Y}/{Y} passing ?
- End-to-end tests: {Z}/{Z} passing ?

**Critical Paths Validated**:
- [ ] {Critical path 1}: ? Validated
- [ ] {Critical path 2}: ? Validated
- [ ] {Critical path 3}: ? Validated

**Test Coverage Summary**:
- Unit test coverage: {X}%
- Integration test coverage: {Y}%
- E2E test coverage: {Z}%

**Known Issues** (if any):
- {Issue 1}: {Status - accepted as known limitation / scheduled for future epic}
- {Issue 2}: {Status}

**QA Retrospective Notes**:
- {What went well in QA process}
- {What could be improved}
- {Recommendations for future epics}

**QA Sign-off Document**: {EPIC_ROOT}/qa_signoff.md

**Next Steps**:
- Tech Lead: Review qa_signoff.md for Epic closure
- Orchestrator: Request Architect sign-off (if all QA approved)

**Context**:
- Epic Plan: {EPIC_ROOT}/plan.md
- Task Plans: {EPIC_ROOT}/tasks/*.md
- QA Reports: {EPIC_ROOT}/qa-reports/*
"@ `
  -Priority "high" `
  -ReplyTo "{original-msg-id}" `
  -Category "epic-qa-signoff-complete"
```

### 4.4 Security Review Complete (› Tech Lead / Orchestrator)

**Scenario**: Security audit befejezve

```powershell
New-Message -From "qa_tester" -To "tech_lead" `
  -Title "Security Review Complete - {EPIC_ID / TASK_ID}" `
  -Body @"
Security review completed for {EPIC_ID / TASK_ID}.

**Security Audit Summary**:
- Vulnerability scan: ? No critical vulnerabilities
- Dependency check: ? All dependencies up-to-date, no known CVEs
- Authentication/Authorization: ? Properly implemented
- Input validation: ? All user inputs sanitized
- SQL injection: ? Parameterized queries used
- XSS protection: ? Output encoding applied

**Security Findings** (if any):

**High Severity** - {count}:
1. {Finding description}
   - Impact: {description}
   - Recommendation: {mitigation steps}

**Medium Severity** - {count}:
1. {Finding description}

**Low Severity / Informational** - {count}:
1. {Finding description}

**Security Best Practices Validated**:
- [ ] OWASP Top 10 compliance ?
- [ ] Sensitive data encryption ?
- [ ] Secure communication (HTTPS) ?
- [ ] Error handling (no sensitive info in error messages) ?

**Security Report**: {EPIC_ROOT}/security-reports/{ID}-security_report.md

**Next Steps**:
- Developer: Address high/medium severity findings (if any)
- Tech Lead: Review security report for Epic closure
- Orchestrator: Track security fixing progress

**Context**:
- Epic/Task Plan: {EPIC_ROOT}/...
- ADR Files: docs/{project}/decisions/ADR-*-security-*.md
"@ `
  -Priority "high" `
  -Category "security-review-complete"
```

---

## ?? 5. Workflow Integration Points

### 5.1 Task QA Request (Developer›QA Tester)

**Incoming Message**: `qa-request` (from Developer/Orchestrator)

**Actions**:

1. Töltsd be: Task plan, implementation-summary, changed files
2. Végrehajtás: [qa_tester.workflow.md](qa_tester.workflow.md) (Section A or B)
3. Deliverables: qa_report.md, test results, bug list (if any)
4. Response: [Template 4.1](#41-qa-testing-complete---no-issues--orchestrator) or [Template 4.2](#42-qa-testing-complete---bugs-found--orchestrator--developer)

### 5.2 Epic QA Sign-off Request (Orchestrator›QA Tester)

**Incoming Message**: `epic-qa-signoff` (from Orchestrator)

**Actions**:

1. Töltsd be: All task plans, implementation summaries, tech_lead_signoff
2. Végrehajtás: Epic-level retrospective QA validation
3. Deliverables: qa_signoff.md (Epic closure document)
4. Response: [Template 4.3](#43-epic-qa-sign-off-complete--orchestrator--tech-lead)

### 5.3 Security Review Request (Tech Lead›QA Tester)

**Incoming Message**: `security-review` (from Tech Lead/Orchestrator)

**Actions**:

1. Töltsd be: Epic/Task context, ADR files, authentication code
2. Végrehajtás: Security audit (vulnerability scan, penetration testing)
3. Deliverables: security_report.md, findings list
4. Response: [Template 4.4](#44-security-review-complete--tech-lead--orchestrator)

---

## ??? PowerShell Helper Quick Reference

```powershell
# Import module
Import-Module .\scripts\communication-hub-helper.ps1

# Check inbox (priority sorted)
Get-PendingMessages -Role "qa_tester" | Format-Table

# High priority only (CRITICAL bugs, security issues)
Get-PendingMessages -Role "qa_tester" -HighPriorityOnly | Format-Table

# Read message
Read-Message -MessageId "msg-007"

# Send response
New-Message -From "qa_tester" -To "orchestrator" `
  -Title "QA Testing Complete" `
  -Body "..." `
  -Priority "high" `
  -ReplyTo "msg-007" `
  -Category "qa-testing-complete"

# Update status
Update-MessageStatus -MessageId "msg-007" -NewStatus "completed"

# Get statistics
Get-MessageStatistics
```

---

**Note**: Ha Multi-Workspace mod NINCS aktív, ezt a fájlt NE töltsd be. A standard `qa_tester.workflow.md` elegendõ single-workspace mode-ban.
