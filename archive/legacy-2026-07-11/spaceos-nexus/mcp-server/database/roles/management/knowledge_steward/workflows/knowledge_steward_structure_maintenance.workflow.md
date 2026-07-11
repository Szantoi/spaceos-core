---
id: workflow-knowledge_steward-structure-maintenance
title: "Knowledge Steward Structure Maintenance Workflow"
description: "Systematic audit and maintenance of the agent system folder structure. Validates frontmatter, naming conventions, orphan cleanup, link integrity, and template compliance. Run monthly or after major Epic cycles."
type: workflow
scope: global
category: maintenance
last_updated: 2026-03-01
---

## Mission: Structural Guardian of the Knowledge Base

**Role**: Knowledge Steward
**Goal**: Systematic audit and maintenance of the agent system folder structure. Validate, clean up, and document the structural health of the knowledge base.

> **Multi-Agent Refactoring Note**: This workflow was refactored into modular components. Focused sub-workflows for specific areas are available:
>
> - `knowledge_steward_frontmatter_enrichment.workflow.md` — Frontmatter validation and enrichment
> - `knowledge_steward_calibration.workflow.md` — Skill/workflow integration
> - `knowledge_steward_communication_hub_archival.workflow.md` — Communication Hub archival

---

## Cognitive Setup (Prompt Engineering)

Before starting maintenance, activate the appropriate **Prompt Engineering Patterns**:

### Core Patterns

1. **Persona Pattern**: You are the Structural Guardian — you protect the order and consistency of the knowledge base.
2. **Fact Check Pattern**: Strict verification for every structure element — no assumptions, only validations.
3. **ReACT Pattern** (Reasoning + Acting): For every step, reason → act → observe cycle.

### Task-Specific Patterns

- **Cognitive Verifier Pattern**: If unclear, ask! (e.g., "Is this folder meant to be empty?")
- **Template Pattern**: Compliance against meta-templates.
- **Alternative Approach Pattern**: If a structural fix is ambiguous, list options.
- **Refusal Pattern**: If a required file is missing, log the issue but continue.

---

## Prerequisites / Input

Before starting structure maintenance, verify:

- [ ] `context_structure_management.knowledge.md` loaded
- [ ] `knowledge_map.md` accessible: `src/agent-system/database/standards/core/knowledge_map.md`
- [ ] Full access to `src/agent-system/database/roles/` folder

---

## Structure Maintenance Workflow

### 1. Structure Audit (ReACT Pattern)

**Goal**: Inventory all agent folders and their contents.

#### 1.1 Agent Folder Inventory

**Acting**: Run the following PowerShell to list the structure:

```powershell
# Full structure listing of the agent roles folder
$rolesPath = "src/agent-system/database/roles"

Get-ChildItem $rolesPath -Recurse -Directory | ForEach-Object {
    $depth = ($_.FullName -replace [regex]::Escape($rolesPath), '' -split '\\').Count - 2
    $indent = '  ' * $depth
    Write-Host "$indent$($_.Name)/"
}
```

**Observation**: Note any unexpected folders.

**Verify checklist for each agent folder**:

- [ ] `role.md` (.role.md) present
- [ ] `runbook.md` (.runbook.md) present (not all agents have one)
- [ ] `workflows/` subfolder present (not all agents have workflows)
- [ ] `knowledge/` subfolder present (optional)
- [ ] `templates/` subfolder present (optional)

**⚠️ PO Note**: If the Product Owner message folder references a new agent that does NOT have a role folder yet, log it in the report (do NOT create the folder automatically).

**Observation**: List all agents and their folder status.

#### 1.2 Core Folder Audit

**Acting**: Verify the core standards folders:

```powershell
$coreChecks = @(
    "src/agent-system/database/standards/core/knowledge_map.md",
    "src/agent-system/database/standards/core/constraints.md",
    "src/agent-system/database/standards/core/roles_registry.md"
)

foreach ($path in $coreChecks) {
    if (Test-Path $path) {
        Write-Host "✅ $path" -ForegroundColor Green
    } else {
        Write-Host "❌ MISSING: $path" -ForegroundColor Red
    }
}
```

