<#
.SYNOPSIS
    Run-PlaywrightTests - Runs the Playwright E2E tests and summarizes the results.
.DESCRIPTION
    This script is the QA Tester tool. It runs the Playwright API E2E test suite
    against the agent-system backend server, then provides a clean PASS/FAIL summary.

    Prerequisites:
    - The agent server is running: npm run dev (in src/agent-system/server)
    - Node.js and npm are installed

.PARAMETER ServerDir
    Path to the server directory. Default: auto-detected.

.EXAMPLE
    .\Run-PlaywrightTests.ps1
    .\Run-PlaywrightTests.ps1 -ServerDir "C:\Dev\JoineryTech.Flow\src\agent-system\server"
#>

param(
    [string]$ServerDir = ""
)

$ErrorActionPreference = "Stop"

# Auto-detect server directory
if (-not $ServerDir) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
    $ServerDir = Join-Path $repoRoot "src\agent-system\server"
}

if (-not (Test-Path $ServerDir)) {
    Write-Host "❌ Server directory not found: $ServerDir" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   🎭 Playwright E2E Test Runner          " -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📁 Server Dir: $ServerDir"
Write-Host ""

# Check if API server is online
Write-Host "🔌 Checking API server connectivity..." -ForegroundColor Gray
try {
    $pingResponse = Invoke-RestMethod -Uri "http://localhost:3000/mcp/tools" -Method Get -TimeoutSec 5
    Write-Host "   ✅ Server is online. Found $($pingResponse.tools.Count) MCP tools." -ForegroundColor Green
}
catch {
    Write-Host "   ❌ Server not reachable at http://localhost:3000" -ForegroundColor Red
    Write-Host "   Start it with: npm run dev (in $ServerDir)" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "▶️  Running Playwright tests..." -ForegroundColor Cyan

# Run playwright
$playwrightResult = & npx playwright test --reporter=list 2>&1
$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host $playwrightResult

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan

if ($exitCode -eq 0) {
    Write-Host "   ✅ RESULT: ALL TESTS PASSED" -ForegroundColor Green
    Write-Host "   Epic closure criterion met for E2E." -ForegroundColor Green
}
else {
    Write-Host "   ❌ RESULT: SOME TESTS FAILED (exit code: $exitCode)" -ForegroundColor Red
    Write-Host "   ⛔ Do NOT close this Epic until all tests pass." -ForegroundColor Red
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

exit $exitCode




