param(
    [string]$RootPath = ".\docs"
)

Write-Host "Unprotecting Core System files in $RootPath..." -ForegroundColor Yellow

$patterns = @("*.policy.md", "*.role.md")

if (-not (Test-Path $RootPath)) {
    Write-Warning "The specified path does not exist: $RootPath"
    exit 1
}

$unlockedCount = 0

foreach ($pattern in $patterns) {
    $files = Get-ChildItem -Path $RootPath -Filter $pattern -Recurse -File

    foreach ($file in $files) {
        if ($file.IsReadOnly) {
            $file.IsReadOnly = $false
            Write-Host "Unlocked: $($file.FullName)" -ForegroundColor Green
            $unlockedCount++
        }
        else {
            Write-Host "Already Unlocked: $($file.FullName)" -ForegroundColor DarkGray
        }
    }
}

Write-Host "Success: $unlockedCount core files were unprotected." -ForegroundColor Yellow




