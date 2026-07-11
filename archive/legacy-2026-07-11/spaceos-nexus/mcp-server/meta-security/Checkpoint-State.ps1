param(
    [string]$RepoPath = ".",
    [switch]$Push
)

Write-Host "Creating pre-maintenance git checkpoint in $RepoPath..." -ForegroundColor Cyan

Push-Location $RepoPath
try {
    $status = git status --short
    if ([string]::IsNullOrWhiteSpace($status)) {
        Write-Host "No uncommitted changes detected. Nothing to checkpoint." -ForegroundColor Green
        exit 0
    }

    Write-Host "Changes detected. Staging files..." -ForegroundColor Yellow
    git add .

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $commitMsg = "chore(agent-system): auto-checkpoint state before maintenance [$timestamp]"

    Write-Host "Committing changes..." -ForegroundColor Yellow
    git commit -m $commitMsg

    if ($Push) {
        Write-Host "Pushing to remote repository..." -ForegroundColor Yellow
        git push
    }

    Write-Host "Success: Checkpoint commit created successfully!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to create checkpoint: $_"
    exit 1
}
finally {
    Pop-Location
}




