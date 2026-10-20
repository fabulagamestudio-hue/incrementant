# Guia — Depósitos (alimentos, construção e água)

Pedido do desenvolvedor em 2026-10-19. Código em `Colony/AntColony.Storage.cs` (campos, levar, pegar, itens soltos), a camada em `World/PixelGrid.Storage.cs` (`StorageKind`, `StoredItems`, `SetStorageRoom`, `SetStored`) e ganchos em `AntColony.cs` (entrega, obra, ociosa), `AntColony.Cultivation.cs` (terra fofa, planta, água) e `AntColony.Waste.cs`. Testes: `Tests/EditMode/StorageTests.cs`.

## O que o jogador vê

Menu Colônia → Ambientes → **DEPÓSITO** abre um submenu ao lado com três pincéis: **Depósito de alimentos**, **Depósito de construção** e **Depósito de água** (índices 8, 9 e 10 de `AntFarmGame.BrushLabels`). Pintam como as outras salas: só no formigueiro (no solo original ou abaixo), nunca sobre outra sala nem sobre outro depósito; o botão direito apaga só o depósito do pincel escolhido. Cada célula vazia do depósito guarda **um item**, desenhado com a cor do material (água em azul) por cima da tinta da sala.

## Item guardado (não bloqueia a passagem)

O item é uma camada à parte, como o nutriente: a célula continua vazia para estrutura, física, água e navegação, então as formigas passam por cima e o depósito enche inteiro, inclusive no meio da sala. Valores em `StoredItems`: o material como byte (planta, `Spoil`, `Refuse`, `TermiteRemains`) ou `StoredItems.Water`. Muda `PixelGrid.StoreVersion`, que refaz os campos.

- Material sólido entrando na célula (grão caindo, obra, canteiro) **destrói** o item (`LoseStoredAt`), como faz com o nutriente.
- Nutriente, canteiro e item não dividem célula (`SetStored`, `SetNutrient`, `FreeBedSides`, `IsBedSpot`).

## Regra de consumo

Com o depósito do tipo pintado, quem consome **só usa o que está guardado nele**; o que vem de fora vai primeiro para ele. Sem o depósito, tudo funciona como antes.

| Tipo | Quem enche | Quem consome (pega do depósito) | Sem depósito |
|---|---|---|---|
| Alimentos | Corte de plantas e pedaços soltos (`StartDelivery` → `TryStartStock`); a colheita passa a ser limitada pelas vagas (`PlantSupplyDemand`) | Canteiros esperando planta e folhas da sala Gotas de Orvalho (`TryStartPlantFromDepot`, `StoreUse.Plant`) | Planta vai direto aos canteiros |
| Construção | Terra cavada (só a que vira terra fofa: sem areia, rocha, planta nem resíduo; guardada como `Spoil`) | Obra (`TryBuildWork` → `StoreUse.Build`), canteiro novo e teto da sala Gotas de Orvalho (`TryStartSoilFetch` → `StoreUse.Soil`) | Terra vai a canteiro/despejo; obra e canteiros usam a pilha de fora |
| Água | Água do mapa: laguinho, poças, sala Gotas de Orvalho (`TryStartWaterStock`, `FetchingWater` com `Ant.StoreTarget`) | Canteiros com planta esperando água (`TryStartWaterFromDepot`, `StoreUse.Water`) | Água vai direto ao canteiro |

- **Depósito cheio:** quem traz desiste (`AbandonStock`): planta vai aos canteiros, terra ao despejo, água ao canteiro (ou volta ao mapa). A colheita para sozinha (vagas zeradas).
- **Sobra na mão:** terra que sobrou de obra ou canteiro (`UpdateHauling`, `SendSoilToDump`), planta sem canteiro alcançável (`UpdateStoring`) e água sem canteiro na sala voltam ao depósito do tipo.
- **Pilha de fora vai ao depósito de construção:** enquanto o depósito de construção tem vaga sem ninguém a caminho, a ociosa busca terra solta da pilha de fora (terra despejada ou lama acima do solo original, inclusive a despejada antes de o depósito existir) e a guarda nele (`TryStartRestock`, `BlockRestockWanted`; o campo da pilha passa a ser montado com vaga livre). Não depende de haver obra ou canteiro pedindo terra. Na ordem da ociosa vem depois dos itens soltos e antes do corte de plantas. O consumo continua saindo só do depósito; com ele cheio, terra cavada vai ao despejo e volta quando abrir vaga.

