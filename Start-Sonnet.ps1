param([string]$Prompt = 'AGENTS.md、CLAUDE.md、Collaboration/STATE.mdを読み、Solが指定した未完了タスクの範囲だけを担当してください。担当が未指定なら状態を報告し、001を再実装しないでください。対象SHA256、実施した検証と未確認事項を報告し、独立レビューとSol受入を待ってください。')
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    & claude --model claude-sonnet-5 $Prompt
    if ($LASTEXITCODE -ne 0) { throw "Claude Code exited with code $LASTEXITCODE" }
} finally { Pop-Location }
