# 40 — Direção de arte unificada e terreno iluminado

Documento do projeto de 2026-10-21. Regras e decisões D1 a D5 aprovadas pelo desenvolvedor em 2026-10-21 (ver "Decisões"); paleta mestra de 32 cores montada e aprovada em 2026-10-21 (etapa 0 concluída). Os números das etapas são pontos de partida.

[Índice da documentação](../README.md)

## Objetivo

1. **Um só padrão de desenho** para tudo que aparece no mundo: densidade de pixel, contorno, paleta, filtro e vista. Hoje cada sistema usa a sua regra (veja o diagnóstico).
2. **O visual de terreno da ilustração do menu principal**: terra com textura rica, borda iluminada onde a terra encontra o túnel, parede de fundo escura nas cavidades e luz quente de fontes pontuais. Só o tratamento (shader, texturas e luz). As formas orgânicas da ilustração ficam de fora: o terreno continua sendo a grade da simulação, cavada célula a célula.

O trabalho foi dividido em etapas que cabem numa sessão cada, separadas entre arte e programação, para avançarem em paralelo quando possível.

## Referências avaliadas

| Referência | O que aproveitar | O que não serve |
|---|---|---|
| Ilustração do menu principal | Textura colorida da terra, borda clara na beira das cavidades, fundo escuro com sombra nos cantos, luz quente com brilho em degraus | Formas orgânicas desenhadas à mão (o terreno do jogo é a grade da simulação) |
| Captura de salas em tiles (top-down, molduras arredondadas, itens em espaços fixos) | Clareza de leitura e tinta translúcida por sala | Autotiles de moldura e itens em espaços fixos: pedem formas "limpas" e grandes, e o terreno muda célula a célula, com salas pintadas a pincel. O terreno arredondado já foi recusado em 2026-10-19 |

## Diagnóstico: onde cada sistema usa uma regra própria

### Densidade (pixels de arte por célula da grade)

| Família | Hoje | Onde está a regra |
|---|---|---|
| Formigas adultas, rainha, cupim alado (108×72 e 144×144), pulgões (72×48) | 36 | `Editor/AntFarm/AntSpriteImporter` (`AdultArtScale` 6 × `PixelsPerCell` 6), `AphidSpriteImporter` |
| Ovo, larva e pupa (18×12) | 6 | `AntSpriteImporter.PixelsPerCell` |
| Textura do terreno | 16 | `CellTileArt.TexelsPerCell`, shader `Incrementant/TerrainTiles` |
| Terreno sem material de tiles | 1 (cor lisa) | `AntFarmView` |
| Bloco carregado pela formiga | 16 | `AntSwarmView.BlockSprite` |
| Cultivo | 16 | `CultivationView` (`Texels`) |
| Plantas e cenário vindos de PNG | 1 (cada pixel ampliado 6×6) | `SceneryStamp.ArtScale`, `SceneryArtImporter` |
| Cenário procedural (sem PNG) | 6 | `SurfaceSceneryPainter.PixelsPerCell` |
| Sobreposição do tutorial | 4 | `TutorialDirector` (`TexelsPerCell`) |
| Mapa de imagem | 1 | `AntFarmMapImporter` |

A formiga tem pixels 36 vezes menores que os da grama ao lado dela e 6 vezes menores que os do próprio ovo.

### Contorno

| Onde | Regra atual |
|---|---|
| Terreno e bloco carregado | Escurece 1 texel em 50% no lado sólido (`_tileEdgeDarkening`, `_carriedEdgeDarkening`) |
| Caminho do túnel na UI | 2 px na cor `#17130F` (`TunnelPathGraphic`) |
| Texto flutuante e tooltips | Componente `Outline` do uGUI, com borda deslocada e suave (`FloatingText`, `HudTooltips`) |
| Título do menu | Contorno marrom próprio (`MenuTitleOutline`) |
| Sprites de formiga e cenário | Desenhado em cada PNG, sem regra comum |

### Paleta

- Terreno: tabela de cores por material (`AntFarmGuide.md`), com variação de tom por célula.
- Cenário procedural: cores fixas no código (`SurfaceSceneryPainter`).
- PNGs gerados no REQ-026 (`Art/PixelArtGeneration.json`): paletas separadas por família (24 cores no cenário, 16 nas formigas e pedras, 8 nos grãos), sem paleta comum.
- Cores soltas nos inspectors: água, nutriente, ovo carregado, barras de postura.

### Filtro e escala da UI

- Tudo usa filtro `Point`, menos os marcadores de cupim (`TermiteMarkers`, `Bilinear`).
- A UI tem escala própria: HUD com `PixelScale` 2, botão do menu com multiplicador 0,6, sprites a 100 PPU e telas de referência de 640×360. Nenhuma tem relação com o pixel do mundo.

### Vista

As formigas têm quadros de lado (`Ant_Walk`, pivô no pé) e de cima (`Ant_Top`, pivô no centro). Terreno e cenário são de corte lateral. A mistura pode continuar, mas precisa virar regra escrita.

## Regras (aprovadas em 2026-10-21)

