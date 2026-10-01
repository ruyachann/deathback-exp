param([Parameter(Mandatory=$true)][string]$ModelId,
      [Parameter(Mandatory=$true)][string]$Decision)
$ErrorActionPreference = 'Stop'
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Collaboration/PAUSE.json')) { throw 'Paused; read Collaboration/RESUME.md.' }
if([string]::IsNullOrWhiteSpace($Decision)) { throw 'A decision request is required.' }
if($ModelId -notmatch '^claude-opus-5(-[0-9]{8})?$') { throw 'Specify the confirmed Opus 5 model ID; no alias or fallback.' }
Push-Location -LiteralPath $PSScriptRoot
try {
    & claude --model $ModelId $Decision
    if($LASTEXITCODE -ne 0) { throw "Opus unavailable or failed: $LASTEXITCODE. Record reason before consulting Astra." }
} finally { Pop-Location }
