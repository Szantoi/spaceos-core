---
id: workflow-knowledge_steward-context-optimization
title: "Knowledge Steward Context Optimization Workflow"
type: workflow
scope: context-optimization
category: optimization
last_updated: 2026-02-18
---

# ?? Context Optimization Workflow

**Szerepk�r**: Knowledge Steward (Chief Librarian)
**Scope**: Token reduction, context cleanup, summary generation
**C�lja**: Akt�v context workspace token haszn�lat optimaliz�l�sa (Epic auditing + context slicing)

---

## ?? Mikor haszn�ld ezt a workflow-t?

**Multi-Workspace Communication Hub trigger**:

- Category: `context-optimization`
- Orchestrator �zenet: Context Optimization Request
- Trigger reason: Token haszn�lat tel�tetts�g > 50%, vagy rendszeres karbantart�s (Weekly)

**Standalone trigger**:

- Token tel�tetts�g: Kontextus haszn�lat > 50%
- Rendszeres karbantart�s: Hetente egyszer (p�nteken)
- Epic lez�r�s ut�n: Architect Sign-off meg�rkez�se ut�n (context slicing sz�ks�ges)
- Orchestrator explicit k�r�s: Context cleanup request

---

## ?? Context Audit (Token Usage Assessment)

**C�l**: Token haszn�lat felm�r�se, legnagyobb context fogyaszt�k azonos�t�sa.

### 1.1 Token Usage Inventory

**Reasoning**: Mekkora a jelenlegi token haszn�lat?

```powershell
# Hozz�vet�leges token count (character count / 4 - rough estimate)
$docsPath = "docs/{project}/"

$totalSize = (Get-ChildItem $docsPath -Recurse -File -Filter "*.md" |
  Measure-Object -Property Length -Sum).Sum

$estimatedTokens = [math]::Round($totalSize / 4)

Write-Host "Estimated active workspace token usage: $estimatedTokens tokens" -ForegroundColor Cyan
Write-Host "Total size: $([math]::Round($totalSize / 1KB, 2)) KB" -ForegroundColor Cyan

# Token budget threshold check (50% = 100,000 tokens assumed budget)
$tokenBudget = 100000
$tokenUsagePercent = [math]::Round(($estimatedTokens / $tokenBudget) * 100, 2)

Write-Host "Token usage: $tokenUsagePercent %" -ForegroundColor $(if ($tokenUsagePercent -gt 50) { 'Yellow' } else { 'Green' })

if ($tokenUsagePercent -gt 50) {
    Write-Host "?? Context cleanup RECOMMENDED (token usage > 50%)" -ForegroundColor Yellow
}
```

---

### 1.2 Context Hotspots Identification

**C�l**: Legnagyobb context fogyaszt�k azonos�t�sa (Epic mapp�k, Task r�szletek).

