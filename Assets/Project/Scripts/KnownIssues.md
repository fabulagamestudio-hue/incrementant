# Known Issues — Incrementant

Bugs, dívidas técnicas e correções relevantes. Cada item cita o guia de sistema afetado. Estado da produção e solicitações ficam em [Production/](../../../Production/PRODUCTION_STATUS.md), não aqui.

## Abertos

| ID | Resumo | Guia afetado | Desde |
|---|---|---|---|
| KI-001 | Render redesenha e reenvia a textura inteira da janela visível todo frame. Desde 2026-10-17 a janela cobre só a vista da câmera (custo limitado pelo zoom máximo, ~1,3 ms no editor), mas ainda sem regiões sujas. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-004 | Mapa de caminhabilidade é recalculado inteiro na primeira montagem dos campos e em cada bloco tocado pela expansão do mundo: ~14 ms (editor) uma vez com o mundo inteiro gerado de uma só vez; expansões normais da câmera custam proporcional à faixa nova. Pode dar um soluço ao revelar áreas grandes de uma vez. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-003 | Formigas se sobrepõem (sem colisão entre elas) e o sprite (3x2 células) cobre parte das paredes em túneis de 1 célula. Aceitável no protótipo. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-011 | Pixel de planta solto que cai fora do alcance das formigas mantém um slot da sala de nutrientes reservado (`PixelGrid.PlantDebrisCount`) até sumir e pode travar a colheita. | `Runtime/AntFarm/AntFarmGuide.md` (Colheita em duas etapas) | 2026-10-17 |

## Abertos (save/load)

| ID | Resumo | Guia afetado | Desde |
|---|---|---|---|
| KI-005 | Save/load tem um único slot (save rápido), sem autosave, e não guarda a posição da câmera nem gotas de chuva no ar. Ao carregar, o sorteio das formigas recomeça da seed. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-007 | Água sem pressão: poças só se nivelam até `HorizontalReach` (64) células; não há vasos comunicantes (água não sobe pelo outro lado de um U). Formigas não desviam da água (a navegação a ignora) e se afogam se o caminho passar por túnel inundado ou fundo de laguinho. Durante a chuva forte a água fica alguns segundos no túnel antes de ser consumida (Erosion Seconds), então ainda dá para afogar. Contato da terra com a água não vai para o save (erosão recomeça a contar). Saves da versão 1 são recusados. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-008 | A rainha usa os quadros das operárias escalados 2x (sem arte própria) e, sem sala da rainha pintada, fica parada no ninho. O teste `Colony_FillsBuildMarksFromTheDeepestPointOutward_UsingDirtFromOutside` é sensível à sequência aleatória: mudar o que a colônia sorteia antes das formigas já o fez falhar (a rainha deixou de sortear o lado ao nascer por isso). | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-009 | A água só é consumida dentro do formigueiro (sem infiltração nem evaporação na superfície). Com chuva forte prolongada, a água que não desce para os túneis se acumula na superfície do formigário fechado; antes da infiltração, uma rodada de teste inundou a superfície em poucos minutos. Falta ver em jogo real se a chance de gota virar água (10%) basta. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-010 | Plantas e nutrientes (protótipo): a formiga com planta na mão espera indefinidamente se a sala de nutrientes for apagada ou ficar sem caminho; o nutriente coberto por terra se perde; com nutrientes guardados, até metade das formigas larga a escavação para buscá-los (valor fixo em `AntColony.MaxFetchers`); a fase do crescimento das plantas recomeça ao carregar um save; touceiras vizinhas podem se sobrepor (a célula em comum é de uma só); cada planta tem a própria textura (~50 a 170 KB cada), refeita só quando uma célula dela muda; uma touceira inteira rende ~300 a 420 nutrientes e leva minutos para ser colhida com poucas formigas. Dois campos de navegação a mais (~8 MB no mundo 960x520). Jardineiras: há uma planta-alvo por vez (todas as jardineiras vão a ela), a colônia começa com `ColonySettings.InitialGardeners` (1) jardineira e as novas vêm da política da rainha (ver KI-012); nada disso foi visto em Play Mode por uma pessoa. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-012 | Criação de formigas (versão mínima): faltam envelhecimento e morte, a recuperação de 5 comuns, penalidade de superlotação, game over, lote de até 3 ovos (Q2), compras B0 a B3 (o berçário só precisa ser pintado e escavado), pincel pago, a reserva inicial de 100 N, cuidadora e soldado, e sprites das crias (só contadores). As células por vaga (`CellsPerAdultSlot` 24, `CellsPerNurserySlot` 16) são uma escala do protótipo, não os 256 do design. A capacidade usa a área alcançável do formigueiro, então a areia caindo e o despejo a mudam e uma cria já reservada pode fazer as adultas passarem do limite sem consequência. Nada disso foi exercitado por uma pessoa em Play Mode. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-006 | Construir não cobra terra do estoque (só consome a pilha de fora), e a formiga pode trancar outras que estão atrás da célula preenchida num beco sem saída; só uma marca de escavação as liberta. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |

## Resolvidos

| ID | Resumo | Correção | Data |
|---|---|---|---|
| KI-002 | Simulação granular e BFS custavam a área gerada inteira a cada passo (77 ms por frame no 8x com 960x520 no editor), limitando o 8x ao FPS. | Blocos 16x16 que dormem quando estáveis (`GranularSimulation` pula blocos e faixas paradas, resultado idêntico à varredura completa), cache de caminhabilidade por bloco (`WalkabilityMap`), BFS que limpa só o alcançado e origens do campo de escavação buscadas pelo lado caminhável. Medido no editor: 2,1 ms por frame no 8x com 366 formigas e o mundo inteiro; Play Mode 102 FPS no 8x × 114 no 1x. | 2026-10-17 |
