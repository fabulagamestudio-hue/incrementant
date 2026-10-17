# Known Issues — Incrementant

Bugs, dívidas técnicas e correções relevantes. Cada item cita o guia de sistema afetado. Estado da produção e solicitações ficam em [Production/](../../../Production/PRODUCTION_STATUS.md), não aqui.

## Abertos

| ID | Resumo | Guia afetado | Desde |
|---|---|---|---|
| KI-001 | Render redesenha e reenvia a textura inteira da janela visível todo frame. Desde 2026-10-17 a janela cobre só a vista da câmera (custo limitado pelo zoom máximo, ~1,3 ms no editor), mas ainda sem regiões sujas. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-004 | Mapa de caminhabilidade é recalculado inteiro na primeira montagem dos campos e em cada bloco tocado pela expansão do mundo: ~14 ms (editor) uma vez com o mundo inteiro gerado de uma só vez; expansões normais da câmera custam proporcional à faixa nova. Pode dar um soluço ao revelar áreas grandes de uma vez. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |
| KI-003 | Formigas se sobrepõem (sem colisão entre elas) e o sprite (3x2 células) cobre parte das paredes em túneis de 1 célula. Aceitável no protótipo. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) | 2026-10-17 |

## Resolvidos

| ID | Resumo | Correção | Data |
|---|---|---|---|
| KI-002 | Simulação granular e BFS custavam a área gerada inteira a cada passo (77 ms por frame no 8x com 960x520 no editor), limitando o 8x ao FPS. | Blocos 16x16 que dormem quando estáveis (`GranularSimulation` pula blocos e faixas paradas, resultado idêntico à varredura completa), cache de caminhabilidade por bloco (`WalkabilityMap`), BFS que limpa só o alcançado e origens do campo de escavação buscadas pelo lado caminhável. Medido no editor: 2,1 ms por frame no 8x com 366 formigas e o mundo inteiro; Play Mode 102 FPS no 8x × 114 no 1x. | 2026-10-17 |
