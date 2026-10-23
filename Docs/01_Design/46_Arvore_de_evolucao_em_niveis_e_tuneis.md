# 46 — Árvore de evolução em níveis e túneis

Documento do projeto de 2026-10-23 (REQ-073). A direção foi aprovada pelo desenvolvedor na conversa de 2026-10-23. Os pontos marcados como **suposição** são propostas do agente e ainda não foram aprovados.

Este documento substitui, no [documento 39](39_Melhorias_incrementais_e_formigueiro_de_dependencias.md), as **duas abas**, o **layout radial** (8 ramos por aba) e os **anéis por profundidade**. Continuam valendo do 39: câmaras e galerias, níveis, marcos, fórmulas de custo e os números da [planilha](../04_Data/39_Melhorias_incrementais_valores.xlsx).

## Por que mudar

- Hoje há 2 abas radiais, cada uma com 8 ramos, que somam 123 nós. Cada ramo agrupa vários tipos de sala e de formiga.
- Com mais salas e classes, um ramo radial a mais estreita todos os outros, e as ligações ficam desenhadas o tempo todo. O jogador perde a leitura.
- Sala e classe que trabalham juntas ficam em abas diferentes, ligadas por "portais" (Q2 → B0, Z0 → A0, J0 → C0).

## Pedido do desenvolvedor

- "Nossa árvore de habilidades vai ter um comportamento similar à de uma árvore, porém como formigueiro e para baixo. Começando da rainha que é a parte de cima do formigueiro, que liga para baixo."
- "As conexões serão baseadas no quanto nós esperamos que o jogador esteja avançado para ir descendo mais fundo no formigueiro. Onde na parte de cima teremos os soldados, fazenda, etc. E mais para o fundo relacionados à forja e minérios."
- "Não manter o nível daquela formiga ou sala exclusiva naquela altura. Por exemplo, melhorar a defesa da armadura das formigas vale para quando estiver liberada a forja, mas é para uma característica base de formiga, que deveria estar acima de todas. Vamos fazer túneis que descem baseados no valor daquele upgrade."
- "Se temos 3 salas no início, se uma delas tem um upgrade avançado, daquela sala faz um túnel para baixo, até o nível que deveria ter aquele upgrade."

## Estrutura

A árvore é um formigueiro em corte, numa tela só:

- **Raiz (Câmara real):** no topo, de onde partem todos os túneis.
- **Níveis:** faixas horizontais, do raso ao fundo. Cada nível representa quanto o jogador deve ter avançado.
- **Domínios:** colunas. Um domínio junta a sala e as classes que trabalham nela, como o berçário e a cuidadora.
- **Salão do domínio:** fica no nível em que o domínio é liberado.
- **Túnel do domínio:** desce do salão até o upgrade mais fundo daquele domínio. Em cada nível em que o túnel passa e há upgrades do domínio, abre uma **câmara lateral** com eles.

Duas leituras:

- **Na horizontal (um nível):** tudo o que o jogador deve conseguir comprar naquela fase.
- **Na vertical (uma coluna):** tudo sobre um domínio, do básico ao avançado.

## Regras

1. **Nível do salão:** o salão fica no nível em que o domínio é liberado, nunca abaixo do nó mais raso dele. Os domínios ligados direto à raiz (Rainha, Formigas, Comum, Obra) ficam no nível 0.
2. **Nível do upgrade:** cada nó tem o seu próprio nível, definido no design pelo valor dele (custo, força, fase esperada), independente do salão. É um campo explícito por nó (`Tier` no código, porque "nível" já é o nível da galeria). Não é calculado pelo custo, para que um rebalanceamento não mude os nós de lugar.
3. **Túneis só descem:** um nó nunca fica acima de nenhum dos seus requisitos (nível ≥ nível de cada requisito). Um teste garante a regra.
4. **Característica base no domínio base:** um upgrade que vale para todas as formigas fica no domínio Formigas, logo abaixo da raiz, mesmo que só abra com a forja. Nesse caso, o túnel de Formigas desce até o nível da forja.
5. **Requisito de outro domínio:** aparece como ligação tracejada, só com o nó selecionado. Dentro do domínio, o túnel e as ligações aparecem sempre.
6. **Até 8 nós por câmara lateral.** Se passar disso, o domínio usa dois níveis, ou a câmara se divide em duas no mesmo nível, uma de cada lado do túnel.
7. **Condição do mundo:** continua no nó ou no salão (primeiro ataque, primeiro minério), separada do nível. O nível diz "quando se espera"; a condição diz "o que precisa ter acontecido".

