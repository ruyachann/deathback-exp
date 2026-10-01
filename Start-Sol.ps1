param([string]$Prompt = 'AGENTS.mdとCollaboration/STATE.mdを読み、通常の計画・タスク分割・実装・結果確認・次計画を担当してください。別セッションのSolとSonnetの独立レビューと報告交換を行い、AGENTS.mdの重要判断・行き詰まり条件だけOpus5優先、次にAstraへ相談してください。')
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    & codex -m gpt-5.6-sol -c 'model_reasoning_effort="high"' $Prompt
    if ($LASTEXITCODE -ne 0) { throw "Codex Sol exited with code $LASTEXITCODE" }
} finally { Pop-Location }
