param(
    [Parameter(Mandatory=$true)][string]$InputImage,
    [Parameter(Mandatory=$true)][string]$OutputImage,
    [int]$Width = 48,
    [int]$Height = 32,
    [switch]$Grounded,
    [string[]]$PaletteHex = @('1B1815','5C1408','9E311E','D3592A','F5EDD9'),
    [switch]$KeepAspect
)
# Pos-processamento deterministico do ImageGen: grade nativa, cinco cores e alpha 0/255.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$palette = $PaletteHex | ForEach-Object {
    [Drawing.ColorTranslator]::FromHtml('#' + $_)
}
$source = [Drawing.Bitmap]::FromFile($InputImage)
$output = New-Object Drawing.Bitmap $Width,$Height
try {
    $contentWidth=[double]$Width; $contentHeight=[double]$Height
    if ($KeepAspect) {
        $ratio=[Math]::Min($Width/[double]$source.Width,$Height/[double]$source.Height)
        $contentWidth=$source.Width*$ratio; $contentHeight=$source.Height*$ratio
    }
    $offsetX=($Width-$contentWidth)/2.; $offsetY=($Height-$contentHeight)/2.
    $maxY = -1
    for ($y=0; $y -lt $Height; $y++) {
        for ($x=0; $x -lt $Width; $x++) {
            $r=0.; $g=0.; $b=0.; $n=0
            foreach ($dy in 0..6) {
                foreach ($dx in 0..6) {
                    $px=$x+($dx+0.5)/7-$offsetX; $py=$y+($dy+0.5)/7-$offsetY
                    if ($px -lt 0 -or $px -ge $contentWidth -or $py -lt 0 -or $py -ge $contentHeight) { continue }
                    $sx = [Math]::Min($source.Width-1,[int][Math]::Floor($px*$source.Width/$contentWidth))
                    $sy = [Math]::Min($source.Height-1,[int][Math]::Floor($py*$source.Height/$contentHeight))
                    $c = $source.GetPixel($sx,$sy)
                    if ($c.A -ge 200) { $r+=$c.R; $g+=$c.G; $b+=$c.B; $n++ }
                }
            }
            if ($n -lt 20) { continue }
            $r/=$n; $g/=$n; $b/=$n
            $best = [double]::MaxValue
            $chosen = $palette[0]
            foreach ($c in $palette) {
                $distance = [Math]::Pow($r-$c.R,2)+[Math]::Pow($g-$c.G,2)+[Math]::Pow($b-$c.B,2)
                if ($distance -lt $best) { $best=$distance; $chosen=$c }
            }
            $output.SetPixel($x,$y,$chosen)
            $maxY = [Math]::Max($maxY,$y)
        }
    }
    if ($maxY -lt 0) { throw 'Imagem sem pixels opacos.' }
    if ($Grounded -and $maxY -lt $Height-1) {
        $shift = $Height-1-$maxY
        for ($y=$Height-1; $y -ge 0; $y--) {
            for ($x=0; $x -lt $Width; $x++) {
                $c = if ($y -ge $shift) { $output.GetPixel($x,$y-$shift) } else { [Drawing.Color]::FromArgb(0,0,0,0) }
                $output.SetPixel($x,$y,$c)
            }
        }
    }
    New-Item -ItemType Directory -Force -Path (Split-Path $OutputImage) | Out-Null
    $output.Save($OutputImage,[Drawing.Imaging.ImageFormat]::Png)
    Write-Output "$OutputImage ($Width x $Height)"
} finally { $source.Dispose(); $output.Dispose() }
