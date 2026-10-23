# Guia — Minérios, fundição, forja e equipamento

Pedido do desenvolvedor em 2026-10-23 (REQ-071). Desenho e números: [documento 45](../../../../../Docs/01_Design/45_Minerios_fundicao_forja_e_equipamento.md). Mundo horizontal (areal e fruteira): [BiomesGuide](../World/BiomesGuide.md).

Código:
- `World/OreVeins.cs`: veios, `OreSettings`/`OreBand` em **Terrain Settings → Minérios em veios**.
- `World/Equipment.cs`: metal, tipo, efeitos e durabilidade.
- `CellMaterial`: valores 21 a 35.
- `StorageKind`: `Ore`, `Smelter`, `Forge`, `Armory`, `Wardrobe`.
- `Colony/AntColony.Workshop.cs`: transformação, logística e equipamento.
- Ganchos em:
  - `AntColony.Storage.cs`: usos, itens soltos e pegar;
  - `AntColony.cs`: entrega, tarefa da ociosa, tempo de cavar, remoção;
  - `AntColony.Raids.cs`: dano dado e recebido, cuspe;
  - `AntColony.Farm.cs`: mordida do pulgão preto.

Testes: `Tests/EditMode/OreTests.cs`, `WorkshopTests.cs` e `LooseOreFallTests.cs`.

## O que o jogador vê

- **Pincéis bloqueados até comprar a câmara em Evolução** (2026-10-23, documento 46, etapa 4): depósito de minério (**V0**), fundição (**V1**), forja (**Y0**), armazém de ferramentas e armas (**Y1**) e armazém de roupas e armaduras (**Y2**), nos níveis 4 e 5 do formigueiro. Sala já pintada num save continua funcionando; só o pincel fica bloqueado (`AntFarmGame.IsBrushUnlocked`, `AntColony.OreStoreUnlocked`/`SmelterUnlocked`/`ForgeUnlocked`/`ArmoryUnlocked`/`WardrobeUnlocked`, lidos de `UpgradeEffects`).
- **Galerias:** **V+1** encurta o tempo da barra e **Y+1** o do equipamento (`Effects.SmeltTime`, `ForgeTime`, aplicados em `TickSmelter`/`TickForge`); **Y+2** aumenta a durabilidade do equipamento novo (`EquipDurability`, em `TryPickEquip`); **E+4**, **S+5** e **O+4** reforçam ferramenta, arma e armadura (`SharpToolTime`, `WeaponDamageBoost`, `ArmorTakenBoost`, em `ToolFactor`, `WeaponFactor` e `ArmorFactor`). Sem compra, todos os fatores valem 1.

- **Veios:** cavando para baixo, aparecem bolsões de minério de uns 10 a 15 células, em três metais:
  - cobre (laranja), raso;
  - ferro (vermelho-escuro), no meio;
  - ouro (amarelo), do meio para baixo, mais denso perto do fundo;
  - abaixo da faixa deles, cobre e ferro ainda aparecem, em bolsões menores e raros.
- **Minério cavado:** vai à **Fundição**, se ela pede; senão ao **Depósito de minério**. Sem nenhum dos dois, fica no chão onde foi cavado. Nunca vai ao despejo.
- **Minério solto:** aparece como um torrão de pedra com pepitas do metal, o mesmo desenho que a formiga leva na boca. Fora do depósito de minério e da fundição ele cai até o chão e faz monte; dentro deles fica onde a formiga o pôs, inclusive na parede de fundo.
- **Fundição** (Ambientes): cada minério com uma serapilheira na sala vira, em 20 s, uma barra do mesmo metal.
- **Forja** (Ambientes): duas barras do mesmo metal viram, em 30 s, o equipamento que mais falta na colônia.
- **Armazéns** (submenu DEPÓSITO):
  - **Armazém de ferramentas e armas**;
  - **Armazém de roupas e armaduras**.
- **Equipamento:**
  - a escavadora e a operária comum buscam no armazém uma **ferramenta** e cavam mais rápido;
  - o soldado e a cuspideira buscam **arma** e **armadura**.
- **Desgaste:** o equipamento gasta com o uso e volta como 1 barra no chão, que vai à forja.

## Como funciona

### Veios (`OreVeins`)