## Itens soltos

Item fora do depósito do tipo dele (depósito apagado ou repintado, folhas do resto de cupim na área de resíduos) é **solto**. As comuns e carregadoras ociosas o levam (`TryStartLoose`, `StoreUse.Loose`, depois da obra) ao destino de sempre: o depósito do tipo, se houver vaga, senão o caminho normal (planta aos canteiros, terra ao despejo, água ao canteiro). Só entra no campo (`_looseField`) o que tem destino agora. Quantas formigas saem para buscar itens soltos é limitado pelo que o destino ainda aceita (`LooseWanted`/`LooseRoom`): vagas do depósito menos quem já vai para ele, ou o pedido dos canteiros e das folhas da sala Gotas de Orvalho menos o que já está na mão ou reservado; terra não tem limite (o despejo aceita tudo). Antes, cada item solto chamava uma formiga: com o depósito de alimentos quase cheio e ~475 folhas soltas, ~180 formigas pegavam folha sem ter onde pô-la (relato de 2026-10-19). Planta solta conta junto dos pedaços no chão (`PlantsWaiting`) para a colheita não cortar além do necessário. Resíduo (`StorageKind.Waste`) nunca é solto: se decompõe onde está.

## Contas e campos

- Por tipo, dois campos (lazy, como o da área de resíduos): até as vagas livres (`_putFields`) e até os itens guardados (`_takeFields`), refeitos na etapa 5 da rodada, antes do cultivo (`BuildStorageFields`). Vagas e itens (`DepotFree`, `DepotStock`) valem da última rodada; quem leva (`_storeIncoming`) e quem busca (`_storeOutgoing`) são recontados a cada `Tick` (`CountStorage`) e somados na hora em que a formiga aceita.
- `Ant.StoreTarget` desvia `Carrying`/`CarryingWater` para `UpdateStocking`; `AntState.TakingStored` + `Ant.StoreUse` é a ida até o item; `Ant.FromStore` marca a planta que saiu do depósito (vai aos canteiros, conta em `_plantsToBeds`). Nada disso vai para o save: ao carregar, `TakingStored` volta ociosa.
- Seções do painel da colônia: levar planta = coleta; buscar planta do depósito = coleta; terra do depósito para canteiro = sala de nutrientes (ou gotas de orvalho); para obra = construção; água para regar = sala de nutrientes; **encher o depósito de água com a água do mapa não é de seção nenhuma** (pedido do desenvolvedor em 2026-10-19: com o Jardim no limite, ninguém enchia o depósito e ~150 formigas ficavam sem trabalho), então não conta no limite nem é parada pelo Jardim desligado, e o painel a mostra em "Enchendo dep. água" (`ColonyCensus.WaterStocking`); reposição e entrega de terra cavada = escavação; itens soltos = coleta.

## Save

Versão 17 de `GameSnapshot`: `StorageRoom` (tipo pintado por célula), `Stored` e `StoredShades`. Saves anteriores voltam sem depósitos; o resíduo antigo (material sólido) vira item ao carregar (`RescanWaste`).

## Suposições do agente (reversíveis)

Um item por célula; areia e rocha cavadas não entram no depósito de construção (vão ao despejo, como antes); item é destruído por material sólido; ícones provisórios (folha, caixa, seta para baixo); a pilha de fora enche o depósito de construção até a última vaga (correção do desenvolvedor em 2026-10-19: antes só repunha com ele vazio). Cupins ainda não tratam os depósitos como setor.