## Níveis

O nível abre pela PROFUNDIDADE de hoje (soma de câmaras compradas, níveis e galerias completas, ver doc 39). Os limites dos níveis 0 a 4 repetem os dos anéis atuais. O do nível 5 é **suposição**.

| Nível | Nome | Abre com | Conteúdo típico |
|---|---|---|---|
| 0 | Câmara real | sempre | Raiz: rainha, operária, postura, túnel |
| 1 | Primeiras salas | sempre | Salas e classes iniciais, áreas pintáveis |
| 2 | Galerias rasas | profundidade 5 | Segunda melhoria das salas, defesa (com o 1º ataque), fazenda |
| 3 | Galerias médias | profundidade 20 | Melhorias combinadas entre domínios |
| 4 | Galerias fundas | profundidade 50 | Nós finais de cada domínio, veios e fundição |
| 5 | Forja | profundidade 100 (**suposição**) | Forja, armazéns e o que depende de equipamento |

Um nível fechado aparece como terra não escavada, com a condição escrita (como o aviso dos anéis hoje). Um nível novo, quando houver conteúdo para ele, entra no fundo.

## Domínios

Ordem das colunas, da esquerda para a direita (**suposição**). Domínios com muitos requisitos entre si ficam lado a lado. Os que nascem fundo ficam na ponta direita.

| Coluna | Domínio | Junta | Salão no nível | Condição do salão |
|---|---|---|---|---|
| 1 | Fazenda | fazenda de pulgões, fazendeira | 1 | — |
| 2 | Coleta | cortadeira, trilhas e plantas de fora | 1 | — |
| 3 | Cultivo | sala de nutrientes, jardineira | 1 | — |
| 4 | Orvalho | sala Gotas de orvalho | 1 | — |
| 5 | Berçário | berçário, cuidadora | 1 | — |
| 6 | Rainha | postura, sala real | 0 | — |
| 7 | Formigas | o que vale para todas as formigas | 0 | — |
| 8 | Comum | operária comum | 0 | — |
| 9 | Obra | túneis, despejo, salas extras | 0 | — |
| 10 | Escavação | escavadora | 1 | — |
| 11 | Depósitos | depósitos, carregadora | 1 | — |
| 12 | Resíduos | área de resíduos, lixeira | 1 | — |
| 13 | Guarda | soldado, cuspideira, entradas e postos | 2 | 1º ataque (nos nós, como hoje) |
| 14 | Minérios | depósito de minério, fundição | 4 | 1º minério cavado (**novo**) |
| 15 | Forja | forja, armazéns | 5 | 1ª barra fundida (**novo**) |

A raiz (P0 Postura, T0 Túnel comum, Q0 Postura inicial, O0 Operária) fica no centro do topo, sobre as colunas 6 a 9.

## Distribuição dos nós atuais

O nível de cada nó é o anel atual (centro = 0, anel N = nível N). A regra 3 foi conferida por script em 2026-10-23 contra `UpgradeCatalog`. Só O+2 precisou mudar: exige B1 (nível 2) e estava no anel 1. Os 123 nós entram sem mudar de id, e os ids da árvore da rainha continuam iguais. O save não muda.

Legenda: *(C)* câmara, *(G)* galeria, *(i)* inicial ou pincel livre, *(ES)* sem sistema hoje (EM BREVE).

### Raiz — nível 0

P0 Postura · T0 Túnel comum · Q0 Postura inicial *(i)* · O0 Operária *(i)*

### Rainha (antes: aba Rainha, ramo Rainha; H+1 vinha do ramo Obra)