- Sorteados de uma vez para o mundo, no construtor do `TerrainGenerator`, com gerador próprio. A profundidade é contada a partir do relevo, depois dos laguinhos.
- Cada bolsão é uma caminhada curta, como o `TileRunner` do Terraria (`Pocket`):
  - começa numa coluna sorteada, a uma profundidade dentro da faixa; **Deep Bias** puxa o sorteio para o fundo (o ouro usa 1);
  - anda **Length** passos de 1 célula e vira até **Turn** (0,6 rad) por passo;
  - ao sair da faixa, inverte a direção vertical;
  - a cada passo carimba um disco de borda roída ao acaso (`Stamp`), com raio sorteado em **Radius** que afina até a metade no último passo.
- **Resto do mapa:** **Scatter Per 1000 Columns** bolsões a 3/4 do raio, do fim da faixa (`Depth.y`) até o fundo. Sem efeito com `Depth.y` 0 (o ouro já vai até o fundo).
- Os valores da cena ficam em **Terrain Settings → Minérios em veios** do `AntFarmGame`; mudar o padrão no código não muda a cena.
- Em `CellAt`, o veio substitui só terra e areia; pedra, água e casca de bolsão ficam como estão.
- Quantidades e faixas por metal: tabela do documento 45 (`OreBand`: **Depth**, **Veins Per 1000 Columns**, **Length**, **Radius**, **Deep Bias**, **Scatter Per 1000 Columns**).
- Materiais:
  - fixos e escaváveis sem E2 (0,5 / 0,8 / 1,1 s);
  - família de junção e borda da pedra (ids 21 a 23 no shader);
  - linhas 21 a 23 do atlas.

### Minério solto (`World/LooseOreFall.cs`)

Pedido do desenvolvedor em 2026-10-23: "adicionar física quando não estiverem no depósito ou em uso".

- **Quem cai** (`LooseOreFall.Falls`): item de minério guardado numa célula que não é do depósito de minério nem da fundição. Na mão da formiga não é item da grade; no depósito e na fundição (em uso) fica parado. Barras, equipamentos e os outros itens não caem.
- **Como:** a cada **Seconds Per Cell** (0,06 s; **Ore Fall Settings** no `AntFarmGame`), uma rodada de baixo para cima desce cada minério 1 célula para o vão sem item. Em cima de outro item, escorrega para a diagonal livre (lado alternado por rodada), como a areia: o monte se espalha antes de empilhar. Só percorre os blocos com item (`GetChunkStoredCount`) e nada faz sem minério guardado (`StoredCount(Ore)`); no máximo 4 rodadas por chamada.
- **Onde roda:** `AntFarmGame`, logo depois da queda dos grãos (`_oreFall.Step`).
- **Formigas:** cada passo de queda muda `StoreVersion`, então o campo dos itens soltos é refeito e quem ia buscar o minério segue para onde ele parou.
- **Arte:** `Art/Ants/Item_*Ore` (8 × 8), o mesmo sprite na boca (`AntSwarmView` → **Ore Sprites**) e na grade (`AntFarmView` → **Ore Item Sprites**; AntFarmGuide, "Minério com sprite próprio").
- Teste: `Tests/EditMode/LooseOreFallTests.cs`.

### Salas como depósitos

- A fundição e a forja são `StorageKind` pintados como depósitos (`SetStorageRoom`, limite de área dos depósitos, `PaintRoom.Smelter`/`Forge`). Cada célula guarda um item.
- **O que entra e o que sai de cada sala:**

  | Sala | Entrada | Saída |
  |---|---|---|
  | Fundição | minério e serapilheira | barra |
  | Forja | barra | ferramenta, arma ou armadura |

- **Itens soltos:**
  - minério e serapilheira dentro da fundição não são itens soltos (`IsWorkshopInput`);
  - a barra pertence à forja (`KindOf` → `Forge`): na fundição, ela é solta e vai à forja;
  - o equipamento pertence aos armazéns (`Armory` para ferramenta e arma, `Wardrobe` para armadura).
- **Itens retidos** (`IsHeldUntilDepot`: néctar, minério, barras e equipamentos):
  - só viram item solto quando o depósito do tipo tem vaga;
  - a vaga também limita quantas formigas saem para buscá-los (`HeldLooseRoom`);
  - na mão, vão sozinhos (`FitsLoad`; minério só com minério do mesmo metal);
  - seguem `ReturnHeldItem`: depósito do tipo, senão o chão.

### Logística

- **Entrega** (`StartDelivery` → `TryStartSmelterDelivery`): o minério e a serapilheira na mão vão primeiro à fundição, se ela pede.
  - Ela pede minério enquanto há menos minério, contando o que está a caminho, que metade das células.
  - Pede serapilheira enquanto há menos combustível que minério + 1.
  - Senão, o minério vai ao depósito de minério (ou ao chão) e a serapilheira ao depósito dela (ou como terra).
