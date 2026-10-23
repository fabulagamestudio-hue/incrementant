# Guia — Biomas da superfície: Serapilheira e Canteiro de flores

Pedido do desenvolvedor em 2026-10-23 (REQ-070). Direção e lista de arte: [documento 44](../../../../../Docs/02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md). Primeiro bioma: a **Serapilheira**, à direita do formigueiro; segundo, no mesmo dia: o **Canteiro de flores**, à esquerda. Os dois no escopo "mundo + coletável": o uso dos insumos nas melhorias foi adiado pelo desenvolvedor ("vamos deixar registrado para uma decisão futura").

Código: `World/SurfaceBiomes.cs` (mapa por coluna), `World/LitterFall.cs` (folhas voltando a cobrir o chão), `World/NectarDrip.cs` (gotas de néctar das flores), `CellMaterial.Litter`/`Compost`/`Nectar` e `StorageKind.Litter`/`Nectar` (os depósitos). Ganchos em `TerrainGenerator.CellAt`, `VegetationSimulation.Populate`, `SurfaceSceneryPainter.Paint`, `AntColony.StartDelivery`/`FitsLoad` e `AntFarmGame` (passo da simulação e pincel). Testes: `Tests/EditMode/BiomeTests.cs`.

## O que o jogador vê

- O formigueiro nasce no **gramado**: grama e trevos colhíveis, sem flores, pedras, cogumelos, gravetos nem grãos no cenário.
- Andando para a esquerda, depois de ~240 células da entrada, fica o **Canteiro de flores** (~360 de largura): chão de terra adubada e só flores colhíveis. Cada flor crescida por inteiro solta de tempos em tempos uma **gota de néctar** no chão ao lado dela (até 2 em volta). Com o **Depósito de néctar** pintado (7º botão do submenu DEPÓSITO) e com vaga, as formigas levam as gotas para lá; sem ele, elas ficam onde caíram. A terra adubada cavada vai como terra.
- Andando com a câmera para a direita, depois de ~240 células da entrada, começa a **Serapilheira**: o chão coberto de folhas secas, com gravetos e folhas secas grandes no cenário e sem plantas colhíveis. A fronteira é irregular (as colunas se misturam numa faixa de ~24 células).
- Cavar a cobertura de folhas (pincel Cavar) rende **serapilheira**. Com o **Depósito de serapilheira** pintado (menu Colônia → Ambientes → DEPÓSITO, 6º botão), ela vai para lá; sem ele, é tratada como terra (depósito de construção, canteiro novo ou despejo), como a grama cavada.
- Com o tempo, as folhas voltam a cobrir o chão onde ele foi raspado.

## Como funciona

### Mapa de biomas (`SurfaceBiomes`)

- **Mundo pequeno no molde do Terraria (REQ-071, documento 45):** sem oceano, masmorra e mal. Na ordem, a partir da entrada: gramado, Serapilheira de um lado e Canteiro do outro e, nas pontas, a **Fruteira** (selva, depois da Serapilheira) e o **Areal** (deserto, depois do Canteiro), até a borda (`FillBeyond`, que começa dentro da transição de fora do lateral para não sobrar gramado entre os dois). O lado sai da seed (**Random Side**, `SurfaceBiomes.LitterSide`), como a selva e a neve no Terraria; **Edge Biomes** desliga as pontas.
- **Areal:** chão de areia e areia até **Desert Depth** (40) células, como o deserto subterrâneo; sem plantas nem cenário. O insumo é a areia, que já vai ao depósito de areia.
- **Fruteira:** chão de terra adubada e só mudas folhosas, nas faixas estreitas do Canteiro. O fruto caído fica para depois.
- `SurfaceBiomes.Band(largura, bioma, de, até)` monta um mapa à mão, sem transição (testes do cenário).
- Um byte por coluna (`SurfaceBiome`: `Meadow`, `LeafLitter`, `FlowerBed`, `Desert`, `FruitGrove`), calculado pela seed e pela entrada no construtor do `TerrainGenerator` e gravado na grade em `Begin` (`PixelGrid.Biomes`). Não vai para o save: ao carregar, o gerador recalcula com a mesma seed. Sem gerador (mapa de imagem, testes antigos), `PixelGrid.Biomes` é tudo gramado (`SurfaceBiomes.Uniform`).
- **Terrain Settings → Biomas da superfície** (`BiomeSettings`): **Litter Offset** (240, células da entrada até o início), **Litter Width** (360; 0 desliga), **Flower Offset** (240, da entrada até o fim do canteiro, para a esquerda), **Flower Width** (360; 0 desliga) e **Blend Width** (24). Cada faixa é pintada por `Strip`, com a mesma mistura nas duas bordas. Na transição, cada coluna fica de um lado pelo ruído dela (30% Perlin + 70% hash), com a chance crescendo para dentro do bioma.
- Com os padrões (mundo de 1920 colunas, entrada em ~832), o Canteiro vai de ~232 a ~592 e a Serapilheira de ~1072 a ~1432; os dois ficam quase todo fora da área inicial (640 a 1280) e aparecem quando a câmera chega lá (mundo expansível).