| Nível | Nós |
|---|---|
| 1 | Q1 Postura acelerada (C) · Q+2 Geleia real (G) · Q+3 Vigor real (G, 1º ataque) · Q+4 Sala real ampla (G) · H+1 Câmara real ampliada (G) |
| 2 | Q2 Postura em lotes (C, exige B0) · Q+1 Fertilidade (G) |
| 4 | Q3 Postura em campo (C, ES) |

### Formigas (domínio novo)

| Nível | Nós |
|---|---|
| 2 | O+2 Vitalidade (G; mudou do anel 1, porque exige B1) |
| 5 | **Novo:** Armadura reforçada (G, exige o armazém de roupas e armaduras), o exemplo do pedido |

O+2 já vale para todas as adultas ("Todas as adultas têm 10% mais vida em combate por nível"). Por isso sai do ramo da Comum, que fica só com o que é da classe.

### Comum (antes: ramo Comum, menos O+2 e W0)

| Nível | Nós |
|---|---|
| 1 | O1 Carga ampliada · O2 Deslocamento rápido · O3 Retirada de companheiras (C) |
| 2 | O+1 Passo firme · O+3 Mandíbula de trabalho (G) |
| 4 | O4 Operária versátil (C, ES, 50 adultas) |

### Cultivo (antes: ramo Cultivo do formigueiro + ramo Jardineira, menos Z0/Z1/Z+1)

| Nível | Nós |
|---|---|
| 1 | C0 Cultivo inicial (i) · C1 Canteiros organizados · J0 Jardineira (C) · C+1 Fungo vigoroso · C+4 Sala de nutrientes ampla (G) |
| 2 | C2 Área de preparo (ES) · C3 Cultivo produtivo · J1 Produção otimizada de fungo (C) · C+2 Colheita nutritiva · C+3 Cura rápida · J+1 Mãos verdes · J+2 Orvalheira (G, exige G0) |
| 3 | C4 Cultivo integrado (ES) · J2 Transplante de fungo (C, ES) |
| 4 | C5 Jardim da rainha · J3 Simbiose (C, ES) |

### Berçário (antes: ramo Berçário + ramo Cuidadora)

| Nível | Nós |
|---|---|
| 1 | B0 Berçário inicial · N0 Cuidadora (C) · B+3 Berçário amplo (G) |
| 2 | B1 Leitos de criação · B2 Área de maturação · N1 Alimentação em lote (C) · N+1 Ninhada atenta · N+2 Turno dobrado (G) |
| 3 | B3 Berçário organizado · N2 Resgate de crias (ES) · N3 Ama de leite (C) · B+1 Leitos aquecidos · B+2 Vagas extras (G) |
| 4 | B4 Ninhada gêmea (C, ES) |

### Depósitos (antes: ramo Depósito + ramo Carregadora)

| Nível | Nós |
|---|---|
| 1 | D0 Depósito inicial · D3 Depósito de construção · D4 Depósito de água · D6 Depósito de areia · D7 Depósito de cogumelos (i) · L0 Carregadora (C) · D+3 Depósitos amplos (G) |
| 2 | D1 Pilhas organizadas (C, ES) · L1 Cargas maiores · L3 Velocidade das carregadoras (C) · D+1 Empilhamento (G, ES) · L+1 Costas largas · L+2 Passo de carga · L+3 Corrida com carga (G) |
| 3 | D2 Espaço para cargas grandes (ES) · L2 Transporte cooperativo (C, ES) · D+2 Conservação (G, ES) |
| 4 | D5 Logística integrada · L4 Comboio (C, ES) |

D1 e D+1 são o lugar natural da pilha por célula do REQ-072 (o nome e o efeito já falam disso). Ainda falta decidir por qual depósito e em que tamanho (ver REQ-072).

### Coleta (antes: ramo Cortadeira + ramo Exterior, menos a fazenda)

| Nível | Nós |
|---|---|
| 1 | F0 Cortadeira · X0 Trilha de forrageio (C) |
| 2 | F1 Cortes maiores (C) · F+1 Serrilha · X+1 Brotação · X+2 Feromônio de trilha · X+3 Faro (G, ES) |
| 3 | F2 Poda seletiva · X1 Jardim cultivado (C, ES) |

