param([string]$Destination=(Join-Path $PSScriptRoot '../../Pebbles'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$allowed=@{}
@('1B1815','382414','463123','785A40','9A7F5E','B9986D','CFB691','E4D2AE','504D55','7D7676','A59685','BBB29F') |
    ForEach-Object {$allowed[[Drawing.ColorTranslator]::FromHtml('#'+$_).ToArgb()]=$true}
$files=@(Get-ChildItem (Join-Path $PSScriptRoot 'Refined') -Filter '*.png')
if($files.Count -ne 14){throw 'Esperados 14 masters refinados.'}
foreach($file in $files){
    $b=[Drawing.Bitmap]::FromFile($file.FullName)
    $layout=[Drawing.Bitmap]::FromFile((Join-Path (Join-Path $PSScriptRoot 'Layouts') $file.Name))
    try {
        if($b.Width -ne $layout.Width*16 -or $b.Height -ne $layout.Height*16){throw "Dimensoes incorretas: $($file.Name)"}
        for($y=0;$y -lt $b.Height;$y++){for($x=0;$x -lt $b.Width;$x++){
            $c=$b.GetPixel($x,$y)
            if($c.A -ne 0 -and ($c.A -ne 255 -or -not $allowed.ContainsKey($c.ToArgb()))){throw "Cor/alpha invalido: $($file.Name) [$x,$y]"}
        }}
    } finally {$b.Dispose();$layout.Dispose()}
}
New-Item -ItemType Directory -Force $Destination | Out-Null
foreach($file in $files){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $Destination $file.Name) -Force}
Write-Output '14 pedras exportadas. PNGs validados antes da copia; arquivos .meta preservados.'
