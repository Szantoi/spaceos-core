$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "--- RBAC Prototype Test ---"

$ApiUrl = "http://localhost:5058/mcp/messages"

$Payload = @{
    jsonrpc = "2.0"
    id      = 1
    method  = "tools/list"
} | ConvertTo-Json

function Test-Role {
    param([string]$Role)

    Write-Host "`n>> Teszteles Role: $Role"

    $Headers = @{
        "Content-Type" = "application/json"
    }

    if ($Role -ne "None") {
        $Headers["X-Agent-Role"] = $Role
    }

    try {
        $Response = Invoke-WebRequest -Uri $ApiUrl -Method Post -Headers $Headers -Body $Payload -UseBasicParsing -ErrorAction Stop
        $ResponseBody = $Response.Content | ConvertFrom-Json

        $Tools = @($ResponseBody.result.tools)
        $ToolNames = $Tools | Select-Object -ExpandProperty name

        Write-Host "Visszakapott eszkozok szama: $($Tools.Count)"
        Write-Host "Eszkozok listaja: $($ToolNames -join ', ')"
    }
    catch {
        Write-Host "Hiba tortent: $_"
    }
}

Test-Role -Role "Explorer"
Test-Role -Role "Developer"
Test-Role -Role "None"

Write-Host "`n--- Test Finished ---"