- **Abastecimento** (`TryStartWorkshopSupply`, `StoreUse.SmelterOre`/`SmelterFuel`): a carregadora (no fim de `TryStockWork`) e a comum (depois da obra) buscam nos depósitos de minério e de serapilheira o que a fundição pede.
- **Contas:** `CountWorkshop` conta, a cada passo, quem leva minério ou serapilheira à fundição, quem vai vestir e quem leva equipamento ao armazém.

### Transformação (`TickWorkshops`, no `Tick` da colônia)

- **Leitura das salas:** `ScanWorkshops` relê as salas quando `StoreVersion` muda: minérios e combustíveis da fundição, barras por metal e equipamentos prontos na forja.
- **Fundição:** cada minério, na ordem, com um combustível disponível, ganha um relógio (`SmeltSeconds`). Ao fim, o combustível some e o minério vira a barra do metal na mesma célula.
- **Forja:**
  - escolhe o tipo que mais falta (`MostWantedEquip`): formigas sem o item, menos o que está no armazém, na forja ou a caminho;
  - usa o melhor metal com 2 barras;
  - ao fim de `ForgeSeconds`, a primeira barra vira o item e a segunda some;
  - sem falta, as barras esperam.
- **Relógios:** não vão para o save; recomeçam ao carregar.

### Equipamento

- **Na formiga:** `Ant.Tool`, `Weapon` e `Armor` (material) e `ToolUses`, `WeaponUses` e `ArmorUses`. Não vão para o save, como os ferimentos.
- **Quem usa** (`UsesEquip`):
  - ferramenta: escavadora e comum (pelo papel, não pela função do momento);
  - arma e armadura: guerreiras.
- **Vestir:** `TryStartEquip`, no começo de `TryTaskWork`, antes de tudo menos o resgate. Cada tipo tem um campo próprio até os itens dele (`BuildEquipFields`); a pegada veste (`TryPickEquip`) em vez de carregar.
- **Efeitos e desgaste:**
  - ferramenta: multiplica o tempo de cavar em `WorkTimeFactor` e gasta 1 uso por célula cavada (`WearTool`, planta e rocha não gastam);
  - arma: multiplica a mordida e o cuspe (`SpitShot.DamageFactor`) e gasta 1 uso por golpe;
  - armadura: multiplica a mordida do cupim e a do pulgão preto e gasta 1 uso por golpe recebido.
- **Gasto:** 1 barra do metal cai no chão (`ScrapEquip`).
- **Morte:** a formiga que morre deixa o que vestia no chão (`DropEquipment`, em `RemoveAnt`).

## Atlas

- Linhas 24 a 35 (`metal_good` em `terrain_atlas.py`): barra, ferramenta, arma e armadura, cada uma em cobre, ferro e ouro. São provisórias e só aparecem no bloco carregado e no item guardado.
- A parede de fundo foi para a 36.
- Salas construídas da fundição, da forja e dos dois armazéns: famílias próprias nas linhas 51 a 58 (59 linhas, 64 × 3776 no 8x8); ver [BuiltRoomsGuide](BuiltRoomsGuide.md).
- No chão ou na parede de fundo, o item guardado aparece com a arte da linha dele (AntFarmGuide, "Itens guardados com a arte do atlas").

## Pendências

- Os números são suposições do agente (documento 45).
- Ícones da HUD: os botões do depósito de minério, da fundição, da forja e dos dois armazéns usam `Art/UI/Sprites/icons/equip_*` (40 × 40, gerados por `Art/UI/Source~/hud_equip_icons.py`; índices 17 a 21 de `brushIcons` em `AntFarmHudSetup`). Não há ícone para ferramenta, arma e armadura soltas: a HUD não mostra esses itens.
- Sprite da formiga equipada: as camadas existem em `Art/Ants/Equipamento` (ver [README de Art/Ants](../../../Art/Ants/README.md)), e o `AntSwarmView` as desenha sobre a formiga que veste o item (`Ant.Tool`/`Weapon`/`Armor`). Falta só o ícone da HUD. O equipamento solto ou na boca usa os sprites `Art/Ants/Item_<Metal><Tipo>` (8 × 8), não as linhas 27 a 35 do atlas.
- O equipamento não vai para o save.
- Os custos e efeitos dos nós da árvore são suposições do agente (documento 46).
