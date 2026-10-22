# Fontes do refinamento do cenário

REQ-054, ART-019, PRG-062 — 2026-10-21. Oito famílias, 55 imagens; Pebbles mantém as fontes próprias em `../Pebbles`.

## Direção e escala

ImageGen integrado, com os PNGs anteriores, a operária refinada e uma pedra refinada como referências. Paleta mestra (32 cores de cenário), luz no alto à esquerda, contorno escuro, planos de luz e sombra definidos, alpha 0/255 e 16 pixels por célula.

O desenvolvedor aprovou limitar a 128 células o maior eixo dos elementos gigantes. São 24 imagens reduzidas proporcionalmente, com dimensões arredondadas à célula; os outros 31 elementos preservam a escala. Nenhum PNG final supera 2048 pixels por eixo. Pivôs normalizados e GUIDs permanecem; dimensões, PPU e limite de importação são atualizados intencionalmente.

## Arquivos

- `refinement-prompts.json`: prompts e referências usados pelo ImageGen integrado.
- `Results/`: resultado individual de cada geração, com caminho da saída original.
- `Generated/`: cópias locais das saídas originais.
- `Layouts/`: referências anteriores e seus metadados.
- `Refined/`: masters finais por família.
- `processed.json`: dimensões originais/finais e escala física de cada variante processada.
- `NormalizeScenery.cs`: amostragem direta na grade final, cores da paleta e contorno. Compilada somente em memória pelo Windows PowerShell, sem exportar executáveis ou assemblies.

## Reproduzir

Execute a partir da raiz do projeto, com Windows PowerShell 5.1:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Assets/Project/Art/Scenery/Source~/RefinedScenery/process-refined.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Assets/Project/Art/Scenery/Source~/RefinedScenery/bake-refined.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Assets/Project/Art/Scenery/Source~/RefinedScenery/preview-refined.ps1 -OutputDirectory H:/Fabula/Games/Incrementant/Docs/02_World_Art_UI
```

O processamento é incremental; a exportação completa exige os 55 masters e valida dimensões, paleta e alpha antes de copiar. `bake-refined.ps1 -Family Clovers` exporta somente uma família completa. Nenhum script escreve `.meta`; a Unity ajusta o PPU pelo importador e preserva os GUIDs.

Os exportadores procedurais antigos sobrescrevem esta arte. Para restaurá-la, use os masters deste pacote e, para as pedras, `../Pebbles/bake-refined.ps1`.

## Plantas colhíveis

`PlantSprites` mantém o sprite original compartilhado e atualiza uma textura pequena, de um texel por célula, em `PlantCellMask`. O shader `Resources/Scenery/DetailedPlants.shader` aplica transparência e marcação sem copiar a textura grande por planta. A máscara de forma/navegação continua separada da arte e conserva pontas de um pixel. Crescimento, parte enterrada, colheita e marcação usam a mesma grade da simulação.
