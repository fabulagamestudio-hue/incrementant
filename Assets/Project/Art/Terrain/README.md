# Atlas do terreno

Atlas RGB desenhado na [paleta mestra](../Palette/README.md) (etapa 5 do [documento 40](../../../../Docs/02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md)). Formato e como o shader o usa: `AntFarmGuide.md`, "Atlas RGB". Prévia de cada linha repetida 2×2: [42_Atlas_do_terreno.png](../../../../Docs/02_World_Art_UI/42_Atlas_do_terreno.png). Maquete de uma sala de cada família construída, ao lado da mesma sala antes do reboco: [43_Salas_construidas.png](../../../../Docs/02_World_Art_UI/43_Salas_construidas.png).

> **Experimento 8x8 (branch `experimento/arte-8x8`, 2026-10-22):** `TerrainAtlas.png` (64 × 3776 desde as salas da fundição, da forja e dos armazéns de 2026-10-23, linhas de 64 × 64) e `GrassOverhang.png` (64 × 8) foram reduzidos pela metade com `../Source~/halve_pixel_art.py`. Os tamanhos abaixo e os geradores ainda são os de 16 texels por célula: rodar um gerador e depois o script de redução. Ver `AntFarmGuide.md`, "Arte 8x8". O contorno preto da terra e das folhas é desenhado pelo shader ("Contorno preto" no mesmo guia), não está nos PNGs.

| Arquivo | Conteúdo |
|---|---|
| `TerrainAtlas.png` | 128 × 7552 px: 59 linhas de 128 × 128, de baixo para cima: os materiais na ordem de `CellMaterial`, a parede de fundo e o par bloco/fundo de cada sala construída (`RoomStyle`). Atribuído em **Tile Atlas** do `AntFarmView` nas cenas AntFarm e MainMenu. Importação: sRGB, Point, sem compressão, sem mipmaps, Read/Write, tamanho máximo 8192 (com 4096 a Unity reduziria o PNG e o jogo cairia no atlas gerado) |
| `Source~/terrain_atlas.py` | Gerador do PNG e das duas prévias (a pasta com `~` não é importada pela Unity) |
| `GrassOverhang.png` | 128 × 16 px RGBA: a grama que passa do topo do bloco de grama para a célula vazia de cima (experimento de 2026-10-22; como o shader usa: `AntFarmGuide.md`, "Transbordo da grama"). Franja de moitas baixas e touceiras em leque, só verdes da paleta (21–27), alpha = folha. Atribuído em **Grass Overhang** do `AntFarmView` nas cenas AntFarm e MainMenu. Importação: sRGB, Point, Repeat, sem compressão, sem mipmaps |
| `Source~/grass_overhang.py` | Gerador do `GrassOverhang.png` e de uma prévia sobre o bloco de grama do atlas: `python -I "Assets/Project/Art/Terrain/Source~/grass_overhang.py" Assets/Project/Art/Palette/MasterPalette.hex Assets/Project/Art/Terrain/GrassOverhang.png [prévia.png] [TerrainAtlas.png]` |

## Como refazer

```bash
python -I "Assets/Project/Art/Terrain/Source~/terrain_atlas.py" Assets/Project/Art/Palette/MasterPalette.hex Assets/Project/Art/Terrain/TerrainAtlas.png Docs/02_World_Art_UI/42_Atlas_do_terreno.png Docs/02_World_Art_UI/43_Salas_construidas.png
```

Precisa de Python com `numpy` e `Pillow`. O script falha se alguma cor sair da paleta. Cada linha é uma função com sementes fixas (`build()`): mudar uma linha não mexe nas outras. Também dá para pintar o PNG à mão, desde que o tamanho, a ordem das linhas e a paleta continuem os mesmos.

## O que cada linha desenha

