param([string]$OutputRoot = (Split-Path $PSScriptRoot -Parent), [string[]]$Only = @())
# Reexporta os masters aprovados/refinados; nao gera anatomia nem modifica metadados Unity.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$palettePath = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'Palette/MasterPalette.hex'
$palette = @(Get-Content -LiteralPath $palettePath | Where-Object { $_.Trim() } | ForEach-Object {
    [Drawing.ColorTranslator]::FromHtml('#' + $_.Trim())
})
$ramps = [ordered]@{
    '' = @(32,33,34); Operaria = @(32,33,34); Soldado = @(35,36,37)
    Coletora = @(23,24,26); Escavadora = @(8,12,13); Cuidadora = @(29,30,31)
    Jardineira = @(38,39,40); Cortadeira = @(41,42,43); Rainha = @(32,33,34)
    Cuspideira = @(25,27,28); Lixeira = @(17,18,20); Fazendeira = @(9,10,11)
}
$baseColors = @('1B1815','5C1408','9E311E','D3592A','F5EDD9')
$total = 0
foreach ($folder in $ramps.Keys) {
    if ($Only.Count -gt 0 -and $Only -notcontains $folder) { continue }
    $queen = $folder -eq 'Rainha'
    $masterDir = Join-Path $PSScriptRoot $(if ($queen) {'Refined/Queen'} else {'Refined/Worker'})
    $destination = if ($folder) { Join-Path $OutputRoot $folder } else { $OutputRoot }
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    $ramp = $ramps[$folder]
    $colors = @($palette[0],$palette[$ramp[0]],$palette[$ramp[1]],$palette[$ramp[2]],$palette[16])
    foreach ($view in @('Top','Walk')) {
        foreach ($frame in 0..3) {
            $name = 'Ant_' + $view + '_' + $frame + '.png'
            $source = [Drawing.Bitmap]::FromFile((Join-Path $masterDir $name))
            try {
                $width = if ($queen) {96} else {48}
                $height = if ($queen) {64} else {32}
                if ($source.Width -ne $width -or $source.Height -ne $height) { throw "Dimensoes incorretas: $masterDir/$name" }
                $output = New-Object Drawing.Bitmap $width,$height
                try {
                    for ($y = 0; $y -lt $height; $y++) {
                        for ($x = 0; $x -lt $width; $x++) {
                            $c = $source.GetPixel($x,$y)
                            if ($c.A -eq 0) { continue }
                            if ($c.A -ne 255) { throw "Alpha intermediario: $name ($x,$y)" }
                            $hex = '{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
                            $index = [Array]::IndexOf($baseColors,$hex)
                            if ($index -lt 0) { throw "Cor fora da paleta: $name $hex" }
                            $output.SetPixel($x,$y,$colors[$index])
                        }
                    }
                    $output.Save((Join-Path $destination $name),[Drawing.Imaging.ImageFormat]::Png)
                    $total++
                } finally { $output.Dispose() }
            } finally { $source.Dispose() }
        }
    }
}
Write-Output "$total sprites exportados; dimensoes, alpha binario e paleta conferidos."