| Regra | Proposta |
|---|---|
| Densidade do mundo | **16 px de arte por célula** para tudo que está no mundo: terreno, formigas, crias, itens, cultivo e cenário próximo |
| Zoom | Em **passos inteiros** de px de tela por célula (16, 32, 48), para cada pixel de arte virar exatamente 1×1, 2×2 ou 3×3 pixels de tela |
| Filtro | `Point` em todo sprite e textura de arte, sem exceção |
| Contorno | 1 pixel desenhado, na cor mais escura da paleta mestra (não preto puro), em sprites e UI. Sai o `Outline` do uGUI. **O terreno não tem contorno nem borda** (decisão de 2026-10-21) |
| Paleta | Uma paleta mestra, tirada do cenário (D5), da qual saem o terreno, os PNGs, as cores dos inspectors e a UI. **32 cores** (2026-10-21), em `Assets/Project/Art/Palette`, mais 12 das formigas (D6) |
| Vista | Adultas de lado quando apoiadas no chão; de cima quando na parede de fundo. Crias e itens de lado, apoiados no chão (D2: os dois conjuntos ficam) |
| UI | ~~Escala em múltiplo inteiro do mesmo pixel~~ Descartada (D7, 2026-10-21): a UI mantém a própria escala, com mais pixels, para não se misturar com o cenário |
| Exceção | Nenhuma: o cenário também vai para 16 px por célula (D4). A exceção de fundo com pixel ×6 foi descartada |

### Por que 16 e não 36 (a densidade das formigas)

A câmera (`MapCamera`, cena `AntFarm`) começa em `_startPixelsPerCell` = 16 px de tela por célula e aproxima no máximo até `_minHalfHeight` = 24 (48 células de altura, ~22 px por célula em 1080p). A formiga tem 108×72 px de arte para 3×2 células, então aparece com 48×32 px no zoom inicial e ~67×45 px no máximo. A arte está sempre reduzida, e com filtro `Point` reduzir é descartar pixels: o detalhe se perde e a formiga "pisca" ao andar e girar.

Subir tudo para 36 multiplicaria por ~5 o volume de arte por área sem efeito na tela. Também estouraria o limite de textura no cenário: uma touceira de ~420 células teria ~15.000 px de altura, e a camada de superfície (640 células) teria ~23.000 px de largura, acima de 16384. Só faria sentido aproximando até ~36 px por célula, o que mostra só ~30 células de altura.

Com 16, o terreno, o bloco carregado e o cultivo já estão no padrão. Redesenham-se as formigas (48×32) e as crias.

### Escala aparente

A ilustração do menu foi pintada com ~3 px de tela por pixel de arte. Com 16 px de arte por célula no zoom inicial de 16, o jogo fica mais fino que o menu. **D1 (2026-10-21): fica como está**: câmera começando em 16 px por célula e arte com pixel de 1×1 dentro dos 16, sem "pixels grossos". O jogo aceita ser mais fino que a ilustração do menu.

**Decidido em 2026-10-21 (D7): o zoom pode aumentar até o passo de 32.** Consequência para a etapa 1: o zoom máximo atual (`_minHalfHeight` = 24, ~22 px por célula em 1080p) não alcança o passo de 32. A etapa 1 precisa escolher entre subir o limite até 32 ou ficar só com 16 como passo inteiro.

## Visual do terreno: as camadas

A ilustração se decompõe em cinco camadas. Todas cabem no shader atual (`Assets/Project/Materials/TerrainTiles.shader`), sem trocar de renderer.

| Camada | Hoje | Mudança |
|---|---|---|
| 1. Textura colorida | Atlas **cinza** (canal R), multiplicando a cor da célula; repete a cada 4 células (64 texels) | Atlas **RGB** pintado; o tom da célula vira um ajuste leve. Repetição de 8×8 células (128 texels) ou 2–3 variantes sorteadas por bloco, para não mostrar o padrão |
| 2. Borda iluminada | 1 texel escurecido no lado sólido | **Recusada em 2026-10-21**: a faixa clara com faixa escura atrás foi testada e o desenvolvedor pediu para tirar a borda. A terra fica sem contorno; o contorno de 1 texel também saiu |
| 3. Parede de fundo | Vazio em cor lisa | Material "fundo" no atlas: terra mais escura, com textura própria, que escurece perto das paredes (sombra de canto) |
| 4. Luz quente | Névoa por distância de xadrez, queda linear até o preto (`Reveal Radius` 5); substituída em 2026-10-21 | Mapa de luz por célula, borrado e quantizado em 4–6 degraus por texel. Substitui a névoa (D3). Decidido em 2026-10-21: a luz só entra de fora, pelas bocas; o interior fica no breu e a superfície totalmente visível |
| 5. Pós-processamento | Nenhum | Bloom leve, vinheta e ajuste de cor quente pelo Volume do URP (`Assets/Settings/DefaultVolumeProfile.asset`) |

### Base técnica comum: distância com sinal

As camadas 2, 3 e 4 dependem de saber, em cada célula, a distância até a fronteira entre terra e vazio: positiva dentro da terra, negativa dentro do túnel. A névoa já calcula a distância até o vazio (`_cellFog`). Ela pode ser generalizada e gravada num canal da textura de materiais que o shader já lê (`_CellMaterials`).

### Restrições