### Orvalho (antes: ramo Orvalho)

| Nível | Nós |
|---|---|
| 1 | G0 Sala Gotas de orvalho (i) · G+3 Sala de orvalho ampla (G) |
| 2 | G1 Folhas coletoras (C) · G+1 Orvalho farto · G+2 Reservatório (G) |
| 3 | G2 Neblina do amanhecer (C, ES) |

### Resíduos (antes: ramo Resíduos; W0 vinha do ramo Comum)

| Nível | Nós |
|---|---|
| 1 | R0 Área de resíduos (i) · R+1 Área de resíduos ampla (G) |
| 2 | R1 Decomposição acelerada · W0 Lixeira (C, 1º ataque) |
| 3 | R2 Filtragem da terra (C) |

### Obra (antes: ramo Obra, menos H+1)

| Nível | Nós |
|---|---|
| 1 | T1 Despejo organizado (C) · T+1 Obra barata (G, ES) · T+3 Salas extras (G) |
| 2 | T+2 Frentes de obra (G, exige E0) |
| 3 | T2 Rotas batidas (C) |

### Escavação (antes: ramo Escavadora)

| Nível | Nós |
|---|---|
| 1 | E0 Escavadora (C) |
| 2 | E1 Remoção ampliada (C) · E+1 Mandíbulas de pá · E+2 Carga de terra (G) |
| 3 | E2 Quebra de rocha (C, exige T+2) · E+3 Britador (G) |
| 4 | E3 Frente larga (C, ES) |
| 5 | **Novo:** Ferramentas afiadas (G, efeito da ferramenta, exige o armazém de ferramentas e armas) |

### Fazenda (antes: câmaras A do ramo Exterior + Z0/Z1/Z+1 do ramo Jardineira)

| Nível | Nós |
|---|---|
| 1 | A0 Fazenda de pulgões · Z0 Fazendeira (C) · A+3 Fazenda ampla (G) |
| 2 | A1 Ninhada de pulgões · Z1 Ordenha cuidadosa (C) · A+1 Rebanho · A+2 Ordenha · Z+1 Passo de pastora (G) |

### Guarda (antes: ramo Soldado + ramo Defesa)

| Nível | Nós |
|---|---|
| 2 | S0 Soldado · K0 Cuspideira (C, 1º ataque) · S1 Mandíbulas fortes · S2 Carapaça resistente · M0 Entradas vigiadas (1º ataque) · M2 Postos de guarda (C) · M+1 Paredes reforçadas (G, ES) · M+2 Vigias (G) |
| 3 | S3 Contenção (C, ES) · M1 Câmara de refúgio (C, ES, 5 ataques) · S+1 Mandíbulas afiadas · S+2 Carapaça grossa · S+3 Recuperação · S+4 Fúria de batalha (G) |
| 4 | S4 Guarda real (C, ES) |
| 5 | **Novo:** Armas balanceadas (G, efeito da arma, exige o armazém de ferramentas e armas) |

### Minérios (domínio novo; REQ-071)

Hoje o depósito de minério e a fundição são pincéis livres, e não há nó nenhum. Proposta (**suposição**, custos a definir):

| Nível | Nós |
|---|---|
| 4 | Depósito de minério (C, i) · Fundição (C) · Fundição rápida (G: tempo da barra) |

### Forja (domínio novo; REQ-071)

Proposta (**suposição**, custos a definir):

| Nível | Nós |
|---|---|
| 5 | Forja (C) · Armazém de ferramentas e armas (C) · Armazém de roupas e armaduras (C) · Forja rápida (G: tempo do equipamento) · Têmpera (G: durabilidade) |

Os ids dos nós novos seguem a convenção do catálogo e são escolhidos na implementação, sem reaproveitar letras em uso.

## Tela

- **Rolagem:** vertical, descendo a partir da raiz, e horizontal quando as colunas não cabem.
- **Zoom em duas leituras:**
  - de longe: só salões, túneis e câmaras laterais, cada um com o progresso (ex.: 4/7);
  - de perto: os nós.
