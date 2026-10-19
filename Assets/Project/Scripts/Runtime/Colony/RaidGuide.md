# Guia — Ataques de cupins, combate e área de resíduos

Pedido do desenvolvedor em 2026-10-18 (REQ-029). Código em `Colony/AntColony.Raids.cs` (ataques, cupins, combate, corpos) e `Colony/AntColony.Waste.cs` (área de resíduos); números em `Colony/RaidSettings.cs` (campo **Raid Settings** do `AntFarmGame`); o cupim em `Colony/Termite.cs`. Onde fica cada arquivo: [Runtime/README.md](../README.md).

## Quando vem um ataque

- **Liberação.** Com **Termite Raids** ligado no `AntFarmGame` (`AntColony.RaidsEnabled`; desligado por padrão na colônia, então os testes antigos não veem ataques), quando a colônia chega uma vez a **Min Ants** (15) formigas adultas, `RaidsUnlocked` fica ligado para sempre (vai para o save) e o relógio começa.
- **Mínimo de formigas (2026-10-18).** Com as formigas abaixo de **Min Ants**, em qualquer momento, o relógio e o alerta ficam parados e nenhum ataque vem; voltam quando a colônia retorna ao mínimo. O F4 ignora isso.
- **Intervalo.** Entre um ataque e o próximo, um tempo sorteado entre **Min/Max Interval Minutes** (1 a 5 minutos de jogo), contado do fim do anterior. Minuto de jogo é tempo real a 1x: o relógio desconta `deltaTime / GameSpeed` (`AntColony.GameSpeed`, posto pelo `AntFarmGame` a cada tick), então 3x e 8x não encurtam o intervalo e a pausa não o consome. O alerta (8 s) e o resto do ataque seguem na escala da simulação. O sorteio usa um gerador próprio (`_raidRng`, da seed do mapa), para não mudar a sequência aleatória das formigas.
- **Alerta.** Quando o tempo acaba, `RaidState.Warning` dura **Warning Seconds** (8 s) e o HUD mostra, no centro do topo, "ATAQUE DE CUPINS IMINENTE! CHEGAM EM N S".
- **Quantos cupins (pedido do desenvolvedor em 2026-10-18).** No fim do alerta (`LaunchRaid`) a fração de cupins por formiga adulta é sorteada entre o mínimo (**Termites Per Ant**, 40%) e o teto do momento (`AntColony.TermiteCap` = `RaidSettings.TermiteCap`): o teto começa no mínimo e sobe **Termites Per Ant Growth** (5 pontos) a cada ataque que termina (`RaidsSurvived`), até **Max Termites Per Ant** (100%). O primeiro ataque vem sempre com 40%; o teto chega a 100% no 12º ataque (reduzido em 2026-10-18: antes subia 20 pontos até 200%). Vêm `ceil(formigas × fração)` cupins, pelo menos 1 (`LastRaidShare` guarda a fração sorteada; não vai para o save, o teto vem de `RaidsSurvived`, que vai).
- **Chegada.** Cada um sorteia uma coluna da área ativa e pousa na primeira célula vazia de cima para baixo com algo sólido embaixo (chão, monte, planta, obra; sem água). Desce voando por **Flight Seconds** (3 s ±20%) a partir de **Flight Height** (40) células acima, com os quadros `Ant_Fly_*`; no chão toca `Ant_Land_*` (as asas caem) por **Landing Seconds** e passa a andar com os quadros sem asas. Durante o voo a célula já é a do pouso: o voo é só desenho.
- **Cheat F4** (Editor e builds de desenvolvimento): `AntColony.ForceRaid()` começa o alerta agora, mesmo com menos de 15 formigas.

## O que os cupins fazem

Todos seguem o mesmo objetivo (`RaidObjective`), nesta ordem:

