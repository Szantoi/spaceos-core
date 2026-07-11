[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$Body = @{ jsonrpc = "2.0"; id = 1; method = "tools/list" } | ConvertTo-Json -Compress

$RespExp = Invoke-RestMethod -Uri "http://localhost:5058/mcp/messages" -Method Post -Headers @{"Content-Type" = "application/json"; "X-Agent-Role" = "Explorer" } -Body $Body -ErrorAction Stop
Write-Host "--- Explorer Tools ---"
Write-Host ($RespExp.result.tools.name -join ", ")

$RespDev = Invoke-RestMethod -Uri "http://localhost:5058/mcp/messages" -Method Post -Headers @{"Content-Type" = "application/json"; "X-Agent-Role" = "Developer" } -Body $Body -ErrorAction Stop
Write-Host "--- Developer Tools ---"
Write-Host ($RespDev.result.tools.name -join ", ")
