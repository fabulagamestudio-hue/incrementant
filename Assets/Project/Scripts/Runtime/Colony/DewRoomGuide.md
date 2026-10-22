# Guia — Sala Gotas de Orvalho

Pedido do desenvolvedor em 2026-10-18 (REQ-032). Código em `Colony/AntColony.Dew.cs` (etapas, gota, terra e folha), ganchos em `AntColony.Cultivation.cs` (terra fofa, planta), `AntColony.cs` (ociosa, tick, demanda de planta), `World/PixelGrid.cs` (`SetDewRoom`), `World/GranularSimulation.cs` (terra presa) e `World/WaterSimulation.cs` (água não absorvida). Números em `ColonySettings` (seção "Sala Gotas de Orvalho"). Testes: `Tests/EditMode/DewRoomTests.cs`.

## O que o jogador vê

Pincel **Gotas de orvalho** (menu Colônia → Ambientes, 5º botão; índice 7 de `AntFarmGame.BrushLabels`, no fim para não mudar os outros). A sala só pinta células do formigueiro e nunca sobre outra sala. Operárias comuns e jardineiras trabalham nela (`AntRoles.WorksDew`). A linha "Gotas de orvalho" do painel IMGUI de debug mostra a etapa.

## Etapas (derivadas da grade a cada rodada de campos; nada disso vai para o save)

1. **Terra fofa.** Em cada coluna pintada com pelo menos **Dew Min Column Height** (4) células, as de cima (**Dew Soil Share**, 30%, arredondado para cima, no mínimo 1) recebem `Spoil`, de cima para baixo (a célula precisa ter algo sólido em cima). Célula já sólida (parede natural) conta como coberta. A terra vem da pilha de fora (`FetchingSoil`/`GatheringSoil`, mesmo campo dos canteiros) ou da terra que as escavadoras entregam quando não há ponto de canteiro (`TryStartBedSoil`). Canteiros têm prioridade sobre a sala nos dois caminhos. `Ant.DewJob` marca a carga que é da sala; as formigas com a carga da sala contam em `_dewCarriers`, limitadas a **uma por ponto livre** (`WantedDewCarriers` = pontos de terra). Chegando com a terra (`UpdateBringingDewSoil`), cada uma traça o caminho pelo campo da terra e **reserva o ponto** (`Ant.SeekTarget`, recontado a cada Tick em `_dewSoilClaims`), como a escavação reserva o pixel; o ponto reservado sai do campo `_dewSoilField` na rodada seguinte e as outras vão a outro (pedido do desenvolvedor em 2026-10-19: antes, uma formiga a cada 8 pontos e todas no mesmo ponto, então a sala era coberta de uma em uma). A configuração `Dew Spots Per Worker` saiu do `ColonySettings`.
2. **Folha** (só com a terra toda posta). Uma `PlantLeaf` logo abaixo da terra, nas colunas de x par. Entra na demanda de planta (`PlantDemand`, que soma os canteiros que esperam planta): as cortadeiras, comuns e (pela ordem de ociosidade) jardineiras cortam e levam a folha pelo mesmo campo `_storeField`; `TryStoreDewLeaf` a pendura.
3. **Gota.** Com as duas etapas sem pontos pendentes, pelo menos uma célula coberta e a seção **Gotas de Orvalho** ligada no painel da colônia (`DewDripping`; desligada, a sala continua pronta, mas não pinga nem mostra barra, KI-052), a cada **Dew Drop Seconds** (24 s desde 2026-10-21, a pedido do desenvolvedor: 3× mais lento que os 8 s anteriores; 4 s e 8 s antes, KI-034 e KI-050) **cada ponto de queda** (um por coluna) solta uma célula de água, na célula livre logo abaixo da terra (ou da folha), então o tamanho da sala conta como o dos canteiros (2026-10-20; antes caía uma só por intervalo, para a sala toda). O G1 dobra por ponto (a segunda cai na célula livre de baixo). O teto é **Dew Water Per Height** (3) × a altura da coluna mais alta da sala (`_dewMaxHeight`) × as colunas com ponto de queda (`_dewDropColumns`, contando as já alagadas), mais o bônus do G+2 (2026-10-20, KI-049: antes não contava a largura, e a sala larga enchia em dois intervalos e parava mesmo com o depósito de água cheio). Por cima de tudo vale o **teto vertical** (KI-050): `Dew Max Water Share` (40%) da altura de cada coluna, somado em `_dewWaterCeiling` (piso por coluna); `TickDew` usa o menor entre os dois tetos, então o bônus do G+2 nunca passa dos 40% e a água não chega à entrada nem escorre para os túneis. Atingido o teto, a gota espera as formigas levarem a água. Com esse teto de células de água na sala, as gotas do intervalo param ao chegar no teto e, já cheia, esperam o próximo. Só conta a água alcançável de casa (`CountDewWater`): água em bolsão fechado da sala não é levada por ninguém nem aparece, então não pode travar a sala (KI-047).
4. **Água não absorvida.** `WaterSimulation.Check` pula a água em célula da sala: ela não vira lama nem é consumida pela parede. Fica para as formigas regarem os canteiros (`TryStartWaterFetch`).

