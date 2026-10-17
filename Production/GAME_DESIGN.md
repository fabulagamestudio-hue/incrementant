# Game design — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** GAME_DESIGN
**Última avaliação:** 2026-10-17

## Resumo

Conceito informado pelo desenvolvedor em 2026-10-17: formigueiro visto de lado, mapa vertical em X/Y, formigas levam pixels do terreno para fora, com controle de mapa e pixels semelhante a Noita. O protótipo implementa a leitura do agente desse conceito (jogador marca o que cavar; terra entregue compra formigas). Loop, metas e progressão ainda não foram definidos.

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| GD-001 | Conceito: formigário de lado com terreno em pixels | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-001 | 2026-10-17 |
| GD-002 | Chuva leve e chuva forte | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-005 | 2026-10-17 |
| GD-003 | Água: laguinhos, erosão e afogamento | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-006 | 2026-10-17 |
| GD-004 | Rainha e sala da rainha | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-007 | 2026-10-17 |
| GD-005 | Plantas, sala de nutrientes e nutrientes como moeda | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-008 | 2026-10-17 |

## Detalhes

### GD-001 — Conceito: formigário de lado com terreno em pixels

Decisões tomadas pelo agente no protótipo, a confirmar: o jogador não controla formigas, só marca pixels a cavar; cada pixel entregue vale 1 de "terra", que compra formigas com custo crescente; o despejo é uma faixa fixa na superfície à direita da entrada. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md).

### GD-002 — Chuva leve e chuva forte

Regras dadas pelo desenvolvedor em 2026-10-17: chuva leve faz a terra que as formigas tiraram desaparecer com o contato da água; chuva forte solidifica os elementos com física. Interpretações do agente, a confirmar: só a terra despejada (`Spoil`) dissolve, areia despejada não; cada célula aguenta 4 gotas; a chuva forte compacta a crosta atingida (2 células de profundidade) de forma permanente, e o compactado continua escavável e volta a ficar solto ao ser despejado; o tempo é trocado manualmente (tecla C / HUD), sem ciclo automático. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Chuva").

### GD-003 — Água: laguinhos, erosão e afogamento

Regras dadas pelo desenvolvedor em 2026-10-17: laguinhos na superfície; a chuva cria pixels de água; a água escorre para baixo e na horizontal formando linhas; dentro do formigueiro, X segundos em contato com terra fixa (natural ou construída) a convertem em terra mole com física; formiga mais de Y segundos debaixo d'água morre. Decidido pelo desenvolvedor em 2026-10-17: a água que molha o terreno dentro do formigueiro é consumida, para não formar lagos lá dentro; só a chuva forte gera água. Valores e interpretações do agente, a confirmar: X = 6 s de contato contínuo, Y = 5 s; terra fixa = terra, grama, terra e areia compactadas (pedra e areia solta não amolecem); só parte das gotas fortes vira água (10%); solo já mole ou solto só absorve a água; decidido pelo desenvolvedor em 2026-10-17: a água só é consumida se entrar no formigueiro construído (laguinhos e poças da superfície não infiltram nem evaporam; a infiltração e a evaporação que o agente tinha acrescentado foram removidas, risco de acúmulo na superfície em KI-009); formigas não desviam da água; o pixel carregado some com a formiga afogada. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Água").

### GD-004 — Rainha e sala da rainha

Regras dadas pelo desenvolvedor em 2026-10-17: a rainha tem o dobro do tamanho das formigas; as ociosas ficam por toda a região do formigueiro conectada ao local da rainha; o jogador pinta a região em que a rainha pode ficar; ela anda só nela; com várias áreas, fica na maior. Interpretações do agente, a confirmar: a sala só pode ser pintada no formigueiro (solo original ou abaixo), inclusive sobre terra ainda maciça, e só as células vazias contam (a sala vale ao ser cavada; pintar não marca para cavar); "maior" = mais células vazias conectadas; sem sala ou sem caminho até ela, a rainha fica parada; a rainha não cava, não carrega e não se afoga; formigas compradas nascem onde a rainha está; "região do formigueiro" = túneis abaixo do solo original ligados à rainha, sem passar por marcas de construção (as ociosas não ficam em cima da obra nem atrás dela); a rainha usa os quadros das operárias, escalados (arte própria pendente). Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Rainha e sala da rainha").

