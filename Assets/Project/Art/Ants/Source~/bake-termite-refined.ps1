param([string]$OutputRoot = (Join-Path (Split-Path $PSScriptRoot -Parent) 'InimigaAlada'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$allowed=@('1B1815','3A1A5C','504D55','6B1F45','D6872F','F5EDD9','BBB29F')
$validated=@()
# Confere o conjunto inteiro antes de substituir qualquer PNG.
foreach($view in @('Top','Walk','Fly','Land')) {
    foreach($frame in 0..3) {
        $name="Ant_${view}_$frame.png"
        $path=Join-Path $PSScriptRoot "TermiteRefined/$name"
        $image=[Drawing.Bitmap]::FromFile($path)
        try {
            if($image.Width -ne 64 -or $image.Height -ne 64){throw "Dimensoes incorretas: $name"}
            $opaque=0
            for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
                $c=$image.GetPixel($x,$y)
                if($c.A -eq 0){continue}
                if($c.A -ne 255){throw "Alpha intermediario: $name"}
                $hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
                if($hex -notin $allowed){throw "Cor fora da paleta: $name ($hex)"}
                $opaque++
            }}
            if($opaque -eq 0){throw "Quadro vazio: $name"}
            $validated+=[pscustomobject]@{Name=$name;Path=$path}
        } finally {$image.Dispose()}
    }
}
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
foreach($item in $validated){Copy-Item -LiteralPath $item.Path -Destination (Join-Path $OutputRoot $item.Name)}
Write-Output '16 sprites exportados; 65.536 pixels conferidos (64x64, paleta e alpha binario). Metadados nao alterados.'
