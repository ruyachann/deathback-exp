param([string]$Prompt = 'AGENTS.md、Collaboration/STATE.md、Docs/Plans/STAGE_PLAN.mdを読み、指定タスクの企画・計画を担当してください。PAUSEと未完了事項を確認し、担当と編集範囲を記録してください。')
$ErrorActionPreference = 'Stop'
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Collaboration/PAUSE.json')) { throw 'Paused; read Collaboration/RESUME.md.' }
Push-Location -LiteralPath $PSScriptRoot
try {
    & codex --model gpt-6-astra -c 'model_reasoning_effort="high"' --cd $PSScriptRoot $Prompt
    if($LASTEXITCODE -ne 0) { throw "Codex exited with code $LASTEXITCODE" }
} finally { Pop-Location }
