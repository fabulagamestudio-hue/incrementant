# 40 — Direção de arte unificada e terreno iluminado

Documento do projeto de 2026-10-21. Regras e decisões D1 a D5 aprovadas pelo desenvolvedor em 2026-10-21 (ver "Decisões"); falta montar a paleta mestra para fechar a etapa 0. Os números das etapas são pontos de partida.

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
| Paleta | Uma paleta mestra, tirada do cenário (D5), da qual saem o terreno, os PNGs, as cores dos inspectors e a UI. Tamanho a definir na montagem |
| Vista | Adultas de lado quando apoiadas no chão; de cima quando na parede de fundo. Crias e itens de lado, apoiados no chão (D2: os dois conjuntos ficam) |
| UI | Escala em múltiplo inteiro do mesmo pixel |
| Exceção | Nenhuma: o cenário também vai para 16 px por célula (D4). A exceção de fundo com pixel ×6 foi descartada |

### Por que 16 e não 36 (a densidade das formigas)

A câmera (`MapCamera`, cena `AntFarm`) começa em `_startPixelsPerCell` = 16 px de tela por célula e aproxima no máximo até `_minHalfHeight` = 24 (48 células de altura, ~22 px por célula em 1080p). A formiga tem 108×72 px de arte para 3×2 células, então aparece com 48×32 px no zoom inicial e ~67×45 px no máximo. A arte está sempre reduzida, e com filtro `Point` reduzir é descartar pixels: o detalhe se perde e a formiga "pisca" ao andar e girar.

Subir tudo para 36 multiplicaria por ~5 o volume de arte por área sem efeito na tela. Também estouraria o limite de textura no cenário: uma touceira de ~420 células teria ~15.000 px de altura, e a camada de superfície (640 células) teria ~23.000 px de largura, acima de 16384. Só faria sentido aproximando até ~36 px por célula, o que mostra só ~30 células de altura.

Com 16, o terreno, o bloco carregado e o cultivo já estão no padrão. Redesenham-se as formigas (48×32) e as crias.

### Escala aparente

A ilustração do menu foi pintada com ~3 px de tela por pixel de arte. Com 16 px de arte por célula no zoom inicial de 16, o jogo fica mais fino que o menu. **D1 (2026-10-21): fica como está**: câmera começando em 16 px por célula e arte com pixel de 1×1 dentro dos 16, sem "pixels grossos". O jogo aceita ser mais fino que a ilustração do menu.

Consequência para a etapa 1: o zoom máximo atual (`_minHalfHeight` = 24, ~22 px por célula em 1080p) não alcança o passo de 32. A etapa 1 precisa escolher entre subir o limite até 32 ou ficar só com 16 como passo inteiro.

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
- **Bloco carregado.** `AntSwarmView.BlockSprite` usa sempre o atlas gerado por código, mesmo com **Tile Atlas** atribuído. Precisa ler o mesmo atlas RGB do terreno.

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

### Etapa 1 — Zoom inteiro e limpeza (programação, pequena)

- `MapCamera`: zoom em passos inteiros de px de tela por célula, com a transição suave existente terminando num valor inteiro. Tamanho de tela com resolução ímpar não pode deslocar meio pixel.
- `TermiteMarkers`: filtro `Point`.
- Trocar o `Outline` do uGUI por contorno de 1 pixel na cor da paleta onde ele aparece.
- **Pronto quando:** formigas andando e girando não piscarem em 16 e 32 px por célula, comparando capturas antes e depois.

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

### Etapa 5 — Atlas desenhado (arte)

- Pintar, na paleta mestra, um tile repetível por material (terra, monte, areia, pedra, rocha-mãe, compactado) e o fundo, no formato da etapa 4.
- Conferir no jogo, nos dois zooms, se a repetição aparece.
- **Pronto quando:** o atlas estiver atribuído no **Tile Atlas** e aprovado em captura de tela.

### Etapa 6 — Mapa de luz (programação)

