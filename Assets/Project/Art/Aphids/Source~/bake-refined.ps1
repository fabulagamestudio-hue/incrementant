param([string]$OutputRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$allowed=@('1B1815','638929','91B13B','B3CF51','437122','32550E','F5EDD9')
$files=@()
foreach($view in @('Walk','Top')){foreach($f in 0..3){
    $name="Aphid_${view}_$f.png";$path=Join-Path $PSScriptRoot "Refined/$name"
    $image=[Drawing.Bitmap]::FromFile($path)
    try {
        if($image.Width -ne 40 -or $image.Height -ne 24){throw "Dimensoes incorretas: $name"}
        $opaque=0
        for($y=0;$y -lt 24;$y++){for($x=0;$x -lt 40;$x++){
            $c=$image.GetPixel($x,$y);if($c.A -eq 0){continue}
            if($c.A -ne 255){throw "Alpha intermediario: $name"}
            $hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
            if($hex -notin $allowed){throw "Cor fora da paleta: $name ($hex)"}
            $opaque++
        }}
        if($opaque -eq 0){throw "Quadro vazio: $name"}
        $files+=[pscustomobject]@{Name=$name;Path=$path}
    } finally {$image.Dispose()}
}}
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
foreach($file in $files){Copy-Item -LiteralPath $file.Path -Destination (Join-Path $OutputRoot $file.Name)}
Write-Output '8 sprites exportados; 7.680 pixels conferidos: 40x24, paleta e alpha binario. Metadados nao alterados.'