### Terreno

`TerrainGenerator.CellAt` → `GroundCover`: a cobertura do chão (as 2 células de cima, `depth < 2`) é `Litter` na Serapilheira, `Compost` no Canteiro de flores e `Grass` no gramado; fundo de laguinho continua areia compactada. Abaixo, a mesma terra, areia e pedra de todo o mapa (a camada rasa própria do bioma, húmus, fica para depois).

### O material `Litter`

- Fixo (não cai), escavável (0,15 s), não amolece com a água. Família de junção e de borda lascada da **grama** (`MergeFamily` 1, `ChipStyleOf` da grama no `TerrainTiles` e em `AntFarmView.MergeFamily`). Entre grama e serapilheira não há mistura (mesma família): a troca é por célula.
- Atlas: linha 18 de `TerrainAtlas.png` (folhas secas sobrepostas com nervura e gravetos miúdos sobre húmus, paleta 1–5, 8–13); com a terra adubada e o néctar, a parede de fundo foi para a 21 (36 linhas). Gerador: `litter()` em `Art/Terrain/Source~/terrain_atlas.py`.

### Terra adubada (`Compost`)

Fixa, escavável (0,25 s), amolece com a água como a grama. Família de junção e borda da grama (id 19 no shader). Cavada, vai como terra. Atlas: linha 19 (terra escura com torrões, palha e pontas de folha).

### Flores e néctar

- `PlantKind.Flower` (2): material `PlantLeaf`, arte das flores (`PlantSprites` carrega `Flower` à parte do trevo). `VegetationSimulation.TryGetPlant` decide pelo bioma da raiz (`SurfaceBiomes`, passado no construtor ou lido da grade no primeiro `Populate`): no Canteiro só flores, no gramado grama e trevo, na Serapilheira nada. No Canteiro, cada faixa tem **Flower Slot Width** (50, em **Vegetation Settings**) células em vez das 100 do gramado, com sorteios próprios (chave da faixa + `FlowerSlotKeys`). As flores continuam colhíveis como antes (dão nutriente); colhida, a flor para de dar néctar até crescer de novo.
- `NectarDrip` (**Nectar Settings** no `AntFarmGame`: **Seconds Per Drop** 30, **Max Per Flower** 2, **Reach** 6): a cada período, cada flor inteira (sem linha enterrada e com a linha de cima da forma no lugar) põe uma gota (`StoredItems.Nectar`) na primeira célula vazia sobre chão firme numa coluna sorteada até 6 da raiz, se houver menos de 2 em volta. Etapa Vegetação do painel F11; o relógio não vai para o save.
- A gota é item solto só quando há depósito de néctar com vaga (`IsLooseItem`); a vaga também limita quantas formigas saem para buscar (`LooseRoom`). Na mão, vai sozinha e segue `ReturnHeldItem` (antes `ReturnMushroom`): depósito do tipo, senão o chão. Nunca vai ao depósito de construção nem ao despejo (`IsBlockItem`).
- `CellMaterial.Nectar` (20) só existe como item e na mão; a linha 20 do atlas (mel âmbar com bolhas de luz) desenha o bloco carregado.

### Insumo da Serapilheira e depósito

