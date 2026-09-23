param([string]$Branch = 'codex/stage-01-looproom')
$ErrorActionPreference = 'Stop'
function Invoke-RepoGit([string[]]$Arguments) {
    & git -C $PSScriptRoot @Arguments
    if($LASTEXITCODE -ne 0) { throw "Git failed: $($Arguments -join ' ')" }
}
$remote = & git -C $PSScriptRoot remote get-url origin
if($LASTEXITCODE -ne 0 -or $remote -ne 'https://github.com/ruyachann/deathback-exp.git') { throw 'Unexpected repository origin.' }
$savedErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & git -C $PSScriptRoot rev-parse --verify --quiet HEAD >$null 2>$null
    $headExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if($headExitCode -eq 0) { throw 'Local history already exists; inspect it before synchronization. No changes made.' }
$staged = & git -C $PSScriptRoot ls-files --cached
if($LASTEXITCODE -ne 0 -or $staged) { throw 'Index is not empty; protect staged user changes before synchronization.' }
Invoke-RepoGit -Arguments @('fetch','origin')
Invoke-RepoGit -Arguments @('show-ref','--verify',"refs/remotes/origin/$Branch")
Invoke-RepoGit -Arguments @('switch','-c',$Branch)
# No local history/index exists; mixed reset creates the index and leaves working files intact.
Invoke-RepoGit -Arguments @('reset','--mixed',"origin/$Branch")
Invoke-RepoGit -Arguments @('branch','--set-upstream-to',"origin/$Branch",$Branch)
Invoke-RepoGit -Arguments @('status','--short','--branch')
Write-Output 'Published history synchronized. Working files were preserved; inspect differences before committing.'
