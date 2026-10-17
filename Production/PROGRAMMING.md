# Programação — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** PROGRAMMING
**Última avaliação:** 2026-10-17

## Resumo

Protótipo de viabilidade do formigário implementado: grade de pixels com física granular, geração de mapa, formigas que cavam pixels marcados e os levam para fora, render por textura e HUD provisório. Núcleo coberto por 7 testes EditMode (passando) e exercitado em Play Mode pelo agente, inclusive com 206 formigas. Input por mouse e HUD ainda não foram usados por uma pessoa. Desempenho aceitável no tamanho atual, com dívidas registradas para mapas maiores.

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| PRG-001 | Grade de pixels, física granular e geração do mapa | VERIFIED | UNDEFINED | [AntFarmSimulationTests.cs](../Assets/Project/Scripts/Tests/EditMode/AntFarmSimulationTests.cs) (7/7) + Play Mode | REQ-001 | 2026-10-17 |
| PRG-002 | Formigas: navegação, escavação e despejo | VERIFIED | UNDEFINED | [AntFarmSimulationTests.cs](../Assets/Project/Scripts/Tests/EditMode/AntFarmSimulationTests.cs) + Play Mode (381 pixels entregues) | PRG-001, REQ-001 | 2026-10-17 |
| PRG-003 | Render, câmera, input de marcação e HUD | IMPLEMENTED | UNDEFINED | [AntFarmGame.cs](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGame.cs), [AntFarmView.cs](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmView.cs) | PRG-001, PRG-002 | 2026-10-17 |
| PRG-004 | Cena AntFarm gerada por script de setup | VERIFIED | UNDEFINED | [AntFarmSceneSetup.cs](../Assets/Project/Scripts/Editor/Setup/AntFarmSceneSetup.cs) (rodado 2x, sem duplicar) | PRG-003 | 2026-10-17 |
| PRG-005 | Mapa fixo na cena (PNG + marcadores), opcional; procedural é o padrão | VERIFIED | UNDEFINED | [TerrainMapCodec.cs](../Assets/Project/Scripts/Runtime/World/TerrainMapCodec.cs), testes de codec (9/9) + Play Mode | PRG-001, PRG-004 | 2026-10-17 |
| PRG-006 | Formigas em sprite (arte em resolução maior, rotação, animação) | VERIFIED | UNDEFINED | [AntSwarmView.cs](../Assets/Project/Scripts/Runtime/AntFarm/AntSwarmView.cs), Play Mode com captura aproximada | PRG-002 | 2026-10-17 |
| PRG-007 | Formigas se agarram em paredes/teto ao perder o piso | VERIFIED | UNDEFINED | [AntColony.cs](../Assets/Project/Scripts/Runtime/Colony/AntColony.cs) `TryCling`, testes (11/11) + Play Mode | PRG-002, PRG-006 | 2026-10-17 |
| PRG-008 | Formigas andam no fundo do formigueiro (vista de cima nos túneis) | VERIFIED | UNDEFINED | [FlowField.cs](../Assets/Project/Scripts/Runtime/Colony/FlowField.cs) `AntNavigation`, testes (12/12) + Play Mode | PRG-007, ART-002 | 2026-10-17 |
| PRG-009 | Cenário da superfície em camadas (céu, distante, médio, perto, frente) | VERIFIED | UNDEFINED | [SurfaceSceneryPainter.cs](../Assets/Project/Scripts/Runtime/AntFarm/SurfaceSceneryPainter.cs), [SurfaceSceneryTests.cs](../Assets/Project/Scripts/Tests/EditMode/SurfaceSceneryTests.cs) (suíte 36/36) + Play Mode | REQ-002, ART-003 | 2026-10-17 |
| PRG-010 | Mundo expansível: geração por blocos conforme a câmera, até o limite do mundo | VERIFIED | UNDEFINED | [TerrainGenerator.cs](../Assets/Project/Scripts/Runtime/World/TerrainGenerator.cs), [WorldExpansionTests.cs](../Assets/Project/Scripts/Tests/EditMode/WorldExpansionTests.cs) (25/25) + Play Mode | REQ-003, PRG-001, PRG-005 | 2026-10-17 |
| PRG-011 | Câmera móvel (pan, zoom, limitador), render em janela e paralaxe do cenário | IMPLEMENTED | UNDEFINED | [MapCamera.cs](../Assets/Project/Scripts/Runtime/AntFarm/MapCamera.cs), [SurfaceSceneryView.cs](../Assets/Project/Scripts/Runtime/AntFarm/SurfaceSceneryView.cs), Play Mode com câmera movida por código | REQ-003, PRG-003, PRG-009 | 2026-10-17 |
| PRG-012 | Simulação por blocos e navegação com cache (8x no mesmo FPS) | VERIFIED | UNDEFINED | [ChunkedSimulationTests.cs](../Assets/Project/Scripts/Tests/EditMode/ChunkedSimulationTests.cs) (30/30) + Play Mode medido | REQ-004, PRG-001, PRG-002, PRG-010 | 2026-10-17 |
| PRG-013 | Chuva leve e chuva forte (gotas, erosão da terra despejada, compactação) | VERIFIED | UNDEFINED | [RainSimulationTests.cs](../Assets/Project/Scripts/Tests/EditMode/RainSimulationTests.cs) (suíte 42/42) + Play Mode | REQ-005, PRG-001 | 2026-10-17 |

