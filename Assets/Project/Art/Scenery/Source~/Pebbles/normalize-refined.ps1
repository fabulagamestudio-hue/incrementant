param(
    [Parameter(Mandatory=$true)][string]$InputImage,
    [Parameter(Mandatory=$true)][string]$LayoutImage,
    [Parameter(Mandatory=$true)][string]$OutputImage
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$palette=@('1B1815','382414','463123','785A40','9A7F5E','B9986D','CFB691','E4D2AE','504D55','7D7676','A59685','BBB29F') |
    ForEach-Object {[Drawing.ColorTranslator]::FromHtml('#'+$_)}
function Bounds($image,[int]$step=1) {
    $l=$image.Width;$r=-1;$t=$image.Height;$b=-1
    for($y=0;$y -lt $image.Height;$y+=$step){for($x=0;$x -lt $image.Width;$x+=$step){
        if($image.GetPixel($x,$y).A -ge 200){
            $l=[Math]::Min($l,$x);$r=[Math]::Max($r,$x)
            $t=[Math]::Min($t,$y);$b=[Math]::Max($b,$y)
        }
    }}
    if($r -lt $l){throw 'Imagem vazia.'}
    return @($l,$t,$r,$b)
}
$source=[Drawing.Bitmap]::FromFile($InputImage)
$layout=[Drawing.Bitmap]::FromFile($LayoutImage)
$width=$layout.Width*16;$height=$layout.Height*16
$output=New-Object Drawing.Bitmap $width,$height
try {
    $a=Bounds $source 3;$b=Bounds $layout
    $sw=$a[2]-$a[0]+1.;$sh=$a[3]-$a[1]+1.
    $scale=[Math]::Min((($b[2]-$b[0]+1.)*16)/$sw,(($b[3]-$b[1]+1.)*16)/$sh)
    $left=(($b[0]+$b[2]+1.)*16-$sw*$scale)/2.
    $top=($b[3]+1.)*16-$sh*$scale
    $cache=@{}
    for($y=0;$y -lt $height;$y++){for($x=0;$x -lt $width;$x++){
        $sx=[int][Math]::Floor(($x+0.5-$left)/$scale+$a[0])
        $sy=[int][Math]::Floor(($y+0.5-$top)/$scale+$a[1])
        if($sx -lt 0 -or $sx -ge $source.Width -or $sy -lt 0 -or $sy -ge $source.Height){continue}
        $c=$source.GetPixel($sx,$sy)
        if($c.A -lt 200){continue}
        $key=$c.ToArgb()
        if(-not $cache.ContainsKey($key)){
            $best=[double]::MaxValue;$chosen=$palette[0]
            foreach($p in $palette){
                $distance=($c.R-$p.R)*($c.R-$p.R)+($c.G-$p.G)*($c.G-$p.G)+($c.B-$p.B)*($c.B-$p.B)
                if($distance -lt $best){$best=$distance;$chosen=$p}
            }
            $cache[$key]=$chosen
        }
        $output.SetPixel($x,$y,$cache[$key])
    }}
    # Contorno de exatamente um texel na silhueta final, sem alpha parcial.
    for($y=0;$y -lt $height;$y++){for($x=0;$x -lt $width;$x++){
        if($output.GetPixel($x,$y).A -eq 0){continue}
        if($x -eq 0 -or $x -eq $width-1 -or $y -eq 0 -or $y -eq $height-1 -or
           $output.GetPixel($x-1,$y).A -eq 0 -or $output.GetPixel($x+1,$y).A -eq 0 -or
           $output.GetPixel($x,$y-1).A -eq 0 -or $output.GetPixel($x,$y+1).A -eq 0){$output.SetPixel($x,$y,$palette[0])}
    }}
    New-Item -ItemType Directory -Force (Split-Path $OutputImage) | Out-Null
    $output.Save($OutputImage,[Drawing.Imaging.ImageFormat]::Png)
    Write-Output "$OutputImage : ${width}x${height}, 16 px/celula, paleta mestra e alpha binario."
} finally {$source.Dispose();$layout.Dispose();$output.Dispose()}