1. **Crias:** ovos no chão e crias nos pontos do berçário (a que está na boca de uma formiga não conta). Cada uma leva **Destroy Seconds** (0,8 s) e sai de `BroodCycle` (`Remove`): a vaga é liberada, o alimento não volta.
2. **Canteiros:** cada canteiro da sala de nutrientes vira entulho.
3. **Setores:** todo bloco sólido **dentro** da área pintada do berçário, da sala de nutrientes e da área de resíduos (terra, areia, obra; não rocha, canteiro, resíduo nem entulho), `CollectSectorTargets`, montado uma vez quando a etapa começa. Pedido do desenvolvedor em 2026-10-18: com a sala pintada maior que a parte escavada, o que ficava dentro da pintura não era alvo de nada; agora cai inteiro antes das paredes, garantindo que nada da área pintada fica de pé. Cada bloco vira entulho, como as paredes.
4. **Paredes:** as células sólidas do formigueiro a até **Wall Layers** (2) células de qualquer célula **pintada** do berçário, da sala de nutrientes ou da área de resíduos, escavada ou não (pedido do desenvolvedor em 2026-10-18: com o setor maior que a parte aberta, a parede é a de fora da área pintada; antes contava só a parte escavada) (`CollectWallTargets`, montado uma vez quando o objetivo começa, no máximo 20 000 células). Fica de fora o que está a até 2 células da sala da rainha, rocha, rocha-mãe, canteiro e resíduo. A segunda camada só fica alcançável quando a primeira cai. A parede derrubada vira entulho no lugar, e no chão e nas laterais ele fica apoiado (o do teto cai e se amontoa nos cantos do chão); por isso o cupim alcança uma parede-alvo também através de até 8 células de entulho encostadas (`CollectWallStands`, `ReachableWall`, no máximo 256 células varridas por parede). Antes (corrigido em 2026-10-18) a segunda camada ficava sem célula livre encostada, o campo não tinha origem e os cupins vagavam até desistir.
5. **Rainha** (`RaidObjective.Queen`, só com **Hunt Queen** ligado em Raid Settings): depois das paredes, vão atrás da rainha por qualquer caminho (cavam túnel se preciso) e a mordem. Pedido do desenvolvedor em 2026-10-18.
6. **Saída:** vão às duas colunas de céu junto de cada lateral da área ativa e somem ali.

O objetivo passa ao próximo quando não tem mais alvo, quando nenhum cupim progride por **Stall Seconds** (30 s: alvo inalcançável). Progredir é estar voando ou pousando, dar um passo que aproxima do objetivo, destruir ou lutar (`_raidProgress`); até 2026-10-18 o tempo corria desde o começo do ataque e só zerava ao destruir algo, então cupins que pousavam longe viam crias, canteiros e paredes vencerem no caminho e iam embora sem atacar ou, para a saída, quando o ataque passa de **Max Raid Seconds** (300 s). Na saída, quem não acha caminho em **Stall Seconds** vai embora voando (some). O ataque termina quando não sobra cupim vivo; os corpos ficam.

**Entulho.** Bloco de setor, parede e canteiro destruídos viram `CellMaterial.Rubble` (terra fofa granular, cai), **sem marca**: os cupins não marcam nada, só destroem o que está no caminho. O jogador marca o entulho com o pincel de cavar para as formigas o recolherem pelo fluxo normal de escavação e o levarem à área de resíduos (ou, sem ela, aos canteiros e ao despejo, como terra; `FitsLoad` não mistura entulho com outra terra).

**Navegação.** Um campo (`_raidField`) com origem no objetivo atual. **Sem caminho livre até o objetivo, o cupim cava um** (pedido do desenvolvedor em 2026-10-18): `PlanTunnel` acha a rota mais barata até encostar num alvo (para a saída, até a coluna de saída), andando (custo 1) ou cavando terra escavável, inclusive entulho e terra das salas (custo `TunnelDigCost` 12, então prefere túneis existentes), por Dijkstra com baldes, até 60 000 células por planejamento e 4 planejamentos por passo; o caminho fica no corpo (`Ant.AddSeekStep`) e `TunnelStep` anda ou cava célula a célula (**Destroy Seconds** cada). A terra do caminho vira túnel e some (`TunnelCellsDug`), sem deixar entulho na passagem; só o alvo do objetivo vira entulho. Rocha e rocha-mãe não são cavadas. Andar, cavar o caminho e destruir contam como progresso para o **Stall Seconds**. O cupim reaproveita o corpo de `Ant` (`Termite.Body`, nunca em `Ants`) e os mesmos passos das formigas (`StepDownhill`, `Wander`, `UpdateSupport`), a **Termite Speed** (15 células/s, menos que a operária, como pede o design). Sem caminho, vaga.

## Rainha, game over e dedo (pedido do desenvolvedor em 2026-10-18)

- **Vida e dano da rainha.** Ela tem **Queen Health** (60) de vida (`Queen.Wounds`, recupera 1/s depois de **Heal Delay Seconds** sem mordida, como as formigas) e morde com **3×** o dano do soldado (**Queen Damage Factor**; 9 por mordida com o soldado base). Não é uma formiga da lista: `AntColony.Queen`.
- **Game over.** Os cupins mordem a rainha quando ela está ao alcance e nenhuma formiga está mais perto (as formigas servem de escudo). Vida a zero = `AntColony.GameOver`: `Tick` para, `AntFarmGame` mostra a tela GAME OVER (IMGUI, `DrawGameOver`: carregar o último save ou novo mapa), bloqueia o pincel e para o relógio da partida. Não vai para o save.
- **Postura** (`AntColony.QueenStance`, botões no menu Rainha, vai para o save, versão 14). **Defensiva** (padrão): só morde de volta o cupim que a mordeu há pouco. **Ofensiva:** com cupim dentro do formigueiro (`_nestIntruders`), sai da sala atrás do invasor mais perto (`HuntIntruder`, mesmo campo do combate) e morde qualquer cupim ao alcance; depois volta à sala pelo caminho de sempre.
- **Dedo.** Sétimo pincel do menu Pintar: o clique esquerdo fere (**Finger Damage**, 2) o cupim vivo mais perto num raio de **Finger Reach** (2) células; não pinta nem desmarca.