```powershell
# Legnagyobb Epic mapp�k (lez�rt Epicek context cleanup jel�ltek)
$epicFolders = Get-ChildItem "$docsPath/epics/" -Directory

$epicSizes = $epicFolders | ForEach-Object {
    $size = (Get-ChildItem $_.FullName -Recurse -File |
      Measure-Object -Property Length -Sum).Sum

    [PSCustomObject]@{
        EpicFolder = $_.Name
        SizeKB = [math]::Round($size / 1KB, 2)
        EstimatedTokens = [math]::Round($size / 4)
    }
} | Sort-Object -Property EstimatedTokens -Descending

Write-Host "`nTop 5 largest Epic folders (context cleanup candidates):" -ForegroundColor Cyan
$epicSizes | Select-Object -First 5 | Format-Table -AutoSize
```

**Observation**: Mely Epicek z�rultak le �s context cleanup jel�ltek?

- [ ] Done/Closed Epic-ek azonos�tva (epic_plan.md status field check)
- [ ] Legnagyobb context fogyaszt�k azonos�tva (token count descending)
- [ ] Context cleanup priority Epic lista k�sz�lt

---

## 1?? Epic Mappa Audit (ReACT Pattern)

**C�l**: A `docs/{project}/epics/{EPIC_ID}/` mapp�k ellen�rz�se �s karbantart�sa.

### 1.1 Epic St�tusz Ellen�rz�s

**Reasoning**: Mely Epicek �s Taskok z�rultak le?

```markdown
1. Navig�lj a `docs/{project}/epics/` mapp�ba
2. Azonos�tsd a lez�rt Epic-eket (st�tusz: Done/Closed/Archived)
3. Minden lez�rt Epic-n�l ellen�rizd a Task st�tuszokat
```

**Acting**: Olvasd be a relev�ns f�jlokat

- `docs/{project}/epics/{EPIC_ID}/epic_plan.md` (St�tusz mez�)
- `docs/{project}/epics/{EPIC_ID}/tasks/{TASK_ID}.md` (Task st�tuszok)
- `docs/{project}/epics/{EPIC_ID}/*_report.md` (Implementation/QA jelent�sek)

**Observation**: Mit tal�lt�l?

- [ ] Lez�rt Epic? (Done/Closed)
- [ ] Minden Task lez�rt? (Done)
- [ ] Van Implementation Report?
- [ ] Van QA Sign-off?
- [ ] Van Architect Sign-off?

**?? Ha MINDEN checklist ?**: Epic **context cleanup javaslat** (summary.md creation + archival trigger)

---

### 1.2 Kritikus Inform�ci� Extrakci� (Fact Summary Pattern)

**C�l**: A lez�rt Epic/Task-b�l csak a d�nt�shozatalhoz SZ�KS�GES inform�ci�kat mentsd meg.

**Fact Summary Pattern alkalmaz�sa**:

Minden lez�rt Task-b�l vond ki az al�bbi inform�ci�kat:

```markdown
## ?? Context Summary: [TASK-ID] - [R�vid N�v]

**1. V�grehajtott V�ltoz�sok (Delta):**
- `�tvonal/f�jl.cs`: [L�trehozva/M�dos�tva/T�r�lve]
- ...

**2. Kritikus D�nt�sek (ADR Lite):**
- ?? [D�nt�s]: [Indokl�s 1 mondatban]

**3. Megmaradt Kock�zatok / Tech Debt:**
- ?? [Le�r�s]: [Mi�rt maradt �gy?]

**4. Tanuls�g (Lessons Learned):**
- ?? [�j szab�ly/minta, amit felismert�nk]
```

**Output**: K�sz�ts egy `summary.md` f�jlt az Epic mapp�ban:

- Hely: `docs/{project}/epics/{EPIC_ID}/summary.md`
- Tartalom: Minden Task Fact Summary-ja

**PowerShell Automated Summary Skeleton**:

```powershell
# summary.md template creation
$epicId = "{EPIC_ID}"
$project = "{project}"
$epicPath = "docs/$project/epics/$epicId"

$summaryPath = "$epicPath/summary.md"

# Create summary skeleton
@"
---
id: epic-summary-$epicId
title: "Epic Summary: $epicId"
type: epic-summary
created: $(Get-Date -Format 'yyyy-MM-dd')
---

# Epic Summary: $epicId

## ?? Epic Overview

**Epic ID**: $epicId
**Status**: Done/Closed
**Completion Date**: {YYYY-MM-DD}

## ?? Task Summaries

{TASK_SUMMARIES_PLACEHOLDER}

## ?? Critical Decisions

{CRITICAL_DECISIONS_PLACEHOLDER}

## ?? Tech Debt & Risks

{TECH_DEBT_PLACEHOLDER}

## ?? Lessons Learned

{LESSONS_LEARNED_PLACEHOLDER}

## ?? Related Archives

- Epic Archive: [docs/archive/$project/epics/$epicId/](../../../archive/$project/epics/$epicId/)
- Communication Hub Archive: [docs/archive/$project/communication_hub/epics/$epicId/](../../../archive/$project/communication_hub/epics/$epicId/) (if Multi-Workspace)
"@ | Out-File $summaryPath -Encoding UTF8

Write-Host "? summary.md skeleton created: $summaryPath" -ForegroundColor Green
Write-Host "?? MANUAL POPULATION NEEDED: Task Summaries, Critical Decisions, Tech Debt, Lessons Learned" -ForegroundColor Yellow
```

**?? FONTOS**: summary.md l�trehoz�sa ut�n az Epic **archiv�l�s-k�sz** (trigger archival workflows)

---

## 3?? Registry & Documentation Update (Fact Check Pattern)

**C�l**: Knowledge Map friss�t�se, link integrit�s biztos�t�sa.

### 3.1 Knowledge Map Friss�t�s

**Fact Check Pattern**: Ellen�rizd szigor�an!

- [ ] A `docs/roles/core/knowledge_map.md` tartalmazza-e az archiv�lt Epic referenci�j�t?
- [ ] A `summary.md` szerepel-e a registryben?
- [ ] Nincsenek t�r�tt linkek az archiv�lt f�jlokra?

**Ha �j skillt vagy template-et hoztak l�tre az Epic sor�n**:

- Add hozz� a `knowledge_map.md`-hez a megfelel� kateg�ri�ban
- Friss�tsd a skill_structure_management.skill.md statisztik�it

**PowerShell Knowledge Map Update**:

```powershell
# Knowledge Map friss�t�s (Epic summary hozz�ad�sa)
$knowledgeMapPath = "docs/roles/core/knowledge_map.md"

$epicSummaryEntry = @"

### Epic: $epicId

- **Summary**: [Epic Summary: $epicId](../../$project/epics/$epicId/summary.md)
- **Archive**: [Epic Archive](../../archive/$project/epics/$epicId/)
- **Status**: Archived
- **Key Topics**: {KEY_TOPICS_PLACEHOLDER}
"@

# Append Epic summary entry to Knowledge Map (manual section selection needed)
Add-Content -Path $knowledgeMapPath -Value $epicSummaryEntry

Write-Host "? Knowledge Map updated: $knowledgeMapPath" -ForegroundColor Green
Write-Host "?? REVIEW NEEDED: Knowledge Map section placement (Epic category)" -ForegroundColor Yellow
```

---

### 3.2 Link Integrit�s (ReACT Pattern)

**Acting**: Keress hivatkoz�sokat az archiv�lt f�jlokra

```powershell
# Keress hivatkoz�sokat a projekt dokument�ci�j�ban (archiv�lt Epic-re)
$searchPattern = "epics/$epicId"

$brokenLinks = Select-String -Path "$docsPath/**/*.md" -Pattern $searchPattern -Recurse

