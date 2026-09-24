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
# Git is optional on the check PC; never let it stop the launch.
$commit = 'unknown'
if(Get-Command git -ErrorAction SilentlyContinue) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $value = & git -C $PSScriptRoot rev-parse --short HEAD 2>$null; if($LASTEXITCODE -eq 0 -and $value) { $commit = $value } }
    finally { $ErrorActionPreference = $previous }
}
$build = Get-Item -LiteralPath $exe
"device=$Device started=$(Get-Date -Format s) build=$($build.LastWriteTime.ToString('s')) commit=$commit desktop=$Desktop" |
    Add-Content -LiteralPath (Join-Path $note 'session-notes.txt') -Encoding utf8
if($Desktop) { Start-Process -FilePath $exe -ArgumentList '--desktop' } else { Start-Process -FilePath $exe }
Invoke-Item -LiteralPath $sessions
Write-Output "Started $exe ($Device). Checklist: Docs/DEVICE_QUICKCHECK.md. Evidence folder: $note"
