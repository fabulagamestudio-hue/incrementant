# Guia — Protótipo do formigário (AntFarm)

Protótipo de viabilidade: um formigueiro visto de lado, num mapa de pixels (1 célula = 1 texel), onde formigas cavam os pixels que o jogador marca e levam cada um para fora, formando uma pilha na superfície. A precisão por pixel e a física granular seguem a referência de *Noita*.

Cena: `Assets/Project/Scenes/AntFarm.unity`, gerada por **Incrementant/Setup/Criar cena AntFarm** (`AntFarmSceneSetup`). A estrutura da cena vem do script (idempotente); mapa e marcadores são conteúdo da cena e podem ser editados à mão.

## Modo do mapa

`AntFarmGame` → **Map Source** decide de onde vem o terreno no Play. O padrão é **Procedural**.

| Modo | Terreno | Ninho e despejo | Preview na cena |
|---|---|---|---|
| **Procedural** (padrão) | `TerrainGenerator` com **Terrain Settings**. Com **Randomize Seed** ligado, cada Play e cada R geram um mapa novo; desligado, a **Seed** repete sempre o mesmo. O mapa cresce com a câmera (ver "Mundo expansível"). | Calculados pelo gerador; os marcadores da cena são ignorados (e somem dos gizmos). | Nenhum (gizmos: contorno do mundo e da área inicial): o mapa só existe no Play. |
| **Image** | O PNG em **Map**. Tamanho fixo: não cresce. | Objetos `Nest`, `DumpStart`, `DumpEnd`. | O próprio PNG. |

O Inspector (`AntFarmGameEditor`) atualiza o preview e o enquadramento da câmera ao trocar o modo. O setup da cena não altera o modo escolhido.

## Mundo expansível (modo Procedural)

O mundo tem tamanho fixo — **Max Width × Max Height** em **Terrain Settings** (padrão 960×520) — e é o limite do mapa: não há mapa infinito. A **área inicial** (**Width × Height**, padrão 320×200) fica no topo e centralizada; só ela é gerada no começo. Conforme a câmera anda, o resto aparece:

- **Área ativa (`PixelGrid.Active`).** A grade é alocada no tamanho do mundo, mas só um retângulo dela "existe": fora dele vale `Bedrock` (`InBounds` é falso), então formigas, BFS, areia e marcas param na borda. O retângulo só cresce e vai sempre até o topo do mundo — cresce para os lados e para baixo.
- **Geração sob demanda.** Todo frame `AntFarmGame.EnsureGenerated` pega a vista da câmera, soma **Generation Margin** células, arredonda para blocos de **Chunk Size** e chama `TerrainGenerator.Expand`, que une isso à área ativa e gera só as faixas novas (esquerda, direita, baixo). Ampliar a área muda `StructureVersion`, e a navegação é refeita.
- **Determinismo por posição.** `TerrainGenerator` calcula o relevo de todas as colunas no construtor e cada célula depende só da seed e de (x, y): blocos gerados em qualquer ordem emendam sem costura (testado em `WorldExpansionTests`). A moldura de `Bedrock` fica nos limites do mundo (x = 0, x = largura − 1, y = 0); o ninho é cavado só uma vez, em `Begin`, dentro da área inicial; a "zona sem areia" em volta do ninho vale por posição, então também se aplica a blocos gerados depois.
- **`TerrainGenerator.Generate(grid, settings, seed)`** continua gerando uma grade inteira de uma vez (mundo = grade): é o que usam a ferramenta "Sortear e fixar mapa" e os testes.

## Câmera

`MapCamera` (campo **Camera Control** do `AntFarmGame`): WASD/setas ou arrastar com o botão do meio movem; Q/E ou Ctrl+scroll dão zoom (o scroll sozinho continua mudando o pincel; Ctrl+scroll mantém o ponto sob o cursor parado).