- **Terreno dinâmico.** Distância e luz mudam quando a terra abre, fecha ou desaba. Elas precisam seguir o cache por bloco do render (`PixelGrid.GetChunkRenderVersion` e o registro de células, descritos em `AntFarmGuide.md`, "Render por bloco"), como a névoa já faz. Senão a borda antiga continua desenhada. O teste `TerrainRenderCacheTests` precisa cobrir os canais novos.
- **Renderer.** O projeto usa o renderer URP 3D (`PC_Renderer`, `Mobile_Renderer`), sem `Light2D`. A luz é um mapa próprio, não as luzes 2D do URP.
- **Zoom afastado.** O shader já apaga a textura quando o texel fica menor que um pixel. A borda e a luz devem continuar visíveis nessa hora (em resolução de célula), para o mapa afastado não ficar chapado.
- **Pixel art.** Luz, sombra e borda são calculadas na posição do texel (arredondada) e quantizadas, para não haver gradiente mais fino que o pixel de arte.
- **Desempenho.** O custo do shader é por pixel de tela e não muda com a densidade da arte. O cuidado é no lado da CPU: refazer distância e luz só nos blocos sujos. Medir na etapa Render do painel F11 (`SimProfiler`).
- **Bloco carregado.** Resolvido na etapa 4: `AntSwarmView.BlockSprite` lê o mesmo atlas do terreno (`TerrainTileAtlas`).

## Plano em etapas

Cada etapa cabe numa sessão de trabalho e termina com algo verificável. As trilhas A (programação do terreno), B (arte) e C (padronização) podem andar em paralelo depois da etapa 0.

| Etapa | Trilha | Depende de | Entrega |
|---|---|---|---|
| 0 | Design | — | Regras aprovadas e paleta mestra |
| 1 | C | 0 | Zoom em passos inteiros e limpeza de filtro e contorno da UI |
| 2 | A | — | Distância com sinal e visualização de depuração |
| 3 | A | 2 | Borda iluminada e parede de fundo no shader |
| 4 | A | 0 | Atlas RGB: formato, repetição maior e bloco carregado no mesmo atlas |
| 5 | B | 0, 4 | Atlas desenhado dos materiais e do fundo |
| 6 | A | 2 | Mapa de luz quantizado |
| 7 | A | 3, 6 | Pós-processamento |
| 8 | B | 0 | Formigas em 48×32, uma classe por sessão |
| 9 | B | 0 | Crias e itens em 16 px |
| 10 | B/C | 0 | Cenário: redesenho em 16 ou exceção de fundo |
| 11 | C | 0, 1 | UI no mesmo pixel |
| 12 | B | 3 | Decorações de borda (opcional) |

### Etapa 0 — Regras e paleta mestra (design e arte)

- Regras e D1 a D5 aprovadas em 2026-10-21.
- Montar a paleta mestra a partir do cenário (D5): terras (claro, médio, escuro, fundo), pedra, areia, rocha-mãe, verde, água, luz quente, contorno. Exportar como PNG de amostras e lista hexadecimal.
- **Pronto quando:** este documento tiver as regras marcadas como aprovadas e a paleta salva no projeto.
- **Feita em 2026-10-21:** o desenvolvedor escolheu 32 cores. Paleta em `Assets/Project/Art/Palette` (`MasterPalette.png` 32×1 e `MasterPalette.hex`, com o [README](../../Assets/Project/Art/Palette/README.md) dos grupos, índices e método) e prancha em [41_Paleta_mestra.png](41_Paleta_mestra.png). São 29 cores reais do cenário (k-means em OKLab por grupo, com as famílias pesando igual), mais dois laranjas das folhas secas e três azuis de água, que o cenário não tem. O cenário remapeado para a paleta ficou quase igual ao original (prévia conferida pelo agente). Aprovada pelo desenvolvedor em 2026-10-21 ("A paleta de 32 cores está aprovada").

### Etapa 1 — Zoom inteiro e limpeza (programação, pequena)

- `MapCamera`: zoom em passos inteiros de px de tela por célula, com a transição suave existente terminando num valor inteiro. Tamanho de tela com resolução ímpar não pode deslocar meio pixel.
- `TermiteMarkers`: filtro `Point`.
- Trocar o `Outline` do uGUI por contorno de 1 pixel na cor da paleta onde ele aparece.
- **Pronto quando:** formigas andando e girando não piscarem em 16 e 32 px por célula, comparando capturas antes e depois.
- **Implementada e aprovada em 2026-10-21 (zoom; "O zoom em passos inteiros está aprovado"):** `MapCamera` com zoom em níveis de 16 e 32 px por célula (**Pixel Step** 16, **Max Pixels Per Cell** 32, pela D7), transição curta terminando no valor exato, zoom contínuo abaixo de 16 e centro alinhado à grade de pixels (meio pixel em resolução ímpar). `MapCameraZoomTests` (2 casos) e a suíte EditMode passando; conferido pelo agente em Play Mode (16 → 32 → 32 → 16 → contínuo → 16, alinhado) e em captura a 32. Fora desta etapa, pela D7: filtro dos marcadores de cupim e contorno do uGUI (são UI). Não conferido: se as formigas giradas ainda piscam (o giro livre do sprite não depende do zoom).

### Etapa 2 — Distância com sinal (programação)

