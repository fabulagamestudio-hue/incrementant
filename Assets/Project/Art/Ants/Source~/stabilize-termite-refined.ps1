param([string]$MasterRoot = (Join-Path $PSScriptRoot 'TermiteRefined'))
# Mantem a carapaca fixa entre os estados; asas claras e suas bordas ficam por cima.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
function Hex($c){return ('{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B)}
foreach($group in @('Top','Side')) {
    $names=if($group -eq 'Top') {@(0..3 | ForEach-Object {"Ant_Top_$_.png"})} else {
        @('Walk','Fly','Land') | ForEach-Object {$view=$_;0..3 | ForEach-Object {"Ant_${view}_$_.png"}}
    }
    $images=@($names | ForEach-Object {[Drawing.Bitmap]::FromFile((Join-Path $MasterRoot $_))})
    try {
        $body=New-Object 'bool[,]' 64,64
        foreach($image in $images) {
            $mask=New-Object 'bool[,]' 64,64
            for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
                $c=$image.GetPixel($x,$y)
                $mask[$x,$y]=$c.A -gt 0 -and (Hex $c) -in @('1B1815','3A1A5C','504D55','D6872F')
            }}
            for($y=1;$y -lt 63;$y++){for($x=1;$x -lt 63;$x++){
                $inside=$true
                for($dy=-1;$dy -le 1 -and $inside;$dy++){for($dx=-1;$dx -le 1;$dx++){
                    if(-not $mask[($x+$dx),($y+$dy)]){$inside=$false;break}
                }}
                if($inside){for($dy=-2;$dy -le 2;$dy++){for($dx=-2;$dx -le 2;$dx++){
                    $xx=$x+$dx;$yy=$y+$dy
                    if($xx -ge 0 -and $xx -lt 64 -and $yy -ge 0 -and $yy -lt 64){$body[$xx,$yy]=$true}
                }}}
            }}
        }
        $anchor=$images[0] # Top_0 ou Walk_0, ambos sem asas.
        for($i=1;$i -lt $images.Count;$i++) {
            $wings=New-Object 'bool[,]' 64,64
            for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
                $c=$images[$i].GetPixel($x,$y)
                if($c.A -gt 0 -and (Hex $c) -in @('F5EDD9','BBB29F')) {
                    for($dy=-1;$dy -le 1;$dy++){for($dx=-1;$dx -le 1;$dx++){
                        $xx=$x+$dx;$yy=$y+$dy
                        if($xx -ge 0 -and $xx -lt 64 -and $yy -ge 0 -and $yy -lt 64){$wings[$xx,$yy]=$true}
                    }}
                }
            }}
            $output=New-Object Drawing.Bitmap 64,64
            try {
                for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
                    $fixed=$body[$x,$y] -or ($group -eq 'Top' -and $x -ge 49) -or ($group -eq 'Side' -and $y -lt 34)
                    $source=if($fixed -and -not $wings[$x,$y]){$anchor}else{$images[$i]}
                    $output.SetPixel($x,$y,$source.GetPixel($x,$y))
                }}
                $images[$i].Dispose()
                $output.Save((Join-Path $MasterRoot $names[$i]),[Drawing.Imaging.ImageFormat]::Png)
            } finally {$output.Dispose()}
        }
        Write-Output "$group : corpo registrado; patas e asas preservadas."
    } finally {foreach($image in $images){$image.Dispose()}}
}