## Detalhes

### PRG-001 — Grade de pixels, física granular e geração do mapa

Mapa 320x200 células; terra/grama estáticas, areia e terra despejada granulares, pedra indestrutível. Funcionamento em [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md).

### PRG-002 — Formigas: navegação, escavação e despejo

Campos de fluxo (BFS) para cavar, despejar e voltar ao ninho. Em Play Mode (2026-10-17, agente): 206 formigas, ~1,2 ms por passo de simulação, 381 pixels entregues e pilha formada na superfície. Dívidas: KI-002 e KI-003 em [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md).

### PRG-003 — Render, câmera, input de marcação e HUD

Render conferido por captura de tela em Play Mode. As marcas foram aplicadas via código, não pelo mouse; o HUD (IMGUI) não aparece na captura. Falta alguém jogar com mouse/teclado para passar a `VERIFIED`. Render custa ~4 ms (KI-001).

### PRG-005 — Mapa fixo na cena (PNG + marcadores), opcional; procedural é o padrão

Pedido pelo desenvolvedor em 2026-10-17 ("tudo está sendo criado em runtime, tem como ser fixo em cena?"). O terreno agora é `Assets/Project/Art/Maps/AntFarm_Map01.png`, visível na cena sem Play; ninho e faixa de despejo são objetos da cena. Validação (agente, 2026-10-17): 9/9 testes EditMode, grade carregada no Play idêntica ao PNG (0 diferenças), formigas cavando e despejando, setup rodado 2x sem duplicar. Edição do PNG num editor de imagem externo não foi testada. Em seguida (mesmo dia), o desenvolvedor pediu geração procedural como padrão: `AntFarmGame` ganhou o modo do mapa (Procedural padrão, Image opcional). Validado pelo agente: cena salva em Procedural sem preview, Play gera mapa diferente do PNG e R sorteia outro; 9/9 testes. Como usar: [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md).

### PRG-006 — Formigas em sprite (arte em resolução maior, rotação, animação)

Pedido pelo desenvolvedor em 2026-10-17 para as formigas parecerem formigas; ele aprovou pixels menores que os do terreno. Só visual: a simulação não mudou. Validado pelo agente em Play Mode (2026-10-17): captura aproximada mostrando formigas subindo a parede giradas, carregando grão e cavando; 206 formigas custam ~0,4 ms por frame na view; 9/9 testes EditMode. A arte é provisória (ART-001). Guia: [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md).

### PRG-007 — Formigas se agarram em paredes/teto ao perder o piso

Pedido pelo desenvolvedor em 2026-10-17 ("andarem nas paredes também, quando não houver piso"). Medição do agente em Play Mode antes da mudança: toda queda acontecia ao cavar o próprio apoio, e em 34 de 34 havia parede ao alcance. Agora a formiga se agarra na superfície vizinha e só cai sem nada ao alcance; o sprite mantém as patas voltadas para a superfície mesmo parada ou cavando. Validação (2026-10-17): 2 testes novos (agarra na parede; cai sem nada ao alcance), 11/11 passando; Play Mode com 36 formigas cavando câmara e corredor, nenhuma queda maior que 1 passo, e captura mostrando formigas alinhadas ao piso diagonal.

### PRG-008 — Formigas andam no fundo do formigueiro (vista de cima nos túneis)

Pedido pelo desenvolvedor em 2026-10-17 ("tem que andar no background do formigueiro, além dos pisos de cenário"). Abaixo do solo original todo vazio passa a ser caminhável (parede de trás do formigário); no céu continua exigindo superfície. No fundo, a formiga é desenhada vista de cima (ART-002). Validação (agente, 2026-10-17): teste novo (fundo caminhável, céu não, caminho pelo meio do túnel), 12/12 passando; Play Mode com câmara grande: formigas subindo pelo meio do poço e atravessando a câmara pelo fundo, captura aproximada conferida.

### PRG-009 — Cenário da superfície em camadas (fundo, perto, frente)

Pedido em REQ-002. `SurfaceSceneryView` pinta por código, a cada mapa, três camadas atrás e na frente do terreno; com ele, o céu do `AntFarmView` fica transparente. Funcionamento no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md). Validação (agente, 2026-10-17): [SurfaceSceneryTests.cs](../Assets/Project/Scripts/Tests/EditMode/SurfaceSceneryTests.cs) (cobertura do céu, grama mais alta que a formiga, frente livre na entrada e no despejo, determinismo), 16/16 passando; Play Mode com escavação: formigas e pilha de despejo desenhadas na frente do cenário, sem erros no console. Em 2026-10-17 os elementos passaram a ser carimbos lidos de PNGs (`SceneryArtSet`, com fallback procedural) posicionados pelo pintor; validação (agente): 2 testes novos (elementos recortados com pivô no chão; cenário usa a biblioteca dada), suíte EditMode 36/36, Play Mode com o catálogo ligado na cena, elementos apoiados no solo e sem erros no console.

