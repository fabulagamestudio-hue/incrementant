# Guia — Fazenda de pulgões, fazendeira e depósitos de areia e de cogumelos

Pedido do desenvolvedor em 2026-10-22. Código em `Colony/AntColony.Farm.cs` (baias, nascimento, ordenha, fazendeira, cogumelo e pulgão preto) e `Colony/Aphid.cs` (o pulgão); a camada pintada em `World/PixelGrid.cs` (`SetFarmRoom`, `IsFarmRoom`); os itens e os depósitos de areia e de cogumelos em `World/PixelGrid.Storage.cs` (`StorageKind.Sand`, `StorageKind.Mushroom`, `StoredItems.Sand`, `StoredItems.Mushroom`). Números em `ColonySettings` (seção "Fazenda de pulgões"). Testes: `Tests/EditMode/FarmTests.cs` e o teste do cogumelo em `GlowMushroomTests.cs`.

## Decisões do desenvolvedor (2026-10-22)

- **Origem:** a fazenda cria os pulgões sozinha, um por vez, até o limite.
- **Campo de areia e água:** comida e bebida. Cada pulgão tem uma baia com um cocho de areia e um de água lado a lado; a fazendeira os enche.
- **Cogumelo luminoso:** insumo do pincel. A ordenha às vezes rende um cogumelo, guardado como item; o pincel Cogumelo luminoso passou a gastar esse cogumelo (a terra não serve mais de substrato).
- **Pulgão preto:** inimigo até morrer.
- **Depósitos:** um de areia e um de cogumelos, separados do depósito de construção (resposta do desenvolvedor no mesmo dia, depois da primeira entrega, que guardava o cogumelo no depósito de construção).

## O que o jogador vê

- **Pincel Fazenda de pulgões** (menu Colônia → Ambientes, último antes de DEPÓSITO; índice 12 de `AntFarmGame.BrushLabels`). Apagado e sem clique até comprar a câmara **A0** em Evolução (`AntFarmGame.IsBrushUnlocked`, `AntColony.FarmUnlocked`). Pinta como as outras salas: só no formigueiro, nunca sobre outra sala ou depósito. Limite de área `ColonySettings.FarmRoomCells` (120) + **A+3** (`PaintRoom.Farm`).
- **Pincel Depósito de areia** (submenu DEPÓSITO, 4º; índice 13). Livre desde o início (nó **D6** do catálogo, inicial). Limite de área próprio, o mesmo dos outros depósitos + D+3 (`PaintRoom.SandStore`).
- **Pincel Depósito de cogumelos** (submenu DEPÓSITO, 5º; índice 14). Livre desde o início (nó **D7**, inicial). Limite de área próprio, como os outros depósitos (`PaintRoom.MushroomStore`).
- **Fazendeira** (`AntRole.Farmer`, nó **Z0** no ramo da jardineira): só pode ser comprada com a fazenda (A0). Arte em `Art/Ants/Fazendeira/` (rampa marrom 9-10-11 da paleta mestra, mesmos masters das outras classes).
- Pulgões desenhados com `Art/Aphids` (`AntSwarmView.RenderAphids`); o pulgão preto com a mesma arte, tingida (**Black Aphid Color**), entre os cupins.

## Baias

`CollectStalls` relê o chão da fazenda quando a pintura (`MarkVersion`) ou o terreno dos blocos da fazenda mudam. Chão = célula da fazenda livre, caminhável, sem nutriente e com algo sólido embaixo (`IsFarmFloor`). Cada trecho reto de chão é cortado da esquerda para a direita em baias de `AntColony.StallWidth` (4) células: **cocho de areia** (x), **cocho de água** (x+1) e o **lugar do pulgão** (x+2; o sprite tem 2,5 células). Sobra menor que 4 no fim do trecho não vira baia.

**Baias com pulgão são fixas**: na releitura, a baia de cada pulgão é mantida antes de cortar o resto, enquanto as 4 células dela continuarem chão (`IsFreeStallSpot`); o corte do resto pula as células já usadas (`LastStallCell`). Assim os cochos não mudam de lugar quando o chão muda ao lado. Pulgão cuja baia sumiu não come nem produz até ganhar outra livre (`NearestFreeStall`, a mais perto dele). A releitura também guarda chão, paredes e teto da fazenda (`_farmSurface`, `IsFarmSurface`: caminhável e encostado em algo sólido), por onde os pulgões passeiam.

## Passeio e comida

Pedido do desenvolvedor em 2026-10-22: o pulgão passeia pela fazenda e só vai aos cochos para comer (`UpdateAphid`).