if ($brokenLinks.Count -gt 0) {
    Write-Host "?? Found $($brokenLinks.Count) references to archived Epic: $epicId" -ForegroundColor Yellow
    Write-Host "  Broken link candidates (may need update to archive path or summary.md):" -ForegroundColor Cyan

    $brokenLinks | Select-Object -First 10 | ForEach-Object {
        $relativePath = $_.Path -replace [regex]::Escape((Get-Location).Path), '.'
        Write-Host "    - $relativePath (line $($_.LineNumber))"
    }
}
```

**Observation**: Vannak akt�v hivatkoz�sok az archiv�lt Epic-re?

**Reasoning**: Ha vannak, akkor:

- **Option A**: Friss�tsd a hivatkoz�st `docs/archive/...` �tvonalra
- **Option B**: Cser�ld a hivatkoz�st a `summary.md`-re (prefer�lt - summary.md ACTIVE workspace-ben marad)

---

## 5?? Log & State Tiszt�t�s

**C�l**: R�gi logok t�rl�se, state.md akt�v context pruning.

### 5.1 Log Purge

**C�l**: 30 napn�l r�gebbi logok t�rl�se (production incident logok kiv�tel�vel).

```powershell
# Log purge (30 napn�l r�gebbi logok)
$logPath = "docs/roles/core/logs/"
$retentionDays = 30

$oldLogs = Get-ChildItem $logPath -Recurse -File -Filter "*.md" |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$retentionDays) }

# Production incident logok kiv�tele (tartalmazz�k "incident" vagy "production" sz�t)
$oldLogsNonProduction = $oldLogs |
  Where-Object { $_.Name -notmatch "incident|production" }

if ($oldLogsNonProduction.Count -gt 0) {
    Write-Host "?? Found $($oldLogsNonProduction.Count) old logs (> $retentionDays days, non-production)" -ForegroundColor Yellow

    # T�rl�s j�v�hagy�sa (manual confirmation)
    $confirmation = Read-Host "Delete these logs? (y/n)"

    if ($confirmation -eq 'y') {
        $oldLogsNonProduction | Remove-Item -Force
        Write-Host "? Deleted $($oldLogsNonProduction.Count) old logs" -ForegroundColor Green
    } else {
        Write-Host "?? Log purge SKIPPED (manual confirmation required)" -ForegroundColor Yellow
    }
}
```

**?? FONTOS**: Production incident logok **meg�rz�se 1 �vig**

---

### 5.2 State Friss�t�s

**C�l**: Lez�rt Epic/Task st�tuszok elt�vol�t�sa `state.md`-b�l (akt�v munk�ra f�kusz).

```powershell
# state.md friss�t�s (lez�rt Epic/Task st�tuszok elt�vol�t�sa)
$statePath = "docs/$project/state.md"

