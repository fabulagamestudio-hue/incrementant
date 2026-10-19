# Guia — Sala Gotas de Orvalho

Pedido do desenvolvedor em 2026-10-18 (REQ-032). Código em `Colony/AntColony.Dew.cs` (etapas, gota, terra e folha), ganchos em `AntColony.Cultivation.cs` (terra fofa, planta), `AntColony.cs` (ociosa, tick, demanda de planta), `World/PixelGrid.cs` (`SetDewRoom`), `World/GranularSimulation.cs` (terra presa) e `World/WaterSimulation.cs` (água não absorvida). Números em `ColonySettings` (seção "Sala Gotas de Orvalho"). Testes: `Tests/EditMode/DewRoomTests.cs`.

## O que o jogador vê

Pincel **Gotas de orvalho** (menu Colônia → Ambientes, 5º botão; índice 7 de `AntFarmGame.BrushLabels`, no fim para não mudar os outros). A sala só pinta células do formigueiro e nunca sobre outra sala. Operárias comuns e jardineiras trabalham nela (`AntRoles.WorksDew`). A linha "Gotas de orvalho" do painel IMGUI de debug mostra a etapa.

## Etapas (derivadas da grade a cada rodada de campos; nada disso vai para o save)

1. **Terra fofa.** Em cada coluna pintada com pelo menos **Dew Min Column Height** (4) células, as de cima (**Dew Soil Share**, 30%, arredondado para cima, no mínimo 1) recebem `Spoil`, de cima para baixo (a célula precisa ter algo sólido em cima). Célula já sólida (parede natural) conta como coberta. A terra vem da pilha de fora (`FetchingSoil`/`GatheringSoil`, mesmo campo dos canteiros) ou da terra que as escavadoras entregam quando não há ponto de canteiro (`TryStartBedSoil`). Canteiros têm prioridade sobre a sala nos dois caminhos. `Ant.DewJob` marca a carga que é da sala; as formigas com a carga da sala contam em `_dewCarriers`, limitadas a uma por **Dew Spots Per Worker** (8) pontos.
2. **Folha** (só com a terra toda posta). Uma `PlantLeaf` logo abaixo da terra, nas colunas de x par. Entra na demanda de planta (`PlantDemand`, que soma os canteiros que esperam planta): as cortadeiras, comuns e (pela ordem de ociosidade) jardineiras cortam e levam a folha pelo mesmo campo `_storeField`; `TryStoreDewLeaf` a pendura.
3. **Gota.** Com as duas etapas sem pontos pendentes e pelo menos uma célula coberta, a cada **Dew Drop Seconds** (8 s) cai uma célula de água de uma coluna sorteada, na célula livre logo abaixo da terra (ou da folha). Com **Dew Max Room Water** (24) células de água na sala, a gota espera o próximo intervalo.
4. **Água não absorvida.** `WaterSimulation.Check` pula a água em célula da sala: ela não vira lama nem é consumida pela parede. Fica para as formigas regarem os canteiros (`TryStartWaterFetch`).

## Jardineiras aceleram a queda (pedido do desenvolvedor em 2026-10-18)

Com a sala pronta e nada mais a fazer (depois de cuidar dos canteiros, regar, levar terra e cortar folha), a **jardineira** ociosa entra no estado `AntState.TendingDew`: anda pelo campo `_dewTendField` (fontes = `_dewDropCells`, a queda das gotas) e fica ao lado dela (`Ant.Interacting`). Cada jardineira ali soma `DewTendBoost` (1 = +100%) ao ritmo do relógio da gota em `TickDew`; no máximo `DewMaxTenders` (3) ao mesmo tempo. Sai (volta a ociosa) se a sala deixa de estar pronta, se a queda some, se o campo não alcança ou se os canteiros voltam a pedir jardineira (`WantsTender`). Operária e outros papéis não cuidam da sala pronta. O estado não vai para o save (volta ociosa).

## Equilíbrio com a sala de nutrientes (2026-10-18)

Terra fofa e planta são pedidas pelos canteiros e pela sala. Quando as duas pedem, o recurso vai para a que tem **menos peças postas**; empate vai para os canteiros.

- **Terra** (`AntColony.ChooseSoilRoom`, em `Cultivation`): compara `_dewSoilPlaced + _dewCarriers` (terra fofa já posta no teto e formigas a caminho) com `BedCount + _soilCarriers`. Vale para quem leva terra cavada (`TryStartBedSoil`) e para quem busca na pilha (`TryStartSoilFetch`). `WantedDewCarriers` e o limite de carregadores dos canteiros seguem valendo.
- **Planta** (`BuildNutrientFields`): os pontos de folha ficam em `_dewStoreSources`; só um dos dois conjuntos de destino entra no campo da planta (`_storeField`): o da sala se `_dewLeavesPlaced < ` canteiros plantados ou brotando, senão o dos canteiros. Se um lado não tem ponto alcançável, o outro recebe. O campo é refeito a cada planta posta, então a "próxima planta" já usa a contagem nova.
- Não há cota por tempo: uma sala sem demanda (sem ponto livre) nunca segura o recurso da outra.

## Mudou a forma da sala (2026-10-18)

A sala é relida a cada rodada; o que as formigas puseram e a forma atual não quer mais é reaproveitado, e a sala não solta gota enquanto houver sobra (`_dewStale`).

- **Terra fofa fora da faixa** (sala cresceu para cima, encolheu, coluna ficou curta demais): `CollectDewStale` a põe em `_reclaimSoil`; `SyncReclaimMarks` (em `Cultivation`) a marca para escavar e as formigas a cavam como qualquer marca. A terra cavada segue `StartDelivery`: canteiro novo ou teto da sala, se pedirem, senão despejo. Marcas postas pela colônia saem sozinhas quando a terra deixa de ser sobra.
- **Folha fora do lugar** (sala cresceu para baixo e a folha ficou dentro da faixa de terra, ou fora da célula certa): `ReleaseStaleDewLeaves` a troca por pixel solto marcado (`FallenLeaf`), que as formigas levam ao ponto de folha certo.
- Apagar a sala inteira não recolhe as folhas (plantas fixas, sem marca de cavar); a terra solta cai.

## Regras de manutenção

- `Spoil` dentro da sala não cai (`GranularSimulation`), nem o que veio de fora ou foi cavado; só a terra posta por formiga importa na prática.
- Ponto só vale se for caminhável, alcançável do campo de casa e sem fechar a passagem (`KeepsPassage`), como os canteiros.
- Salvar: a sala é o bit 2 do byte `NurseryRoom` do `GameSnapshot` (sem mudar a versão do save; saves antigos têm 0). A terra e a folha são células comuns; o relógio da gota recomeça ao carregar.

## Suposições do agente (reversíveis; ver `ColonySettings`)

Altura mínima de coluna 4; 30% por coluna; folha em colunas pares, uma por coluna; gota de 1 célula; X = 8 s; teto de 24 células de água; ícone provisório do botão (folha). Cupins ainda não tratam a sala como setor (não a destroem).