- **Limitador:** a vista nunca sai do mundo (mais **Camera Margin** células acima do topo). Num eixo em que a vista é maior que o mundo, a câmera centraliza (`MapCamera.ClampCenter`).
- **Zoom:** o mais aberto é o enquadramento inicial (área inicial + margem, como era a câmera fixa); o mais fechado é **Min Half Height**. Limitar o zoom aberto segura o custo do render e quanto do mundo precisa estar gerado.
- **Render em janela:** o `AntFarmView` só desenha o recorte da grade em volta da vista (`AntFarmView.WindowFor`, em passos de 16 células), num `SpriteRenderer` filho `TerrainWindow` criado no Play. O `SpriteRenderer` do próprio `WorldView` fica desligado no Play e serve só para o preview do PNG no Editor.

## Mapa fixo na cena (modo Image)

O terreno é uma imagem: `Assets/Project/Art/Maps/AntFarm_Map01.png`, atribuída em `AntFarmGame._map`. 1 pixel = 1 célula, y = 0 embaixo. O mesmo PNG é o sprite do `WorldView`, então o mapa aparece na cena sem dar Play; no Play o `AntFarmView` troca o sprite pela textura dinâmica (e a cena volta ao PNG ao sair).

| Cor no PNG | Material |
|---|---|
| Transparente (alpha < 128) | Céu (vazio acima do solo) |
| `#36251B` (54,37,27) | Túnel já aberto (vazio subterrâneo) |
| `#765234` (118,82,52) | Terra |
| `#567A34` (86,122,52) | Grama |
| `#D4BA78` (212,186,120) | Areia |
| `#706E76` (112,110,118) | Pedra |
| `#9C7046` (156,112,70) | Terra despejada (granular) |
| `#2E2828` (46,40,40) | Rocha-mãe (borda indestrutível) |

Valores exatos em `CellMaterials.BaseColor`; a leitura aceita a cor mais próxima, então pequenas variações de pintura não quebram o mapa. O tom (variação de cor) não vem do PNG: é recalculado por posição (`TerrainMapCodec.ShadeAt`).

- **Editar o terreno:** pinte o PNG em qualquer editor (lápis de 1 px, sem antialiasing) e salve; a Unity reimporta e a cena atualiza. Todo PNG em `Assets/Project/Art/Maps/` recebe automaticamente as configurações certas (`AntFarmMapImporter`: Point, sem compressão, Read/Write, sprite 1 ppu com pivô no canto inferior esquerdo).
- **Solo original (céu × túnel):** em cada coluna é o pixel não transparente mais alto. Pinte túneis iniciais com a cor de túnel, não transparente, senão a coluna vira céu.
- **Ninho e despejo:** filhos `Nest`, `DumpStart` e `DumpEnd` de `AntFarm`. Arraste na cena (gizmos: círculo vermelho = ninho, linhas amarelas = faixa de despejo; só o X dos marcadores de despejo importa). As formigas nascem na célula do `Nest`; se ela for sólida, sobem até achar espaço.
- **Fixar um mapa sorteado:** **Incrementant/Mapa/Sortear e fixar mapa na cena (sobrescreve o PNG)** gera com `TerrainGenerator` (usando as **Terrain Settings** do Inspector), sobrescreve o PNG atribuído, reposiciona os marcadores e troca o modo para **Image**. Não tem desfazer; salve a cena depois.

## Controles (Play Mode)

| Entrada | Ação |
|---|---|
| Botão esquerdo (arrastar) | Marca pixels para escavação dentro do pincel |
| Botão direito (arrastar) | Remove marcas |
| Scroll | Tamanho do pincel |
| WASD / setas, ou arrastar com o botão do meio | Move a câmera |
| Q / E, ou Ctrl + scroll | Zoom (Ctrl + scroll mira no cursor) |
| Espaço / botão do HUD | Compra formiga com a terra entregue |
| 1 / 2 / 3 | Velocidade x1 / x3 / x8 |
| C / botões do HUD | Tempo: seco → chuva leve → chuva forte |
| R | Procedural: gera outro mapa · Image: reinicia o mapa da cena |

