param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$items=Get-Content (Join-Path $PSScriptRoot 'processed.json') -Raw|ConvertFrom-Json
$ant=[Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot '../../../Ants/Ant_Walk_0.png'))
$font=New-Object Drawing.Font 'Arial',13
$title=New-Object Drawing.Font 'Arial',19
function Canvas([int]$rows,[int]$cardHeight=500){
    $b=New-Object Drawing.Bitmap 1760,($rows*$cardHeight+64)
    $g=[Drawing.Graphics]::FromImage($b)
    $g.Clear([Drawing.Color]::FromArgb(48,46,43))
    $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
    return @($b,$g)
}
function Card($g,$item,[int]$i,[int]$columns=2,[int]$cardHeight=500){
    $before=[Drawing.Bitmap]::FromFile((Join-Path (Join-Path (Join-Path $PSScriptRoot 'Layouts') $item.Family) ($item.Name+'.png')))
    $after=[Drawing.Bitmap]::FromFile((Join-Path (Join-Path (Join-Path $PSScriptRoot 'Refined') $item.Family) ($item.Name+'.png')))
    try {
        $cardWidth=1760/$columns;$panel=$cardWidth/2
        $x=($i%$columns)*$cardWidth;$y=[Math]::Floor($i/$columns)*$cardHeight+64
        $viewHeight=$cardHeight-130;$viewWidth=$panel-48;$ground=$y+64+$viewHeight
        $g.DrawString(($item.Name+' | '+$item.Family),$font,[Drawing.Brushes]::White,$x+12,$y+4)
        $g.DrawString(('ANTES: '+$item.OriginalWidth+'x'+$item.OriginalHeight+' cel.'),$font,[Drawing.Brushes]::LightGray,$x+12,$y+32)
        $g.DrawString(('DEPOIS: '+$item.WorldWidth+'x'+$item.WorldHeight+' cel.'),$font,[Drawing.Brushes]::LightGray,$x+$panel+12,$y+32)
        $sb=[Math]::Min(2.0,[Math]::Min($viewWidth/($before.Width*16),$viewHeight/($before.Height*16)))
        $sa=[Math]::Min(2.0,[Math]::Min($viewWidth/$after.Width,$viewHeight/$after.Height))
        $bw=[int]($before.Width*16*$sb);$bh=[int]($before.Height*16*$sb)
        $aw=[int]($after.Width*$sa);$ah=[int]($after.Height*$sa)
        $g.DrawImage($before,[int]($x+12),[int]($ground-$bh),$bw,$bh)
        $g.DrawImage($after,[int]($x+$panel+12),[int]($ground-$ah),$aw,$ah)
        $g.DrawImage($ant,[int]($x+$panel+12),[int]$ground,[int][Math]::Max(1,48*$sa),[int][Math]::Max(1,32*$sa))
        $g.DrawString('operaria 3x2 cel. em escala',$font,[Drawing.Brushes]::LightGray,$x+$panel+122,$y+$cardHeight-37)
    } finally {$before.Dispose();$after.Dispose()}
}
try {
    New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
    $representatives=@()
    foreach($family in $items.Family|Sort-Object -Unique){
        $group=@($items|Where-Object Family -eq $family|Sort-Object Name)
        $columns=2;$cardHeight=500
        if($family -in @('DryLeaves','Twigs')){$columns=1;$cardHeight=240}
        if($family -eq 'Grains'){$cardHeight=300}
        $canvas=Canvas ([int][Math]::Ceiling($group.Count/[double]$columns)) $cardHeight;$b=$canvas[0];$g=$canvas[1]
        try {
            $g.DrawString(($family+' | antes/depois ajustados ao quadro; dimensoes indicam tamanho no mundo'),$title,[Drawing.Brushes]::White,12,14)
            for($i=0;$i -lt $group.Count;$i++){Card $g $group[$i] $i $columns $cardHeight}
            $b.Save((Join-Path $OutputDirectory ('Cenario_'+$family+'_refinado_2026-10-21.png')),[Drawing.Imaging.ImageFormat]::Png)
        } finally {$g.Dispose();$b.Dispose()}
        $representatives+=$group[0]
    }
    $canvas=Canvas ([int][Math]::Ceiling($representatives.Count/2.0));$b=$canvas[0];$g=$canvas[1]
    try {
        $g.DrawString('CENARIO REFINADO | uma amostra por familia | limite aprovado: 128 celulas',$title,[Drawing.Brushes]::White,12,14)
        for($i=0;$i -lt $representatives.Count;$i++){Card $g $representatives[$i] $i}
        $b.Save((Join-Path $OutputDirectory 'Cenario_refinado_2026-10-21.png'),[Drawing.Imaging.ImageFormat]::Png)
    } finally {$g.Dispose();$b.Dispose()}
} finally {$ant.Dispose();$font.Dispose();$title.Dispose()}