- Fontes de luz por célula: sala da rainha, cultivo, entrada e superfície; depois, decorações luminosas.
- Mapa em resolução de célula ou de bloco, borrado, com luz ambiente baixa, enviado ao shader e quantizado em 4–6 degraus por texel. **Substitui a névoa** (D3, 2026-10-21): a névoa sai (código, opção **Fog Of War** e o que mais depender dela) quando a luz entrar.
- Refeito só nos blocos sujos ou quando uma fonte muda.
- **Pronto quando:** a luz acompanhar a abertura de salas e a etapa Render do F11 continuar dentro do orçamento atual.
- **Implementada em 2026-10-21** (`AntFarmView.Light.cs`, `TerrainTiles.shader`) e ajustada no mesmo dia pelo desenvolvedor: **a luz só entra de fora**. Tudo acima do solo original tem luz cheia e fica totalmente visível; a luz cai reta pelas bocas, sem perder força e sem limite de profundidade, até bater num bloco (o feixe), e de lá se espalha perdendo 1 nível (de 16) por célula de túnel e 4 por célula de terra; o interior longe dos feixes cai num breu (túnel com piso de 0,08, terra preta). O feixe foi pedido pelo desenvolvedor no segundo ajuste, com desenho. Salas não emitem luz (a primeira versão tinha túnel, sala da rainha e sala de nutrientes como fontes; recusado). Marcações e pincel ficam acima da luz. 5 degraus no shader, interpolados por texel. A névoa saiu (código e opção **Fog Of War**). `TerrainRenderCacheTests` confere a luz contra uma propagação na grade inteira, o feixe num poço de 40 células (que apaga ao ser tapado longe da janela) e o pincel acima da luz. Redesenhar o mundo inteiro: ~119 ms (85,4 ms com a névoa); etapa Render do F11 parado ~0,06 ms. Aprovada pelo desenvolvedor em 2026-10-21 ("a luz está aprovada assim"). Falta medir com chuva forte e cavando. Fontes internas (cogumelos luminosos, salas) ficam para quando o desenvolvedor pedir (etapa 12).

### Etapa 7 — Pós-processamento (programação, pequena)

- Volume do URP: bloom leve só nos tons claros, vinheta e ajuste de cor quente. Opção nas configurações para desligar o bloom.
- **Pronto quando:** capturas antes e depois forem aprovadas e a opção funcionar.

### Etapa 8 — Formigas em 16 px (arte, uma classe por sessão)

- Redesenhar em 48×32 (3×2 células) os quadros `Ant_Walk_0–3` e `Ant_Top_0–3` de cada pasta em `Art/Ants`, mais rainha, cupim alado e pulgões, na paleta mestra e com o contorno da regra.
- Ajustar `AntSpriteImporter` e `AphidSpriteImporter` para 16 PPU e conferir os pivôs.
- **Pronto quando:** cada classe for conferida em jogo nos dois zooms, ao lado do terreno novo.

### Etapa 9 — Crias e itens (arte)

- Ovo, larva e pupa em 16 px por célula. Itens carregados (nutriente, água, ovo) como sprites da paleta, em vez de cores do inspector.
- **Pronto quando:** crias e cargas forem conferidas em jogo.

### Etapa 10 — Cenário (arte e programação)

- Redesenhar plantas e elementos de `Art/Scenery` em 16 px por célula (D4, 2026-10-21: sem exceção de fundo), dividindo texturas grandes quando passarem do limite.
- Alinhar as cores do cenário procedural à paleta mestra.
- **Pronto quando:** a superfície for aprovada em captura junto do terreno.

### Etapa 11 — UI no mesmo pixel (programação e arte)

- Escala da HUD e do menu em múltiplo inteiro do pixel do mundo; molduras e ícones na paleta mestra.
- **Pronto quando:** o HUD for conferido em 1080p e 1440p sem pixels de tamanhos diferentes.

### Etapa 12 — Decorações de borda (opcional)

- Raízes no teto, musgo no chão e cogumelos luminosos colocados nas bordas das cavidades, pela distância com sinal (como objetos; a terra em si continua sem borda). Os cogumelos viram fontes de luz da etapa 6.

## Decisões

Tomadas pelo desenvolvedor em 2026-10-21. Substituem a decisão de 2026-10-17 de a formiga ter pixels menores que os do terreno.

| ID | Decisão | Escolha |
|---|---|---|
| D1 | Escala aparente | Fica como está: câmera começando em 16 px por célula, sem pixels grossos |
| D2 | Vista das formigas | Mantém os dois conjuntos (lado e cima), com a regra da tabela de regras |
| D3 | Névoa e luz | A luz substitui a névoa |
| D4 | Cenário | Tudo na mesma resolução: cenário redesenhado em 16 px por célula, sem exceção de fundo |
| D5 | Paleta | "O cenário é a definição dos pixels": a paleta mestra sai do cenário e vale para tudo. Tamanho a definir na montagem |

## Como validar

- Capturas fixas para comparar antes e depois: mesma seed, mesma posição de câmera, zoom de 16 e 32 px por célula, uma sala, um túnel, a superfície e uma área de areia.
- Desempenho pela etapa Render do painel F11 (`SimProfiler`), parado e com chuva forte.
- `TerrainRenderCacheTests` para tudo que o shader passar a ler da grade.
- Movimento: formigas andando e girando no zoom inicial, sem pixels piscando.
