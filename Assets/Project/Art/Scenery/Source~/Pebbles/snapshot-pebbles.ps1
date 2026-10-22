# Guarda ou restaura uma cópia dos 14 masters das pedras, para testar mudanças com rollback.
#   .\snapshot-pebbles.ps1 -Save    -Name antes-textura
#   .\snapshot-pebbles.ps1 -Restore -Name antes-textura
#   .\snapshot-pebbles.ps1 -List
# Restaurar copia o snapshot para Refined/ e para os PNGs do jogo; os .meta não são tocados.
param([string]$Name, [switch]$Save, [switch]$Restore, [switch]$List)
$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot 'Snapshots'
$refined = Join-Path $PSScriptRoot 'Refined'
$game = Join-Path $PSScriptRoot '../../Pebbles'

if ($List) {
    if (Test-Path $root) { Get-ChildItem $root -Directory | ForEach-Object { $_.Name } }
    return
}
if (-not $Name) { throw 'Informe -Name.' }
if ($Save -eq $Restore) { throw 'Use -Save ou -Restore.' }
$dir = Join-Path $root $Name

if ($Save) {
    if (Test-Path $dir) { throw "Snapshot '$Name' já existe; escolha outro nome." }
    New-Item -ItemType Directory -Force $dir | Out-Null
    # Copia os PNGs do jogo, que são o estado visível; avisa se o master divergir.
    foreach ($f in Get-ChildItem $game -Filter 'Pebble_*.png') {
        $m = Join-Path $refined $f.Name
        if ((Get-FileHash $f.FullName).Hash -ne (Get-FileHash $m).Hash) { Write-Warning "$($f.Name): master em Refined difere do PNG do jogo; salvo o do jogo." }
        Copy-Item -LiteralPath $f.FullName -Destination $dir
    }
    Write-Output "Snapshot '$Name' salvo com $((Get-ChildItem $dir -Filter *.png).Count) pedras."
}
else {
    if (-not (Test-Path $dir)) { throw "Snapshot '$Name' não existe." }
    $files = @(Get-ChildItem $dir -Filter 'Pebble_*.png')
    if ($files.Count -ne 14) { throw "Snapshot '$Name' tem $($files.Count) pedras; esperadas 14." }
    foreach ($f in $files) {
        Copy-Item -LiteralPath $f.FullName -Destination (Join-Path $refined $f.Name) -Force
        Copy-Item -LiteralPath $f.FullName -Destination (Join-Path $game $f.Name) -Force
    }
    Write-Output "Snapshot '$Name' restaurado em Refined e nos PNGs do jogo. Reimporte na Unity."
}