- `StorageKind.Litter` (7) e `StoredItems.Litter`. `AntColony.StartDelivery`: serapilheira na mão vai ao depósito de serapilheira (`TryStartStock`); sem ele, ou cheio, segue o caminho da terra (`IsBlockItem` aceita, `ToDumped` → `Spoil`). Com o depósito pintado, `FitsLoad` não mistura serapilheira e terra na mesma carga.
- Item solto de serapilheira (depósito apagado) volta ao depósito dela ou vai como terra, pelo caminho dos itens soltos.
- Pincel `BrushMode.LitterStore` (índice 15), `PaintRoom.LitterStore` (limite de área dos depósitos), cor `_litterStoreColor` no `AntFarmView`. Livre desde o início, como os outros depósitos; não tem nó no catálogo de melhorias (um nó a mais desequilibrava o desenho da árvore, `UpgradeCatalogTests`).
- O insumo ainda **não tem uso**: só se acumula. O uso nas melhorias e nas salas (documento 44, "Em aberto") é a próxima etapa.

### Folhas que voltam (`LitterFall`)

- `AntFarmGame` → **Litter Settings**: **Seconds Per Cell** (4 s de jogo; 0 desliga) e **Layer Depth** (2). A cada período, sorteia até 16 colunas da Serapilheira dentro da área ativa e repõe a célula mais baixa que falta na cobertura (`TryRestoreAt`).
- Só repõe apoiada em terra, com tudo acima livre até o solo original e céu acima dele: não tapa poço nem túnel, não cobre monte de terra despejada, água, item guardado, nutriente, marca ou sala, e não cai em formiga (`AntColony.IsAntAt`). Roda na etapa Vegetação do painel F11.
- O relógio não vai para o save (ao carregar, recomeça do zero).

### Cenário

`SurfaceSceneryPainter.Paint` só põe **gravetos e folhas secas**, e só onde o elemento inteiro cabe na Serapilheira (`FindX` com o bioma; 160 tentativas). Quantidades por 100 colunas do bioma: **Litter Twigs** 1,2 e **Litter Dry Leaves** 2 (`SurfaceScenerySettings`). Pedras, cogumelos e grãos saíram da geração (voltam com a margem úmida); os PNGs e a arte procedural deles continuam. Plantas colhíveis só nascem com raiz no gramado (`VegetationSimulation.Populate`).

## Como estender (próximo bioma)

1. Valor novo em `SurfaceBiome` e faixa em `BiomeSettings`/`SurfaceBiomes.Generate` (`Strip`; os próximos ficam depois dos dois atuais, mais longe da entrada).
2. Cobertura do chão em `TerrainGenerator.GroundCover`; material novo no fim de `CellMaterial`, com linha no atlas (`terrain_atlas.py` → reduzir com `halve_pixel_art.py` → inserir na posição do enum; ver o script de inserção usado para a serapilheira no histórico do REQ-070) e família em `MergeFamily`/`ChipStyleOf` (C# e shader).
3. Elementos do cenário do bioma em `SurfaceSceneryPainter.Paint` com `PlaceIn(bioma, ...)`.
4. Insumo: `StorageKind` novo no fim (gravado no save), `KindSlots`/`StorageKinds`/`_storeFieldIdle` com uma posição a mais, rota em `StartDelivery`, pincel no fim de `BrushMode`/`BrushLabels`/`BrushTint`, `PaintRoom`, `HudTooltipTexts`, `AntFarmHudSetup` (rodar **Incrementant/Setup/Montar HUD da partida**) e textos em `LocalizationCatalog` (rodar **Configurar localização**).

## Pendências

- Uso dos insumos (melhorias, salas), adiado pelo desenvolvedor; camadas rasas próprias, fundo de parallax e transbordo de cada bioma; silhueta do Canteiro (vaso ou borda); entrada secundária (posto avançado). Ver o documento 44.
- Save de antes de 2026-10-23: as flores do gramado eram variantes do trevo; ao carregar, essas plantas podem ficar com a forma diferente da que está nas células (a forma sai da seed e do tipo). Só no branch experimental.
- Arte provisória: tile do atlas gerado por código; a aprovação visual é do desenvolvedor.