## Como funciona

**Passo fixo.** `AntFarmGame.Update` move a câmera, gera o que a vista alcançar (modo Procedural), acumula tempo e roda `_ticksPerSecond` passos por segundo (limitado por `_maxTicksPerFrame`). Cada passo: `RainSimulation.Step`, `GranularSimulation.Step` e depois `AntColony.Tick`. No fim do frame, `AntFarmView.Render` redesenha a textura inteira. A velocidade (x1/x3/x8) só multiplica quantos passos rodam por frame; o passo é sempre o mesmo, então o resultado da simulação não depende da velocidade nem do FPS. O custo por passo segue o que está mudando (blocos acordados, ver abaixo), não o tamanho do mundo: é isso que deixa o x8 no mesmo FPS que o x1.

**Grade (`World/PixelGrid`).** Arrays planos de material, tom (variação de cor) e marca. Fora dos limites conta como `Bedrock`. Dois contadores avisam quem depende da grade:
- `StructureVersion` muda quando uma célula passa de vazia para sólida ou o contrário (invalida a navegação);
- `MarkVersion` muda quando marcas são criadas ou removidas.
**Blocos (16x16 células).** Toda mudança de célula (`Set`, `MoveCell`, expansão da área ativa, `SetSurfaceY`) "toca" o bloco dela e os blocos encostados na célula: acorda o bloco para a simulação granular e muda a versão dele (`GetChunkVersion`). A grade também conta marcas por bloco (`GetChunkMarkedCount`). Quem escrever na grade precisa passar por esses métodos, nunca pelos arrays, senão bloco dormindo não acorda e cache por bloco fica velho.

Só a área ativa (`Active`) existe para o jogo; fora dela, inclusive dentro da capacidade do array, conta como `Bedrock`. `SurfaceY` por coluna guarda o solo original: vazio acima dele é céu, abaixo é túnel. Isso só afeta a cor e a faixa de despejo.

**Materiais (`World/CellMaterial`).** `Dirt`/`Grass` são estáticos e escaváveis; `Sand` e `Spoil` (terra despejada) são granulares e escaváveis; `PackedSpoil`/`PackedSand` (granular compactado pela chuva forte) são estáticos e escaváveis; `Rock` e `Bedrock` não são escaváveis. Tempos de escavação e cores estão na tabela `CellMaterials`; mudar um material é editar essa tabela.

**Física granular (`World/GranularSimulation`).** Varre de baixo para cima; o grão cai reto ou escorrega em diagonal, com a lateral livre (não atravessa quinas). A direção horizontal alterna por linha e por passo para as pilhas não penderem para um lado. Areia sob um túnel recém-aberto desaba e soterra; as formigas soterradas são empurradas para cima. Só varre os blocos acordados (`IsChunkAwake`) e pula faixas de blocos inteiras sem nada acordado. Um bloco que passa um passo inteiro sem ser tocado dorme: todos os grãos nele estão parados, e só uma mudança perto deles pode soltá-los, mudança que acorda o bloco. Por isso o resultado é idêntico ao da varredura completa (`ChunkedSimulationTests` compara as duas, com escavação e despejo no meio). O estado acordado é lido ao entrar em cada bloco, então um grão que sai pela borda acorda o vizinho ainda no mesmo passo, como antes.