- **Passeio** (`StrollAphid`): sorteia uma célula de `_farmSurface` (chão, parede ou teto), anda até ela a **Aphid Speed** (4 células/s) sem sair da fazenda (`StepAphid`, que prefere passos encostados em algo sólido: sobe pela parede em vez de cortar pelo fundo do túnel; o desenho gira o sprite de lado na parede e usa o de cima no fundo, como nas formigas), para de um terço a 100% de **Aphid Rest Seconds** (4 s) e sorteia outra. Passeia também enquanto é ordenhado: a fazendeira o acompanha.
- **Fome** (`Aphid.Fed`): com fome e os dois cochos cheios, vai até o lugar dele na baia (x+2, virado para os cochos), fica **Aphid Eat Seconds** (2 s) comendo e esvazia os dois cochos (`Eat`). Alimentado, volta a passear. A ordenha o deixa com fome de novo. A fome vai para o save (versão 26).

Os itens das células das baias (`_stallCells`) são presos: não são itens soltos e não recebem o que a ordenha solta.

## Nascimento

Com vaga no limite e baia livre (`CanBreedAphid`), o relógio conta **Aphid Breed Seconds** (60 s; **A1** corta pela metade) e o pulgão nasce na primeira baia livre. Limite = **Aphid Base Cap** (2) + **A+1** (+1 por nível, teto 10).

## Produção

A cada passo, para cada pulgão com baia (`UpdateAphid`), onde ele estiver:

- **Ordenha:** com a fazendeira ao lado (`TendingAphid`, `Ant.Interacting`) e **o pulgão alimentado**, o relógio corre a `Tree.MilkRate` (Z1: ×1,5). Ao chegar a **Aphid Milk Seconds** (20 s) × `Effects.MilkTime` (**A+2**, −5% por nível), o pulgão fica com fome e solta **Milk Sand** (2) areias, **Milk Leaves** (1) folha e **Milk Water** (2) águas, e com **Milk Mushroom Chance** (25%, sorteio próprio `FarmRng`) um cogumelo luminoso.
- **Sem ordenha:** a cada **Aphid Drip Seconds** (40 s) solta 1 água, sem comer nada.
- Tudo sai como **item guardado** nas células livres e caminháveis da fazenda mais perto do pulgão (`FindFarmSpots`, busca pela fazenda de até 800 células). Sem lugar para todos os itens da colheita, nenhum sai e o pulgão espera.

Destino do que sai (regra dos itens soltos, ver [StorageGuide](StorageGuide.md)):

| Item | Com depósito do tipo | Sem ele |
|---|---|---|
| Areia | Depósito de areia | Fica na fazenda (vira comida dos cochos); fora da fazenda, despejo |
| Folha | Depósito de alimentos | Canteiros que esperam planta; senão fica |
| Água | Depósito de água | Canteiros que esperam água; senão fica (a fazendeira também a usa nos cochos) |
| Cogumelo | Depósito de cogumelos (com vaga) | Fica onde está; o pincel o busca ali (nunca vai ao depósito de construção nem ao despejo) |

## Fazendeira

Ordem do papel (`TryFarmWork`, em `TryTaskWork` logo depois da lixeira): (1) **encher cocho vazio** (`TryStartTroughFill`: areia antes de água; reserva o cocho mais perto sem dona, vai pegar o item em `TakingStored` com `StoreUse.FarmSand`/`FarmWater` pelos campos `_farmSandField`/`_farmWaterField` — qualquer areia ou água guardada ou solta fora dos cochos e da conversão dos resíduos — e leva em `FeedingAphid`); (2) **ordenhar** um pulgão sem fazendeira (`TryStartAphidCare`, primeiro os alimentados ou de cochos cheios (`CanMilk`), depois o mais perto; `TendingAphid` com `Ant.EggId` = id do pulgão; ela segue o pulgão pelo passeio). Ela larga a ordenha quando o pulgão dela está com fome, tem cocho vazio e há item que ninguém foi buscar, e quando o sino toca. Sem trabalho, faz o da comum (segunda função); `RoleWorkWaiting` a devolve ao papel quando a fazenda volta a ter trabalho (`FarmWorkWaiting`). Velocidade 22 (como a jardineira) × **Z+1**; custo de cria 24; peso no automático 5.

Contas do passo: `ResetFarmCounts` (cochos vazios, fazendeira de cada pulgão) e `CountFarm` (quem enche cada tipo, cochos com dona, quem leva cogumelo). Nada disso vai para o save: ao carregar, as fazendeiras voltam ociosas.

## Cogumelo luminoso