| Linha | Material | Desenho | Cores (índices da paleta) |
|---|---|---|---|
| 1 | Terra | Manchas em 3 tons com relevo e pedrinhas cinza | 3, 9, 10; pedrinhas 17, 18, 20 |
| 2 | Grama | Manchas de verde | 23, 24, 25 |
| 3 | Areia | Pontilhado em dois tons e grãos soltos | 5, 6, 7, 15, 16 |
| 4 | Pedra | Pedras (Voronoi) com frestas escuras e luz no alto | 1, 17–20 |
| 5 | Terra despejada | Torrões soltos e arredondados | 3, 9–12 |
| 6 | Rocha-mãe | Blocos grandes quase pretos | 0, 1, 17 |
| 7, 8 | Terra e areia compactadas | Camadas horizontais onduladas | 4, 5, 9 / 5, 6, 7 |
| 9 | Terra mole | Manchas escuras com brilho úmido | 1–4 |
| 10–13 | Plantas e restos de planta | Riscos de fibra (vertical na grama, diagonal na folha) | 7, 9, 23–27 |
| 14–16 | Entulho, resíduo, restos de cupim | Pedaços com relevo | 0–5, 8–11, 17, 18 |
| 17 | Cogumelo luminoso | Manchas claras (o shader soma a emissão) | 7, 13, 15, 16 |
| 18 | Serapilheira (REQ-070, 2026-10-23) | Folhas secas sobrepostas com nervura e luz na borda, gravetos miúdos sobre húmus. No 8x8 foi gerada a 16 texels, reduzida com `halve_pixel_art.py` e inserida no PNG atual (as outras linhas não foram regeradas) | 1–5, 8–13 |
| 19 | Terra adubada (REQ-070) | Terra escura com torrões, palha picada e pontas de folha em traços de 2 texels (para sobreviver à redução a 8) | 1–4, 6, 7, 25 |
| 20 | Néctar (REQ-070) | Mel âmbar com bolhas de luz; só no bloco carregado e no item | 7, 12, 13, 15, 16 |
| 21–23 | Minérios de cobre, ferro e ouro (REQ-071; refeitos em 2026-10-23) | A pedra com pepitas do metal, em contorno escuro de 2 texels, luz no alto/esquerda, facetas em dois tons e brilhos | pedra 0, 1, 17–20; cobre 7, 8, 12, 16, 34; ferro 18, 31, 44–46 (pepitas claras de aço, para destacar da pedra); ouro 11–13, 16, 46 |
| 24–26 | Barras de cobre, ferro e ouro (REQ-071; refeitas em 2026-10-23) | Lingotes de 32 × 16 texels (16 × 8 no 8x8) em fileiras alternadas, borda escura, topo claro, lado e base na sombra, brilho no topo; só no bloco carregado e no item guardado | cobre 7, 8, 11, 12, 16, 32, 34; ferro 0, 17–19, 44–46; ouro 8, 10–13, 16 |
| 27–35 | Ferramentas (27–29), armas (30–32) e armaduras (33–35) de cobre, ferro e ouro (REQ-071; refeitas em 2026-10-23) | Um objeto de 8 × 8 texels por célula sobre fundo escuro: picareta, espada e escudo com bossa central, na cor do metal, com cabo de madeira; só no bloco carregado e no item guardado | cobre 1, 4, 5, 7, 8, 12; ferro 0, 4, 5, 18, 44, 46; ouro 2, 4, 5, 11, 13, 16 |
| 0, 36 | Vazio e parede de fundo | Tom do túnel com poucas manchas escuras | 0, 1, 2 |
| 51–58 | Bloco e fundo das salas da fundição, da forja e dos armazéns de armas e de armaduras (2026-10-23) | Fundição: tijolo refratário e brasa nas frestas. Forja: pedra escura com rebites e brilho do fogo. Armas: tábuas e cabideiro. Armaduras: painéis claros e tecido azul. Inseridas no fim do PNG atual, reduzidas a 8 | 0–2, 8, 32–34; 0, 1, 8, 17, 18, 20, 33; 1–4, 9, 10, 18; 1, 3–6, 14, 29, 30 |

Luz de cima à esquerda: as formas clareiam no alto/esquerda e escurecem embaixo/direita. Nenhum padrão de material marca a borda do tile, então a repetição a cada 8 células fica disfarçada.

### Salas construídas (REQ-058, 2026-10-22)

Linhas 19 a 32, um par por `RoomStyle` (bloco, depois fundo). Os cinco depósitos usam o mesmo par. As formigas rebocam com o bloco as paredes de terra em contato com o vão da sala (em volta e dentro da área pintada) e, com a sala pronta, o vão mostra o fundo (regras em `Scripts/Runtime/Colony/BuiltRoomsGuide.md`).

- **Bloco:** a terra que as formigas põem nos slots. Diferente dos materiais, é feito de unidades regulares com a junta alinhada à grade das células, para o construído se distinguir da terra natural; um slot isolado mostra unidades inteiras.
- **Fundo:** a parede da sala construída. Parte do tom do túnel, um degrau mais claro e calmo (índice 2 em vez de 1), com o motivo da família; o shader ainda o suaviza com **Back Detail**.

| Linhas | Sala | Bloco | Fundo | Cores (índices da paleta) |
|---|---|---|---|---|
| 19, 20 | Rainha | Lajota clara de 1 célula, chanfro largo, losango dourado embutido | Reboco com treliça de losangos em sulco e cravos dourados | 3, 5, 6, 11–15 / 1–3, 11, 12 |
| 21, 22 | Cultivo | Torrões de húmus de ½ célula com folha picada e fio de fungo | Rede de micélio claro com nós de fungo | 1–5, 15, 24, 25 / 1–4, 14 |
| 23, 24 | Berçário | Almofadas creme de ½ célula, quinas cortadas | Nichos redondos (berços) em favo, um por célula | 5, 6, 14–16 / 0–3 |
| 25, 26 | Resíduos | Crosta escura em fiadas tortas, ferrugem e cascalho | Parede encardida com escorridos | 0–3, 8, 17 / 0–3 |
| 27, 28 | Orvalho | Ladrilho de pedra vidrado de 1 célula, reflexo, gota e junta úmida | Manchas de umidade, escorridos azulados e gotas | 1, 17–20, 29–31 / 0–3, 29–31 |
| 29, 30 | Fazenda | Trançado de fibra (cesto) em retalhos de ½ célula | Musgo escuro e pingos de melada | 9, 22–25 / 1–3, 7, 22, 23 |
| 31, 32 | Depósito | Tijolos de barro cozido em amarração corrida, dois por célula | Prateleiras a cada 2 células e esteios a cada 4 | 2, 9–12 / 0–4 |
