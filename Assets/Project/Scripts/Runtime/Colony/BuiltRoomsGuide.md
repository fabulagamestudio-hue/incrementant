# Guia — Salas construídas (REQ-058)

Pedido do desenvolvedor: quando uma sala é construída, o visual dela muda; as formigas põem terra nos slots do ambiente. Decisões de 2026-10-22: um visual por família (os cinco depósitos dividem um), bloco e fundo por família, slots nas paredes de terra **em contato com o vão** da sala, **só visual** e o fundo aparece **com a sala pronta**. Na primeira versão (mesmo dia) o teto e a terra dentro da área pintada ficavam naturais; o desenvolvedor pediu, depois de jogar, para converter também as paredes de dentro e todas as que encostam no ambiente.

Código: `Colony/AntColony.Lining.cs` (slots, sala pronta, reboco e a classe `RoomLining`); camadas na grade em `World/PixelGrid.cs` (`GetLining`/`SetLining`, `IsRoomFinished`/`SetRoomFinished`); desenho em `AntFarm/AntFarmView.cs` (`ComputeCell`) e no shader `Materials/TerrainTiles.shader`; arte e linhas do atlas em `Art/Terrain/README.md` e `AntFarm/CellTileArt.cs` (`RoomStyle`, `StyleOf`, `RoomBlockRow`, `RoomBackRow`). Testes: `Tests/EditMode/RoomLiningTests.cs`.

## Slots

A partir de cada célula **vazia** (o vão) de uma sala pintada, são slots as 8 vizinhas (piso, teto, lados e quinas; `AntNavigation.Dx`/`Dy`) que:

- são terra que dá para rebocar (`RoomLining.CanLine`: terra, grama, areia, terra despejada, lama e as compactadas; pedra, rocha-mãe, plantas, restos e cogumelo ficam como estão);
- estão no formigueiro (no solo original ou abaixo dele);
- não têm marca de escavação (vão virar túnel);
- dentro de uma área pintada (parede de dentro: terra pintada que não foi cavada, ou de outra sala encostada), não são terra fofa (`Spoil`) nem canteiro: a terra fofa ali é o canteiro ou o teto da sala Gotas de Orvalho, posta de propósito pelas formigas.

Célula vazia em volta da sala (boca de túnel) nunca é slot. Só a camada que encosta no vão é slot: depois de rebocada, a terra atrás dela não encosta no vão. Como o slot já é sólido, rebocar não fecha passagem nem tira células vazias da sala: berçário, depósitos, vagas de adulta e o resto contam o mesmo vão de antes.

## Obra

O reboco é obra (seção **Obra** do painel da colônia), pelo mesmo caminho das marcas de construção:

1. `UpdateRoomLining` (rodada de campos, etapa da obra) refaz `_liningSlots` quando a pintura (`MarkVersion`), a estrutura ou o reboco (`LiningVersion`) mudam.
2. `AddLiningSources` (dentro de `BuildConstructionFields`) põe no campo da obra as células caminháveis ligadas à saída ao lado de cada slot; o slot já reservado só entra na fila de espera. Conta os slots alcançáveis (`_liningReachable`). **Prioridade:** construção primeiro (decisão do desenvolvedor em 2026-10-22, depois de, em jogo, as salas não serem rebocadas quando o reboco esperava as automatizações). A obra marcada e o reboco das salas são escolhas pontuais do jogador e dividem o mesmo campo, sem ordem entre si. A comum (e quem age como ela) tenta construir logo depois do socorro às soterradas, antes do cuidado das salas, do ovo, da rega, da comida da rainha, de cavar e do abastecimento automático (`TryTaskWork`, passo 22); a escavadora cava antes de construir, e cuidadora e jardineira ficam no papel delas. O reboco não espera os canteiros, o teto do orvalho, a área de resíduos nem o depósito de construção encher: com depósito, usa a terra guardada nele.
3. A obra pedida (`BuildWorkCount` = marcas de construção + slots alcançáveis) vale para aceitar o pedido (`TryBuildWork`), a demanda da seção Obra, o depósito de construção (`ExtraWanted`) e o campo da pilha com depósito.
4. A formiga pega a terra como na obra (pilha solta de fora, ou o depósito de construção se houver), leva até o slot e o reserva (`TryStartPlace`). Em `FinishPlace`, alvo sem marca de construção é reboco (`FinishLining`): a célula vira o compactado do que ela levava (`CellMaterials.ToPacked`) e recebe o reboco do ambiente (`LiningKindAt`: o da própria célula, se ela for parede de dentro da área pintada, ou o do vão que encosta nela).