A marca do pincel continua sendo uma marca de obra do tipo cogumelo, mas fica fora da obra de terra: `BuildConstructionFields` põe as marcas de cogumelo num campo próprio (`_mushroomSiteField`) e a ordem de profundidade de uma não segura a outra. A comum (ou quem faz o trabalho dela) com marca esperando e cogumelo ao alcance vai buscá-lo (`TryStartMushroomWork`, seção Construção, `StoreUse.Mushroom`), sobe ao campo das marcas (`UpdateHaulingMushroom`) e o encaixa (`TryStartPlace`/`FinishPlace` só casam cogumelo com marca de cogumelo). O cogumelo vem do depósito de cogumelos (`StorageKind.Mushroom`) ou de onde a ordenha o deixou. Sem marca, ele volta ao depósito de cogumelos ou fica no chão (`ReturnMushroom`); nunca vai ao despejo. Na mão, ele vai sozinho (`FitsLoad`).

## Depósitos de areia e de cogumelos

**Cogumelos** (`StorageKind.Mushroom`, 6): o cogumelo solto (fora do depósito dele) só é item solto com o depósito de cogumelos pintado e com vaga (`IsFarmHeld`); aí as formigas o levam para lá. Sem isso, fica onde está, à espera de uma marca do pincel.

**Areia** (`StorageKind.Sand`, 5): areia cavada (`StartDelivery`, com o depósito pintado; com ele, a mão não mistura areia com terra), areia solta da fazenda e a que a área de resíduos solta no modo terra → areia e água vão para ele. Sem ele, a areia vai ao despejo como antes (a da fazenda fica lá).

## Cupins e o pulgão preto

- **Objetivo novo** `RaidObjective.Aphids`, logo depois das crias: o campo dos cupins aponta para os pulgões saudáveis (`CollectAphidSources`). Em qualquer objetivo, o cupim com pulgão ao alcance da mordida (e sem formiga ao alcance) o morde.
- **A mordida deixa o pulgão preto** (`CorruptAphid`): ele sai da fazenda e vira um `Termite` com `Aphid = true`, vida **Black Aphid Health** (12), dano **Black Aphid Damage** (1,5 por mordida, a cada Attack Seconds) e velocidade **Black Aphid Speed** (8). Usa o combate dos cupins: as formigas o caçam (dentro do formigueiro, todas), as cuspideiras cospem nele, ele conta no alerta e nas marcas vermelhas.
- **Comportamento** (`UpdateBlackAphid`): morde a formiga (ou a rainha) ao alcance; senão anda até a formiga mais perto a até 16 células; senão vaga. Não segue o objetivo, não vai embora na saída do ataque e a chuva forte não o mata. O ataque só termina quando ele morre. Morto, deixa 1 folha e 1 resto (`AphidsKilled`).

## Save (versões 24 e 26)

Fazenda no bit 5 do bloco `NurseryRoom`; pulgões saudáveis (célula) e o relógio do próximo (`Aphids`, `AphidBreedTimer`); desde a 26, a fome de cada um (`AphidFed`, um bool por pulgão no fim; saves antigos voltam com todos com fome). A baia não vai: ao carregar, cada pulgão fica com a livre mais perto dele. Depósitos de areia (5) e de cogumelos (6) e os itens areia e cogumelo cabem no formato da versão 17. O pulgão preto, como os cupins, não vai.

## Suposições do agente (reversíveis; ver `ColonySettings` e o catálogo)

Baia de 4 células; pulgão a 4 células/s, pausa de até 4 s entre passeios e 2 s comendo; limite 2 + 1 por nível; nascimento a cada 60 s; água a cada 40 s; ordenha de 20 s com 2 areias, 1 folha, 2 águas e 25% de cogumelo; pulgão preto com 12 de vida, 1,5 de dano e 8 células/s. Custos: A0 150, A1 300, A+1 120, A+2 100, A+3 60, Z0 200, Z1 260, Z+1 100. A fazenda fica no ramo Exterior da aba Formigueiro e a fazendeira no ramo da jardineira. Ícones provisórios (formiga para a fazenda, caixa para o depósito de areia). Ícone provisório do depósito de cogumelos: o cogumelo.

## Como estender

- Números: `ColonySettings` (Inspector do `AntFarmGame`). Bônus: `UpgradeEffects` (A0, A1, A+1, A+2, A+3, Z+1) e `QueenTree.MilkRate` (Z1).
- Outro produto da ordenha: some em `Milk`/`DropFarmItems` e decida o destino em `IsFarmHeld`/`LooseHasDestination`.
- Outra classe que trabalhe na fazenda: `AntRoles.Farms`.