**Observation**: Record all missing or unexpected files.

---

### 2. Frontmatter Validation (Fact Check Pattern)

**Goal**: Ensure all Markdown files have valid YAML frontmatter.

#### 2.1 Structural Presence Check

**Acting**: Find files missing frontmatter:

```powershell
# Find all .md files missing frontmatter
$rolesPath = "src/agent-system/database/roles"

Get-ChildItem $rolesPath -Recurse -Filter "*.md" | ForEach-Object {
    $firstLine = Get-Content $_.FullName -TotalCount 1
    if ($firstLine -ne '---') {
        Write-Host "MISSING frontmatter: $($_.FullName)" -ForegroundColor Yellow
    }
}
```

**Observation**: List all files missing frontmatter.

#### 2.2 ID Format Validation

**Acting**: Validate `id:` field format in frontmatter (pattern: `{type}-{name}` or `{type}-{agent}-{name}`):

```powershell
# Validate id fields
Get-ChildItem $rolesPath -Recurse -Filter "*.md" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match '^---') {
        $idMatch = [regex]::Match($content, 'id:\s*(.+)')
        if (!$idMatch.Success) {
            Write-Host "MISSING id field: $($_.FullName)" -ForegroundColor Yellow
        }
    }
}
```

**Valid ID format examples**:

- Role: `role-{agent}` (e.g., `role-architect`)
- Workflow: `workflow-{agent}-{name}` (e.g., `workflow-architect-closure`)
- Skill: `skill-{domain}-{name}` (e.g., `skill-backend-clean-architecture`)
- Runbook: `runbook-{agent}` (e.g., `runbook-backend_developer`)

#### 2.3 Type Field Validation

**Valid `type:` values**:

| Type Value | Use Case |
|:-----------|:---------|
| `role` | Role definition files (.role.md) |
| `workflow` | Workflow files (.workflow.md) |
| `skill` / `knowledge` | Knowledge/skill files (.knowledge.md) |
| `runbook` | Runbook files (.runbook.md) |
| `template` | Template files (.template.md) |
| `standard` | Standards files (constraints, registry) |

**Acting**: Find invalid type values:

```powershell
$validTypes = @('role', 'workflow', 'skill', 'knowledge', 'runbook', 'template', 'standard')

Get-ChildItem $rolesPath -Recurse -Filter "*.md" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $typeMatch = [regex]::Match($content, 'type:\s*(.+)')
    if ($typeMatch.Success) {
        $typeVal = $typeMatch.Groups[1].Value.Trim()
        if ($typeVal -notin $validTypes) {
            Write-Host "INVALID type '$typeVal': $($_.FullName)" -ForegroundColor Yellow
        }
    }
}
```

---

### 3. Naming Convention Check (Template Pattern)

**Goal**: Verify folder and file names follow conventions.

#### 3.1 Folder Naming Errors

**Known naming convention errors to fix**:

| Incorrect Name | Correct Name | Location |
|:---------------|:-------------|:---------|
| `tamplate` | `templates` | Any agent folder |
| `Workflow` | `workflows` | Any agent folder |
| `Knowledge` | `knowledge` | Any agent folder |
| `Template` | `templates` | Any agent folder |

**Acting**: Find incorrectly named folders:

```powershell
$incorrectFolders = @('tamplate', 'Workflow', 'Knowledge', 'Template')

Get-ChildItem $rolesPath -Recurse -Directory | Where-Object {
    $_.Name -in $incorrectFolders
} | ForEach-Object {
    Write-Host "INCORRECT folder name: $($_.FullName)" -ForegroundColor Red
    Write-Host "  Rename to: $($_.Name.ToLower() -replace 'tamplate', 'templates')" -ForegroundColor Yellow
}
```

#### 3.2 File Naming Convention

**Expected file naming patterns**:

| File Type | Pattern | Example |
|:----------|:--------|:--------|
| Role | `{agent}.role.md` | `architect.role.md` |
| Runbook | `{agent}.runbook.md` | `architect.runbook.md` |
| Workflow | `{agent}[_{name}].workflow.md` | `architect_closure.workflow.md` |
| Knowledge | `{name}.knowledge.md` | `clean_architecture.knowledge.md` |
| Template | `{name}.template.md` | `skill_structure.template.md` |

