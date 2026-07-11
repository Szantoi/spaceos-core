param(
    [string]$RootPath = "."
)

$ErrorActionPreference = "Continue"

# TASK DATA
$tasksData = @{
    9 = @("Schema design & ERD", "SQLite migration setup", "Seeder: roles database", "Seeder: workflows", "RbacFilter refactor", "Bootstrap queries", "Testing & validation", "Performance tuning")
    10 = @("Bootstrap schema", "Tool handler layer", "Query optimization", "Session lifecycle", "E2E: both tracks", "Error handling layer", "Performance validation", "Documentation")
    11 = @("Middleware architecture", "Middleware implementation", "Error factory", "Audit logger", "Two-track routing", "RBAC refactor", "E2E: middleware errors", "E2E: routing")
    12 = @("Episode schema", "FTS5 indexing", "ChromaDB strategy", "store_experience() impl", "search_experience() FTS", "search_experience() semantic", "E2E: store & search", "Performance: optimize")
    13 = @("Discovery roles definition", "Seed discovery data", "reference_prior_discovery()", "submit_discovery_outcome()", "Two-track RBAC", "E2E: discovery flow", "E2E: tool blocking")
    14 = @("Transport abstraction", "HTTP transport", "Plugin system", "Bootstrap plugin", "Context & discovery plugins", "Memory plugins", "Legacy adapter", "Resource templates", "Sampling & completion", "Notification debouncing", "E2E: transports", "Architecture doc")
}

$template = @'
---
id: {TASKID}
title: "{TITLE}"
type: task
epic: EPIC-{EPIC}
milestone: M02
project: mcp-maintenance
scope: [implementation, testing]
status: pending
priority: P0
assignee: [role: backend_developer, qa_tester, architect]
created: {DATE}
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
depends_on: []
related_tasks: []
blockers: []
---

# {TASKID}: {TITLE}

## Purpose

Implementation task for M02 milestone. Reference EPIC-{EPIC} goal.md and state.md for detailed requirements.

---

## Acceptance Criteria

### Functional Requirements
- [ ] Implementation matches technical specification
- [ ] All required functionality is operational
- [ ] Integration with dependent systems verified

### Quality Requirements
- [ ] Code follows project conventions (see: database/standards/)
- [ ] Code coverage ≥ 80%
- [ ] Documentation updated
- [ ] Security review completed (if applicable)

### Delivery
- [ ] Deliverable artifact(s) created
- [ ] Peer review completed
- [ ] Ready for downstream tasks
- [ ] Definition of Done satisfied

---

## Input / Output

### Input
| Source | Format | Notes |
|:-------|:-------|:------|
| EPIC-{EPIC}/goal.md | Markdown | Strategic requirements |
| EPIC-{EPIC}/state.md | Markdown | Detailed specifications |
| database/standards/ | Markdown | Project standards |

### Output
| Artifact | Format | Location | Consumer |
|:---------|:-------|:---------|:---------|
| Implementation | TypeScript | `src/` | Downstream |
| Tests | TypeScript | `src/tests/` | QA |
| Documentation | Markdown | `Docs/` | Team |

---

## Technical Specification

Reference EPIC-{EPIC}/state.md for implementation details.

### Test Scenarios

| # | Scenario | Expected | Status |
|:--|:---------|:---------|:------:|
| 1 | Happy path | [per spec] | ☐ |
| 2 | Edge case | [per spec] | ☐ |
| 3 | Error case | [error handling] | ☐ |

---

## Blockers / Dependencies

| Item | Status | Risk |
|:-----|:-------|:-----|
| Upstream EPIC | ⏳ Pending | 🟡 Monitor |

---

## Effort Estimate

- Design: 4h
- Implementation: 16h
- Testing: 8h
- Documentation: 4h
- **Total: 32h (1 week)**

---

## Related Documentation

- `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_{EPIC}/goal.md`
- `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_{EPIC}/state.md`
- `database/standards/`

---

## Success Checklist (DoD)

- ✅ AC satisfied
- ✅ Tests passing
- ✅ Code reviewed
- ✅ Documentation updated
- ✅ Security review done (if applicable)
- ✅ Ready for deployment

'@

$today = Get-Date -Format "yyyy-MM-dd"
$totalCreated = 0

Write-Host "🚀 Generating 51 M02 Task files..." -ForegroundColor Cyan
Write-Host ""

foreach ($epicNum in $tasksData.Keys | Sort-Object) {
    $tasks = $tasksData[$epicNum]
    $taskDir = Join-Path $RootPath "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_02\epic_$epicNum\tasks"

    # Create directory
    if (-not (Test-Path $taskDir)) {
        New-Item -ItemType Directory -Path $taskDir -Force | Out-Null
    }

    # Generate task files
    for ($i = 0; $i -lt $tasks.Count; $i++) {
        $taskNum = $i + 1
        $taskId = "TASK-$epicNum-$($taskNum.ToString('00'))"
        $title = $tasks[$i]

        $content = $template `
            -replace '{TASKID}', $taskId `
            -replace '{TITLE}', $title `
            -replace '{EPIC}', $epicNum `
            -replace '{DATE}', $today

        $filePath = Join-Path $taskDir "$taskId.md"
        Set-Content -Path $filePath -Value $content -Encoding UTF8 -Force
        Write-Host "✅ Created: $taskId - $title" -ForegroundColor Green
        $totalCreated++
    }
    Write-Host ""
}

Write-Host ""
Write-Host "✅ GENERATION COMPLETE!" -ForegroundColor Cyan
Write-Host "📊 Total files created: $totalCreated / 51" -ForegroundColor Cyan
Write-Host ""
Write-Host "📂 Distribution:" -ForegroundColor Cyan
Write-Host "  EPIC-09: 8 tasks  ✅" -ForegroundColor Gray
Write-Host "  EPIC-10: 8 tasks  ✅" -ForegroundColor Gray
Write-Host "  EPIC-11: 8 tasks  ✅" -ForegroundColor Gray
Write-Host "  EPIC-12: 8 tasks  ✅" -ForegroundColor Gray
Write-Host "  EPIC-13: 7 tasks  ✅" -ForegroundColor Gray
Write-Host "  EPIC-14: 12 tasks ✅" -ForegroundColor Gray
