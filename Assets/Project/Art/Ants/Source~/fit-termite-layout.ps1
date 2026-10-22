param(
    [Parameter(Mandatory=$true)][string]$ImagePath,
    [Parameter(Mandatory=$true)][string]$LayoutImage
)
# Registra a arte pela carapaca, preservando o enquadramento comum da animacao.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
function BodyBounds($image) {
    $mask=New-Object 'bool[,]' 64,64
    for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
        $c=$image.GetPixel($x,$y)
        $hex='{0:X2}{1:X2}{2:X2}' -f $c.R,$c.G,$c.B
        $mask[$x,$y]=$c.A -gt 0 -and $hex -in @('3A1A5C','504D55')
    }}
    $left=64;$right=-1;$top=64;$bottom=-1
    # A erosao descarta antenas e detalhes finos; o nucleo e so a carapaca.
    for($y=1;$y -lt 63;$y++){for($x=1;$x -lt 63;$x++){
        $inside=$true
        for($dy=-1;$dy -le 1 -and $inside;$dy++){for($dx=-1;$dx -le 1;$dx++){
            if(-not $mask[($x+$dx),($y+$dy)]){$inside=$false;break}
        }}
        if($inside){$left=[Math]::Min($left,$x);$right=[Math]::Max($right,$x);$top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)}
    }}
    if($right -lt $left){throw 'Carapaca nao localizada na paleta esperada.'}
    return @($left,$top,$right,$bottom)
}
$image=[Drawing.Bitmap]::FromFile($ImagePath)
$layout=[Drawing.Bitmap]::FromFile($LayoutImage)
$output=New-Object Drawing.Bitmap 64,64
try {
    $a=BodyBounds $image;$b=BodyBounds $layout
    $scale=($b[2]-$b[0]+1.)/($a[2]-$a[0]+1.)
    $ax=($a[0]+$a[2])/2.;$ay=($a[1]+$a[3])/2.
    $bx=($b[0]+$b[2])/2.;$by=($b[1]+$b[3])/2.
    for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
        $sx=[int][Math]::Round(($x-$bx)/$scale+$ax)
        $sy=[int][Math]::Round(($y-$by)/$scale+$ay)
        if($sx -ge 0 -and $sx -lt 64 -and $sy -ge 0 -and $sy -lt 64){$output.SetPixel($x,$y,$image.GetPixel($sx,$sy))}
    }}
    $image.Dispose()
    $output.Save($ImagePath,[Drawing.Imaging.ImageFormat]::Png)
    Write-Output ('Enquadramento registrado: '+[IO.Path]::GetFileName($ImagePath))
} finally {$image.Dispose();$layout.Dispose();$output.Dispose()}
