param([string[]]$Family=@(),[string]$Destination=(Join-Path $PSScriptRoot '../..'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
if(-not ('Fabula.Incrementant.ArtTools.NormalizeScenery' -as [type])){Add-Type -Path (Join-Path $PSScriptRoot 'NormalizeScenery.cs') -ReferencedAssemblies System.Drawing}
$palette=@(Get-Content (Join-Path $PSScriptRoot '../../../Palette/MasterPalette.hex') | Where-Object {$_ -match '^#?[0-9A-Fa-f]{6}$'} | Select-Object -First 32)
$jobs=@((Get-Content (Join-Path $PSScriptRoot 'refinement-prompts.json') -Raw|ConvertFrom-Json).entries)
$processed=Get-Content (Join-Path $PSScriptRoot 'processed.json') -Raw|ConvertFrom-Json
if($Family.Count){$jobs=@($jobs|Where-Object {$Family -contains $_.Family});$processed=@($processed|Where-Object {$Family -contains $_.Family})}
if($jobs.Count -eq 0 -or $jobs.Count -ne $processed.Count){throw 'Familia/conjunto ainda incompleto.'}
foreach($job in $jobs){
    $p=@($processed|Where-Object Name -eq $job.Name)
    if($p.Count -ne 1){throw ('Master ausente/duplicado: '+$job.Name)}
    $path=Join-Path (Join-Path (Join-Path $PSScriptRoot 'Refined') $job.Family) ($job.Name+'.png')
    [Fabula.Incrementant.ArtTools.NormalizeScenery]::Validate($path,[int]$p[0].Width,[int]$p[0].Height,$palette)
}
foreach($job in $jobs){
    $path=Join-Path (Join-Path (Join-Path $PSScriptRoot 'Refined') $job.Family) ($job.Name+'.png')
    $dest=Join-Path $Destination $job.Family
    New-Item -ItemType Directory -Force $dest | Out-Null
    Copy-Item -LiteralPath $path -Destination (Join-Path $dest ($job.Name+'.png')) -Force
}
Write-Output ($jobs.Count.ToString()+' PNGs validados e exportados; .meta nao escritos pelo script.')
