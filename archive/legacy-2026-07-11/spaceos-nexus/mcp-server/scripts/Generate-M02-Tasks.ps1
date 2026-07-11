#Requires -Version 7.0
<#
.SYNOPSIS
Generates 51 TASK-XX-YY.md files for M02 EPICs in JoineryTech MCP Server.

.DESCRIPTION
Auto-generates task files for EPIC-09 through EPIC-14 using the template structure.
Creates directories as needed and outputs file paths.

.PARAMETER RootPath
Workspace root path (default: current directory)

.PARAMETER Force
Overwrite existing files if they exist

.EXAMPLE
.\Generate-M02-Tasks.ps1 -RootPath "c:\Users\szant\Documents\Development\JoineryTech.McpServer"
#>

param(
    [string]$RootPath = ".",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# ========== TASK DATA DEFINITIONS ==========
$tasksData = @{
    "EPIC-09" = @(
        @{ title = "Schema design & ERD"; description = "Design database schema and create Entity Relationship Diagram for workflow state tracking and resource management." }
        @{ title = "SQLite migration setup"; description = "Create SQLite migration framework, script runner, and schema initialization." }
        @{ title = "Seeder: roles database"; description = "Implement database seeder for role definitions with proper constraints and relationships." }
        @{ title = "Seeder: workflows"; description = "Create workflow template seeder with state transitions and validation rules." }
        @{ title = "RbacFilter refactor"; description = "Refactor RbacFilter to support database-driven role definitions and permissions." }
        @{ title = "Bootstrap queries"; description = "Implement efficient bootstrap query patterns for session initialization." }
        @{ title = "Testing & validation"; description = "Write comprehensive unit and integration tests for all database operations." }
        @{ title = "Performance tuning"; description = "Optimize database indexes, query execution plans, and connection pooling." }
    )
    "EPIC-10" = @(
        @{ title = "Bootstrap schema"; description = "Create bootstrap query schema to efficiently initialize session state from database." }
        @{ title = "Tool handler layer"; description = "Build tool handler that bridges MCP requests to database-driven role definitions." }
        @{ title = "Query optimization"; description = "Optimize bootstrap queries for minimal latency and resource usage." }
        @{ title = "Session lifecycle"; description = "Implement session lifecycle hooks (create, resume, checkpoint, cleanup)." }
        @{ title = "E2E: both tracks"; description = "End-to-end tests covering both business and technical RBAC tracks." }
        @{ title = "Error handling layer"; description = "Build comprehensive error handling for database failures and recovery." }
        @{ title = "Performance validation"; description = "Validate bootstrap performance meets SLA requirements." }
        @{ title = "Documentation"; description = "Document bootstrap architecture, query patterns, and integration points." }
    )
    "EPIC-11" = @(
        @{ title = "Middleware architecture"; description = "Design middleware stack for authentication, RBAC, request validation, and response transformation." }
        @{ title = "Middleware implementation"; description = "Implement authentication, RBAC, logging, and error middleware." }
        @{ title = "Error factory"; description = "Create error factory for consistent error transformation and formatting." }
        @{ title = "Audit logger"; description = "Build audit logger middleware for compliance and troubleshooting." }
        @{ title = "Two-track routing"; description = "Implement routing that branches based on RBAC role (business/technical)." }
        @{ title = "RBAC refactor"; description = "Refactor RBAC enforcement using middleware patterns." }
        @{ title = "E2E: middleware errors"; description = "Test error handling across middleware stack with various failure scenarios." }
        @{ title = "E2E: routing"; description = "Test two-track routing behavior for different RBAC roles." }
    )
    "EPIC-12" = @(
        @{ title = "Episode schema"; description = "Define schema for storing episodes (experience instances with metadata)." }
        @{ title = "FTS5 indexing"; description = "Implement full-text search indexing for episode content and metadata." }
        @{ title = "ChromaDB strategy"; description = "Design ChromaDB integration strategy for semantic search." }
        @{ title = "store_experience() implementation"; description = "Implement tool to store experiences/episodes with proper validation." }
        @{ title = "search_experience() FTS"; description = "Implement FTS-based experience search using SQLite FTS5." }
        @{ title = "search_experience() semantic"; description = "Implement semantic search using ChromaDB vector similarity." }
        @{ title = "E2E: store & search"; description = "End-to-end tests for storing and searching experiences." }
        @{ title = "Performance: optimize"; description = "Optimize vector storage, indexing, and search performance." }
    )
    "EPIC-13" = @(
        @{ title = "Discovery roles definition"; description = "Define discovery-specific roles with appropriate tool permissions." }
        @{ title = "Seed discovery data"; description = "Create seeder for discovery templates, structures, and initial data." }
        @{ title = "reference_prior_discovery()"; description = "Implement tool to reference and integrate prior discovery findings." }
        @{ title = "submit_discovery_outcome()"; description = "Implement tool to formally submit and store discovery outcomes." }
        @{ title = "Two-track RBAC"; description = "Implement two-track RBAC for discovery domain tools." }
        @{ title = "E2E: discovery flow"; description = "End-to-end test of complete discovery workflow with RBAC." }
        @{ title = "E2E: tool blocking"; description = "Verify tool access is properly blocked for unauthorized roles." }
    )
    "EPIC-14" = @(
        @{ title = "Transport abstraction"; description = "Design abstract transport interface for pluggable communication layers." }
        @{ title = "HTTP transport"; description = "Implement HTTP transport layer implementation." }
        @{ title = "Plugin system"; description = "Create plugin system for registering and loading transports." }
        @{ title = "Bootstrap plugin"; description = "Implement bootstrap plugin for session initialization." }
        @{ title = "Context & discovery plugins"; description = "Implement context document and discovery workflow plugins." }
        @{ title = "Memory plugins"; description = "Implement in-memory cache and storage plugins." }
        @{ title = "Legacy adapter"; description = "Create adapter for legacy/external systems integration." }
        @{ title = "Resource templates"; description = "Define and implement resource template system." }
        @{ title = "Sampling & completion"; description = "Implement sampling strategies and completion optimization." }
        @{ title = "Notification debouncing"; description = "Implement notification debouncing and batching." }
        @{ title = "E2E: transports"; description = "End-to-end tests for all transport implementations." }
        @{ title = "Architecture doc"; description = "Complete documentation of transport architecture and plugin system." }
    )
}

# ========== TEMPLATE FUNCTION ==========
function Get-TaskContent {
    param(
        [string]$TaskId,
        [string]$Title,
        [string]$EpicNumber,
        [string]$Description
    )

    $epicPath = "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_${EpicNumber}"
    $today = Get-Date -Format "yyyy-MM-dd"

    $content = @"
---
id: $TaskId
title: "$Title"
type: task
epic: EPIC-$EpicNumber
milestone: M02
project: mcp-maintenance
scope: [implementation, testing]
status: pending
priority: P0
assignee: [role: backend_developer, qa_tester, architect]
created: $today
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
depends_on: []
related_tasks: []
blockers: []
---

# $TaskId: $Title

## Purpose

$Description

---

## Acceptance Criteria

### Functional Requirements
- [ ] Implementation matches technical specification
- [ ] All required functionality is operational
- [ ] Integration with dependent systems verified

### Quality Requirements
- [ ] Code follows project conventions (see: database/standards/00-foundation/)
- [ ] Code coverage ≥ 80%
- [ ] Documentation updated
- [ ] Security review completed (if applicable)

### Delivery
- [ ] Deliverable artifact(s) created
- [ ] Peer review completed
- [ ] Ready for handoff to downstream task
- [ ] Definition of Done checklist completed

---

## Input / Output

### Input
| Source | Format | Notes |
|:-------|:-------|:------|
| EPIC-$EpicNumber/goal.md | Markdown | Strategic requirements |
| EPIC-$EpicNumber/state.md | Markdown | Detailed specification |
| database/standards/ | Markdown | Standards & conventions |

### Output (Delivery Artifacts)
| Artifact | Format | Location | Consumed By |
|:---------|:-------|:---------|:------------|
| Implementation | TypeScript | \`src/\` | Downstream tasks |
| Tests | TypeScript | \`src/tests/\` | QA validation |
| Documentation | Markdown | \`Docs/\` | Team reference |

---

## Technical Specification

### Design / Implementation Approach

To be detailed during implementation planning phase. Reference EPIC-$EpicNumber state.md for detailed requirements.

### Data Structures / Interfaces

See EPIC-$EpicNumber state.md for interface and schema definitions.

### Database Changes (if applicable)

See EPIC-$EpicNumber state.md for migration specifications.

### API Endpoints / Tool Interfaces (if applicable)

See EPIC-$EpicNumber state.md for endpoint specifications.

---

## Test Scenarios

| # | Scenario | Input | Expected Output | Pass? |
|:--|:---------|:------|:---------------:|:-----:|
| 1 | Happy path — normal case | [spec dependent] | [spec dependent] | ☐ |
| 2 | Edge case — boundary | [spec dependent] | [spec dependent] | ☐ |
| 3 | Error case — validation | [spec dependent] | [error handling] | ☐ |

---

## Blockers / Dependencies

| Item | Status | Risk | Mitigation |
|:-----|:-------|:-----|:-----------|
| Upstream EPIC completion | ⏳ Pending | 🟡 | Monitor milestone progress |

---

## Effort Estimate

- **Design / Analysis:** 4 hours
- **Implementation:** 16 hours
- **Testing / QA:** 8 hours
- **Documentation:** 4 hours
- **Total:** 32 hours (1 week sprint)

---

## Related Documentation

- \`Docs/$epicPath/goal.md\` — Epic-level requirements
- \`Docs/$epicPath/state.md\` — Detailed specification
- \`database/standards/\` — Project standards and conventions

---

## Notes / Implementation Hints

- Refer to EPIC-$EpicNumber state.md for comprehensive technical context
- Follow project conventions in database/standards/00-foundation/
- Engage architect for design review before implementation
- Document decisions and trade-offs as implementation progresses

---

## Success Checklist (Definition of Done)

- ✅ All Acceptance Criteria passed
- ✅ Tests written and passing (unit + integration)
- ✅ Code peer-reviewed
- ✅ Documentation updated
- ✅ Security review completed (if applicable)
- ✅ Database migrations tested (if applicable)
- ✅ Related EPIC state.md references updated
- ✅ Ready for downstream tasks / deployment

---

## Implementation Notes (Updated During Work)

[Space for developer notes and learnings during implementation]
"@

    return $content
}

# ========== MAIN GENERATION LOGIC ==========
function Generate-TaskFiles {
    param(
        [string]$RootPath,
        [switch]$Force
    )

    # Resolve root path
    $RootPath = Resolve-Path $RootPath -ErrorAction Stop
    Write-Host "🚀 Generating 51 M02 Task files in: $RootPath" -ForegroundColor Cyan

    $generatedFiles = @()
    $taskCounter = 0
    $totalTasks = 51

    foreach ($epicKey in $tasksData.Keys | Sort-Object) {
        $epicNumber = $epicKey -replace "EPIC-", ""
        $tasksArray = $tasksData[$epicKey]
        $taskCount = $tasksArray.Count

        # Ensure directory exists
        $taskDir = Join-Path $RootPath "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_${epicNumber}/tasks"
        if (-not (Test-Path $taskDir)) {
            New-Item -ItemType Directory -Path $taskDir -Force | Out-Null
            Write-Host "📁 Created directory: $taskDir" -ForegroundColor Green
        }

        # Generate task files
        for ($i = 0; $i -lt $taskCount; $i++) {
            $taskCounter++
            $taskNum = $i + 1
            $taskId = "TASK-${epicNumber}-$(([string]$taskNum).PadLeft(2, '0'))"
            $task = $tasksArray[$i]
            $title = $task.title
            $description = $task.description

            # Generate content
            $content = Get-TaskContent -TaskId $taskId -Title $title -EpicNumber $epicNumber -Description $description

            # Create file
            $fileName = "${taskId}.md"
            $filePath = Join-Path $taskDir $fileName

            if ((Test-Path $filePath) -and -not $Force) {
                Write-Host "⏭️  Skipped: $fileName (already exists)" -ForegroundColor Yellow
            } else {
                Set-Content -Path $filePath -Value $content -Encoding UTF8 -Force
                Write-Host "✅ Created: $taskId ($taskCounter/$totalTasks)" -ForegroundColor Green
                $generatedFiles += $filePath
            }
        }
    }

    Write-Host ""
    Write-Host "✅ Generation Complete!" -ForegroundColor Cyan
    Write-Host "📊 Total files created: $($generatedFiles.Count) / $totalTasks" -ForegroundColor Cyan
    Write-Host ""

    return $generatedFiles
}

# ========== OUTPUT FILE PATHS ==========
function Write-FileManifest {
    param(
        [string[]]$Files,
        [string]$RootPath
    )

    $manifestPath = Join-Path $RootPath "scripts/m02-tasks-manifest.txt"
    $manifest = @("# M02 Task Files Generated", "# $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')", "") + $Files

    Set-Content -Path $manifestPath -Value ($manifest -join "`n") -Encoding UTF8
    Write-Host "📋 Manifest saved: $manifestPath" -ForegroundColor Cyan
}

# ========== EXECUTION ==========
try {
    $generated = Generate-TaskFiles -RootPath $RootPath -Force:$Force
    Write-FileManifest -Files $generated -RootPath $RootPath

    Write-Host ""
    Write-Host "📂 File Distribution:" -ForegroundColor Cyan
    Write-Host "  EPIC-09: 8 tasks  → milestone_02/epic_09/tasks/" -ForegroundColor Gray
    Write-Host "  EPIC-10: 8 tasks  → milestone_02/epic_10/tasks/" -ForegroundColor Gray
    Write-Host "  EPIC-11: 8 tasks  → milestone_02/epic_11/tasks/" -ForegroundColor Gray
    Write-Host "  EPIC-12: 8 tasks  → milestone_02/epic_12/tasks/" -ForegroundColor Gray
    Write-Host "  EPIC-13: 7 tasks  → milestone_02/epic_13/tasks/" -ForegroundColor Gray
    Write-Host "  EPIC-14: 12 tasks → milestone_02/epic_14/tasks/" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
}