O reboco some quando o material da célula muda (`PixelGrid.Set`): cavada, ou terra que vira lama encostada na água. O slot volta a pedir terra.

## Sala pronta

`ScanRoomPart` percorre cada trecho conectado de um ambiente (vizinhança de 4 lados, como o limite de pintura). O trecho fica pronto quando tem vão, nenhum slot sem reboco e nenhuma célula com marca de escavação. As células dele recebem `SetRoomFinished` (estado derivado, refeito a cada rodada que muda algo; não vai para o save).

## Desenho

- **Bloco:** célula com reboco grava no R de `_CellMaterials` a linha `RoomBlockRow` da família e, como cor, a da terra com o tom da célula (a cor de referência da linha). Sem Tile Material, a célula mostra a cor do material, como antes.
- **Fundo:** célula vazia de sala pronta grava `RoomBackRow`. O shader trata essas linhas (as pares depois da parede de fundo) como vazio: sombra de canto e luz mínima do túnel, com a arte da família. A pintura da sala (opção **Mostrar ambientes**) continua por cima.

## Save

Versão 25: o reboco vai nos bits 3-6 do bloco `Marked` (`RoomLining.KindOf` = ambiente + 1). Saves anteriores têm 0 ali e voltam sem reboco; as formigas rebocam de novo. Não renumerar `PaintRoom` sem migrar o save.

## Como estender

- **Famílias da fundição, da forja e dos armazéns de equipamento (pedido do desenvolvedor em 2026-10-23, depois do teste de jogo):** `RoomStyle.Smelter`, `Forge`, `Armory` e `Wardrobe` (linhas 51 a 58 do atlas). Fundição: tijolo refratário vermelho com fuligem e brasa nas frestas do fundo. Forja: pedra escura com cinta e rebites e o brilho do fogo no fundo, com tenazes penduradas. Armas: tábuas verticais e, no fundo, tábuas com cabideiro. Armaduras: painéis de madeira clara e tecido azul em trama. Os outros depósitos (alimentos, construção, água, areia, cogumelos, serapilheira, néctar e minério) seguem com a família `Store`.
- **Visual próprio para um depósito:** acrescente a família em `RoomStyle` (as linhas do atlas crescem 2 no fim), o par no gerador do atlas (`Art/Terrain/Source~/terrain_atlas.py`) e o caso em `CellTileArt.StyleOf`.
- **Bônus de sala pronta:** a sala pronta já existe por célula (`PixelGrid.IsRoomFinished`); um bônus pode ler a fração de células prontas de cada ambiente. Hoje nada lê isso fora do desenho (decisão de 2026-10-22: só visual).
- **Deixar alguma parede natural** (ex.: o teto): filtrar a direção em `ScanRoomPart` (o laço das 8 vizinhas) e em `LiningKindAt`.

## Cuidados

- Os slots seguem o vão de fato: terra pintada e ainda não cavada (sem marca) que encosta no vão é rebocada como parede de dentro; a que está atrás dela, não.
- Sem terra solta lá fora nem depósito de construção com terra, o reboco espera (como qualquer obra).
- O reboco e as marcas de construção dividem as mesmas formigas da seção Obra; muitos slots podem atrasar a obra que o jogador marcou.
- Enquanto houver obra ou reboco, as comuns deixam de cuidar das salas, levar ovo, regar e alimentar a rainha; muitos slots de uma vez (pintar várias salas) seguram esse trabalho até a obra acabar. O limite da seção Obra no painel da colônia controla quantas formigas vão.
- Com depósito de construção, o reboco gasta a terra guardada: a obra marcada depois pode esperar terra nova.
- Testes das automatizações (canteiros, orvalho, depósitos, resíduos) começam com as salas já rebocadas (`RoomLiningTests.LineAllRooms`): sem isso, a construção leva a terra antes.
