param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Unity Editor was not found. Specify -UnityEditor with its path.' }
$demoRoot = $PSScriptRoot
$demoLogs = Join-Path $demoRoot 'Logs'
New-Item -ItemType Directory -Path $demoLogs -Force | Out-Null
$buildLog = Join-Path $demoLogs 'build.log'
$packageLog = Join-Path $demoLogs 'upm.log'
$buildArguments = @('-batchmode', '-quit', '-projectPath', ('"' + $demoRoot + '"'), '-executeMethod', 'DemoSetup.Build', '-logFile', ('"' + $buildLog + '"'), '-upmLogFile', ('"' + $packageLog + '"'))
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList $buildArguments -PassThru -Wait -WindowStyle Hidden
if ($buildProcess.ExitCode -ne 0) { throw "Unity failed with exit code $($buildProcess.ExitCode). Inspect Logs/build.log and Logs/upm.log." }
$expectedBuild = Join-Path $demoRoot 'Builds\Windows\LoopRoom.exe'
if (-not (Test-Path -LiteralPath $expectedBuild)) { throw 'No executable was produced. Inspect Logs/build.log.' }
Write-Output "Built: $expectedBuild"
