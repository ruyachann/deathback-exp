param([ValidateSet('Quest3','Quest3S','Unknown')][string]$Device = 'Unknown', [switch]$Desktop)
# Start the Windows build for a Quest 3 / 3S device check (Docs/DEVICE_QUICKCHECK.md).
# If scripts are blocked: powershell -ExecutionPolicy Bypass -File .\Start-DeviceCheck.ps1 -Device Quest3S
$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'Builds/Windows/LoopRoom.exe'
if(-not (Test-Path -LiteralPath $exe)) {
    $unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe'
    throw "Build not found: $exe`nBuild it first: Unity menu 'LoopRoom/4 - Build Windows demo', or`n  & '$unity' -batchmode -quit -projectPath '$PSScriptRoot' -executeMethod DemoSetup.Build -logFile '$PSScriptRoot\Collaboration\local-logs\unity-build.log'"
}
$sessions = Join-Path $env:USERPROFILE 'AppData\LocalLow\LoopRoomDemo\The Room Before\Sessions'
New-Item -ItemType Directory -Force $sessions | Out-Null
$note = Join-Path $PSScriptRoot "Collaboration/evidence/$(Get-Date -Format yyyyMMdd)-device-check"
New-Item -ItemType Directory -Force $note | Out-Null
$buildInfoPath = Join-Path $PSScriptRoot 'Builds/Windows/build-info.json'
$buildInfo = $null
$buildInfoRaw = $null
if(Test-Path -LiteralPath $buildInfoPath) {
    try {
        $buildInfoRaw = Get-Content -LiteralPath $buildInfoPath -Raw -Encoding utf8
        if([string]::IsNullOrWhiteSpace($buildInfoRaw)) { throw 'build-info.json is empty.' }
        $buildInfo = $buildInfoRaw | ConvertFrom-Json
        if($null -eq $buildInfo) { throw 'build-info.json has no JSON object.' }
    }
    catch { Write-Warning "build-info.json を読み取れません。古いビルドとして扱います: $($_.Exception.Message)" }
}
else { Write-Warning 'build-info なし（古いビルド）' }
# Git is optional on the check PC; never let it stop the launch.
$commit = 'unknown'
if(Get-Command git -ErrorAction SilentlyContinue) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $value = & git -C $PSScriptRoot rev-parse HEAD 2>$null; if($LASTEXITCODE -eq 0 -and $value) { $commit = [string]$value } }
    finally { $ErrorActionPreference = $previous }
}
$build = Get-Item -LiteralPath $exe
$sessionNotes = Join-Path $note 'session-notes.txt'
"device=$Device started=$(Get-Date -Format s) build=$($build.LastWriteTime.ToString('s')) commit=$commit desktop=$Desktop" |
    Add-Content -LiteralPath $sessionNotes -Encoding utf8
if($null -ne $buildInfoRaw) {
    'build-info-begin' | Add-Content -LiteralPath $sessionNotes -Encoding utf8
    $buildInfoRaw | Add-Content -LiteralPath $sessionNotes -Encoding utf8
    'build-info-end' | Add-Content -LiteralPath $sessionNotes -Encoding utf8
}
if($null -ne $buildInfo) {
    if($buildInfo.dirty -eq $true) { Write-Warning '未コミットの変更を含むビルドです。build-info.json の dirtyFiles を確認してください。' }
    if($commit -ne 'unknown' -and $buildInfo.commit -and $buildInfo.commit -ne 'unknown' -and $commit -ne $buildInfo.commit) {
        Write-Warning "現在の HEAD ($commit) とビルドの commit ($($buildInfo.commit)) が異なります。"
    }
}
if($Desktop) { Start-Process -FilePath $exe -ArgumentList '--desktop' } else { Start-Process -FilePath $exe }
Invoke-Item -LiteralPath $sessions
Write-Output "Started $exe ($Device). Checklist: Docs/DEVICE_QUICKCHECK.md. Evidence folder: $note"
