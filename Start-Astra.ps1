param([string]$Prompt = 'AGENTS.md と Collaboration/STATE.md を読み、重要判断・行き詰まりに関する指定の相談だけを担当してください。相談が未指定なら論点を確認し、通常作業はSolへ戻してください。')
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    & codex -m gpt-6-astra -c 'model_reasoning_effort="high"' $Prompt
    if ($LASTEXITCODE -ne 0) { throw "Codex exited with code $LASTEXITCODE" }
} finally { Pop-Location }