### GD-005 — Plantas, sala de nutrientes e nutrientes como moeda

Regras dadas pelo desenvolvedor em 2026-10-17: o jogador coleta folhas e gramas da superfície, que crescem a cada X tempo até o tamanho máximo; as formigas colhem da parte de cima para a de baixo e levam à sala de nutrientes; cada pixel vira um nutriente levado à sala da rainha, que o consome e aumenta o contador; nutrientes são a moeda para criar formigas; os slots da sala não obstruem a passagem mas ocupam o pixel; sem slot livre, as formigas não colhem. Decidido pelo desenvolvedor em 2026-10-17: as plantas colhíveis usam os sprites de grama do cenário em primeiro plano (não linhas verdes), a grama do fundo fica mais clara, e crescer é o sprite subir para fora do chão. Decidido pelo desenvolvedor em 2026-10-17: os pedaços da planta são colhidos como os da terra. Interpretações do agente, a confirmar: a forma da planta é a silhueta do sprite (1 pixel do terreno = bloco de 6x6 pixels do desenho; uma touceira tem ~300 a 420 pedaços, um trevo ~70 a 80), então não foi preciso aumentar as plantas; as plantas nascem completas e X = 1 s para repor cada pedaço, de baixo para cima (o sprite não sobe mais, porque arrastaria os buracos); a muda folhosa usa os sprites de trevo; ~60% das faixas de 24 colunas têm planta, longe da entrada e do despejo; "coletar" = pincel próprio **Colher** (o pincel Cavar não pega plantas); a raiz nunca é colhida e, marcada, faz o que cresce nascer marcado (colheita automática); a sala de nutrientes é pintada como a da rainha (só no formigueiro, não sobrepõe a sala da rainha); a rainha consome ao receber (sem tempo de consumo), também fora da sala dela; até metade das formigas busca nutrientes ao mesmo tempo; a terra despejada deixou de ser moeda e virou estatística; o preço da formiga continua 25 × 1,12^compradas, agora em nutrientes. Decidido pelo desenvolvedor em 2026-10-17: o pixel guardado fica na sala por 10 s (velocidade 1×) antes de virar nutriente que pode ser levado à rainha, e a formiga não espera esse tempo. Decidido pelo desenvolvedor em 2026-10-17: toda grama em primeiro plano tem de ser colhível (os tufos da frente viraram grama colhível e grama/trevos só de cenário ficam desbotados ao fundo) e cada pedaço colhido leva 1 minuto para voltar no 1× (por pedaço, não pela planta inteira; confirmado pelo desenvolvedor em 2026-10-17). Decidido pelo desenvolvedor em 2026-10-17: o cenário perto não tem plantas; toda planta da superfície (grama, tufo, trevo, flor) é colhível, e as faixas média e distante ficam como estão. Interpretação do agente, a confirmar: cogumelos, gravetos e folhas secas continuam só cenário; para a superfície não ficar vazia, há até uma planta a cada 12 colunas (85% de chance). Decidido pelo desenvolvedor em 2026-10-17: o pixel que cresce não nasce marcado para colheita (a colheita automática pela raiz marcada saiu); a planta cresce pela base, com a ponta sempre criada, e cada passo empurra a planta inteira (todas as colunas) para cima, levando junto o pixel em colheita. O passo sobe 1 linha (1 célula) a cada 60 s no 1× (confirmado pelo desenvolvedor em 2026-10-17). Interpretações do agente, a confirmar: planta toda para fora com a ponta colhida rebrota como uma planta baixa com ponta, na altura que sobrou, e volta a subir; buracos no meio de uma planta com a ponta inteira não voltam. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Plantas, sala de nutrientes e nutrientes").

## Riscos e bloqueios

- Nenhum.

## Não verificado

- Se o loop do protótipo é divertido: depende de playtest do desenvolvedor.