**Chuva (`World/RainSimulation`).** Tempo em `AntFarmGame` → **Weather** (inicial) e **Rain Settings**; no Play, troca com C ou pelo HUD. As gotas são da simulação (passo fixo, sorteio pela seed do mapa): nascem na última linha da área ativa, em colunas sorteadas (**Drops Per 100 Columns** por segundo), caem **Fall Speed** células por passo pelas células vazias e agem na primeira sólida que encontram, inclusive no fundo do poço quando caem pela entrada. Gotas no ar terminam a queda com o efeito do tipo em que nasceram.
- **Chuva leve:** só a terra despejada (`Spoil`) reage. Cada gota soma 1 de umidade (`PixelGrid.GetWetness`); ao chegar em **Light Hits To Dissolve** (4), a célula some e o que estava em cima desaba pela física granular. A umidade acompanha o grão quando ele cai (`MoveCell`), zera quando o material muda (`Set`) e, fora da chuva leve, cai 1 a cada **Dry Seconds** (`PixelGrid.DryAll`, pulado quando `WetCount` é 0). A célula molhada é desenhada mais escura (`AntFarmView._wetDarkening`).
- **Chuva forte:** troca a célula granular atingida, e as granulares logo abaixo até **Heavy Soak Depth** (2), pela versão compactada (`CellMaterials.ToPacked`: `PackedSpoil`, `PackedSand`), que não cai. Forma uma crosta: as próximas gotas param nela. É permanente; o compactado continua escavável e volta a ser solto ao ser despejado (`ToDumped`). Um grão atingido no meio da queda fica parado no ar.
- **Desenho:** `AntFarmView` pinta cada gota como um risco vertical (`_dropStreak` × queda por passo) na textura do terreno, translúcido sobre o céu transparente.

**Carga do mapa (`World/TerrainMapCodec`).** `Decode` lê os pixels do PNG, classifica cada um pela cor mais próxima da paleta, calcula o solo original de cada coluna e aplica o tom. `Encode` faz o caminho inverso (usado pela ferramenta de Editor). A posição dos marcadores vira `TerrainLayout` em `AntFarmGame.LayoutFromMarkers`.

**Geração (`World/TerrainGenerator`).** Relevo por Perlin, bolsões de pedra e areia por limiar de ruído (`TerrainSettings`), moldura de `Bedrock` (o "vidro" do formigário), poço e câmara iniciais. Areia em volta do ninho inicial vira terra para não desabar no começo. Dos dois lados da entrada ficam pilhas de `Spoil` (a terra que saiu do poço), configuradas em **Entrance Mound** (altura do pico e comprimento da encosta de fora; altura 0 desliga): a coluna ao lado do poço fica sem terra e o topo muda no máximo 1 célula entre colunas vizinhas, para a física granular não desmanchar a pilha nem derramá-la no poço (`Generator_PilesStableSpoilMoundsOnBothSidesOfTheEntrance`). São células comuns: as formigas andam por cima e dá para cavá-las. Devolve `TerrainLayout` (entrada, ninho, faixa de despejo).

**Navegação (`Colony/FlowField`).** Regra de caminhabilidade em `AntNavigation.IsWalkable`: abaixo do solo original, todo vazio é caminhável, porque a formiga se apoia no fundo do formigueiro (a parede de trás do formigário) e atravessa túneis e câmaras pelo meio. No céu, só fica em célula vazia que encosta (8 vizinhos) em algo sólido: anda no chão, nas paredes e no teto da superfície e da pilha. Se perder o apoio (por exemplo, ao cavar o próprio piso), se agarra na célula caminhável vizinha (`AntColony.TryCling`: prefere descer encostada, depois os lados, depois subir) e só cai quando não há nenhuma ao alcance. Três campos de distância (BFS de 8 direções sobre células caminháveis):
- **dig**: origens = células caminháveis vizinhas de pixels marcados;
- **dump**: origens = células caminháveis acima do solo original, dentro da faixa de despejo;
- **home**: origens = em volta do ninho.
São recalculados quando `StructureVersion`/`MarkVersion` mudam, no máximo a cada `FieldRebuildInterval` segundos (tempo simulado). Antes de recalcular, `WalkabilityMap.Refresh` atualiza um cache de `IsWalkable` só nos blocos cuja versão mudou; o BFS e as origens dos campos leem o cache, que nesse momento é igual à regra exata. O BFS limpa só as células que alcançou na vez anterior. As origens do campo *dig* são procuradas pelo lado caminhável (células caminháveis com vizinho marcado, só em blocos com marca ou encostados neles), porque a área marcada costuma ser grande e maciça; a ordem das origens não muda as distâncias. O movimento das formigas continua usando `AntNavigation.IsWalkable` direto (o cache só é atualizado na reconstrução). Cada formiga desce o gradiente do campo da tarefa atual (empates sorteados).