if (Test-Path $statePath) {
    Write-Host "?? state.md found: $statePath" -ForegroundColor Cyan
    Write-Host "  Manual review needed: Remove closed Epic/Task statuses (active work ONLY)" -ForegroundColor Yellow

    # P�lda: lez�rt Epic st�tuszok keres�se
    $closedEpics = Select-String -Path $statePath -Pattern "status:\s*(Done|Closed|Archived)"

    if ($closedEpics.Count -gt 0) {
        Write-Host "  Found $($closedEpics.Count) closed Epic/Task entries in state.md" -ForegroundColor Yellow
        Write-Host "  ?? MANUAL CLEANUP NEEDED: Remove these entries from state.md" -ForegroundColor Yellow
    }
}
```

**?? MANUAL REVIEW NEEDED**: state.md csak akt�v munk�t tartalmazzon (lez�rt Epic/Task state pruning)

---

## ?? Context Optimization Metrics (Multi-Workspace Response)

**Ha Multi-Workspace Communication Hub workflow r�sze**:

### Response Message Template

**Category**: context-optimization-complete

**Required Deliverables**:

- [ ] Token reduction: Before `{X}` tokens � After `{Y}` tokens (reduction: `{Z}` tokens, `{%}%`)
- [ ] Summary.md created count: `{N}` Epics
- [ ] Archival triggered: `{M}` Epics (archival-ready)
- [ ] Knowledge Map updated: `{K}` Epic summaries added
- [ ] Log purge: `{L}` old logs deleted (retention: 30 days)
- [ ] state.md pruning: `{P}` closed Epic/Task entries identified (manual cleanup needed)

### PowerShell Response Message Example

```powershell
New-Message -From "knowledge_steward" -To "orchestrator" `
  -Title "Context Optimization Complete - $project" `
  -Category "context-optimization-complete" `
  -Body @"
Context optimization completed successfully.

**Deliverables:**
- Token reduction: Before $tokensBefore tokens � After $tokensAfter tokens
  - Reduction: $tokenReduction tokens ($tokenReductionPercent %)
- Summary.md created: $summaryCount Epics
  - Epic IDs: $epicIdList
- Archival triggered: $archivalCount Epics (archival-ready - summary.md + sign-offs complete)
- Knowledge Map updated: $knowledgeMapCount Epic summaries added
- Log purge: $logPurgeCount old logs deleted (retention: 30 days, non-production)
- state.md pruning: $statePruneCount closed Epic/Task entries identified (manual cleanup needed)

**Next Steps:**
- Trigger Epic archival workflows: epic_archival.workflow.md + communication_hub_archival.workflow.md (if Multi-Workspace)
- Review Knowledge Map: Ensure Epic summary references correct
- state.md manual cleanup: Remove closed Epic/Task entries (active work ONLY)
"@
```

---

## ?? Troubleshooting

### Problem: Token reduction minimal (< 10%)

**Symptom**: Context optimization token reduction < 10% (ineffective cleanup)

**Solution**:

```powershell
# Identify largest context hotspots (not archived yet)
$nonArchivedEpics = Get-ChildItem "$docsPath/epics/" -Directory |
  Where-Object {
    $epicPlanPath = Join-Path $_.FullName "epic_plan.md"
    $status = Select-String -Path $epicPlanPath -Pattern "status:\s*(Done|Closed)" -Quiet
    $status -eq $true  # Done/Closed but NOT archived yet
  }

if ($nonArchivedEpics.Count -gt 0) {
    Write-Host "?? Found $($nonArchivedEpics.Count) Done/Closed Epics NOT archived (archival candidates)" -ForegroundColor Yellow

    $nonArchivedEpics | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Cyan
    }

    Write-Host "  Recommendation: Trigger Epic archival workflows for these Epics" -ForegroundColor Yellow
}
```

