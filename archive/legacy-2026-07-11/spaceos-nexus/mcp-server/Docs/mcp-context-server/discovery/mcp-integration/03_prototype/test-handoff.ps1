$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "--- JSON Handoff Prototype Test ---"

$ApiUrl = "http://localhost:5058/mcp/messages"

$HandoffArgs = @{
    sourceRole  = "TestRole123"
    targetRole  = "Designer"
    summary     = "Ez egy többsoros summary`nMasodik sor`nA harmadik sor."
    actionItems = @(
        "Elso cselekedet",
        "Masodik, idezojelet tartalmazo cselekedet: `"valami`""
    )
}

$PayloadTriggerJson = @{
    jsonrpc = "2.0"
    id      = 1
    method  = "tools/call"
    params  = @{
        name      = "trigger_handoff"
        arguments = $HandoffArgs
    }
} | ConvertTo-Json -Depth 5 -Compress

$BytesTrigger = [System.Text.Encoding]::UTF8.GetBytes($PayloadTriggerJson)

Write-Host "Küldöm a Handoff-ot..."
$RespTrigger = Invoke-RestMethod -Uri $ApiUrl -Method Post -Headers @{"Content-Type" = "application/json; charset=utf-8"; "X-Agent-Role" = "Developer" } -Body $BytesTrigger -ErrorAction Stop

Write-Host "Válasz a trigger-re:"
$RespTrigger | ConvertTo-Json -Depth 5

Write-Host "`nLekérem a kiosztott feladatokat (Pending Handoffs)..."
$PayloadGetJson = @{
    jsonrpc = "2.0"
    id      = 2
    method  = "tools/call"
    params  = @{
        name      = "get_pending_handoffs"
        arguments = @{ roleName = "Designer" }
    }
} | ConvertTo-Json -Depth 5 -Compress

$BytesGet = [System.Text.Encoding]::UTF8.GetBytes($PayloadGetJson)

$RespGet = Invoke-RestMethod -Uri $ApiUrl -Method Post -Headers @{"Content-Type" = "application/json; charset=utf-8"; "X-Agent-Role" = "Developer" } -Body $BytesGet -ErrorAction Stop

Write-Host "Visszakapott 'get_pending_handoffs' eredmény:"
$RespGet | ConvertTo-Json -Depth 5
