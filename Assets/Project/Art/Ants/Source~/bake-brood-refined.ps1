param([string]$OutputRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$palettes=@{
    Egg=@('1B1815','CFB691','E4D2AE','F5EDD9')
    Larva=@('1B1815','CFB691','E4D2AE','F5EDD9','B9986D','823A10')
    Pupa=@('1B1815','B9986D','DEB06D','E4D2AE','823A10')
}
$files=@()
foreach($kind in @('Egg','Larva','Pupa')){foreach($frame in 0..1){
    $name="${kind}_Idle_$frame.png"
    $path=Join-Path $PSScriptRoot "BroodRefined/$name"
    $image=[Drawing.Bitmap]::FromFile($path)
    try {
        if($image.Width -ne 48 -or $image.Height -ne 32){throw "Dimensoes incorretas: $name"}
        $opaque=0
        for($y=0;$y -lt 32;$y++){for($x=0;$x -lt 48;$x++){
            $c=$image.GetPixel($x,$y);if($c.A -eq 0){continue}
            if($c.A -ne 255){throw "Alpha intermediario: $name"}
            $hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
            if($hex -notin $palettes[$kind]){throw "Cor fora da paleta: $name ($hex)"}
            $opaque++
        }}
        if($opaque -eq 0){throw "Quadro vazio: $name"}
        $files+=[pscustomobject]@{Name=$name;Path=$path}
    } finally {$image.Dispose()}
}}
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
foreach($file in $files){Copy-Item -LiteralPath $file.Path -Destination (Join-Path $OutputRoot $file.Name)}
Write-Output '6 sprites exportados; 9.216 pixels conferidos: 48x32, paletas e alpha binario. Metadados nao alterados.'
