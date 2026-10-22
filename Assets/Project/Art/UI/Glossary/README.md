# Imagens do glossário

Uma imagem 640x360 por entrada do glossário; o nome do arquivo é o id da entrada em `GlossaryContent` (ex.: `nutrientroom.png`). O setup do HUD (**Incrementant/Setup/Montar HUD da partida**) liga todas as imagens desta pasta à janela; arquivo sem entrada correspondente gera aviso no Console. `GlossaryImageImporter` importa como Sprite Point, sem compressão. Guia: [GlossaryGuide](../../../Scripts/Runtime/Glossary/GlossaryGuide.md).

## Fotos do jogo (salas e ferramentas)

`queenroom`, `nutrientroom`, `nursery`, `waste`, `dew`, `farm`, `depots`, `light` (= `glowbrush`), `build` e `dig`, tiradas em 2026-10-22 a pedido do desenvolvedor: num mapa novo (não no quicksave), salas redondas de 16 células de diâmetro cavadas longe do ninho, sem formigas, rebocadas, com cogumelos luminosos nas paredes e no teto para iluminar, e com os elementos de cada sala (canteiros e nutrientes, ovo/larva/pupa, resíduos, teto de terra fofa e folhas, pulgões, itens de cada depósito, marcas de cavar e de construir). "Mostrar ambientes" desligado (aparece o visual da sala pronta) e barras de progresso escondidas. Captura da Game view a 32 px por célula, recorte central 1280x720 reduzido exatamente pela metade.

Para refazer depois de mudar a arte do terreno, repita o processo pelo MCP da Unity em Play Mode (o passo a passo está no histórico da sessão de 2026-10-22; as APIs usadas são `PixelGrid.Set`, `Set*Room`, `SetLining`, `SetBed`, `SetNutrient`, `SetStored`, `SetMarked`, `SetBuildMarked`, `AntColony.RestoreQueen`, `RestoreFarm` e `Brood.Restore`).

## Imagens de sprites (objetos e formigas)

`nutrients`, `brood`, `queen`, os dez papéis de formiga, `raids`, `blackaphid`, `soil`, `water`, `rubble` e `plants`: montadas por `Source~/compose_sprites.py` com os sprites de `Art/Ants`, `Art/Aphids` e o atlas do terreno, ampliados por fator inteiro sobre a foto `light.png`. Para refazer:

```powershell
python "Assets/Project/Art/UI/Glossary/Source~/compose_sprites.py"
```
