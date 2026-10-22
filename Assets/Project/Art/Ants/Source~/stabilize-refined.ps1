param([string]$MasterRoot = (Join-Path $PSScriptRoot 'Refined'))
# Estabiliza a carapaca sobre as poses do ImageGen antes de exportar as variantes.
# Abertura morfologica separa o corpo espesso das patas finas; a uniao das quatro
# poses recebe os pixels do quadro zero, inclusive o transparente ao redor do corpo.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
foreach ($kind in @('Worker','Queen')) {
    foreach ($view in @('Top','Walk')) {
        $images = @(0..3 | ForEach-Object {
            [Drawing.Bitmap]::FromFile((Join-Path $MasterRoot "$kind/Ant_${view}_$_.png"))
        })
        try {
            $w=$images[0].Width; $h=$images[0].Height
            $radius=if ($kind -eq 'Queen') {2} else {1}
            $mask=New-Object 'bool[,]' $w,$h
            foreach ($image in $images) {
                for ($y=$radius; $y -lt $h-$radius; $y++) {
                    for ($x=$radius; $x -lt $w-$radius; $x++) {
                        $inside=$true
                        for ($dy=-$radius; $dy -le $radius -and $inside; $dy++) {
                            for ($dx=-$radius; $dx -le $radius; $dx++) {
                                if ($image.GetPixel($x+$dx,$y+$dy).A -eq 0) { $inside=$false; break }
                            }
                        }
                        if (-not $inside) { continue }
                        $padding=$radius+1
                        for ($dy=-$padding; $dy -le $padding; $dy++) {
                            for ($dx=-$padding; $dx -le $padding; $dx++) {
                                $xx=$x+$dx; $yy=$y+$dy
                                if ($xx -ge 0 -and $xx -lt $w -and $yy -ge 0 -and $yy -lt $h) { $mask[$xx,$yy]=$true }
                            }
                        }
                    }
                }
            }
            # Antenas nao participam do passo. A vista superior as tem a direita;
            # na vista lateral elas ficam acima da carapaca.
            for ($y=0; $y -lt $h; $y++) {
                for ($x=0; $x -lt $w; $x++) {
                    if (($view -eq 'Top' -and $x -ge [int]($w*0.82)) -or
                        ($view -eq 'Walk' -and $y -lt [int]($h*0.55))) { $mask[$x,$y]=$true }
                }
            }
            foreach ($frame in 1..3) {
                $output=New-Object Drawing.Bitmap $w,$h
                try {
                    for ($y=0; $y -lt $h; $y++) {
                        for ($x=0; $x -lt $w; $x++) {
                            $image=if ($mask[$x,$y]) {$images[0]} else {$images[$frame]}
                            $output.SetPixel($x,$y,$image.GetPixel($x,$y))
                        }
                    }
                    $path=Join-Path $MasterRoot "$kind/Ant_${view}_$frame.png"
                    $images[$frame].Dispose()
                    $output.Save($path,[Drawing.Imaging.ImageFormat]::Png)
                } finally { $output.Dispose() }
            }
            Write-Output "$kind $view : corpo e antenas estabilizados; poses das patas preservadas."
        } finally { foreach ($image in $images) { $image.Dispose() } }
    }
}
