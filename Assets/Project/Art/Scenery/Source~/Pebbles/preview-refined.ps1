param([Parameter(Mandatory=$true)][string]$OutputImage)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$sheet=New-Object Drawing.Bitmap 1760,2580
$g=[Drawing.Graphics]::FromImage($sheet)
$g.Clear([Drawing.Color]::FromArgb(48,46,43))
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
$font=New-Object Drawing.Font 'Arial',14
$title=New-Object Drawing.Font 'Arial',20
$ant=[Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot '../../../Ants/Ant_Walk_0.png'))
try {
    $g.DrawString('PEBBLES | antes / depois | operaria na mesma escala de cada par',$title,[Drawing.Brushes]::White,12,12)
    $i=0
    Get-ChildItem (Join-Path $PSScriptRoot 'Refined') -Filter '*.png' | Sort-Object Name | ForEach-Object {
        $before=[Drawing.Bitmap]::FromFile((Join-Path (Join-Path $PSScriptRoot 'Layouts') $_.Name))
        $after=[Drawing.Bitmap]::FromFile($_.FullName)
        try {
            $x=($i%2)*880;$y=[Math]::Floor($i/2)*360+60
            $s=[Math]::Min(2.0,[Math]::Min(390.0/$after.Width,230.0/$after.Height))
            $w=[int]($after.Width*$s);$h=[int]($after.Height*$s)
            $g.DrawString(($_.BaseName+' | '+$before.Width+' x '+$before.Height+' celulas'),$font,[Drawing.Brushes]::White,$x+12,$y+4)
            $g.DrawString('ANTES',$font,[Drawing.Brushes]::LightGray,$x+12,$y+32)
            $g.DrawString('DEPOIS',$font,[Drawing.Brushes]::LightGray,$x+450,$y+32)
            $g.DrawImage($before,[int]($x+12),[int]($y+64+230-$h),$w,$h)
            $g.DrawImage($after,[int]($x+450),[int]($y+64+230-$h),$w,$h)
            $g.DrawImage($ant,[int]($x+450),[int]($y+296),[int][Math]::Max(1,48*$s),[int][Math]::Max(1,32*$s))
            $g.DrawString('operaria: 3 x 2 celulas',$font,[Drawing.Brushes]::LightGray,$x+560,$y+315)
        } finally {$before.Dispose();$after.Dispose()}
        $i++
    }
    $sheet.Save($OutputImage,[Drawing.Imaging.ImageFormat]::Png)
} finally {$ant.Dispose();$g.Dispose();$sheet.Dispose();$font.Dispose();$title.Dispose()}
