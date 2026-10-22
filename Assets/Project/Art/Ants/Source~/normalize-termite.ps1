param(
    [Parameter(Mandatory=$true)][string]$InputImage,
    [Parameter(Mandatory=$true)][string]$OutputImage
)
# Mesma normalizacao das formigas, com canvas e paleta atuais dos cupins.
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'normalize-refined.ps1') -InputImage $InputImage -OutputImage $OutputImage `
    -Width 64 -Height 64 -KeepAspect -PaletteHex @('1B1815','3A1A5C','504D55','6B1F45','D6872F','F5EDD9','BBB29F')
