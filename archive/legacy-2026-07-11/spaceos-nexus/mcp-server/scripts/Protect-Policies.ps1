<#
.SYNOPSIS
    Protects MCP Role Policy JSON files by setting them to Read-Only.
.DESCRIPTION
    This script iterates through all JSON files in the API's Config/Roles directory
    and applies the Read-Only attribute to prevent accidental or malicious modification.
#>

$rolesPath = Join-Path $PSScriptRoot "..\..\JoineryTech.Flow.Api\Config\Roles"
$fullPath = Resolve-Path $rolesPath

if (Test-Path $fullPath) {
    Write-Host "Protecting policy files in: $fullPath" -ForegroundColor Cyan
    $files = Get-ChildItem -Path $fullPath -Filter "*.json"

    foreach ($file in $files) {
        if (-not $file.IsReadOnly) {
            $file.Attributes = $file.Attributes -bor [System.IO.FileAttributes]::ReadOnly
            Write-Host "Set Read-Only: $($file.Name)" -ForegroundColor Green
        }
        else {
            Write-Host "Already protected: $($file.Name)" -ForegroundColor Yellow
        }
    }
}
else {
    Write-Error "Policies directory not found at $fullPath"
}