## Jardineiras aceleram a queda (pedido do desenvolvedor em 2026-10-18)

Com a sala pronta e nada mais a fazer (depois de cuidar dos canteiros, regar, levar terra e cortar folha), a **jardineira** ociosa entra no estado `AntState.TendingDew`. Mesma ideia da sala de nutrientes: a sala é dividida em **blocos** de `DewPointsPerTender` (4) pontos de queda vizinhos (`_dewDropCells`, em ordem de x) e chama **uma jardineira por bloco**; cada uma cuida de vários pontos. Ela escolhe um bloco que outra ainda não escolheu (`_dewTendTargets`, chave = bloco + 1 em `Ant.EggId`; evita o último que cuidou), anda até o ponto do meio dele (`WalkToCell` com `_dewTendField`) e fica ao lado (`Ant.Interacting`). O relógio da gota em `TickDew` anda a `1 + DewTendBoost × (pontos com jardineira ÷ pontos de queda)`: sala toda coberta soma `DewTendBoost` (1 = +100%), cobertura parcial soma a parte proporcional. Sai (volta a ociosa) se a sala deixa de estar pronta, se a queda some, se o campo não alcança ou se os canteiros voltam a pedir jardineira e a seção Jardim do painel tem vaga para ela (`GardenCanTake`; sem a vaga, ela saía do orvalho, a sala de nutrientes a recusava e ela ficava parada indo e voltando, relato de 2026-10-19); se o bloco some (sala encolheu), escolhe outro. Operária e outros papéis não cuidam da sala pronta. O estado não vai para o save (volta ociosa).

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
- Reserva de ponto de terra: a mudança das reservas (`DewClaimsChanged`) pede uma rodada de campos, como as reservas da escavação; `BuildDewSpots` grava a reserva já vista no começo (inclusive sem sala), senão a rodada se repetiria para sempre.
- Ponto só vale se for caminhável, alcançável do campo de casa e sem fechar a passagem (`KeepsPassage`), como os canteiros.
- Salvar: a sala é o bit 2 do byte `NurseryRoom` do `GameSnapshot` (sem mudar a versão do save; saves antigos têm 0). A terra e a folha são células comuns; o relógio da gota recomeça ao carregar.

## Suposições do agente (reversíveis; ver `ColonySettings`)

Altura mínima de coluna 4; 30% por coluna; folha em colunas pares, uma por coluna; gota de 1 célula por ponto de queda; X = 4 s; teto de 3 células de água por célula de altura da sala; ícone provisório do botão (folha). Cupins ainda não tratam a sala como setor (não a destroem).
