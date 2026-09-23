# Report Codex 5-hour / weekly limit usage and apply the 10% pause rule (AGENTS.md).
# Codex: runs a one-word non-ephemeral `codex exec` probe (ephemeral runs do not record
# rate_limits) and reads rate_limits from the newest rollout file.
# Claude: cannot be read from a shell. Pass the numbers the Claude Code usage card /
# get_usage tool shows with -ClaudeFiveHourUsed / -ClaudeWeeklyUsed.
# Exit code 0 = continue, 3 = pause required, 2 = could not read a limit (ask the user).
param([double]$ClaudeFiveHourUsed = -1, [double]$ClaudeWeeklyUsed = -1, [double]$MinRemaining = 10)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$codex = $env:CODEX_CLI_PATH
if(-not $codex -or -not (Test-Path -LiteralPath $codex)) { $codex = (Get-Command codex -ErrorAction SilentlyContinue).Source }
if(-not $codex) {
    $codex = Get-ChildItem "$env:LOCALAPPDATA\OpenAI\Codex\bin\*\codex.exe" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
$rows = @()
if($codex) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { 'Reply with the single word OK.' | & $codex --ask-for-approval never exec --model gpt-5.6-sol --sandbox read-only -c mcp_servers.peer_claude.enabled=false -C $root - *> $null }
    finally { $ErrorActionPreference = $previous }
    $rollout = Get-ChildItem "$env:USERPROFILE\.codex\sessions" -Recurse -Filter 'rollout-*.jsonl' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $line = Select-String -LiteralPath $rollout.FullName -Pattern '"rate_limits"' | Select-Object -Last 1
    if($line) {
        $limits = ($line.Line | ConvertFrom-Json).payload.rate_limits
        $rows += [pscustomobject]@{ Product = 'Codex'; Window = '5-hour'; Used = [double]$limits.primary.used_percent; Resets = [DateTimeOffset]::FromUnixTimeSeconds($limits.primary.resets_at).ToLocalTime() }
        $rows += [pscustomobject]@{ Product = 'Codex'; Window = 'weekly'; Used = [double]$limits.secondary.used_percent; Resets = [DateTimeOffset]::FromUnixTimeSeconds($limits.secondary.resets_at).ToLocalTime() }
    }
}
if($ClaudeFiveHourUsed -ge 0) { $rows += [pscustomobject]@{ Product = 'Claude'; Window = '5-hour'; Used = $ClaudeFiveHourUsed; Resets = $null } }
if($ClaudeWeeklyUsed -ge 0) { $rows += [pscustomobject]@{ Product = 'Claude'; Window = 'weekly'; Used = $ClaudeWeeklyUsed; Resets = $null } }
foreach($r in $rows) { $r | Add-Member Remaining (100 - $r.Used) }
$rows | Format-Table Product, Window, Used, Remaining, Resets -AutoSize | Out-String | Write-Output
$missing = @('Codex 5-hour', 'Codex weekly', 'Claude 5-hour', 'Claude weekly') | Where-Object { $n = $_ -split ' '; -not ($rows | Where-Object { $_.Product -eq $n[0] -and $_.Window -eq $n[1] }) }
if($rows | Where-Object { $_.Remaining -lt $MinRemaining }) { Write-Output "PAUSE: a limit has less than $MinRemaining% remaining. Save progress and create Collaboration/PAUSE.json."; exit 3 }
if($missing) { Write-Output "UNKNOWN: $($missing -join ', ') not read. Do not assume; ask the user (AGENTS.md)."; exit 2 }
Write-Output "OK: every limit has at least $MinRemaining% remaining."
exit 0