## Combate

- **Quem caça (pedido do desenvolvedor em 2026-10-18).** Com cupim na superfície, só o soldado larga o trabalho e desce o campo do combate até o mais perto; as outras só se defendem quando um cupim as morde e continua ao alcance (`IsUnderAttack`), e voltam ao trabalho sem cupim ao alcance. **Com cupim dentro do formigueiro** (vivo, no chão, abaixo da superfície: `IsInsideNest`, `_nestIntruders`), **todas as formigas** largam o trabalho e caçam os invasores: o campo do combate passa a apontar só para os de dentro (`CollectCombatSources`) até não restar nenhum; depois voltam ao trabalho (os da superfície voltam a ser só do soldado). Sem soldado, quem cruza o caminho dos cupins na superfície ainda se defende.
- **Interrupção.** Quem entra em luta (`InterruptForCombat`, estado `AntState.Fighting`): ovo ou cria na boca volta ao chão, o nutriente que ia à rainha fica onde ela está, o bloco de água se perde e a carga na mão fica (é entregue depois). O soldado desce o campo do combate (`_combatField`, as células em volta de cada cupim no chão) até ficar a **Attack Reach** (2) células de um cupim.
- **Mordidas.** A cada **Attack Seconds** (1 s), formiga e cupim mordem quem estiver ao alcance. Dano e vida da tabela 15 do design: produtivas 1/s e vida 6 (carregadora e escavadora 8); soldado 3/s e vida 18 (S1: 4,5/s; S2: vida 27; `QueenTree.Damage`/`MaxHealth`). Cupim: vida 10, mordida de 2. O cupim com formiga ao alcance para e luta em vez de seguir o objetivo.
- **Morte.** A formiga que chega à vida do papel morre (`RemoveCasualties`, `AntsKilledInCombat`) **mesmo com a morte desligada (F3)**: decisão do desenvolvedor em 2026-10-18. A vida (`Ant.Wounds`) recupera 1 ponto/s depois de **Heal Delay Seconds** (10 s) sem levar mordida; não vai para o save. A rainha não luta nem é atacada.
- Sem cupim no chão, quem lutava volta a ficar ociosa e retoma o trabalho.

## Corpos: folhas e restos

O cupim morto (`TermitePhase.Dead`) fica no chão com **Corpse Leaves** (2) folhas e **Corpse Remains** (1) resto. A comum, a carregadora e o soldado ociosos (`AntRoles.ClearsCorpses`; prioridade logo antes de buscar nutriente) vão até ele (`AntState.Dismembering`, campo do combate apontando para os corpos), trabalham **Dismember Seconds** (1 s) e saem com uma parte:

- **Folha** (`PlantLeaf` na boca) só se algum canteiro espera planta (`CanHarvest`): vai ao canteiro como um pedaço de planta colhido (conta em `HarvestedPixels`).
- **Resto** (`CellMaterial.Refuse` na boca): vai à área de resíduos; sem área, ao despejo como terra (nunca vira canteiro).

Folhas sem canteiro esperando ficam no corpo até abrir vaga. O corpo some quando não sobra parte.

## Área de resíduos

- **Liberação.** O pincel **Resíduos** (6º do HUD, tecla B) fica bloqueado até o primeiro ataque terminar (`AntColony.WasteUnlocked`, `AntFarmGame.IsBrushUnlocked`). O mesmo fim de ataque liga `QueenTree.SoldiersAvailable`, que abre o ramo do soldado (S0 a S2; S3, a contenção, segue EM BREVE).
- **Pintura.** Camada `PixelGrid.SetWasteRoom`, como o berçário: só no formigueiro, nunca sobre outra sala. Muda `MarkVersion`. Desenho em `AntFarmView` com **Waste Room Color**.
- **Depósito.** Entulho e restos na mão vão à área (`TryStartWaste` em `StartDelivery`, antes dos canteiros e do despejo; estado `CarryingWaste`, campo `_wasteField`). A formiga larga cada pixel num ponto livre vizinho (`IsWasteSpot`: célula da área, vazia, apoiada em algo sólido embaixo, caminhável e sem fechar passagem, `KeepsPassage`), que vira `Refuse` (fixo). Sem ponto ou sem caminho, entrega como terra.
- **Decomposição.** Cada célula de resíduo some sozinha depois de **Waste Decay Seconds** (120 s, o R0 do design). Ao carregar um save, o tempo recomeça cheio para todo `Refuse` do mapa.

