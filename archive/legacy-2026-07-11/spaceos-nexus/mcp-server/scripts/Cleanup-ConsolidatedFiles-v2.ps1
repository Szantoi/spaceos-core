# Cleanup Consolidated Files - Detailed version
# 2026-03-08

$ErrorActionPreference = "SilentlyContinue"

Write-Host "🧹 Detailed Cleanup of Redundant Files" -ForegroundColor Green

# Navigate to project root
Set-Location "c:\Users\szant\Documents\Development\JoineryTech.McpServer"

# ============================================================================
# MILESTONE 01
# ============================================================================

Write-Host "`n📁 M01 Cleanup..." -ForegroundColor Cyan

$m01 = "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_01"

# Epic 00
Write-Host "  ├─ epic_00: Deleting templates & old reports..."
@(
    "$m01\epic_00\tasks",
    "$m01\epic_00\COMPLETION_CHECKLIST.md",
    "$m01\epic_00\DAILY_STANDUP_TEMPLATE.md",
    "$m01\epic_00\REMEDIATION_SUMMARY.md",
    "$m01\epic_00\TECH_LEAD_BRIEF.md",
    "$m01\epic_00\TECH_LEAD_GUIDE.md",
    "$m01\epic_00\README.md"
) | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item $_ -Recurse -Force
        Write-Host "     ✓ Deleted: $(Split-Path $_ -Leaf)"
    }
}

# Epic 01
Write-Host "  ├─ epic_01: Deleting tasks..."
if (Test-Path "$m01\epic_01\tasks") {
    Remove-Item "$m01\epic_01\tasks" -Recurse -Force
    Write-Host "     ✓ Deleted: tasks/"
}

# Epic 02
Write-Host "  ├─ epic_02: Deleting tasks..."
if (Test-Path "$m01\epic_02\tasks") {
    Remove-Item "$m01\epic_02\tasks" -Recurse -Force
    Write-Host "     ✓ Deleted: tasks/"
}

# Epic 08
Write-Host "  ├─ epic_08: Deleting reports, audits & tasks..."
@(
    "$m01\epic_08\tasks",
    "$m01\epic_08\AUDIT-CLOSURE-REPORT.md",
    "$m01\epic_08\AUDIT-DOCUMENTATION-INDEX.md",
    "$m01\epic_08\BEST-PRACTICES-AUDIT-SUMMARY.md",
    "$m01\epic_08\CRITICAL-ANALYSIS-AUDIT-REPORT.md",
    "$m01\epic_08\DOCUMENTATION-INDEX.md",
    "$m01\epic_08\EPIC-08-COMPLETION-REPORT.md",
    "$m01\epic_08\FIX-PROPOSAL-EXPONENTIAL-BACKOFF-JITTER.md",
    "$m01\epic_08\TASK-08-02-SQLite-Locking-Guide.md",
    "$m01\epic_08\README.md"
) | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item $_ -Recurse -Force
        Write-Host "     ✓ Deleted: $(Split-Path $_ -Leaf)"
    }
}

# Epic 09
Write-Host "  ├─ epic_09: Deleting tasks..."
if (Test-Path "$m01\epic_09\tasks") {
    Remove-Item "$m01\epic_09\tasks" -Recurse -Force
    Write-Host "     ✓ Deleted: tasks/"
}

# M01 root templates
Write-Host "  └─ M01 root: Deleting index templates..."
if (Test-Path "$m01\M01-DOCUMENTATION-INDEX.md") {
    Remove-Item "$m01\M01-DOCUMENTATION-INDEX.md" -Force
    Write-Host "     ✓ Deleted: M01-DOCUMENTATION-INDEX.md"
}

# ============================================================================
# MILESTONE 02
# ============================================================================

Write-Host "`n📁 M02 Cleanup..." -ForegroundColor Cyan

$m02 = "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_02"

# Clean all epicsDElete task folders from each epic
@(9, 10, 11, 12, 13, 14) | ForEach-Object {
    $epicNum = $_
    Write-Host "  ├─ epic_$epicNum`: Deleting tasks..."
    if (Test-Path "$m02\epic_$epicNum\tasks") {
        Remove-Item "$m02\epic_$epicNum\tasks" -Recurse -Force
        Write-Host "     ✓ Deleted: tasks/"
    }
}

# Delete old status directories
Write-Host "  ├─ Deleting 02-planning/..."
if (Test-Path "$m02\02-planning") {
    Remove-Item "$m02\02-planning" -Recurse -Force
    Write-Host "     ✓ Deleted: 02-planning/"
}

Write-Host "  ├─ Deleting 05-status/..."
if (Test-Path "$m02\05-status") {
    Remove-Item "$m02\05-status" -Recurse -Force
    Write-Host "     ✓ Deleted: 05-status/"
}

Write-Host "  └─ M02 cleanup complete"

# ============================================================================
# ROOT CLEANUP
# ============================================================================

Write-Host "`n📁 Root Directory Cleanup..." -ForegroundColor Cyan

$rootTestFiles = @(
    "test-full.txt",
    "test-output.txt",
    "test-results-v2.txt",
    "test-results-fixed.txt",
    "test-agentdb-failures.txt",
    "wal-test.txt",
    "full-output.txt"
)

$rootTestFiles | ForEach-Object {
    if (Test-Path $_) {
        Remove-Item $_ -Force
        Write-Host "  ✓ Deleted: $_"
    }
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-Host "`n✅ Cleanup Complete!`n" -ForegroundColor Green

Write-Host "📊 Summary:"
Write-Host "  ├─ M01: Removed TASK files, templates, reports"
Write-Host "  ├─ M02: Removed TASK files, old status reports"
Write-Host "  └─ Root: Removed test output artifacts"

Write-Host "`n📁 What remains (SSOT only):"
Write-Host "  ├─ epic_*/goal.md"
Write-Host "  ├─ epic_*/state.md"
Write-Host "  ├─ epic_*/implementation-summary/"
Write-Host "  ├─ M01_CONSOLIDATED_SUMMARY.md"
Write-Host "  └─ M02_CONSOLIDATED_SUMMARY.md"

Write-Host "`n🎯 Next: Commit changes to git"
Write-Host ""
