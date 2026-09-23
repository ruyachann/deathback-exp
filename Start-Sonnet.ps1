param([string]$Prompt = 'AGENTS.md、CLAUDE.md、Collaboration/STATE.mdを読み、指定された未完了タスクの範囲だけを担当してください。担当が未指定なら状態を報告してください。対象SHA256、実施した検証と未確認事項を報告し、別セッションの独立レビューとタスク担当による受入を待ってください。')
$ErrorActionPreference = 'Stop'
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Collaboration/PAUSE.json')) { throw 'Paused; read Collaboration/RESUME.md.' }
Push-Location -LiteralPath $PSScriptRoot
try {
    & claude --model claude-sonnet-5 $Prompt
    if ($LASTEXITCODE -ne 0) { throw "Claude Code exited with code $LASTEXITCODE" }
} finally { Pop-Location }