- Generalizar o cálculo da névoa para uma distância com sinal por célula (limitada a poucas células, por exemplo ±4) e gravá-la num canal da textura de materiais.
- Atualização pelo cache de blocos e pelo registro de células. Estender `TerrainRenderCacheTests` para comparar o canal novo com um cálculo do zero.
- Uma visualização de depuração (cores por distância), ligada no inspector do `AntFarmView`.
- **Pronto quando:** o teste passar e a visualização acompanhar escavação, desabamento e chuva sem resto antigo.
- **Implementada em 2026-10-21:** canal G da textura `_CellMaterials` (agora RG16, 128 + distância, alcance ±4, distância de xadrez), **Debug Signed Distance** no `AntFarmView` e dois casos novos em `TerrainRenderCacheTests` (cache × desenho do zero e varredura direta da grade, com e sem plantas desenhadas à parte). Detalhes no `AntFarmGuide.md` ("Tiles 16x16"). Falta uma pessoa conferir a visualização jogando (escavação, desabamento e chuva).

### Etapa 3 — Borda iluminada e parede de fundo (programação)

- Shader: faixa clara de 2–3 texels e faixa escura atrás, no lado sólido; no lado vazio, material de fundo escurecendo por distância (sombra de canto). Tudo quantizado por texel.
- Parâmetros no material (largura das faixas, cores da paleta, força da sombra), com valores provisórios até a etapa 5.
- A névoa existente continua funcionando por cima.
- **Pronto quando:** capturas em 16 e 32 px por célula mostrarem túneis e salas com borda e fundo legíveis, e o painel F11 não mostrar alta na etapa Render.
- **Implementada em 2026-10-21** no `TerrainTiles.shader`: parede de fundo com a textura da terra e sombra de canto em 3 degraus até 6 texels, com valores provisórios no material. A borda clara (3 texels) com faixa escura (2) também foi feita, mas o desenvolvedor não gostou e pediu para tirar: a terra ficou sem borda nem contorno. O agente conferiu capturas em 16, 32 e ~4 px por célula; a etapa Render do F11 ficou em ~1,1 ms de média com o jogo parado. Parede de fundo com sombra de canto aprovada pelo desenvolvedor em 2026-10-21. Falta medir com chuva forte.

### Etapa 4 — Atlas RGB (programação)

- O shader lê a cor do atlas (RGB) e usa o tom da célula como ajuste leve. O layout passa a ter um material de fundo e período de 8×8 células (128 texels), ou variantes por bloco.
- Novo atlas procedural de transição (`CellTileArt`) no formato novo, para o jogo funcionar até a arte chegar.
- `AntSwarmView.BlockSprite` lê o mesmo atlas atribuído ao terreno.
- Documentar o formato do atlas no `AntFarmGuide.md` (tamanho por material, ordem de `CellMaterial`, o que cada canal significa).
- **Pronto quando:** um atlas de teste com cores fortes aparecer igual no terreno e no bloco carregado.
- **Implementada em 2026-10-21** (`CellTileArt`, `TerrainTileAtlas`, `TerrainTiles.shader`, `AntSwarmView.BlockSprite`): 128 × 128 texels por linha (8 × 8 células), uma linha por `CellMaterial` e a parede de fundo por último (128 × 2432 px). O texel é a cor final do material; o shader divide a cor da célula pela cor de referência da linha e aplica o resultado sobre a arte, então tom, umidade, marcas e água continuam por cima. Com o zoom aberto sobra a média da linha. **Tile Tone Strength** (0,5) reduz a variação de tom por célula. O atlas gerado repete o padrão de antes sobre a cor de referência, então o terreno fica como era até a etapa 5. Atlas fora do formato gera erro e cai no gerado. Formato documentado no `AntFarmGuide.md` ("Atlas RGB"). Verificado pelo agente: `TerrainTileAtlasTests` (5) e `TerrainRenderCacheTests` passando, 405 testes EditMode passando antes do teste novo; em Play Mode, um atlas de listras de cores fortes trocado durante o jogo apareceu no terreno e na parede de fundo, e o bloco carregado bateu texel a texel com ele (0 de 196 diferentes). Variantes sorteadas por bloco ficaram de fora: a repetição de 8 células pode ser conferida com a arte da etapa 5.

### Etapa 5 — Atlas desenhado (arte)

- Pintar, na paleta mestra, um tile repetível por material (terra, monte, areia, pedra, rocha-mãe, compactado) e o fundo, no formato da etapa 4.
- Conferir no jogo, nos dois zooms, se a repetição aparece.
- **Pronto quando:** o atlas estiver atribuído no **Tile Atlas** e aprovado em captura de tela.
- **Feita em 2026-10-21:** `Assets/Project/Art/Terrain/TerrainAtlas.png` (128 × 2432, as 19 linhas), desenhado só com a paleta mestra (o gerador confere) por `Source~/terrain_atlas.py`, que fica no projeto para refazer ou ajustar ([README](../../Assets/Project/Art/Terrain/README.md)). Prévia em [42_Atlas_do_terreno.png](42_Atlas_do_terreno.png). Terra em manchas com pedrinhas, areia pontilhada, pedra e rocha-mãe em blocos, monte em torrões soltos, compactados em camadas, plantas em fibras e parede de fundo calma no tom do túnel; luz de cima à esquerda. Atribuído em **Tile Atlas** nas cenas AntFarm e MainMenu, e o setup da cena passa a atribuí-lo quando o campo estiver vazio. Conferido pelo agente em Play Mode no zoom inicial e no máximo (~22 px por célula; 32 depende da etapa 1): sem emenda visível, a terra despejada (média `9E6533`) se distingue da terra (`7E512F`), sem erros no console. A primeira versão do monte parecia calçamento e foi refeita em torrões. Aprovado pelo desenvolvedor em 2026-10-21 ("O atlas do terreno está aprovado").