**Comportamento (`Colony/AntColony`).** Ociosa → desce o campo *dig* → ao chegar (distância 0) escolhe um pixel marcado vizinho e cava (`DigSeconds × DigTimeMultiplier`) → passa a carregar → desce o campo *dump* → dentro da faixa anda até um X sorteado (espalha a pilha) → solta o pixel acima de si como `Spoil` (areia continua areia) → ganha 1 de terra. Sem trabalho alcançável, vaga perto do ninho. Marcas inalcançáveis (atrás de pedra, sem túnel até elas) são ignoradas até ficarem acessíveis.

**Economia.** Cada pixel entregue soma 1 em `StoredPixels`. Nova formiga custa `AntBaseCost × AntCostGrowth^(formigas compradas)`. É o gancho mínimo de incremental; valores provisórios em `ColonySettings`.

**Render do terreno (`AntFarm/AntFarmView`).** Uma `Texture2D` RGBA32 com filtro Point num `SpriteRenderer` filho (`TerrainWindow`, 1 unidade = 1 célula), cobrindo só a janela da grade em volta da câmera (ver "Câmera"). Desenha células, marcas (tinta amarela), estacas da faixa de despejo e o contorno do pincel. Só desenha formigas (3 px) como fallback, quando não há `AntSwarmView` com quadros.

**Formigas em sprite (`AntFarm/AntSwarmView`).** As formigas são desenhadas à parte, com pixels 6x menores que os do terreno (decisão do desenvolvedor em 2026-10-17: a formiga pode ter pixels menores). Um `SpriteRenderer` por formiga, criado sob demanda como filho de `AntFarm/Ants` (não fica salvo na cena). É só visual; a simulação continua em células:
- **Posição:** interpola entre a célula anterior e a atual com `Ant.FromX/FromY`, `MoveStart` e `MoveDuration` (gravados pela colônia a cada passo ou queda) e `AntColony.SimulationTime`.
- **Vista de cima × de lado:** no fundo do formigueiro (túnel, sem nada sólido encostado) usa os quadros vista de cima (`_topFrames`) e gira livre para a direção do passo. Encostada numa superfície, ou no céu, usa os quadros de lado.
- **Rotação (de lado):** as patas apontam sempre para a superfície. A soma dos vizinhos sólidos diz onde está o chão (piso, parede, teto ou quina, em degraus de 45°) e define o giro, mesmo parada ou cavando; o espelhamento horizontal (`flipX`) segue o sentido do passo ao longo da superfície. Sem um lado claro (túnel de 1 célula ou queda), o corpo se alinha ao passo. Há zona morta para não piscar quando o passo é perpendicular à superfície.
- **Animação:** troca de quadro pela distância andada (`_framesPerCell`); cavando, troca por tempo (`_digFramesPerSecond`).
- **Pixel carregado:** sprite filho na mandíbula, com a cor do material.