**Acting**: Find files not following the convention:

```powershell
# Check role files (must end in .role.md)
Get-ChildItem $rolesPath -Recurse -Filter "*.md" | Where-Object {
    $_.Name -match "role" -and $_.Name -notmatch "\.role\.md$"
} | ForEach-Object {
    Write-Host "UNUSUAL role filename: $($_.FullName)" -ForegroundColor Yellow
}
```

---

### 4. Orphan and Cleanup (Alternative Approach Pattern)

**Goal**: Remove or document empty folders, debug files, and temporary notes.

#### 4.1 Empty Folders

**Acting**: Find empty folders:

```powershell
Get-ChildItem $rolesPath -Recurse -Directory | Where-Object {
    (Get-ChildItem $_.FullName -Recurse -File).Count -eq 0
} | ForEach-Object {
    Write-Host "EMPTY folder: $($_.FullName)" -ForegroundColor Yellow
}
```

**Decision Tree**:

```
Empty folder found?
├── Is it a placeholder (e.g., future agent)?
│   ├── YES → Add a .gitkeep file (preserve the folder)
│   └── NO → Delete the folder
└── Is the agent folder itself empty?
    ├── YES → Log in report (do NOT delete automatically — consult Orchestrator)
    └── NO → Investigate subdirectory structure
```

#### 4.2 Debug Files and Temporary Notes

**Known debug files to review** (log, do NOT delete automatically):

| File | Location | Type |
|:-----|:---------|:-----|
| `backend_developer/jegyzet.txt` | engineering/backend_developer/ | Personal note |
| `qa_tester/Jegyzetek/` | engineering/qa_tester/ | Notes folder |
| `*.tmp` | Any location | Temp file |
| `debug_*.md` | Any location | Debug document |
| `test_*.md` | Any location | Test document |

**Acting**: Search for debug and temp files:

```powershell
# Find debug and temp files
$debugPatterns = @('*.tmp', 'debug_*.md', 'test_*.md', 'jegyzet.txt', 'notes.txt')

foreach ($pattern in $debugPatterns) {
    Get-ChildItem $rolesPath -Recurse -Filter $pattern | ForEach-Object {
        Write-Host "DEBUG/TEMP file: $($_.FullName)" -ForegroundColor Yellow
    }
}
```

**Action for found debug files**:

1. **Log** in the structure maintenance report
2. **Consult** the responsible agent (via Orchestrator)
3. **Only delete** after explicit approval

---

### 5. Link Integrity Check (Fact Check Pattern)

**Goal**: Ensure all internal references point to existing files.

#### 5.1 Internal Reference Check

**Acting**: Find broken links in Markdown files:

```powershell
$linkErrors = @()

Get-ChildItem $rolesPath -Recurse -Filter "*.md" | ForEach-Object {
    $filePath = $_.FullName
    $content = Get-Content $filePath -Raw
    $basePath = Split-Path $filePath

    # Find Markdown links [text](path)
    $links = [regex]::Matches($content, '\[.*?\]\(([^)]+)\)')

    foreach ($link in $links) {
        $linkTarget = $link.Groups[1].Value

        # Skip external URLs (http/https)
        if ($linkTarget -match '^https?://') { continue }

        # Skip anchor-only links (#section)
        if ($linkTarget -match '^#') { continue }

        # Check relative path
        $fullTarget = Join-Path $basePath $linkTarget
        $fullTarget = [System.IO.Path]::GetFullPath($fullTarget)

        if (-not (Test-Path $fullTarget)) {
            $linkErrors += [PSCustomObject]@{
                File = $filePath -replace [regex]::Escape((Get-Location).Path), '.'
                BrokenLink = $linkTarget
            }
        }
    }
}

if ($linkErrors.Count -gt 0) {
    Write-Host "BROKEN LINKS found: $($linkErrors.Count)" -ForegroundColor Red
    $linkErrors | Format-Table -AutoSize
} else {
    Write-Host "All links valid." -ForegroundColor Green
}
```