### Etapa 6 — Mapa de luz (programação)

- Fontes de luz por célula: sala da rainha, cultivo, entrada e superfície; depois, decorações luminosas.
- Mapa em resolução de célula ou de bloco, borrado, com luz ambiente baixa, enviado ao shader e quantizado em 4–6 degraus por texel. **Substitui a névoa** (D3, 2026-10-21): a névoa sai (código, opção **Fog Of War** e o que mais depender dela) quando a luz entrar.
- Refeito só nos blocos sujos ou quando uma fonte muda.
- **Pronto quando:** a luz acompanhar a abertura de salas e a etapa Render do F11 continuar dentro do orçamento atual.
- **Penumbra em vez de breu (2026-10-21, pedido do desenvolvedor: "não seja preto absoluto dentro do formigueiro. Deixe apenas uma penumbra forte"):** o túnel longe da luz fica no piso 0,22 (`_InteriorAmbient`). A penumbra é só dentro do formigueiro: a terra abaixo da superfície que não é caminho das formigas continua preta (correção do desenvolvedor no mesmo dia; um piso de 0,12 na terra foi recusado). O texto abaixo, que fala em breu, descreve a versão anterior.
- **Implementada em 2026-10-21** (`AntFarmView.Light.cs`, `TerrainTiles.shader`) e ajustada no mesmo dia pelo desenvolvedor: **a luz só entra de fora**. Tudo acima do solo original tem luz cheia e fica totalmente visível; a luz cai reta pelas bocas, sem perder força e sem limite de profundidade, até bater num bloco (o feixe), e de lá se espalha perdendo 1 nível (de 16) por célula de túnel e 4 por célula de terra; o interior longe dos feixes cai num breu (túnel com piso de 0,08, terra preta). O feixe foi pedido pelo desenvolvedor no segundo ajuste, com desenho. Salas não emitem luz (a primeira versão tinha túnel, sala da rainha e sala de nutrientes como fontes; recusado). Marcações e pincel ficam acima da luz. 5 degraus no shader, interpolados por texel. A névoa saiu (código e opção **Fog Of War**). `TerrainRenderCacheTests` confere a luz contra uma propagação na grade inteira, o feixe num poço de 40 células (que apaga ao ser tapado longe da janela) e o pincel acima da luz. Redesenhar o mundo inteiro: ~119 ms (85,4 ms com a névoa); etapa Render do F11 parado ~0,06 ms. Aprovada pelo desenvolvedor em 2026-10-21 ("a luz está aprovada assim"). Falta medir com chuva forte e cavando. Fontes internas (cogumelos luminosos, salas) ficam para quando o desenvolvedor pedir (etapa 12).

### Etapa 7 — Pós-processamento (programação, pequena)

- Volume do URP: bloom leve só nos tons claros, vinheta e ajuste de cor quente. Opção nas configurações para desligar o bloom.
- **Pronto quando:** capturas antes e depois forem aprovadas e a opção funcionar.
- **Implementada em 2026-10-21** (`PostProcessSetup`, menu **Incrementant/Setup/Configurar pós-processamento**, também chamado pelo setup das cenas AntFarm e MainMenu): perfil `Assets/Project/Settings/PostProcessProfile.asset` num Volume global `PostProcess`, com a câmera em pós-processamento. Valores iniciais: bloom com limiar 0,75 (linear), intensidade 0,6, espalhamento 0,55, meia resolução, sem filtro de alta qualidade; vinheta 0,25 com suavidade 0,45; temperatura +10 (`WhiteBalance`, para esquentar sem escurecer a paleta); sem tonemapping. Com limiar 0,85 o bloom quase não aparecia (0,4% dos pixels mudavam na vitrine do menu); com 0,75 ele brilha nos caules, pedras e destaques claros da superfície e não toca a terra nem o breu (halo de no máximo 1/255 abaixo da superfície). Opção **BRILHO (BLOOM)** nas configurações (`GameSettingsData.Bloom`, aplicada por `PostProcessSettingsBinder` na cópia do perfil). O agente conferiu capturas com e sem o Volume no menu e na partida e a opção ligando e desligando o bloom em Play Mode. Falta a aprovação do desenvolvedor e medir o custo de GPU (a etapa Render do F11 não inclui o pós-processamento do URP).
- **Bloom só nos cogumelos (2026-10-21):** o desenvolvedor não viu diferença no bloom dos tons claros e pediu bloom nos cogumelos luminosos, "por hora só nos cogumelos". O shader do terreno multiplica a cor do cogumelo por **Glow Mushroom Emission** (2,5×, HDR) e o bloom passou a limiar 1,1 (acima do branco), intensidade 1,5, espalhamento 0,4 e 3 iterações: só o que emite em HDR brilha, a superfície clara não. Com intensidade 1 o halo mal aparecia; com 6 iterações e espalhamento de 0,55 ou mais, virava uma névoa grande em volta da galeria. No mesmo dia a luz dos cogumelos trocou o azul pelo amarelado sutil (pedido do desenvolvedor): cubo amarelo claro e tom próprio da luz que sai dele (**Glow Light Tint**), sem a penumbra azulada. Logo depois, a pedido dele, a luz dos cogumelos deixou de ser em degraus: gradiente contínuo, com a borda também interpolada (a luz de fora segue em degraus). Em seguida a luz do cogumelo passou a entrar mais nas paredes (2 níveis por célula de terra) e a terminar numa curva que chega a zero antes da última célula, sem corte nem linha na borda. O desenvolvedor aprovou esse desfoque, pediu o alcance anterior de volta (4 por célula de terra) e o mesmo gradiente suave para toda a luz: a luz de fora também deixou os 5 degraus e usa a mesma curva (o "quantizado em degraus" acima ficou para trás). Depois, a pedido dele, a luz do cogumelo passou a multiplicar a cor do próprio cogumelo no terreno em volta (um campo de cor só, **Glow Mushroom Color**, com a força em **Glow Color Strength**) e o bloom dos cogumelos subiu de intensidade 1,5 para 3. Conferido pela captura da Game view, com e sem o bloom.

