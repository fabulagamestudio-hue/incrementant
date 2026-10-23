# Elementos de cenário — pixel art

> **Experimento 8x8 (branch `experimento/arte-8x8`, 2026-10-22):** os 48 PNGs refinados (Clovers, DryLeaves, Flowers, Grains, Grass, Pebbles) estão com metade do tamanho descrito abaixo, a 8 px por célula (`../Source~/halve_pixel_art.py`), e com contorno preto de 1 texel por dentro (`../Source~/outline_pixel_art.py`). Os legados de 6 PPU (Mushrooms, Tufts, Twigs) não mudaram: 1 pixel deles = 1 célula, e o shader `SceneryCells` desenha cada célula a 8 texels com o contorno preto (antes 16). Os exportadores ainda geram a 16. Ver `Scripts/Runtime/AntFarm/AntFarmGuide.md`, "Arte 8x8" e "Contorno preto". Desde 2026-10-23 os gravetos do jogo são montados por peças ([`../SceneryKits/`](../SceneryKits/README.md)); os PNGs de `Twigs/` só voltam se o campo **Twig Kit** do `SurfaceSceneryView` ficar vazio.

## Atualização de 2026-10-21 — Pebbles

As 14 [pedras](Pebbles/README.md) foram refinadas a 16 pixels por célula, com tamanho físico preservado, no estilo das formigas. São desenhadas como sprites separados da máscara de navegação, sem ampliar a camada inteira. Fontes, prompts e reexportação em `Source~/Pebbles`. O restante desta página descreve a entrega anterior e as famílias que continuam no caminho legado.

## Entrega de 2026-10-18

Arte revisada em 2026-10-18 (REQ-026), usando `image_gen.imagegen` integrado e os PNGs anteriores como referência. [Prompts e processamento](../PixelArtGeneration.json).

| Pasta | PNGs | Detalhes |
|---|---|---|
| `Clovers/` | 6 | Nervuras, volumes e luz nas folhas |
| `DryLeaves/` | 6 | Nervura central, dobras e bordas secas |
| `Flowers/` | 4 | Pétalas sobrepostas, miolo e folhas |
| `Grains/` | 8 | Pequenos planos de luz e fendas |
| `Grass/` | 10 | Lâminas sobrepostas e reflexos nas bordas |
| `Mushrooms/` | 8 | Volume do chapéu, lamelas e fibras do caule |
| `Pebbles/` | 14 | Facetas, fissuras e variação mineral |
| `Tufts/` | 8 | Dobras e separação das folhas |
| `Twigs/` | 5 | Casca, nós e pontas partidas |

Os 69 arquivos mantêm nomes, dimensões, GUIDs e pivôs. Foram preparados com amostragem nearest-neighbor, paleta conjunta de até 24 cores por família (pedras 16, grãos 8) e alpha exclusivamente 0/255. Não há pixels de borda semitransparentes. A escala existente `SceneryStamp.ArtScale = 6` foi preservada.

Importação: `SceneryArtImporter` usa filtro Point, sem mipmaps nem compressão, leitura habilitada e 6 pixels por unidade. Formato e integração: [AntFarmGuide.md](../../Scripts/Runtime/AntFarm/AntFarmGuide.md), seção "Elementos isolados". As formas das plantas e as áreas caminháveis seguem a máscara da nova arte; os desenhos mantêm enquadramento e proporções, mas não uma máscara idêntica à provisória.

O menu de exportação procedural sobrescreve a arte revisada se acionado. O setup exporta apenas pastas vazias. Editar durante o Play só afeta a biblioteca no próximo Play.

Validação: dimensões, alpha binário, paletas e metadados conferidos nos 141 PNGs da entrega (1.390.673 pixels); prévia das nove famílias inspecionada. Nenhuma build gerada. A validação em jogo está registrada separadamente em ART-010, `Production/ART.md`.