#### 5.2 Knowledge Map Reference Validation

**Acting**: Verify that all paths in `knowledge_map.md` point to existing files:

```powershell
$knowledgeMapPath = "src/agent-system/database/standards/core/knowledge_map.md"
$mapContent = Get-Content $knowledgeMapPath -Raw

# Extract file paths (backtick-wrapped)
$pathMatches = [regex]::Matches($mapContent, '`(src/agent-system/[^`]+)`')

$missingFiles = @()
foreach ($match in $pathMatches) {
    $path = $match.Groups[1].Value
    if (-not (Test-Path $path)) {
        $missingFiles += $path
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "MISSING files in knowledge_map.md: $($missingFiles.Count)" -ForegroundColor Red
    $missingFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
} else {
    Write-Host "All knowledge_map.md references valid." -ForegroundColor Green
}
```

---

### 6. Template Compliance Check (Template Pattern)

**Goal**: Verify that all role files, workflows, and skills follow the meta-template structure.

#### 6.1 Meta-Templates

**Available meta-templates** (in `templates/` folder):

| Template | Purpose | Required Sections |
|:---------|:--------|:------------------|
| `role_structure.template.md` | Role definition | Identity, Responsibilities, Workflow Triggers, Communication Protocol |
| `workflow_structure.template.md` | Workflow | Mission, Prerequisites, Steps, DoD, Triggers |
| `skill_structure.template.md` | Skill/Knowledge | When to Load, Architecture, Code Patterns, Common Errors |
| `runbook_structure.template.md` | Runbook | Purpose, Quick Reference, Common Scenarios |

#### 6.2 Compliance Checklist (per file type)

**Role files (.role.md) must contain**:

- [ ] YAML frontmatter with `id`, `title`, `type: role`, `description`
- [ ] `## Identity` or equivalent section
- [ ] `## Core Responsibilities` or equivalent section
- [ ] `## Workflow Triggers` or equivalent section
- [ ] `## Communication Protocol` or equivalent section

**Workflow files (.workflow.md) must contain**:

- [ ] YAML frontmatter with `id`, `title`, `type: workflow`, `description`
- [ ] Mission statement or goal section
- [ ] Step-by-step instructions
- [ ] DoD (Definition of Done) checklist
- [ ] Workflow Triggers section

**Knowledge/Skill files (.knowledge.md) must contain**:

- [ ] YAML frontmatter with `id`, `title`, `type: skill` or `knowledge`
- [ ] "When to load?" or context section
- [ ] Core knowledge content
- [ ] Related skills/references

---

### 7. Structural Statistics Update (Fact Summary Pattern)

**Goal**: Update statistics in `context_structure_management.knowledge.md`.

#### 7.1 Statistics Collection

**Acting**: Collect current statistics:

```powershell
$rolesPath = "src/agent-system/database/roles"

$stats = @{
    TotalAgents    = (Get-ChildItem $rolesPath -Directory -Depth 0 | Measure-Object).Count
    TotalRoles     = (Get-ChildItem $rolesPath -Recurse -Filter "*.role.md" | Measure-Object).Count
    TotalWorkflows = (Get-ChildItem $rolesPath -Recurse -Filter "*.workflow.md" | Measure-Object).Count
    TotalRunbooks  = (Get-ChildItem $rolesPath -Recurse -Filter "*.runbook.md" | Measure-Object).Count
    TotalSkills    = (Get-ChildItem $rolesPath -Recurse -Filter "*.knowledge.md" | Measure-Object).Count
    TotalTemplates = (Get-ChildItem $rolesPath -Recurse -Filter "*.template.md" | Measure-Object).Count
    TotalMarkdown  = (Get-ChildItem $rolesPath -Recurse -Filter "*.md" | Measure-Object).Count
}

Write-Host "=== Agent System Statistics ===" -ForegroundColor Cyan
$stats.GetEnumerator() | Sort-Object Key | Format-Table -AutoSize
```

**Per-agent breakdown**:

```powershell
# Per-agent file counts
$domainFolders = Get-ChildItem $rolesPath -Directory

foreach ($domain in $domainFolders) {
    Write-Host "`n[$($domain.Name)]" -ForegroundColor Cyan
    $agents = Get-ChildItem $domain.FullName -Directory
    foreach ($agent in $agents) {
        $roleCount     = (Get-ChildItem $agent.FullName -Recurse -Filter "*.role.md" | Measure-Object).Count
        $workflowCount = (Get-ChildItem $agent.FullName -Recurse -Filter "*.workflow.md" | Measure-Object).Count
        $runbookCount  = (Get-ChildItem $agent.FullName -Recurse -Filter "*.runbook.md" | Measure-Object).Count
        $skillCount    = (Get-ChildItem $agent.FullName -Recurse -Filter "*.knowledge.md" | Measure-Object).Count
        $templateCount = (Get-ChildItem $agent.FullName -Recurse -Filter "*.template.md" | Measure-Object).Count

        Write-Host "  $($agent.Name): Role=$roleCount, Workflows=$workflowCount, Runbooks=$runbookCount, Skills=$skillCount, Templates=$templateCount"
    }
}
```

#### 7.2 Update Context Structure Management Skill

**Acting**: Update the statistics table in `context_structure_management.knowledge.md`:

```markdown
File: src/agent-system/database/knowledge/management/context_structure_management.knowledge.md

Section: ## Structural Statistics (Reference)

Update the agent statistics table with current collected values.
Also update: last_updated field in frontmatter.
```

---

### 8. Knowledge Map Update (Fact Check Pattern)

**Goal**: Keep `knowledge_map.md` synchronized with the actual file system state.

#### 8.1 Add New Files

**Acting**: Find files present on disk but missing from the Knowledge Map:

```powershell
$knowledgeMapPath = "src/agent-system/database/standards/core/knowledge_map.md"
$mapContent = Get-Content $knowledgeMapPath -Raw

# Find all skill/knowledge files on disk
$diskFiles = Get-ChildItem "src/agent-system/database" -Recurse -Filter "*.knowledge.md" |
    Select-Object -ExpandProperty FullName |
    ForEach-Object { $_ -replace [regex]::Escape((Get-Location).Path + '\'), '' -replace '\\', '/' }

# Find files not mentioned in knowledge_map.md
$missingFromMap = $diskFiles | Where-Object {
    $mapContent -notmatch [regex]::Escape($_)
}

if ($missingFromMap) {
    Write-Host "Files on disk NOT in knowledge_map.md:" -ForegroundColor Yellow
    $missingFromMap | ForEach-Object { Write-Host "  - $_" }
} else {
    Write-Host "All disk files are in knowledge_map.md" -ForegroundColor Green
}
```

**Add missing files to the Knowledge Map**:

- Determine the correct section (Skill Catalog, Workflows, Templates, etc.)
- Add entry following the existing table format
- Update `last_updated` and `version` in frontmatter

#### 8.2 Remove Broken References

**Acting**: Find Knowledge Map entries pointing to non-existent files:

```powershell
# Re-use validation script from Step 5.2
# Same logic — run $missingFiles check
```

**For broken references**:

- If the file was moved: Update the path
- If the file was deleted: Remove the entry
- If the file should exist: Investigate (log in report)

---

### 9. Structure Maintenance Report (Fact Summary Pattern)

**Goal**: Comprehensive report of findings and actions taken.

#### Report Template

```markdown
## Knowledge Steward - Structure Maintenance Report

**Date:** {YYYY-MM-DD}
**Scope:** Agent System folder structure audit
**Trigger:** {Monthly maintenance / Post-Epic cycle / Manual request}

---

### 1. Structure Audit

**Agents Inventoried:** {X}
**Issues Found:**
- Missing role file: {list}
- Unexpected folders: {list}
- Core files status: ✅ All present / ❌ Missing: {list}

---

### 2. Frontmatter Validation

**Files Checked:** {X}
**Missing Frontmatter:** {X files — list if any}
**Invalid id fields:** {X files — list if any}
**Invalid type fields:** {X files — list if any}

---

### 3. Naming Convention Check

**Incorrect folder names:** {X — list and action taken}
**Non-standard file names:** {X — list and action taken}

---

### 4. Orphan and Cleanup

**Empty folders:** {X — list and action taken}
**Debug/temp files found:** {X — list, ownership noted}

---

### 5. Link Integrity

**Broken internal links:** {X — list if any}
**Knowledge Map broken refs:** {X — list if any}

---

### 6. Template Compliance

**Files reviewed:** {X}
**Non-compliant files:** {X — list and action taken}

---

### 7. Statistics Updated

| Agent | Role | Workflow | Runbook | Skills | Templates |
|-------|:----:|:--------:|:-------:|:------:|:---------:|
| orchestrator | ✅ | ✅ (3) | ✅ | ✅ (0) | ✅ (0) |
| knowledge_steward | ✅ | ✅ (7) | ✅ | ✅ (2) | ✅ (3) |
| ... | ... | ... | ... | ... | ... |

---

### 8. Knowledge Map

**New files added to map:** {X}
**Broken references removed/fixed:** {X}

---

### Summary

- 🟢 **Fully compliant**: {X agents}
- 🟡 **Minor issues**: {X agents — list}
- 🔴 **Critical issues**: {X agents — list}

**Actions Completed**: {summary of changes}
**Actions Pending**: {items requiring Orchestrator consultation}
```

---

## DoD (Definition of Done)

The structure maintenance workflow is complete when:

### Structural Integrity

- [ ] All agent folders inventoried
- [ ] Empty folders documented / `.gitkeep` added or documented
- [ ] Core standards files verified present
- [ ] Debug/temp files logged

### Frontmatter Consistency

- [ ] All `.md` files have valid YAML frontmatter
- [ ] All `id:` fields follow the naming convention
- [ ] All `type:` fields contain a valid value
- [ ] All `last_updated:` dates are present

### Link Integrity

- [ ] All internal Markdown links validated
- [ ] Knowledge Map broken references resolved
- [ ] No broken links in any `.md` file

### Documentation Quality

- [ ] Statistics in `context_structure_management.knowledge.md` updated
- [ ] Knowledge Map synchronized with actual file system
- [ ] Structure maintenance report created and delivered to Orchestrator

### Cleanup

- [ ] Incorrectly named folders renamed (or logged)
- [ ] Non-compliant files fixed (or logged)

---

## Workflow Triggers

When to start this structure maintenance workflow:

1. **Monthly maintenance**: First maintenance Monday of each month
2. **After major Epic cycle**: After multiple Epics close (batch check)
3. **After major refactoring**: After major structural changes (new agents, folder reorganization)
4. **Orchestrator request**: If structural inconsistencies are reported

---

## Changelog

| Date | Version | Author | Change Description |
|:-----|:--------|:-------|:-------------------|
| 2026-02-15 | 1.0 | knowledge_steward | Initial creation — basic structure audit |
| 2026-02-17 | 1.1 | knowledge_steward | Added frontmatter validation (Step 2) |
| 2026-02-18 | 1.2 | knowledge_steward | Added naming convention check (Step 3) and orphan cleanup (Step 4) |
| 2026-02-19 | 1.3 | knowledge_steward | Added link integrity check (Step 5) and template compliance (Step 6) |
| 2026-02-20 | 2.0 | knowledge_steward | Major refactoring — moved frontmatter enrichment to separate workflow; added statistics collection (Step 7) and knowledge map update (Step 8); finalized DoD and Changelog |
| 2026-03-01 | 2.1 | knowledge_steward | Rewritten to English/UTF-8; filename-only references; `description` field added to frontmatter |

---

## Related Documents

- **context_structure_management.knowledge.md** — Agent system structural knowledge
- **knowledge_map.md** — Global knowledge registry
- **knowledge_steward_frontmatter_enrichment.workflow.md** — Frontmatter enrichment sub-workflow
- **knowledge_steward_calibration.workflow.md** — Knowledge integration after Architect Sign-off
- **knowledge_steward_communication_hub_archival.workflow.md** — Communication Hub archival workflow
- **role_structure.template.md** — Role meta-template
- **workflow_structure.template.md** — Workflow meta-template
- **skill_structure.template.md** — Skill meta-template