### Etapa 8 — Formigas em 16 px (arte, uma classe por sessão)

- Redesenhar em 48×32 (3×2 células) os quadros `Ant_Walk_0–3` e `Ant_Top_0–3` de cada pasta em `Art/Ants`, mais rainha, cupim alado e pulgões, na paleta mestra e com o contorno da regra.
- Ajustar `AntSpriteImporter` e `AphidSpriteImporter` para 16 PPU e conferir os pivôs.
- **Pronto quando:** cada classe for conferida em jogo nos dois zooms, ao lado do terreno novo.
- **Operárias e variantes de função feitas em 2026-10-21:** os 8 quadros (Walk e Top 0–3) em 48 × 32, redesenhados por `Art/Ants/Source~/ant_sprites.py` com as proporções do desenho aprovado (gáster grande, cintura, tórax, cabeça com olho, mandíbula e antena com cotovelo), patas em tripé de 4 quadros, luz de cima à esquerda e contorno de 1 px no índice 0. A raiz e `Operaria/` usam a rampa vermelha; Soldado, Coletora, Escavadora, Cuidadora, Jardineira e Cortadeira usam a rampa da sua cor (64 PNGs, mesmos caminhos e GUIDs). Como a paleta não tinha as cores das formigas, ela ganhou 12 cores (D6). `AntSpriteImporter` passou a 16 PPU para os quadros novos; os de tamanho antigo (108 × 72 e 144 × 144) seguem em 36 até serem redesenhados. Conferido pelo agente na importação (48 × 32, 16 PPU, pivôs (24, 8) e (24, 16)) e em Play Mode no zoom inicial. **Faltam:** rainha (hoje desenhada com os quadros da operária ampliados 2×, então com pixel do dobro do tamanho), cupim alado e pulgões. Operárias e variantes aprovadas pelo desenvolvedor em 2026-10-21 ("As formigas novas estão aprovadas").
- **Rainha feita em 2026-10-21:** quadros próprios de 96 × 64 em `Art/Ants/Rainha` (mesmo gerador, escala 2: gáster maior e listrado, tórax maior, patas de 2 px, marcas das asas), na rampa vermelha. Importados a 32 px por unidade e desenhados com Queen Scale 2, então o pixel é o mesmo das operárias; ligados em `AntSwarmView` (campos novos **Queen Walk Frames** / **Queen Top Frames**) nas cenas AntFarm e MainMenu e pelo setup da cena. Conferido pelo agente na importação (96 × 64, 32 PPU, pivôs (48, 16) e (48, 32)) e em Play Mode. Aprovada pelo desenvolvedor em 2026-10-21 ("A rainha está aprovada").
- **Cupim alado feito em 2026-10-21:** os 16 quadros de `Art/Ants/InimigaAlada` em 64 × 64 (as mesmas 4 × 4 células) a 16 px por célula, pelo mesmo gerador: corpo da operária em carvão-violeta, patas vinho, olho âmbar, asas claras batendo no voo e se soltando no pouso. Mesmos GUIDs. Conferido pelo agente na importação (16 PPU, pivôs (32, 16) e (32, 32)) e em Play Mode num ataque forçado (cupins andando de lado e de cima, no tamanho das operárias; o voo não chegou a ser capturado). Aprovado pelo desenvolvedor em 2026-10-21 ("O cupim alado está aprovado").
- **Pulgões feitos em 2026-10-21:** os 8 quadros de `Art/Aphids` em 40 × 24 (2,5 × 1,5 células) a 16 px por célula, pelo mesmo gerador, nos verdes da paleta; `AphidSpriteImporter` passou a 16 PPU. Conferidos pelo agente na importação (16 PPU, pivôs (20, 6) e (20, 12)) e em prévia ao lado da operária; não há conferência em jogo porque os pulgões ainda não são usados por nenhum código nem cena. Aprovados pelo desenvolvedor em 2026-10-21 ("Os pulgões estão aprovados"). **Etapa 8 concluída:** todas as classes desenhadas a 16 px por célula e aprovadas.

