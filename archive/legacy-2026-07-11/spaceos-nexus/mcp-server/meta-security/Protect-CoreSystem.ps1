param(
    [string]$RootPath = ".\docs"
)

Write-Host "Protecting Core System files in $RootPath..." -ForegroundColor Cyan

$patterns = @("*.policy.md", "*.role.md")

if (-not (Test-Path $RootPath)) {
    Write-Warning "The specified path does not exist: $RootPath"
    exit 1
}

$lockedCount = 0

foreach ($pattern in $patterns) {
    $files = Get-ChildItem -Path $RootPath -Filter $pattern -Recurse -File

    foreach ($file in $files) {
        if (-not $file.IsReadOnly) {
            $file.IsReadOnly = $true
            Write-Host "Locked: $($file.FullName)" -ForegroundColor Green
            $lockedCount++
        }
        else {
            Write-Host "Already Locked: $($file.FullName)" -ForegroundColor DarkGray
        }
    }
}

Write-Host "Success: $lockedCount core files were protected." -ForegroundColor Cyan




