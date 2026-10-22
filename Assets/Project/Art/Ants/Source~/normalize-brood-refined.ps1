param(
    [Parameter(Mandatory=$true)][string]$InputImage,
    [Parameter(Mandatory=$true)][string]$OutputImage,
    [Parameter(Mandatory=$true)][ValidateSet('Egg','Larva','Pupa')][string]$Kind,
    [Parameter(Mandatory=$true)][string]$LayoutImage
)
# Amostra o desenho original diretamente na grade final, sem reduzir e reampliar.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$palettes=@{
    Egg=@('1B1815','CFB691','E4D2AE','F5EDD9')
    Larva=@('1B1815','CFB691','E4D2AE','F5EDD9','B9986D','823A10')
    Pupa=@('1B1815','B9986D','DEB06D','E4D2AE','823A10')
}
$palette=@($palettes[$Kind] | ForEach-Object {[Drawing.ColorTranslator]::FromHtml('#'+$_)})
function Bounds($image,[int]$step=1){
    $left=$image.Width;$right=-1;$top=$image.Height;$bottom=-1
    for($y=0;$y -lt $image.Height;$y+=$step){for($x=0;$x -lt $image.Width;$x+=$step){
        if($image.GetPixel($x,$y).A -ge 200){
            $left=[Math]::Min($left,$x);$right=[Math]::Max($right,$x)
            $top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)
        }
    }}
    if($right -lt $left){throw 'Sprite vazio.'}
    return @([Math]::Max(0,$left-$step+1),[Math]::Max(0,$top-$step+1),
        [Math]::Min($image.Width-1,$right+$step-1),[Math]::Min($image.Height-1,$bottom+$step-1))
}
$source=[Drawing.Bitmap]::FromFile($InputImage)
$layout=[Drawing.Bitmap]::FromFile($LayoutImage)
$output=New-Object Drawing.Bitmap 48,32
try {
    # Passo 4 na fonte grande, com margem conservadora menor que um pixel final.
    $a=Bounds $source 4;$b=Bounds $layout
    $sw=$a[2]-$a[0]+1.;$sh=$a[3]-$a[1]+1.
    $scale=[Math]::Min(($b[2]-$b[0]+1.)/$sw,($b[3]-$b[1]+1.)/$sh)
    $left=($b[0]+$b[2]+1.-$sw*$scale)/2.
    $top=$b[3]+1.-$sh*$scale
    for($y=0;$y -lt 32;$y++){for($x=0;$x -lt 48;$x++){
        $r=0.;$g=0.;$blue=0.;$n=0
        foreach($dy in 0..6){foreach($dx in 0..6){
            $sx=[int][Math]::Floor(($x+($dx+0.5)/7-$left)/$scale+$a[0])
            $sy=[int][Math]::Floor(($y+($dy+0.5)/7-$top)/$scale+$a[1])
            if($sx -lt 0 -or $sx -ge $source.Width -or $sy -lt 0 -or $sy -ge $source.Height){continue}
            $c=$source.GetPixel($sx,$sy)
            if($c.A -ge 200){$r+=$c.R;$g+=$c.G;$blue+=$c.B;$n++}
        }}
        if($n -lt 20){continue}
        $r/=$n;$g/=$n;$blue/=$n
        $best=[double]::MaxValue;$chosen=$palette[0]
        foreach($c in $palette){
            $distance=[Math]::Pow($r-$c.R,2)+[Math]::Pow($g-$c.G,2)+[Math]::Pow($blue-$c.B,2)
            if($distance -lt $best){$best=$distance;$chosen=$c}
        }
        $output.SetPixel($x,$y,$chosen)
    }}
    # Contorno de um pixel da paleta: nao deixa a mistura da borda virar marrom.
    $outline=[Drawing.ColorTranslator]::FromHtml('#1B1815')
    for($y=0;$y -lt 32;$y++){for($x=0;$x -lt 48;$x++){
        if($output.GetPixel($x,$y).A -eq 0){continue}
        if($x -eq 0 -or $x -eq 47 -or $y -eq 0 -or $y -eq 31 -or
            $output.GetPixel($x-1,$y).A -eq 0 -or $output.GetPixel($x+1,$y).A -eq 0 -or
            $output.GetPixel($x,$y-1).A -eq 0 -or $output.GetPixel($x,$y+1).A -eq 0){$output.SetPixel($x,$y,$outline)}
    }}
    New-Item -ItemType Directory -Force -Path (Split-Path $OutputImage) | Out-Null
    $output.Save($OutputImage,[Drawing.Imaging.ImageFormat]::Png)
    Write-Output "$Kind : amostragem direta em 48x32, paleta e alpha binario."
} finally {$source.Dispose();$layout.Dispose();$output.Dispose()}