- **Refinamento de aparência em 2026-10-21 (REQ-049, ART-014):** após aprovação da amostra Ant_Top_0, os 64 quadros das operárias/funções e os 8 da Rainha receberam carapaça mais segmentada e sombreada, mantendo as rampas, resoluções, caminhos, GUIDs, PPU e pivôs. Masters ImageGen e reexportação atual em `Art/Ants/Source~/Refined` e `bake-refined.ps1`; o gerador procedural anterior é histórico e sobrescreve este refinamento. Paleta, alpha binário e prancha conferidos; 72 sprites reimportados na Unity sem divergências e console sem erros. Reprodução em Play Mode e aprovação visual do conjunto pendentes. [Prancha](Formigas_refinadas_2026-10-21.png).

- **Refinamento dos cupins em 2026-10-21 (REQ-050, ART-015):** os 16 quadros de `Art/Ants/InimigaAlada` receberam o tratamento das formigas aprovadas, mantendo 64×64, 16 PPU, pivôs, GUIDs e sete cores da paleta (carvão/violeta, vinho, âmbar e asas claras). Voo, pouso com perda das asas e caminhada lateral/superior preservados; o último pouso coincide com Walk_0. Masters ImageGen em `Art/Ants/Source~/TermiteRefined`, reexportados por `bake-termite-refined.ps1`. Paleta, alpha, prancha e importação dos 16 sprites conferidos; reprodução em Play Mode e aprovação visual do conjunto pendentes. [Prancha](Cupins_refinados_2026-10-21.png).

- **Refinamento dos pulgões em 2026-10-21 (REQ-052, ART-017):** oito quadros Walk/Top 0–3 em 40×24 refinados com ImageGen, mantendo paleta verde, corpo em pera, cornículos, antenas e seis patas. Mesmos caminhos, GUIDs, 16 PPU e pivôs. Masters e reexportação em `Art/Aphids/Source~`; o grupo procedural `aphids` sobrescreve esta aparência. Paleta, alpha, prancha e importação dos oito sprites na Unity conferidos; aprovação visual e reprodução das animações pendentes. O pacote ainda aguarda integração no gameplay. [Prancha](Pulgoes_refinados_2026-10-21.png).

### Etapa 9 — Crias e itens (arte)

- **Refinamento das crias em 2026-10-21 (REQ-051, ART-016):** seis quadros Egg/Larva/Pupa Idle 0–1 em 48×32 refinados com ImageGen, mantendo as paletas por estágio, escala, pivô e GUIDs. Ovo com volume, larva em gomos e pupa com dobras do casulo; contorno de um pixel na cor 0, alpha binário e luz superior esquerda. Masters em `Art/Ants/Source~/BroodRefined`, reexportação por `bake-brood-refined.ps1`; o gerador procedural `brood` anterior sobrescreve este refinamento. Prancha antes/depois e importação dos seis sprites na Unity conferidas; reprodução em Play Mode e aprovação visual do conjunto pendentes. [Prancha](Crias_refinadas_2026-10-21.png).

- Ovo, larva e pupa em 16 px por célula. Itens carregados (nutriente, água, ovo) como sprites da paleta, em vez de cores do inspector.
- **Pronto quando:** crias e cargas forem conferidas em jogo.
- **Feita em 2026-10-21:** ovo, larva e pupa em 48 × 32 (mesmas 3 × 2 células) a 16 px por célula, e sprites de 16 × 16 para o nutriente e a água carregados (`Item_Nutrient`, `Item_Water`), na paleta, pelo gerador `Art/Ants/Source~/ant_sprites.py` (grupo `brood`). `AntSpriteImporter` passa as crias redesenhadas a 16 PPU e os itens com pivô no centro; `AntSwarmView` ganhou **Nutrient Sprite** e **Water Sprite**, ligados nas cenas AntFarm e MainMenu e pelo setup. Conferido pelo agente: importação (48 × 32 e 16 × 16 a 16 PPU, pivôs (24, 8) e (8, 8)), prévia ao lado da operária e suíte EditMode 410/410. **Não conferido em jogo:** nem a partida nova nem a vitrine do menu têm crias ou cargas nos primeiros segundos; o desenvolvedor conferiu em jogo e aprovou em 2026-10-21 ("Conferi as crias e os itens em jogo e estão aprovados"). **Etapa 9 concluída.** As cores de nutriente e água pintadas no terreno (`AntFarmView`, `CultivationView`) continuam no Inspector.

### Etapa 10 — Cenário (arte e programação)

- Redesenhar plantas e elementos de `Art/Scenery` em 16 px por célula (D4, 2026-10-21: sem exceção de fundo), dividindo texturas grandes quando passarem do limite.
- Alinhar as cores do cenário procedural à paleta mestra.
- **Pronto quando:** a superfície for aprovada em captura junto do terreno.
- **Em andamento (2026-10-21):** o desenvolvedor escolheu "detalhe por célula" em vez de redesenhar a arte 16 vezes maior (que estouraria o limite de textura das camadas do mundo e mudaria como as plantas viram células) ou de encolher o cenário. Feito: os 69 PNGs de `Art/Scenery` remapeados para a paleta mestra (`Source~/palette_remap.py`, `.meta` intactos); shader `Incrementant/SceneryCells` (`Materials/SceneryCells.mat`, campo **Cell Material** do `SurfaceSceneryView`, ligado nas cenas e pelo setup) nas camadas de perto e da frente e nas plantas, desenhando cada célula a 16 texels com contorno de 1 texel na silhueta, realce e sombra na borda e granulado em manchas de 2x2; faixas de grama distante pintadas a 16 px por célula (`SurfaceSceneryPainter.BandPixelsPerCell`). Um pontilhado ordenado entre células ficou como opção desligada (`_BlendWidth`), porque virou linhas de pontos nas bordas de contraste. Suíte EditMode 410/410. Avaliação do agente: as células ainda leem como blocos grandes (o pixel da arte continua valendo 1 célula).
- **Elementos de cenário adiados (2026-10-21):** o desenvolvedor pediu para pular cogumelos, pedras, gravetos, folhas e plantas: "a estratégia delas vai ser diferente já que tem que ser grandes em relação às formigas". Ficam como estão (remapeados para a paleta, com o shader de detalhe por célula) até a nova estratégia ser definida.

