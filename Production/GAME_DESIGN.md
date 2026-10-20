# Game design — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** GAME_DESIGN
**Última avaliação:** 2026-10-19

## Resumo

Conceito informado pelo desenvolvedor em 2026-10-17: formigueiro visto de lado, mapa vertical em X/Y, formigas levam pixels do terreno para fora, com controle de mapa e pixels semelhante a Noita. O protótipo implementa a leitura do agente desse conceito (jogador marca o que cavar; terra entregue compra formigas). O documento revisado fornecido em 2026-10-17 descreve agora loop, metas e progressão, transcritos em Docs/ (GD-006); propostas numéricas e decisões em aberto permanecem como tais. A correspondência dessa referência com o protótipo não foi avaliada nesta conversão.

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| GD-001 | Conceito: formigário de lado com terreno em pixels | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-001 | 2026-10-17 |
| GD-002 | Chuva leve e chuva forte | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-005 | 2026-10-17 |
| GD-003 | Água: laguinhos, erosão e afogamento | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-006 | 2026-10-17 |
| GD-004 | Rainha e sala da rainha | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-007 | 2026-10-17 |
| GD-005 | Plantas, sala de nutrientes e nutrientes como moeda | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-17 | REQ-008 | 2026-10-17 |
| GD-007 | Ataques de cupins, combate e área de resíduos | IN_PROGRESS | UNDEFINED | Informado pelo desenvolvedor em 2026-10-18 | REQ-029 | 2026-10-18 |
| GD-008 | Melhorias incrementais e formigueiro de dependências | IMPLEMENTED | UNDEFINED | [Documento 39](../Docs/01_Design/39_Melhorias_incrementais_e_formigueiro_de_dependencias.md) e [planilha](../Docs/04_Data/39_Melhorias_incrementais_valores.xlsx) escritos em 2026-10-19; níveis repetíveis e valores aprovados pelo desenvolvedor em 2026-10-19; sem teste em jogo nem construção | REQ-039 | 2026-10-19 |
| GD-006 | Documento de design revisado organizado em Markdown | VERIFIED | UNDEFINED | Conferência automática em 2026-10-17: 38 seções, 507 parágrafos, 28 tabelas, 209 linhas e 604 células preservados; 156 links locais válidos. [Índice e conferência](../Docs/README.md) | REQ-009 | 2026-10-17 |

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