**Recommendation**: Trigger Epic archival workflows (epic_archival.workflow.md + communication_hub_archival.workflow.md)

---

### Problem: summary.md creation BLOCKED (missing sign-offs)

**Symptom**: summary.md creation failed (QA Sign-off vagy Architect Sign-off hi�nyzik)

**Solution**:

```powershell
# Check missing sign-offs
$missingSignoffs = @()

if (-not (Test-Path "$epicPath/qa_signoff.md")) {
    $missingSignoffs += "QA Sign-off"
}

if (-not (Test-Path "$epicPath/architect_signoff.md")) {
    $missingSignoffs += "Architect Sign-off"
}

if ($missingSignoffs.Count -gt 0) {
    Write-Host "?? summary.md creation BLOCKED - missing sign-offs:" -ForegroundColor Red
    $missingSignoffs | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }

    Write-Host "  Recommendation: Request missing sign-offs (Orchestrator dispatch)" -ForegroundColor Yellow

    # Multi-Workspace: Response Message
    New-Message -From "knowledge_steward" -To "orchestrator" `
      -Title "Context Optimization BLOCKED - $epicId" `
      -Category "context-optimization-blocked" `
      -Priority "high" `
      -Body @"
Context optimization CANNOT proceed due to missing sign-offs.

**Epic**: $epicId
**Missing Components**:
$(($missingSignoffs | ForEach-Object { "- $_" }) -join "`n")

**Recommended Actions**:
- Request QA Sign-off (orchestrator � qa_tester)
- Request Architect Sign-off (orchestrator � architect)
"@
}
```

---

## ? Workflow Completion Checklist

**Context Optimization Complete amikor**:

- [ ] Token usage inventory complete (workspace token count calculated)
- [ ] Context hotspots identified (largest Epic folders identified)
- [ ] Epic Audit complete (Done/Closed Epics identified)
- [ ] summary.md created (lez�rt Epicekhez Fact Summary Pattern alkalmazva)
- [ ] Knowledge Map updated (Epic summaries added)
- [ ] Link integrity check PASSED (broken links identified + fix plan)
- [ ] Log purge complete (30 napn�l r�gebbi logok t�r�lve - non-production)
- [ ] state.md pruning candidates identified (manual cleanup recommended)
- [ ] Archival triggered (Epic archival workflows dispatch - ha summary.md + sign-offs complete)
- [ ] Response Message sent (ha Multi-Workspace)

---

## ?? Related Workflows

| Workflow | Purpose | When to Use |
|:---------|:--------|:------------|
| [knowledge_steward_communication_hub_archival.workflow.md](knowledge_steward_communication_hub_archival.workflow.md) | Communication Hub messages archival | After context optimization (Epic confirmed closed) |
| [knowledge_steward.workflow.md](knowledge_steward.workflow.md) | Comprehensive Knowledge Steward workflow (all sections) | Master workflow reference (deprecated - use specific workflows) |
| [knowledge_steward_multi_workspace.workflow.md](knowledge_steward_multi_workspace.workflow.md) | Multi-Workspace Communication Hub protocol | Multi-Workspace deployment (category routing) |

---

## ?? Knowledge Base

**Context Optimization Philosophy**:

- **Token Budget Management**: Active workspace token usage < 50% (context cleanup trigger)
- **Fact Summary Pattern**: Csak kritikus inform�ci�k meg�rz�se (d�nt�sek, tech debt, lessons learned)
- **Context Slicing**: R�szletes implement�ci�s dokumentumok arch�vba (summary.md workspace-ben marad)
- **Proactive Cleanup**: Rendszeres karbantart�s (Weekly) + Event-driven cleanup (Epic closure)

**Optimization Triggers**:

- **Token Tel�tetts�g**: > 50% token usage
- **Epic Closure**: Architect Sign-off ut�n (archival-ready)
- **Rendszeres Karbantart�s**: Hetente (p�nteken)
- **Orchestrator Request**: Explicit context cleanup request

**Multi-Workspace Integration**:

- Category: `context-optimization` (EXISTING category)
- Prerequisites: NEM - standalone workflow is (context audit always runnable)
- Response Message: Context Optimization Complete � Orchestrator (token reduction metrics, archival triggers)
