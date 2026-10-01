# Run a command (or read a text file) and save its real output as a PNG evidence image.
# Usage:
#   .\Tools\Capture-Evidence.ps1 -Title 'task007 model checks' -Command 'python x.py' -OutFile Collaboration\evidence\...\tests.png
#   .\Tools\Capture-Evidence.ps1 -Title '...' -InputFile out.txt -OutFile ...png
# The header records title, local time, git commit and working directory. Output text is not edited.
param(
    [Parameter(Mandatory)][string]$Title,
    [string]$Command,
    [string]$InputFile,
    [Parameter(Mandatory)][string]$OutFile,
    [int]$MaxLines = 120
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
if($Command) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $body = & powershell.exe -NoProfile -ExecutionPolicy Bypass -Command $Command 2>&1 | Out-String; $code = $LASTEXITCODE }
    finally { $ErrorActionPreference = $previous }
    $body = "PS> $Command`n$body`n[exit code: $code]"
} elseif($InputFile) {
    $body = Get-Content -LiteralPath $InputFile -Raw -Encoding utf8
} else { throw 'Specify -Command or -InputFile.' }
$commit = (& git -C $root rev-parse --short HEAD 2>$null)
$header = "$Title`n$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')  commit $commit  cwd $((Get-Location).Path)"
$lines = ($body -replace "`r", '') -split "`n"
if($lines.Count -gt $MaxLines) { $lines = $lines[0..($MaxLines-1)] + "... ($($lines.Count - $MaxLines) more lines truncated; full text saved next to the image)" }
$font = New-Object System.Drawing.Font('Consolas', 11)
$bold = New-Object System.Drawing.Font('Consolas', 12, [System.Drawing.FontStyle]::Bold)
$probe = New-Object System.Drawing.Bitmap 1, 1
$g = [System.Drawing.Graphics]::FromImage($probe)
$lineHeight = [int][Math]::Ceiling($font.GetHeight($g)) + 2
$width = 600
foreach($l in ($header -split "`n") + $lines) { $w = [int]$g.MeasureString($l, $bold).Width; if($w -gt $width) { $width = $w } }
$g.Dispose(); $probe.Dispose()
$width = [Math]::Min($width + 40, 2400)
$height = 30 + $lineHeight * (3 + $lines.Count)
$bitmap = New-Object System.Drawing.Bitmap $width, $height
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.Clear([System.Drawing.Color]::FromArgb(24, 26, 30))
$y = 12
foreach($l in $header -split "`n") { $g.DrawString($l, $bold, [System.Drawing.Brushes]::Khaki, 16, $y); $y += $lineHeight }
$g.DrawLine([System.Drawing.Pens]::DimGray, 16, $y + 2, $width - 16, $y + 2); $y += $lineHeight
foreach($l in $lines) {
    # Colour is a reading aid only; the text itself is the evidence.
    $brush = if($l -match 'FAIL|isError: True|fatal:|Exception|error CS\d') { [System.Drawing.Brushes]::Salmon } elseif($l -match 'PASS|exit=0|approve|=True') { [System.Drawing.Brushes]::LightGreen } else { [System.Drawing.Brushes]::Gainsboro }
    $g.DrawString($l, $font, $brush, 16, $y); $y += $lineHeight
}
$g.Dispose()
New-Item -ItemType Directory -Force (Split-Path $OutFile -Parent) | Out-Null
$bitmap.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png); $bitmap.Dispose()
Set-Content -LiteralPath ([IO.Path]::ChangeExtension($OutFile, '.txt')) -Value "$header`n`n$body" -Encoding utf8
Write-Output "Saved $OutFile"