Regras dadas pelo desenvolvedor em 2026-10-17: o jogador coleta folhas e gramas da superfície, que crescem a cada X tempo até o tamanho máximo; as formigas colhem da parte de cima para a de baixo e levam à sala de nutrientes; cada pixel vira um nutriente levado à sala da rainha, que o consome e aumenta o contador; nutrientes são a moeda para criar formigas; os slots da sala não obstruem a passagem mas ocupam o pixel; sem slot livre, as formigas não colhem. Decidido pelo desenvolvedor em 2026-10-17: as plantas colhíveis usam os sprites de grama do cenário em primeiro plano (não linhas verdes), a grama do fundo fica mais clara, e crescer é o sprite subir para fora do chão. Decidido pelo desenvolvedor em 2026-10-17: os pedaços da planta são colhidos como os da terra. Interpretações do agente, a confirmar: a forma da planta é a silhueta do sprite (1 pixel do terreno = bloco de 6x6 pixels do desenho; uma touceira tem ~300 a 420 pedaços, um trevo ~70 a 80), então não foi preciso aumentar as plantas; as plantas nascem completas e X = 1 s para repor cada pedaço, de baixo para cima (o sprite não sobe mais, porque arrastaria os buracos); a muda folhosa usa os sprites de trevo; ~60% das faixas de 24 colunas têm planta, longe da entrada e do despejo; "coletar" = pincel próprio **Colher** (o pincel Cavar não pega plantas); a raiz nunca é colhida e, marcada, faz o que cresce nascer marcado (colheita automática); a sala de nutrientes é pintada como a da rainha (só no formigueiro, não sobrepõe a sala da rainha); a rainha consome ao receber (sem tempo de consumo), também fora da sala dela; até metade das formigas busca nutrientes ao mesmo tempo; a terra despejada deixou de ser moeda e virou estatística; o preço da formiga continua 25 × 1,12^compradas, agora em nutrientes. Decidido pelo desenvolvedor em 2026-10-17: o pixel guardado fica na sala por 10 s (velocidade 1×) antes de virar nutriente que pode ser levado à rainha, e a formiga não espera esse tempo. Decidido pelo desenvolvedor em 2026-10-17: toda grama em primeiro plano tem de ser colhível (os tufos da frente viraram grama colhível e grama/trevos só de cenário ficam desbotados ao fundo) e cada pedaço colhido leva 1 minuto para voltar no 1× (por pedaço, não pela planta inteira; confirmado pelo desenvolvedor em 2026-10-17). Decidido pelo desenvolvedor em 2026-10-17: o cenário perto não tem plantas; toda planta da superfície (grama, tufo, trevo, flor) é colhível, e as faixas média e distante ficam como estão. Interpretação do agente, a confirmar: cogumelos, gravetos e folhas secas continuam só cenário; para a superfície não ficar vazia, há até uma planta a cada 12 colunas (85% de chance). Decidido pelo desenvolvedor em 2026-10-17: o pixel que cresce não nasce marcado para colheita (a colheita automática pela raiz marcada saiu); a planta cresce pela base, com a ponta sempre criada, e cada passo empurra a planta inteira (todas as colunas) para cima, levando junto o pixel em colheita. O passo sobe 1 linha (1 célula) a cada 60 s no 1× (confirmado pelo desenvolvedor em 2026-10-17). Decidido pelo desenvolvedor em 2026-10-17: no crescimento máximo, a planta só volta a crescer quando não tiver mais nenhum pixel para colher; aí reinicia, volta para debaixo da terra e sobe de novo completa. Interpretação do agente, a confirmar: "nenhum pixel para colher" = só sobrou a linha do chão, que nunca é colhida; a planta reiniciada nasce sem marcas. Decidido pelo desenvolvedor em 2026-10-18 (REQ-027): o pixel não cura mais sozinho; o cultivo pede terra fofa (a terra com física), depois a planta no mesmo bloco e depois um bloco de água, tirado de qualquer água do mapa; regado, o canteiro brota no tempo da antiga cura e põe um nutriente pronto em cada lado livre (no chão, 3); a terra fica e só a planta e a água se gastam. Decidido pelo desenvolvedor em 2026-10-18: a terra fofa também vem da pilha de fora e o canteiro pode ficar nas paredes e no meio da sala, posto como um bloco de obra. Interpretação do agente, a confirmar: o canteiro fica preso onde foi posto (a terra solta continua com física) e fica a pelo menos 3 células de outro. Ver [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Plantas, sala de nutrientes e nutrientes").

### GD-006 — Documento de design revisado organizado em Markdown

Referência fornecida pelo desenvolvedor: `Increment_Ant_Game_Design_Completo_Revisado.docx`, consolidação de 2026-10-17. As 38 seções foram transcritas integralmente e distribuídas nas cinco pastas temáticas de [Docs/](../Docs/README.md), com tabelas, índice e navegação sequencial. A numeração representa assuntos, não marcos obrigatórios de execução; a proposta de recorte da jam permanece na seção 36. Valores propostos e decisões em aberto não foram promovidos a decisões aprovadas.

Validação de 2026-10-17: comparação automática, em ordem, de todos os parágrafos e células com o DOCX, contagem das seções/tabelas e conferência dos links locais e da codificação UTF-8. VERIFIED refere-se à conversão documental. Não houve avaliação de balanceamento, reconciliação com o código nem mudança dos estados de implementação dos sistemas GD-001 a GD-005.

### GD-007 — Ataques de cupins, combate e área de resíduos

Regras do pedido de 2026-10-18 (REQ-029) e decisões tomadas na conversa: parede destruída vira terra fofa solta; área de resíduos é sala pintada; formiga pode morrer em combate mesmo com a morte desligada. Valores vindos do design (seções 15, 21, 27 e 32): vida 6/8/18, dano 1/s e 3/s, cupim com vida 10 e mordida 2/s, decomposição 120 s. Pedido complementar do mesmo dia: só o soldado caça cupins, as demais só se defendem quando mordidas (como no design); a quantidade de cupins é sorteada entre 40% e um teto que cresce a cada ataque até 200% (crescimento de 20 pontos escolhido pelo agente, a confirmar). Diferença do design ainda a decidir: no design a invasora entra por um acesso e ataca formigas (inclusive a rainha, com game over); no pedido, os cupins vão atrás de crias, canteiros e paredes. Regras e números implementados: [RaidGuide.md](../Assets/Project/Scripts/Runtime/Colony/RaidGuide.md).

### GD-008 — Melhorias incrementais e formigueiro de dependências

Pedido de 2026-10-19 (REQ-039): expandir os upgrades das formigas e do formigueiro num incremental pago com nutrientes, só documentado. O documento 39 propõe três camadas (câmaras de compra única, galerias com nível e marcos), anéis de profundidade e túneis de dependência (simples, de nível, cruzados entre abas e de junção), nas duas abas da tela Evolução. Números na planilha de `Docs/04_Data/`.

**Revisão do design revisado:** a seção 28 diz "uma compra por nó, sem níveis infinitos já definidos"; decidido pelo desenvolvedor em 2026-10-19 que os níveis repetíveis do documento 39 passam a valer (físicos com teto, economia e combate infinitos) e que os valores sugeridos da planilha são os iniciais. Também acrescenta requisitos cruzados a nós que já existem (Q2, L2, D2, E2, J0, J1, N0) e transforma em nós os pincéis hoje livres (C0, R0, G0 como iniciais; D0, D3, D4 como compras baratas, com migração de save). Até a implementação, o jogo segue as regras atuais.

## Riscos e bloqueios

- Nenhum.

## Não verificado

- Se o loop do protótipo é divertido: depende de playtest do desenvolvedor.