- **Pebbles retomados em 2026-10-21 (REQ-053, ART-018):** o desenvolvedor pediu começar o refinamento dos props pelas pedras, no estilo das formigas e maiores que elas. As 14 variantes ganham 16 vezes a largura/altura em pixels e passam a 16 PPU, conservando as dimensões físicas de 8×5 a 76×62 células. Sprite detalhado separado da máscara de navegação, com renderização por prop; a camada inteira não é ampliada. As demais famílias seguem adiadas. [Arte e reexportação](../../Assets/Project/Art/Scenery/Pebbles/README.md). Aprovação visual e conferência em Play Mode pendentes.

- **Demais famílias retomadas em 2026-10-21 (REQ-054, ART-019, PRG-062):** pedido de aplicar o tratamento dos Pebbles às outras 55 imagens de Scenery. O desenvolvedor aprovou limitar o maior eixo dos elementos gigantes a 128 células: 24 imagens mudam proporcionalmente de tamanho físico e 31 preservam a escala, todas a 16 px/célula. Isso leva a estimativa de RGBA de cerca de 800 para 207 MiB por cópia de textura. Plantas compartilham a arte detalhada e recebem máscara de um texel por célula para crescimento, colheita e marcação, sem cópia da textura grande por instância. Fontes e prompts em `Art/Scenery/Source~/RefinedScenery`. Refinamento em andamento; aprovação visual do conjunto e Play Mode pendentes.

### Etapa 11 — UI no mesmo pixel (programação e arte)

- Escala da HUD e do menu em múltiplo inteiro do pixel do mundo; molduras e ícones na paleta mestra.
- **Pronto quando:** o HUD for conferido em 1080p e 1440p sem pixels de tamanhos diferentes.
- **Descartada em 2026-10-21 (D7):** a UI fica com a escala própria, com mais pixels, para não se misturar com os elementos de cenário.

### Etapa 12 — Decorações de borda (opcional)

- Raízes no teto, musgo no chão e cogumelos luminosos colocados nas bordas das cavidades, pela distância com sinal (como objetos; a terra em si continua sem borda). Os cogumelos viram fontes de luz da etapa 6.
- **Cogumelo luminoso adiantado em 2026-10-21, a pedido do desenvolvedor:** em vez de decoração automática, é um bloco de uma célula (cubo provisório, `CellMaterial.GlowMushroom`) que o jogador marca no túnel com o pincel **Cogumelo luminoso** e as formigas encaixam pela obra, com terra como substrato. É fonte de luz da etapa 6 com a mesma regra de espalhamento; o nível começou em 10 e, a pedido do desenvolvedor ("dobre a intensidade"), foi ao teto da regra, 16 (alcance de 15 células de túnel, dentro da folga do bloco). Dobrar de fato (20) passaria do limite de níveis e da folga do bloco. Detalhes no `AntFarmGuide.md` ("Cogumelo luminoso"). Falta a arte do cogumelo.

## Decisões

Tomadas pelo desenvolvedor em 2026-10-21. Substituem a decisão de 2026-10-17 de a formiga ter pixels menores que os do terreno.

| ID | Decisão | Escolha |
|---|---|---|
| D1 | Escala aparente | Fica como está: câmera começando em 16 px por célula, sem pixels grossos |
| D2 | Vista das formigas | Mantém os dois conjuntos (lado e cima), com a regra da tabela de regras |
| D3 | Névoa e luz | A luz substitui a névoa |
| D4 | Cenário | Tudo na mesma resolução: cenário redesenhado em 16 px por célula, sem exceção de fundo |
| D5 | Paleta | "O cenário é a definição dos pixels": a paleta mestra sai do cenário e vale para tudo. 32 cores (2026-10-21) |
| D7 | UI e zoom | A UI mantém a escala própria, com mais pixels que o mundo, "para ela não se misturar com os elementos de cenários" (a etapa 11 sai do plano); o zoom máximo pode subir até 32 px por célula ("O zoom também pode aumentar"), o que fecha a dúvida da etapa 1 |
| D6 | Cores das formigas | Ampliar a paleta (2026-10-21): rampas de 3 tons para vermelho, roxo, rosa e verde-azulado (índices 32–43), que as 32 não tinham |

## Como validar

- Capturas fixas para comparar antes e depois: mesma seed, mesma posição de câmera, zoom de 16 e 32 px por célula, uma sala, um túnel, a superfície e uma área de areia.
- Desempenho pela etapa Render do painel F11 (`SimProfiler`), parado e com chuva forte.
- `TerrainRenderCacheTests` para tudo que o shader passar a ler da grade.
- Movimento: formigas andando e girando no zoom inicial, sem pixels piscando.