- **Túnel:** um traço de terra por coluna, do salão até a câmara lateral mais funda. As câmaras laterais saem dele.
- **Ligações:** dentro da coluna, sempre. Entre colunas, só com o nó selecionado: tracejadas, com as cores atuais (verde atendido, vermelho faltando, azul o que o nó libera).
- **Nível fechado:** terra escura com o aviso "TERRA NÃO ESCAVADA · ABRE COM PROFUNDIDADE N". Os nós dentro dele ficam apagados, como hoje.
- Sem abas e sem portais. Os requisitos que hoje cruzam abas viram ligações comuns.
- **Mantidos de hoje:** detalhe do nó, seletor ×1/×10/MÁX, estados dos nós e efeitos visuais. Ver o [HudGuide](../../Assets/Project/Scripts/Runtime/UI/HudGuide.md), "Tela Evolução".

**Como ficou na implementação (etapa 2, 2026-10-23):**
- Logo abaixo da Câmara real passa uma **galeria horizontal**, e o túnel de cada coluna desce dela. A Câmara real fica sozinha acima da galeria, e os salões de Rainha, Formigas, Operária e Obra ficam pendurados abaixo dela, como as outras colunas (ajuste pedido pelo desenvolvedor em 2026-10-23). Os encontros da galeria com as colunas são abertos. A ligação entre colunas sobe pelo túnel, anda pela galeria e desce na outra coluna.
- O salão de cada domínio mostra o nome e o progresso o tempo todo (ex.: "CULTIVO 1/16").
- **Pendente:** os nós ainda não somem quando o jogador afasta o zoom. De longe (zoom mínimo 0,2), o formigueiro inteiro cabe na tela, com nós pequenos.
- Câmaras laterais com 4 linhas ficam mais altas que largas, porque a largura é limitada pela coluna (420 px).

## Compra

Uma regra só para todos os nós: nível aberto, condição do mundo, requisitos, teto e saldo (`UpgradeBoard.Unlockable` + saldo). Hoje os nós da árvore da rainha seguem só `QueenTree.Check` e ignoram os requisitos cruzados e os anéis do catálogo (ver [UpgradesGuide](../../Assets/Project/Scripts/Runtime/Upgrades/UpgradesGuide.md)). Com a árvore única, isso acaba.

**Risco:** saves antigos podem ter nós da rainha comprados sem o requisito cruzado (Q2 sem B0, por exemplo). O nó continua comprado. A regra vale só para compras novas.

## Etapas de implementação (proposta)

1. **Dados:**
   - `UpgradeDef` ganha `Domain` e `Tier`, que substituem `Tab`/`Branch`/`Ring` quando a tela mudar (etapa 2). **Feito em 2026-10-23;**
   - tabela de domínios e níveis;
   - testes: túnel só desce, até 8 nós por câmara lateral, todos os nós com domínio.
2. **Layout e tela** (**feito em 2026-10-23**, menos os nós sumirem de longe):
   - `UpgradeLayout` em colunas e faixas;
   - `BuildCaves` com salões, túneis verticais e câmaras laterais;
   - zoom em duas leituras e ligações só na seleção;
   - remontar o HUD.
3. **Compra unificada** por `UpgradeBoard.Unlockable`. Desde a etapa 2, câmaras e galerias vivas já abrem pelo nível do formigueiro (`TierOpen`). Faltam os nós da rainha.
4. **Nós novos:** Minérios, Forja e os nós de nível 5 de Formigas, Escavação e Guarda, com custos e efeitos aprovados.

## Em aberto

- Limite de profundidade do nível 5 (100 é suposição).
- Ordem das colunas.
- Nós, custos e efeitos dos domínios Minérios e Forja e dos nós de nível 5.
- Se os pincéis do depósito de minério e da fundição continuam livres ou viram câmaras pagas.
- Pilha por célula do REQ-072 em D1/D+1: quais depósitos e que tamanho.
- Abrir a tela continua sem pausar o jogo (seção 34).
