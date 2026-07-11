# Cleanup Consolidated Task Files - M01 & M02
# Töröl felesleges fájlokat (TASK-*.md, old reports, templates)
# 2026-03-08

$ErrorActionPreference = "SilentlyContinue"

Write-Host "🧹 Starting cleanup of consolidated files..." -ForegroundColor Green

# ============================================================================
# MILESTONE 01 CLEANUP
# ============================================================================

Write-Host "`n📁 M01 Cleanup..." -ForegroundColor Cyan

$m01Path = "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_01"
Set-Location $m01Path

# Epic 00 - Delete old files
Write-Host "  ├─ epic_00: Removing TASK files & templates..."
Remove-Item "epic_00\tasks\TASK-00-*.md" -Force 2>$null
Remove-Item "epic_00\COMPLETION_CHECKLIST.md" -Force 2>$null
Remove-Item "epic_00\DAILY_STANDUP_TEMPLATE.md" -Force 2>$null
Remove-Item "epic_00\REMEDIATION_SUMMARY.md" -Force 2>$null
Remove-Item "epic_00\TECH_LEAD_BRIEF.md" -Force 2>$null
Remove-Item "epic_00\TECH_LEAD_GUIDE.md" -Force 2>$null
Remove-Item "epic_00\README.md" -Force 2>$null
Remove-Item "epic_00\tasks" -Recurse -Force 2>$null

# Epic 01 - Clean up
Write-Host "  ├─ epic_01: Removing old files..."
Remove-Item "epic_01\tasks\*" -Recurse -Force 2>$null
Remove-Item "epic_01\tasks" -Recurse -Force 2>$null

# Epic 02 - Clean up
Write-Host "  ├─ epic_02: Removing old files..."
Remove-Item "epic_02\tasks\*" -Recurse -Force 2>$null
Remove-Item "epic_02\tasks" -Recurse -Force 2>$null

# Epic 08 - Delete old files & reports
Write-Host "  ├─ epic_08: Removing TASK files, reports & templates..."
Remove-Item "epic_08\tasks\TASK-08-*.md" -Force 2>$null
Remove-Item "epic_08\QUALITY_AUDIT_REPORT.md" -Force 2>$null
Remove-Item "epic_08\BEST_PRACTICES_AWS_OPTIMIZATION.md" -Force 2>$null
Remove-Item "epic_08\IMPLEMENTATION_PLAN_v1.md" -Force 2>$null
Remove-Item "epic_08\E2E_TEST_REPORT.md" -Force 2>$null
Remove-Item "epic_08\CODE_QUALITY_SUMMARY.md" -Force 2>$null
Remove-Item "epic_08\COMPLETION_CHECKLIST.md" -Force 2>$null
Remove-Item "epic_08\DAILY_STANDUP_TEMPLATE.md" -Force 2>$null
Remove-Item "epic_08\README.md" -Force 2>$null
Remove-Item "epic_08\tasks" -Recurse -Force 2>$null

# Epic 09 - Delete old files
Write-Host "  ├─ epic_09: Removing TASK files & templates..."
Remove-Item "epic_09\tasks\TASK-09-*.md" -Force 2>$null
Remove-Item "epic_09\COMPLETION_CHECKLIST.md" -Force 2>$null
Remove-Item "epic_09\DAILY_STANDUP_TEMPLATE.md" -Force 2>$null
Remove-Item "epic_09\README.md" -Force 2>$null
Remove-Item "epic_09\tasks" -Recurse -Force 2>$null

# Delete root M01 template files
Write-Host "  └─ M01 root: Removing old templates..."
Remove-Item "M01-DOCUMENTATION-INDEX.md" -Force 2>$null

Write-Host "  ✅ M01 cleanup complete`n"

# ============================================================================
# MILESTONE 02 CLEANUP
# ============================================================================

Write-Host "📁 M02 Cleanup..." -ForegroundColor Cyan

$m02Path = "..\milestone_02"
Set-Location $m02Path

# Clean all epic task directories
@(9..14) | ForEach-Object {
  $epicNum = $_
  Write-Host "  ├─ epic_$epicNum`: Removing old files..."
  Remove-Item "epic_$epicNum\tasks\*" -Recurse -Force 2>$null
  Remove-Item "epic_$epicNum\tasks" -Recurse -Force 2>$null
  Remove-Item "epic_$epicNum\*.md" -Force -Exclude @("goal.md", "state.md", "README.md") 2>$null
  Remove-Item "epic_$epicNum\COMPLETION_CHECKLIST.md" -Force 2>$null
  Remove-Item "epic_$epicNum\DAILY_STANDUP_TEMPLATE.md" -Force 2>$null
  Remove-Item "epic_$epicNum\README.md" -Force 2>$null
}

# Delete old status report directories
Write-Host "  ├─ Removing 02-planning/ directory..."
Remove-Item "02-planning" -Recurse -Force 2>$null

Write-Host "  ├─ Removing 05-status/ directory..."
Remove-Item "05-status" -Recurse -Force 2>$null

Write-Host "  └─ M02 cleanup complete`n"

# ============================================================================
# ROOT DIRECTORY CLEANUP (Optional)
# ============================================================================

Write-Host "📁 Root Directory Cleanup (optional test artifacts)..." -ForegroundColor Cyan

Set-Location "..\..\..\..\.."

Write-Host "  ├─ Removing test output .txt files..."
Remove-Item "test-full.txt" -Force 2>$null
Remove-Item "test-output.txt" -Force 2>$null
Remove-Item "test-results*.txt" -Force 2>$null
Remove-Item "wal-test.txt" -Force 2>$null
Remove-Item "full-output.txt" -Force 2>$null
Remove-Item "test-agentdb-failures.txt" -Force 2>$null

Write-Host "  └─ Root cleanup complete`n"

# ============================================================================
# VERIFICATION
# ============================================================================

Write-Host "✅ Cleanup Complete!" -ForegroundColor Green
Write-Host "`n📊 Verification:"

Write-Host "`n  Milestone_01/epic_00 contents:"
Get-ChildItem "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_01\epic_00" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    - $($_.Name)" }

Write-Host "`n  Milestone_01/epic_08 contents:"
Get-ChildItem "Docs\mcp-context-server\delivery\mcp-maintenance\milestones\milestone_01\epic_08" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "    - $($_.Name)" }

Write-Host "`n🎯 That's it! Old files cleaned up."
Write-Host "📝 New consolidated summaries are ready:"
Write-Host "   - Docs/mcp-context-server/delivery/.../milestone_01/M01_CONSOLIDATED_SUMMARY.md"
Write-Host "   - Docs/mcp-context-server/delivery/.../milestone_02/M02_CONSOLIDATED_SUMMARY.md"
Write-Host ""
