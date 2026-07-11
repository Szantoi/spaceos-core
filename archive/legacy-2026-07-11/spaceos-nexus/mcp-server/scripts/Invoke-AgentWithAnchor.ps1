param(
    [Parameter(Mandatory = $true)]
    [string]$Role,

    [Parameter(Mandatory = $false)]
    [int]$RetryCount = 0
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RolesDir = Join-Path $ScriptDir "..\database\roles"

# Find the schema file
$SchemaFile = Get-ChildItem -Path $RolesDir -Filter "$Role.schema.yaml" -Recurse | Select-Object -First 1

if (-not $SchemaFile) {
    # Fallback to direct name if recursive child item fails
    $DirectPath = Join-Path $RolesDir "$Role\$Role.schema.yaml"
    if (Test-Path $DirectPath) {
        $SchemaFile = Get-Item $DirectPath
    }
}

if (-not $SchemaFile) {
    Write-Error "Schema file for role '$Role' not found."
    exit 1
}

$identity = "$Role Agent"
$style = "Professional Technical"
$inPersona = $false

# Improved YAML-ish parsing for persona block
$Lines = Get-Content $SchemaFile.FullName
foreach ($line in $Lines) {
    if ($line -match "^persona:") {
        $inPersona = $true
        continue
    }

    if ($inPersona) {
        if ($line -match "^\s+identity:\s*(.*)") {
            $identity = $matches[1].Trim().Trim('"').Trim("'")
        }
        elseif ($line -match "^\s+style:\s*(.*)") {
            $style = $matches[1].Trim().Trim('"').Trim("'")
        }
        elseif ($line -match "^[a-zA-Z]") {
            $inPersona = $false
        }
    }
}

$systemPromptHeader = @"
======================================================================
🛡️ PERSONA ANCHOR WRAPPER v1.0
======================================================================
ROLE: $Role
IDENTITY: $identity
STYLE: $style
======================================================================

# SYSTEM CORE INSTRUCTIONS
- You are strictly bound by the constraints defined in your role schema.
- You must maintain the persona of $identity at all times.
- Your communication style must be $style.

# PERSONA ANCHOR (Enforced Identity)
🚨 You are acting as: $identity
🚨 Your expected style is: $style
🚨 DO NOT drift from this role. If asked to perform tasks outside your scope, decline politely but firmly.

"@

if ($RetryCount -ge 3) {
    $systemPromptHeader += @"

# 🚨 STEP-BACK INJECTION (Enforced Strategy Change) 🚨
You have failed to complete this task in $RetryCount attempts.
You are currently experiencing "Incremental Fix Blindness".

CRITICAL INSTRUCTION:
1. STOP attempting small code edits or direct fixes.
2. STEP BACK and analyze the root cause of the failure.
3. PROPOSE 3 different high-level architectural or logic strategies before taking any further action.
4. WAIT for architectural alignment after your proposal.
"@
}

$systemPromptHeader += "`n======================================================================"

Write-Output $systemPromptHeader
