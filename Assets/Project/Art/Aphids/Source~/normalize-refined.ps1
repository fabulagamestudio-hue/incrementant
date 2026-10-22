param(
    [Parameter(Mandatory=$true)][string]$InputImage,
    [Parameter(Mandatory=$true)][string]$OutputImage,
    [Parameter(Mandatory=$true)][string]$LayoutImage
)
# Amostragem direta da fonte, registrada pelo corpo verde para preservar a animacao.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$palette=@('1B1815','638929','91B13B','B3CF51','437122','32550E','F5EDD9') | ForEach-Object {[Drawing.ColorTranslator]::FromHtml('#'+$_)}
function BodyBounds($image,[int]$step=1){
    $left=$image.Width;$right=-1;$top=$image.Height;$bottom=-1
    for($y=0;$y -lt $image.Height;$y+=$step){for($x=0;$x -lt $image.Width;$x+=$step){
        $c=$image.GetPixel($x,$y)
        # Os tres verdes do corpo sao mais claros que patas/antenas; o brilho creme e excluido.
        if($c.A -ge 200 -and $c.R -ge 80 -and $c.R -le 205 -and $c.G -ge 115 -and $c.G -le 235 -and $c.B -le 140){
            $left=[Math]::Min($left,$x);$right=[Math]::Max($right,$x)
            $top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)
        }
    }}
    if($right -lt $left){throw 'Corpo verde nao localizado.'}
    return @($left,$top,$right,$bottom)
}
function AlphaBounds($image,[int]$step=1){
    $left=$image.Width;$right=-1;$top=$image.Height;$bottom=-1
    for($y=0;$y -lt $image.Height;$y+=$step){for($x=0;$x -lt $image.Width;$x+=$step){
        if($image.GetPixel($x,$y).A -ge 200){
            $left=[Math]::Min($left,$x);$right=[Math]::Max($right,$x)
            $top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)
        }
    }}
    return @($left,$top,$right,$bottom)
}
$source=[Drawing.Bitmap]::FromFile($InputImage)
$layout=[Drawing.Bitmap]::FromFile($LayoutImage)
$output=New-Object Drawing.Bitmap 40,24
try {
    $a=BodyBounds $source 4;$b=BodyBounds $layout
    $scale=($b[2]-$b[0]+1.)/($a[2]-$a[0]+1.)
    $ax=($a[0]+$a[2])/2.;$ay=($a[1]+$a[3])/2.
    $bx=($b[0]+$b[2])/2.;$by=($b[1]+$b[3])/2.
    $all=AlphaBounds $source 4
    $bounds=AlphaBounds $layout
    $pred=@((($all[0]-$ax)*$scale+$bx),(($all[1]-$ay)*$scale+$by),
        (($all[2]-$ax)*$scale+$bx),(($all[3]-$ay)*$scale+$by))
    # So os apendices que sairiam do canvas sao comprimidos no espaco do original.
    # O corpo nao e reduzido para acomodar antenas mais abertas na fonte gerada.
    for($y=0;$y -lt 24;$y++){for($x=0;$x -lt 40;$x++){
        $r=0.;$g=0.;$blue=0.;$n=0
        foreach($dy in 0..6){foreach($dx in 0..6){
            $px=$x+($dx+0.5)/7-0.5;$py=$y+($dy+0.5)/7-0.5
            if($pred[0] -lt 0 -and $px -lt $b[0]){$px=$b[0]-($b[0]-$px)*($b[0]-$pred[0])/[Math]::Max(1.,$b[0]-$bounds[0])}
            if($pred[2] -gt 39 -and $px -gt $b[2]){$px=$b[2]+($px-$b[2])*($pred[2]-$b[2])/[Math]::Max(1.,$bounds[2]-$b[2])}
            if($pred[1] -lt 0 -and $py -lt $b[1]){$py=$b[1]-($b[1]-$py)*($b[1]-$pred[1])/[Math]::Max(1.,$b[1]-$bounds[1])}
            if($pred[3] -gt 23 -and $py -gt $b[3]){$py=$b[3]+($py-$b[3])*($pred[3]-$b[3])/[Math]::Max(1.,$bounds[3]-$b[3])}
            $sx=[int][Math]::Floor(($px-$bx)/$scale+$ax)
            $sy=[int][Math]::Floor(($py-$by)/$scale+$ay)
            if($sx -lt 0 -or $sx -ge $source.Width -or $sy -lt 0 -or $sy -ge $source.Height){continue}
            $c=$source.GetPixel($sx,$sy)
            if($c.A -ge 200){$r+=$c.R;$g+=$c.G;$blue+=$c.B;$n++}
        }}
        if($n -lt 20){continue}
        $r/=$n;$g/=$n;$blue/=$n;$best=[double]::MaxValue;$chosen=$palette[0]
        foreach($c in $palette){
            $d=[Math]::Pow($r-$c.R,2)+[Math]::Pow($g-$c.G,2)+[Math]::Pow($blue-$c.B,2)
            if($d -lt $best){$best=$d;$chosen=$c}
        }
        $output.SetPixel($x,$y,$chosen)
    }}
    # Borda da carapaca na cor de contorno; apendices finos mantem os verdes escuros.
    for($y=0;$y -lt 24;$y++){for($x=0;$x -lt 40;$x++){
        $c=$output.GetPixel($x,$y)
        if($c.A -eq 0 -or $c.R -lt 80 -or $c.R -gt 205 -or $c.G -lt 115){continue}
        if($x -eq 0 -or $x -eq 39 -or $y -eq 0 -or $y -eq 23 -or
           $output.GetPixel($x-1,$y).A -eq 0 -or $output.GetPixel($x+1,$y).A -eq 0 -or
           $output.GetPixel($x,$y-1).A -eq 0 -or $output.GetPixel($x,$y+1).A -eq 0){$output.SetPixel($x,$y,$palette[0])}
    }}
    New-Item -ItemType Directory -Force -Path (Split-Path $OutputImage) | Out-Null
    $output.Save($OutputImage,[Drawing.Imaging.ImageFormat]::Png)
    Write-Output ('40x24, paleta e alpha binario: '+[IO.Path]::GetFileName($OutputImage))
} finally {$source.Dispose();$layout.Dispose();$output.Dispose()}