### PRG-010 — Mundo expansível: geração por blocos conforme a câmera, até o limite do mundo

Pedido em REQ-003. O mundo procedural tem tamanho fixo (padrão 960x520); só a área inicial (320x200, no topo e ao centro) é gerada no começo e o resto é gerado em blocos de 32 células quando a vista da câmera se aproxima, para os lados e para baixo. Funcionamento no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Mundo expansível"). Validação (agente, 2026-10-17): [WorldExpansionTests.cs](../Assets/Project/Scripts/Tests/EditMode/WorldExpansionTests.cs) (blocos em qualquer ordem iguais à geração de uma vez, moldura nos limites, fora da área ativa vale rocha-mãe, BFS e areia param na borda), 25/25 testes; Play Mode: câmera mandada para fora do mundo parou no canto e a área gerada cresceu até x = 0 e y = 0, sem erros.

### PRG-011 — Câmera móvel (pan, zoom, limitador), render em janela e paralaxe do cenário

Pedido em REQ-003. `MapCamera` move (WASD/setas, botão do meio) e dá zoom (Q/E, Ctrl+scroll), presa aos limites do mundo e com zoom aberto máximo igual ao enquadramento inicial. O terreno passou a ser desenhado só na janela visível. Céu e duas faixas de grama distante acompanham a câmera com fatores 0,75 e 0,5 (paralaxe). Validação do agente (2026-10-17): limitador e paralaxe conferidos em Play Mode com a câmera posicionada por código e capturas de tela; render ~1,3 ms. Falta alguém usar teclado/mouse para passar a `VERIFIED`.

### PRG-012 — Simulação por blocos e navegação com cache (8x no mesmo FPS)

Pedido em REQ-004. A grade foi dividida em blocos 16x16 que dormem quando estáveis; a simulação granular pula blocos e faixas paradas com resultado idêntico à varredura completa. A navegação usa um cache de caminhabilidade por bloco, o BFS limpa só o que alcançou e o campo de escavação procura origens pelo lado caminhável. Funcionamento no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md). Validação (agente, 2026-10-17): 5 testes novos em [ChunkedSimulationTests.cs](../Assets/Project/Scripts/Tests/EditMode/ChunkedSimulationTests.cs) (granular igual à varredura completa, cache igual à regra exata inclusive após expansão, campo reconstruído igual ao novo, blocos dormem), 30/30 passando; campo de escavação comparado ao método antigo com 0 células diferentes. Medido no editor, mundo inteiro 960x520: simulação no 8x caiu de ~77 ms para ~2,1 ms por frame (366 formigas). Play Mode com 156 formigas: 114 FPS no 1x e 102 FPS no 8x, simulação a ~7,8x do tempo de jogo (o restante perdido vem das pausas do editor causadas pelo MCP). Fica o render (~4,3 ms, igual em qualquer velocidade, KI-001).

### PRG-013 — Chuva leve e chuva forte

Pedido em REQ-005. [RainSimulation.cs](../Assets/Project/Scripts/Runtime/World/RainSimulation.cs) solta gotas no topo da área ativa; elas caem pelas células vazias e agem na primeira sólida: a chuva leve molha a terra despejada até dissolvê-la, a chuva forte troca o granular atingido por materiais compactados novos (`PackedSpoil`, `PackedSand`). Funcionamento em [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Chuva"). Validação (agente, 2026-10-17): 6 testes novos (dissolve só a terra despejada, compacta e a crosta não cai, compactado volta a soltar ao ser despejado, umidade acompanha o grão e seca, gota não atravessa sólido), suíte 42/42; Play Mode: chuva leve dissolveu as 116 células dos montes da entrada; chuva forte compactou 100 células (crosta de 2) de um monte de teste. Troca do tempo pela tecla C e pelo HUD não foi usada por uma pessoa (o agente trocou por código).

## Riscos e bloqueios

- PRG-012 — Primeira montagem do cache de caminhabilidade com o mundo inteiro gerado custa ~14 ms uma vez (KI-004). O render (~4,3 ms) passou a ser o maior custo por frame (KI-001).

## Não verificado

- Input real por mouse/teclado e leitura do HUD (PRG-003), inclusive pan/zoom da câmera (PRG-011).
- Desempenho em build (só medido no editor), inclusive o 8x (PRG-012).
- Edição do PNG do mapa em editor de imagem externo (PRG-005).
- Troca do tempo pela tecla C / HUD e ritmo da chuva em jogo real (PRG-013).
