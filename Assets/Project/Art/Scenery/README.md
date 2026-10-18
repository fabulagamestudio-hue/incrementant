# Elementos de cenário — pixel art

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