**Cenário da superfície (`AntFarm/SurfaceSceneryView` + `SurfaceSceneryPainter`).** Pintado por código a cada mapa novo (Play e R), determinístico pela seed do mapa, em pixel art com a mesma densidade das formigas (6 px por célula). A escala parte da formiga: 3 células ≈ 5 mm, logo 1 célula ≈ 1,7 mm. Daí as medidas, em células: grama alta 25–70 (passa do topo do mapa, cortada na borda), flor (margarida) 32–46, cogumelo 6–18, trevo ~12 de largura, folha seca 18–34, graveto 30–60, pedrinhas 1–6 de raio, grãos < 1. São cinco camadas (`SpriteRenderer` filhos de `AntFarm/Scenery`, criados no Play, não salvos), pintadas para a largura do mundo inteiro:
- **Céu** (ordem −30): coluna de 1 pixel com o gradiente, esticada na largura da vista e reposicionada todo frame; o gradiente fica preso à altura do mapa (cor de baixo na base da área inicial, de cima no topo do mundo).
- **Grama distante** (ordem −20) e **grama média** (ordem −15): ladrilhos de `BandTileCells` (160) células que se repetem na horizontal (`SpriteDrawMode.Tiled`), desbotados na cor do céu. **Paralaxe:** `SurfaceSceneryView.UpdateParallax` (chamado todo frame) desloca cada faixa por `(câmera − âncora) × fator` — **Far Parallax** 0,75 e **Mid Parallax** 0,5 (0 = presa ao mapa, 1 = presa à tela) — nos dois eixos; a âncora é a posição inicial da câmera, em que a base das folhas fica no solo da entrada. Embaixo das folhas há `BandFill` (72) células de campo liso, que cobrem o vão quando a câmera sobe; quando ela desce, a faixa some atrás do terreno. A posição é arredondada para a grade de pixels da arte, para o Point filter não tremer.
- **Perto** (ordem −10): grama, flores, trevos, cogumelos, gravetos, folhas secas, pedrinhas e grãos, apoiados no solo original e com a base enterrada atrás do terreno.
- **Frente** (ordem 20, acima das formigas): poucos tufos baixos escuros, nunca a menos de `ForegroundClearance` células da entrada nem da faixa de despejo.

**Elementos isolados (PNGs editáveis).** Cada elemento das camadas perto e frente é um PNG próprio em `Assets/Project/Art/Scenery/<Pasta>/<Elemento>_NN.png` (`Grass`, `Tufts`, `Flowers`, `Clovers`, `Mushrooms`, `Twigs`, `DryLeaves`, `Pebbles`, `Grains`), uma variante por arquivo. O `SurfaceSceneryPainter.Paint` só **posiciona**: sorteia uma variante do elemento (`SceneryElement`), escolhe o x e carimba (`SceneryStamp`) com o pivô no chão. Regras de posição: o pivô vai no ponto mais baixo do solo sob a base do elemento (opaco até 1 célula do pivô), para não boiar em desnível; trevos e cogumelos saem em grupinhos de 1 a 3 (±6 e ±5 células); pedrinhas são enterradas 60–100% (perto) ou 50–90% (frente) de 70% do raio; alfa parcial é composto por cima. Céu e faixas distantes continuam pintados por código.
- **Pivô = ponto de apoio no topo do solo** (6 px por célula). O que fica abaixo do pivô fica enterrado (a grama exportada tem 1 célula = 6 px abaixo). PNG novo entra com pivô no centro da base; para enterrar parte dele, ajuste no Sprite Editor.
- **Editar:** abra o PNG em qualquer editor (pixel art, sem antialiasing) e salve; pode mudar o tamanho, mas aí confira o pivô. Importação automática (`SceneryArtImporter`): sprite Point, sem compressão, **Read/Write ligado** (obrigatório: o cenário copia os pixels), 6 ppu; o pivô só é definido no primeiro import.
- **Adicionar/remover variantes:** coloque ou apague PNGs na pasta do elemento; o catálogo `Assets/Project/Data/SceneryArtSet.asset` (`SceneryArtSet`, atribuído em `Scenery._art`) se atualiza sozinho (ou **Incrementant/Arte/Atualizar catálogo do cenário**). Elemento sem nenhum PNG (ou `_art` vazio) usa as variantes procedurais (`SceneryStampLibrary.Default`).
- **Recriar a arte:** **Incrementant/Arte/Exportar elementos do cenário (sobrescreve)** regrava as variantes procedurais (`SurfaceSceneryPainter.PaintElement`, seed `SceneryStampLibrary.DefaultSeed`) por cima dos arquivos de mesmo nome. O setup da cena só exporta elementos cuja pasta está vazia.
- Os PNGs são lidos uma vez por sessão de Play (`SurfaceSceneryView` guarda a biblioteca); editar durante o Play só vale no próximo Play.

