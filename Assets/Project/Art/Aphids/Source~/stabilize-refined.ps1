param([string]$MasterRoot = (Join-Path $PSScriptRoot 'Refined'))
# Mantem corpo, cabeca, antenas e corniculos do quadro zero; as patas fazem o passo.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
foreach($view in @('Walk','Top')){
    $images=@(0..3 | ForEach-Object {[Drawing.Bitmap]::FromFile((Join-Path $MasterRoot "Aphid_${view}_$_.png"))})
    try {
        $body=New-Object 'bool[,]' 40,24
        foreach($image in $images){
            for($y=1;$y -lt 23;$y++){for($x=1;$x -lt 39;$x++){
                $inside=$true
                for($dy=-1;$dy -le 1 -and $inside;$dy++){for($dx=-1;$dx -le 1;$dx++){
                    if($image.GetPixel($x+$dx,$y+$dy).A -eq 0){$inside=$false;break}
                }}
                if($inside){for($dy=-2;$dy -le 2;$dy++){for($dx=-2;$dx -le 2;$dx++){
                    $xx=$x+$dx;$yy=$y+$dy
                    if($xx -ge 0 -and $xx -lt 40 -and $yy -ge 0 -and $yy -lt 24){$body[$xx,$yy]=$true}
                }}}
            }}
        }
        foreach($f in 1..3){
            $output=New-Object Drawing.Bitmap 40,24
            try {
                for($y=0;$y -lt 24;$y++){for($x=0;$x -lt 40;$x++){
                    $fixed=$body[$x,$y] -or $x -lt 9 -or ($view -eq 'Top' -and $x -ge 28) -or ($view -eq 'Walk' -and $y -lt 8)
                    $image=if($fixed){$images[0]}else{$images[$f]}
                    $output.SetPixel($x,$y,$image.GetPixel($x,$y))
                }}
                $images[$f].Dispose()
                $output.Save((Join-Path $MasterRoot "Aphid_${view}_$f.png"),[Drawing.Imaging.ImageFormat]::Png)
            } finally {$output.Dispose()}
        }
        Write-Output "$view : corpo estabilizado nas quatro poses."
    } finally {foreach($image in $images){$image.Dispose()}}
}
