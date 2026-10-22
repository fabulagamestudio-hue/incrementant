param([ValidateRange(4,128)][int]$MaxWorldDimension=128)
$ErrorActionPreference='Stop'
# Windows PowerShell 5.1 fornece System.Drawing sem dependências adicionais.
Add-Type -AssemblyName System.Drawing
if(-not ('Fabula.Incrementant.ArtTools.NormalizeScenery' -as [type])){Add-Type -Path (Join-Path $PSScriptRoot 'NormalizeScenery.cs') -ReferencedAssemblies System.Drawing}
$palette=@(Get-Content (Join-Path $PSScriptRoot '../../../Palette/MasterPalette.hex') | Where-Object {$_ -match '^#?[0-9A-Fa-f]{6}$'} | Select-Object -First 32)
$manifest=Get-Content (Join-Path $PSScriptRoot 'refinement-prompts.json') -Raw | ConvertFrom-Json
$processed=@()
foreach($job in $manifest.entries){
    $resultPath=Join-Path (Join-Path $PSScriptRoot 'Results') ($job.Name+'.json')
    if(-not (Test-Path -LiteralPath $resultPath)){continue}
    $result=Get-Content $resultPath -Raw | ConvertFrom-Json
    $width=[int]$job.Width;$height=[int]$job.Height
    if($MaxWorldDimension -gt 0 -and [Math]::Max($width,$height) -gt $MaxWorldDimension){
        $scale=$MaxWorldDimension/[double][Math]::Max($width,$height)
        $width=[Math]::Max(4,[int][Math]::Round($width*$scale));$height=[Math]::Max(4,[int][Math]::Round($height*$scale))
    }
    $raw=Join-Path (Join-Path $PSScriptRoot 'Generated') ($job.Name+'.png')
    $dir=Join-Path (Join-Path $PSScriptRoot 'Refined') $job.Family
    New-Item -ItemType Directory -Force $dir | Out-Null
    $output=Join-Path $dir ($job.Name+'.png')
    $layout=Join-Path (Join-Path (Join-Path $PSScriptRoot 'Layouts') $job.Family) ($job.Name+'.png')
    if(-not (Test-Path -LiteralPath $raw)){Copy-Item -LiteralPath $result.generatedFile -Destination $raw}
    $b=$null;$current=$false
    if(Test-Path -LiteralPath $output){$b=[Drawing.Bitmap]::FromFile($output);$current=$b.Width -eq $width*16 -and $b.Height -eq $height*16;$b.Dispose()}
    if(-not $current){[Fabula.Incrementant.ArtTools.NormalizeScenery]::Run($raw,$layout,$output,$width*16,$height*16,$palette)}
    $processed+=[PSCustomObject]@{Family=$job.Family;Name=$job.Name;Width=$width*16;Height=$height*16;WorldWidth=$width;WorldHeight=$height;OriginalWidth=$job.Width;OriginalHeight=$job.Height}
}
$processed | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'processed.json') -Encoding UTF8
Write-Output ($processed.Count.ToString()+' sprites normalizados; limite em celulas: '+$MaxWorldDimension)