## Tempestades (pedido do desenvolvedor em 2026-10-18)

A cada período sorteado de 1 a 10 minutos de jogo (`RainSimulation`, `RainSettings.Storm*`) há **50%** de chance (`StormChance`, valor assumido) de uma chuva forte de **1 minuto**. Os minutos são de jogo, como o intervalo dos ataques: não encurtam a 3x/8x (`Step(grid, dt, gameSpeed)`). Mudar o tempo pela tecla C cancela a tempestade e sorteia outro período; o fundo do menu não sorteia.

- **Cupins morrem.** `AntColony.Raining` (o `AntFarmGame` liga a cada passo quando o tempo é `HeavyRain`, de tempestade ou da tecla C) faz `DrownTermites` matar todo cupim vivo, voando, no chão ou dentro do ninho; o corpo fica para as formigas e conta em `TermitesKilled`. Chuva leve não mata.
- **Ataque espera.** Com chuva forte, o alerta e a chegada não começam (o relógio fica zerado até o céu abrir); o F4 ignora a espera, mas os cupins morrem no passo seguinte.
- Save versão 15 guarda o relógio da tempestade (`SecondsToStormRoll`, `StormRemaining`).

## Save (versão 13)

Guarda `RaidsUnlocked`, `RaidsSurvived` e o tempo até o próximo ataque, e a área de resíduos como bit 1 do bloco do berçário (bit 0 = berçário). O ataque em curso, os cupins e os corpos **não** vão: ao carregar volta sem cupins; um save feito durante o alerta ou o ataque volta com o próximo alerta na hora. Saves antigos voltam sem área de resíduos e com os ataques ainda não liberados.

## Desenho

`AntSwarmView.RenderTermites` (chamado pelo `AntFarmGame` depois das formigas): voo (`_termiteFly`, desacelerando até o pouso), pouso (`_termiteLand`, tocado uma vez), caminhada com o mesmo giro e vista de cima das formigas (`_termiteWalk`/`_termiteTop`), corpo de barriga para cima e apagado (**Termite Corpse Color**) e um piscar vermelho ao levar mordida (**Termite Hit Color**). Ordem de desenho acima das formigas. Os quadros vêm de `Art/Ants/InimigaAlada/` e são ligados por **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins** (também pelo setup da cena); sem eles, os cupins existem mas não aparecem. Formiga mordendo ou desmembrando anima como quem trabalha, virada para o alvo (`Ant.Biting`).

HUD: alerta no topo central (`AntFarmHud._raidAlert`): contagem do aviso, "CUPINS ATACANDO: N · objetivo" e, por 6 s, o fim do ataque (o primeiro avisa que liberou o soldado e a área de resíduos). Ver [HudGuide](../UI/HudGuide.md). Painel F2: linha "Cupins". Painel F11: etapas "cupins/resíduos" (campos) e "cupins/combate".

## Como estender

- **Outro inimigo:** um `TermitePhase`/objetivo novo no fim dos enums; o corpo continua sendo um `Ant` fora de `Ants`.
- **Números:** todos em `RaidSettings` (Inspector). Para variar em jogo, leve-os para estado da colônia em vez de mudar o asset serializado.
- **Mais setores atacados:** acrescente a camada em `IsSector` e o contador por bloco em `CollectWallTargets`.

## Testes

`Tests/EditMode/RaidTests.cs`: com o setor pintado maior que a parte escavada, os cupins derrubam todo o interior pintado e só depois as paredes de fora, inteiras; cupim que pousa longe ainda destrói canteiro e paredes; as duas camadas em volta de uma sala fechada caem inteiras, com e sem física granular; início só com 15 formigas, alerta antes, 40% de cupins voando; só o soldado caça e as demais seguem trabalhando; a comum mordida se defende e volta ao trabalho; teto de 40% subindo 5 pontos por ataque até 100%; cupim dentro do formigueiro faz todas caçarem e voltarem ao trabalho; só o soldado caça cupim na superfície, com o sorteio dentro dele; o alerta do F4 não é trocado pelo intervalo quando a colônia chega a 15 formigas durante ele (bug achado no Play Mode); ataques desligados; ordem crias → canteiros → paredes (sala da rainha poupada) → saída e liberação do soldado e da área; formigas matam o cupim e o corpo vira folhas (canteiros) e resto (área de resíduos), que se decompõe; morte em combate com a morte desligada; save da área e do estado dos ataques. Limitações: [KnownIssues.md](../../KnownIssues.md) (KI-018).
