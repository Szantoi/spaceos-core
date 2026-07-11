#!/usr/bin/env pwsh
<#
.SYNOPSIS
EPIC-10 Phase 2 — Daily Monitoring Quick Check
Coordinator script to collect standups and check progress (runs in <2 minutes)

.DESCRIPTION
Checks for standup files from Dev A/B/C at 09:00, 12:00, 18:00 UTC
Reports missing standups, RED statuses, completion reports

.EXAMPLE
.\Monitor-EPIC10-Phase2.ps1 -Cycle morning
.\Monitor-EPIC10-Phase2.ps1 -Cycle midday
.\Monitor-EPIC10-Phase2.ps1 -Cycle evening

.NOTES
Run at: 09:00, 12:00, 18:00 UTC
Requires: PowerShell 7+
#>

param(
    [ValidateSet("morning", "midday", "evening")]
    [string]$Cycle = "morning",

    [string]$EpicPath = "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_10/devs"
)

# Colors
$GREEN = "`e[32m"
$RED = "`e[31m"
$YELLOW = "`e[33m"
$RESET = "`e[0m"

function Write-Status {
    param([string]$Text, [string]$Color)
    Write-Host "$Color$Text$RESET"
}

function Check-Standup {
    param(
        [string]$DevName,
        [string]$TaskId,
        [string]$Cycle,
        [string]$BasePath
    )

    $today = Get-Date -Format "yyyy-MM-dd"
    $feedbackPath = "$BasePath/$DevName/feedback"

    if (-not (Test-Path $feedbackPath)) {
        Write-Status "❌ $DevName: Feedback folder missing!" Red
        return $false
    }

    $standupFile = "$feedbackPath/DEV-$(($DevName.ToUpper() -replace 'dev-'))-$TaskId-STANDUP-$(($Cycle.ToUpper()))-$today.md"

    if (Test-Path $standupFile) {
        Write-Status "✅ $DevName: Standup received" Green

        # Quick parse standup status
        $content = Get-Content $standupFile -Raw
        if ($content -match "status:\s*red") {
            Write-Status "   ⚠️  RED STATUS FOUND - blocker escalation needed!" Red
            return $false
        } elseif ($content -match "status:\s*yellow") {
            Write-Status "   🟡 Yellow status - minor delay" Yellow
        }
        return $true
    } else {
        Write-Status "❌ $DevName: Standup missing! Expected: $(Split-Path -Leaf $standupFile)" Red
        return $false
    }
}

function Check-CompletionReport {
    param(
        [string]$DevName,
        [string]$TaskId,
        [string]$BasePath
    )

    $feedbackPath = "$BasePath/$DevName/feedback"
    $completionFile = "$feedbackPath/DEV-$(($DevName.ToUpper() -replace 'dev-'))-COMPLETION-$TaskId.md"

    if (Test-Path $completionFile) {
        Write-Status "📦 $DevName: Completion report received!" Green

        # Count AC from file
        $content = Get-Content $completionFile -Raw
        if ($content -match "AC.*?(\d+)/(\d+)") {
            $passed = $matches[1]
            $total = $matches[2]
            Write-Status "   AC: $passed/$total verified" $(if ($passed -eq $total) { "Green" } else { "Red" })
            return ($passed -eq $total)
        }
    }
    return $false
}

# Main Monitoring Loop
Write-Host ""
Write-Status "═══════════════════════════════════════════════════" Blue
Write-Status "EPIC-10 Phase 2 — Daily Monitoring: $($Cycle.ToUpper())" Cyan
Write-Status "═══════════════════════════════════════════════════" Blue
Write-Host ""

$epicFullPath = (Resolve-Path $EpicPath -ErrorAction SilentlyContinue).Path
if (-not $epicFullPath) {
    Write-Status "❌ ERROR: Epic path not found: $EpicPath" Red
    exit 1
}

$devs = @(
    @{ Name = "dev-a"; Task = "TASK-10-06" }
    @{ Name = "dev-b"; Task = "TASK-10-07" }
    @{ Name = "dev-c"; Task = "TASK-10-08" }
)

$allGreen = $true
$completions = @()

foreach ($dev in $devs) {
    Write-Host ""
    Write-Status "📋 Checking: $($dev.Name)" Cyan
    Write-Host ""

    # Check standup
    $standupOk = Check-Standup -DevName $dev.Name -TaskId $dev.Task -Cycle $Cycle -BasePath $epicFullPath
    if (-not $standupOk) {
        $allGreen = $false
    }

    # Check completion (only on evening cycle)
    if ($Cycle -eq "evening") {
        $completionOk = Check-CompletionReport -DevName $dev.Name -TaskId $dev.Task -BasePath $epicFullPath
        if ($completionOk) {
            $completions += $dev.Name
        }
    }
}

# Summary
Write-Host ""
Write-Status "─────────────────────────────────────────────────" Cyan
if ($allGreen) {
    Write-Status "✅ All standups received!" Green
} else {
    Write-Status "❌ Some standups missing or RED status found" Red
}

if ($completions.Count -gt 0) {
    Write-Status "📦 Completions ready for validation: $($completions -join ', ')" Green
}

Write-Host ""
Write-Status "Next steps:" Yellow
Write-Status "1. Review standups in feedback/ folders" Yellow
Write-Status "2. Update DAILY-MONITORING.md matrix" Yellow
Write-Status "3. Post feedback files for blockers (if any)" Yellow
if ($completions.Count -gt 0) {
    Write-Status "4. Review completion reports for AC validation" Yellow
}

Write-Host ""
Write-Status "═══════════════════════════════════════════════════" Blue
Write-Status "Monitoring cycle complete. Check back at next standup time." Blue
Write-Status "═══════════════════════════════════════════════════" Blue

exit $(if ($allGreen) { 0 } else { 1 })
