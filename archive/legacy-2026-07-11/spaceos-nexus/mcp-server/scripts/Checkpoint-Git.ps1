<#
.SYNOPSIS
    Creates an automated Git checkpoint (commit).
.DESCRIPTION
    Stages all changes and creates a commit with a timestamped message
    to ensure traceability and recoverability during agent operations.
#>

param(
    [string]$Message = "Agent Checkpoint"
)

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$finalMessage = "[$timestamp] $Message"

Write-Host "Creating Git checkpoint: $finalMessage" -ForegroundColor Cyan

# Check if it's a git repo
if (git rev-parse --is-inside-work-tree 2>$null) {
    git add .
    git commit -m $finalMessage
    Write-Host "Checkpoint created successfully." -ForegroundColor Green
}
else {
    Write-Warning "Not a Git repository. Skipping checkpoint."
}