Com um `SurfaceSceneryView` atribuído em `AntFarmGame._scenery`, o `AntFarmView` deixa o céu transparente (`Initialize(grid, transparentSky)`) e o terreno (ordem 0) fica entre o fundo e a frente: pilha de despejo e formigas aparecem na frente do cenário. Sem ele, volta o céu em gradiente da textura. É só visual: nada entra na grade nem na navegação, e cavar a superfície não remove o que está atrás (o cenário fica atrás do "vidro" do formigário). Quantidades por 100 células em **Settings** do componente.

**Arte das formigas.** `Assets/Project/Art/Ants/`, 18x12 px, virada para a direita: `Ant_Walk_0..3.png` de lado (patas para baixo) e `Ant_Top_0..3.png` vista de cima (simétrica). Importação automática (`AntSpriteImporter`): 6 px por célula (a formiga mede 3x2 células), Point; pivô a 3 px das patas nos quadros de lado e no centro nos de cima. A arte inicial é gerada por código (`AntSpriteGenerator`) e é provisória: pode ser repintada à mão mantendo tamanho e orientação. O setup só gera os quadros que faltam; **Incrementant/Arte/Regerar sprites das formigas** sobrescreve tudo. A câmera é controlada por `MapCamera` (ver "Câmera").

## Como estender

- **Material novo:** valor novo no fim do enum (não reordene), e entradas em `IsGranular`/`IsDiggable`/`DigSeconds`/`BaseColor`.
- **Upgrades incrementais:** os pontos naturais são `AntSpeed`, `DigTimeMultiplier`, capacidade de carga (hoje 1 pixel em `Ant.Carried`) e o custo das formigas. Para variar em jogo, mova os valores para estado de execução da colônia em vez de alterar o `ColonySettings` serializado.
- **Mundo maior ou menor:** `TerrainSettings.MaxWidth/MaxHeight` (limite do mundo) e `Width/Height` (área inicial e zoom máximo aberto). O render não depende do tamanho do mundo (janela) e a simulação granular e o BFS custam o que está mudando ou alcançável, não a área gerada. O que ainda escala com a área: memória (grade, 3 campos e o cache de caminhabilidade) e a primeira montagem do cache (ver `KnownIssues.md`).
- **Elemento novo de cenário:** valor no fim de `SceneryElement`, desenho em `SurfaceSceneryPainter.PaintElement`, contagem em `SceneryStampLibrary.VariantCount`, campo em `SceneryArtSet`, pasta em `SceneryArtExporter.FolderOf` e a regra de posição em `Paint` (com quantidade em `SurfaceScenerySettings`).
- **Mais camadas de paralaxe:** pinte outro ladrilho em `SurfaceSceneryPainter` (como `DistantBand`), acrescente em `SceneryLayers` e posicione em `UpdateParallax` com um fator próprio.

## Cuidados

- O cenário da superfície é estático (sem vento) e só existe no Play; a cena no Editor mostra só o mapa.
- Nada do mundo é descartado ao se afastar: a área gerada só cresce. O teto de memória é o tamanho do mundo (grade + 3 campos de BFS ≈ 15 MB em 960×520).
- Ao aumentar **Max Width** muito, as camadas perto/frente do cenário (largura do mundo × 6 px) podem passar do limite de textura (16384 px ≈ 2730 células).

- As formigas em sprite (3x2 células) são maiores que a célula que ocupam na simulação; em túneis de 1 célula elas cobrem um pouco das paredes. É só visual.
- O HUD é IMGUI provisório; a captura por câmera (`manage_camera screenshot`) não o mostra.
- Capturas de tela pelo MCP durante o Play Mode podem disparar um aviso interno da Unity ("PlayerLoop ... called recursively") e pausar o editor se *Error Pause* estiver ligado. Não é do código do jogo.
- Pendências de desempenho e limitações: [KnownIssues.md](../../KnownIssues.md).
