# Solicitações do desenvolvedor

**Projeto:** Incrementant
**Última atualização:** 2026-10-18

## Índice

| ID | Data | Título | Setores | Prioridade | Estado |
|---|---|---|---|---|---|
| REQ-001 | 2026-10-17 | Protótipo de viabilidade do formigário em pixels | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |
| REQ-002 | 2026-10-17 | Cenário na superfície na escala das formigas | ART, PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-003 | 2026-10-17 | Mapa procedural que cresce com a câmera, paralaxe e limite de câmera | PROGRAMMING, ART | UNDEFINED | IMPLEMENTED |
| REQ-004 | 2026-10-17 | Velocidade 8x com o mesmo FPS e simulação constante | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-005 | 2026-10-17 | Chuva leve (dissolve a terra despejada) e chuva forte (compacta o granular) | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |
| REQ-006 | 2026-10-17 | Água: laguinhos, chuva que cria água, erosão da terra e afogamento | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |
| REQ-007 | 2026-10-17 | Formiga rainha e pintura da sala da rainha | PROGRAMMING, GAME_DESIGN, ART | UNDEFINED | IMPLEMENTED |
| REQ-008 | 2026-10-17 | Sala de nutrientes, colheita de plantas da superfície e nutrientes como moeda | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |
| REQ-009 | 2026-10-17 | Ler o design revisado e organizar Markdown por assunto em Docs | GAME_DESIGN | UNDEFINED | VERIFIED |
| REQ-010 | 2026-10-17 | Montar o HUD da partida com os sprites 9-slice | PROGRAMMING, ART | UNDEFINED | IMPLEMENTED |
| REQ-011 | 2026-10-17 | Painel da rainha para escolher qual formiga criar | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-012 | 2026-10-17 | Mais tipos de formiga e início com 1 jardineira | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-013 | 2026-10-17 | Ciclo de criação (ovo, larva, pupa, berçário) seguindo o design | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-014 | 2026-10-17 | Sprites e desenho da cria (ovo, larva, pupa) no berçário | ART, PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-015 | 2026-10-17 | Operária leva o ovo da rainha ao berçário e cheat F1 de formigas | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-016 | 2026-10-17 | Construir do lado de fora do formigueiro | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-017 | 2026-10-17 | Árvore da rainha (classes e melhorias) | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-018 | 2026-10-17 | Revisar as funções de cada formiga por papel contra o design | PROGRAMMING, GAME_DESIGN | UNDEFINED | IN_PROGRESS |
| REQ-019 | 2026-10-18 | Variantes de cor das formigas em pastas por função | ART | UNDEFINED | IMPLEMENTED |
| REQ-020 | 2026-10-18 | Formigas andam livres do lado de fora (meio da arte do cenário e das obras) | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-021 | 2026-10-18 | Reserva do pixel a coletar: uma formiga por pixel, sem bando indo e voltando | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-022 | 2026-10-18 | Menu do ESC e menu de configurações do jogo | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-023 | 2026-10-18 | Alimentar o jogo com SFX e VFX | AUDIO, ART, PROGRAMMING | UNDEFINED | PLANNED |
| REQ-024 | 2026-10-18 | Experimento: célula desenhada em 16x16 e mapa gerado dobrado (branch `experimento/pixel-16x16`) | ART, PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-025 | 2026-10-18 | Ampliar a resolução da arte das formigas em 6x mantendo o tamanho no jogo | ART, PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-026 | 2026-10-18 | Melhorar formigas adultas e cenário em pixel art sem blur nas bordas | ART | UNDEFINED | IMPLEMENTED |
| REQ-027 | 2026-10-18 | Arte de formiga inimiga com asas na chegada e sem asas após pousar | ART | UNDEFINED | IMPLEMENTED |
| REQ-029 | 2026-10-18 | Ataques de cupins, formigas de ataque (soldado) e área de resíduos | PROGRAMMING, GAME_DESIGN, ART | UNDEFINED | IMPLEMENTED |
| REQ-030 | 2026-10-18 | Tutorial da primeira abertura do jogo | PROGRAMMING, GAME_DESIGN | UNDEFINED | IMPLEMENTED |
| REQ-031 | 2026-10-18 | Menu principal com fundo de colônia trabalhando e sem splash da Unity | PROGRAMMING, ART | UNDEFINED | IMPLEMENTED |
| REQ-032 | 2026-10-18 | Sala Gotas de Orvalho | PROGRAMMING | UNDEFINED | IMPLEMENTED |
| REQ-033 | 2026-10-18 | Arte do pulgão menor que a formiga | ART | UNDEFINED | IMPLEMENTED |
| REQ-034 | 2026-10-18 | Efeitos de dopamina (juice): botões animados, partículas e animações de feedback | PROGRAMMING, ART | UNDEFINED | IN_PROGRESS |

## Registros

### REQ-001 — Protótipo de viabilidade do formigário em pixels

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** Protótipo para avaliar a viabilidade do jogo: formigueiro visto de lado, como um formigário; mapa vertical em X e Y; formigas pegam pixels do terreno e levam para o lado de fora; controle de precisão de mapa e pixels semelhante ao de Noita. Pouco tempo de desenvolvimento.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** —
- **Itens relacionados:** PRG-001, PRG-002, PRG-003, PRG-004, PRG-005, PRG-006, PRG-007, PRG-008, ART-001, ART-002, GD-001
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** Validar com playtest se o recorte (marcar para cavar + comprar formigas) é o loop desejado; definir o que o protótipo precisa provar para ser considerado viável.
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: núcleo testado (7 testes EditMode) e exercitado em Play Mode pelo agente; falta playtest do desenvolvedor.
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: mapa, ninho e despejo fixos na cena em vez de gerados em runtime (PRG-005).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: geração procedural como padrão, mapa fixo vira opção (PRG-005).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: formigas com aparência de formiga, em sprite com pixels menores que o terreno (PRG-006, ART-001).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: formigas andam nas paredes quando não há piso, em vez de cair (PRG-007).
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: formigas andam no fundo do formigueiro, além dos pisos (PRG-008, ART-002).

### REQ-002 — Cenário na superfície na escala das formigas

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART, PROGRAMMING
- **Descrição:** "Crie um cenário na superfície, considerando tamanho das formigas."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001
- **Itens relacionados:** PRG-009, ART-003
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** Aprovação visual do cenário provisório; se o cenário deve ter vento/animação ou interagir com as formigas (hoje é só visual).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: cenário pintado por código em três camadas, 4 testes EditMode novos (16/16) e conferido em Play Mode pelo agente; falta aprovação visual do desenvolvedor.
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: cada elemento do cenário vira um asset PNG separado, editável fora da Unity, e a geração posiciona esses assets (ART-003, PRG-009). 2 testes novos (36/36) e Play Mode conferido pelo agente.

### REQ-003 — Mapa procedural que cresce com a câmera, paralaxe e limite de câmera

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, ART
- **Descrição:** "Faz o mapa ser 'procedural', podendo crescer para os lados e para baixo, conforme movimentação de camera. Considere o mundo exterior para fazer paralax, e bote um limitador de movimento de camera para não ter mapa infinito."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001, REQ-002
- **Itens relacionados:** PRG-010, PRG-011, ART-003
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** Tamanho do mundo (hoje 960x520, área inicial 320x200) e se o zoom máximo aberto deve continuar preso ao enquadramento inicial; controles da câmera (WASD/botão do meio, Q/E ou Ctrl+scroll) a aprovar.
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: mundo gerado por blocos conforme a câmera, limitador e paralaxe em duas faixas; 25/25 testes EditMode e Play Mode pelo agente (câmera movida por código); falta uso real de mouse/teclado pelo desenvolvedor.

### REQ-004 — Velocidade 8x com o mesmo FPS e simulação constante

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Estamos limitados ao FPS local. Precisamos agora encontrar a otimização para poder fazer isso funcionar com o mesmo FPS no 8x, e manter a simulação constante."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001, REQ-003
- **Itens relacionados:** PRG-012
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md), [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) (KI-002, KI-004)
- **Decisões pendentes:** Confirmar em jogo que o 8x ficou no FPS desejado (medido só no editor, pelo agente).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: simulação por blocos dormentes e navegação com cache; 30/30 testes EditMode (inclui equivalência com a varredura completa); Play Mode pelo agente com o mundo inteiro e 156 formigas: 102 FPS no 8x × 114 no 1x. Falta o desenvolvedor confirmar jogando.

### REQ-005 — Chuva leve (dissolve a terra despejada) e chuva forte (compacta o granular)

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** "Vamos adicionar 2 sistemas de chuva agora. A chuva leve faz os grãos de terra que as formigas pegaram irem desaparecendo conforme a água entra em contato com elas. A chuva forte solidifica os elementos com física."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001
- **Itens relacionados:** PRG-013, GD-002
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Chuva")
- **Decisões pendentes:** Ver GD-002 (quando chove, se a compactação é permanente, se a chuva leve também afeta areia despejada e o compactado, ritmo da erosão).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: `RainSimulation` com gotas por célula, 6 testes EditMode novos (suíte 42/42) e Play Mode pelo agente (tempo trocado por código); falta o desenvolvedor jogar e aprovar ritmo e visual.

### REQ-006 — Água: laguinhos, chuva que cria água, erosão da terra e afogamento

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** "Na superfície haverão laguinhos, e a chuva criará pixels de água. A água vai procurar escorrer horizontalmente para formar linhas horizontais e também para baixo. Dentro do formigueiro, se ela ficar X segundos em contato com terra fixa, seja natural ou construída, ela vai converter em terra mole com física. Se a formiga ficar mais de Y segundos debaixo da água ela morre."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-005
- **Itens relacionados:** PRG-014, GD-003
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Água")
- **Decisões pendentes:** Ver GD-003 (valores de X e Y, se as formigas devem desviar da água).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: `WaterSimulation`, laguinhos no gerador e afogamento na colônia; 13 testes EditMode novos (suíte 66/66) e Play Mode pelo agente com chuva forte no 8x (tempo trocado por código).
  - 2026-10-17 — Ajuste pedido pelo desenvolvedor: "A água só pode ser consumida se entrar no formigueiro construido pelo jogador". Infiltração e evaporação na superfície removidas; suíte 74/74.
  - 2026-10-17 — ajuste pedido pelo desenvolvedor: a água que molha o terreno dentro do formigueiro é consumida (não forma lagos) e só a chuva forte gera água. 3 testes novos, suíte 69/69.

### REQ-007 — Formiga rainha e pintura da sala da rainha

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN, ART
- **Descrição:** "Vamos criar a formiga rainha. Ela vai ter o dobro do tamanho das formigas. As formigas vão agora ficar em idle em toda a região do formigueiro conectada com o local da rainha. Junto com isso, crie um sistema de pintura de salas, onde o jogador vai poder 'pintar' a região onde a rainha vai poder ficar. Ela anda apenas naquela região. Em caso múltiplas áreas de rainha, ela fica na maior."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001
- **Itens relacionados:** PRG-015, GD-004
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Rainha e sala da rainha")
- **Decisões pendentes:** Ver GD-004 (comportamento sem sala, formigas novas nascendo na rainha, arte própria da rainha).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: rainha em `AntColony` (sprite 2x em `AntSwarmView`), pincel "Sala da rainha" (tecla B), ociosas pela região do formigueiro ligada à rainha, save versão 3; 6 testes EditMode novos (suíte 75/75) e Play Mode pelo agente (sala pintada e cavada por código).

### REQ-008 — Sala de nutrientes, colheita de plantas da superfície e nutrientes como moeda

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** Nova sala de nutrientes para guardar os recursos coletados fora do formigueiro. O jogador "coleta" folhas e gramas da superfície, que crescem a cada X tempo até o tamanho máximo. As formigas colhem da parte de cima para a de baixo e levam à sala de nutrientes; cada pixel vira um nutriente, levado à sala da rainha, que o consome e aumenta o contador de nutrientes. Nutrientes são a moeda para criar formigas. Os slots da sala não obstruem a passagem das formigas, mas ocupam o pixel; sem slot livre, as formigas não colhem mais.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001, REQ-007
- **Itens relacionados:** PRG-016, GD-005
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Plantas, sala de nutrientes e nutrientes")
- **Decisões pendentes:** Ver GD-005 (valor de X, formas e densidade das plantas, pincel próprio de colheita, colheita automática pela raiz marcada, preço das formigas em nutrientes).
- **Histórico:**
  - 2026-10-17 — registrado.
  - 2026-10-17 — IMPLEMENTED: plantas em `VegetationSimulation` (materiais `PlantGrass`/`PlantLeaf`), pincéis "Colher" e "Sala de nutrientes", colheita, guarda e entrega à rainha em `AntColony`, compra de formigas com nutrientes, save versão 4; 7 testes EditMode novos (suíte 81/81) e Play Mode pelo agente (sala e marcas feitas por código: 12 pixels colhidos e 11 consumidos em ~30 s simulados, sem erros).
  - 2026-10-17 — Ajuste pedido: plantas desenhadas com os sprites de grama/trevo em primeiro plano, crescendo para fora do chão; grama do fundo mais clara. Feito (suíte 82/82, Play Mode pelo agente).
  - 2026-10-17 — Ajuste pedido: colher os pedaços da planta como os da terra. Feito: forma da planta = silhueta do sprite em células, sprite recortado pelo que sobra; plantas nascem completas e se recompõem de baixo para cima (suíte 82/82, Play Mode pelo agente).
  - 2026-10-17 — Ajuste pedido: o pixel guardado leva 10 s (1×) para virar nutriente, sem a formiga esperar. Feito: cura no slot (`ColonySettings.NutrientCureSeconds`), cor de planta indo para a de nutriente, HUD com "curando", save guarda o tempo que falta (suíte 84/84; sem Play Mode).
  - 2026-10-17 — Ajuste pedido: toda grama em primeiro plano colhível e crescimento mais lento (~1 min). Feito: tufos da frente viraram variantes da grama colhível, trevos de cenário desbotados ao fundo, 60 s por pedaço reposto (suíte 84/84; Play Mode pelo agente: só captura de tela, sem colheita).
  - 2026-10-17 — Ajuste pedido: tirar todas as plantas do cenário perto (todas colhíveis); média e distante sem mudança. Feito: o cenário perto só tem cogumelos, gravetos, folhas secas, pedrinhas e grãos; flores viraram mudas folhosas colhíveis; densidade das plantas colhíveis subiu (faixa de 12 colunas, 85%) (suíte 83/83; Play Mode pelo agente: 23 plantas colhíveis na área inicial, só captura de tela).
  - 2026-10-17 — Ajuste pedido: o que cresce não nasce marcado; a planta cresce pela base com a ponta sempre criada, empurrando a planta inteira para cima. Feito: linhas enterradas por planta (`GetSunkRows`), passo de 60 s sobe a planta 1 linha com buracos e marcas, rebrota da ponta colhida, sprite acompanha, formiga acompanha o pixel que subiu, save versão 5 (suíte 84/84; Play Mode pelo agente com crescimento acelerado: rebrote, subida, marcas e colheita durante o crescimento conferidos; dois defeitos achados e corrigidos).
  - 2026-10-17 — Ajuste pedido: no crescimento máximo, só voltar a crescer quando não houver mais pixel para colher, reiniciando a planta para subir completa. Feito em `VegetationSimulation.Grow`/`Sprout` (suíte 85/85; Play Mode pelo agente com crescimento acelerado).
  - 2026-10-17 — Ajuste pedido: a jardineira escolhe sozinha as plantas maiores e mais próximas do formigueiro (nesta ordem), corta sempre de cima para baixo, o jogador não marca mais a colheita e a planta só vale depois de crescer um mínimo. Feito: papel `AntRole.Gardener` (só ele colhe; sem a classe completa da árvore da rainha), planta-alvo em `AntColony`, `VegetationSettings.MinHarvestGrowth` (padrão 1 = completa), pincel "Colher" removido, save versão 7 (suíte 94/94; sem Play Mode).
  - 2026-10-17 — Ajuste pedido: a jardineira só corta (as comuns recolhem os pixels soltos) e o pixel que uma formiga está cortando/pegando fica travado para as outras buscarem outro. Feito em `AntColony` (`_claimed`); teste em Play Mode a 8× com 50 jardineiras e 10 comuns pelo agente: 0 travas duplicadas, colheita e consumo pela rainha funcionando, gargalo nas poucas comuns (suíte 94/94).
  - 2026-10-17 — Ajuste pedido: aumentar o tempo da folha até virar nutriente e, com a sala de nutrientes cheia, os cortadores voltarem ao formigueiro e só retomarem abaixo da metade. Feito: cura de 10 s para 30 s (padrão e valor da cena) e pausa da colheita com histerese em `AntColony` (`HarvestPaused`); teste novo, suíte 128/128; sem Play Mode.

### REQ-009 — Ler o design revisado e organizar Markdown por assunto em Docs

- **Data de registro:** 2026-10-17
- **Estado:** VERIFIED
- **Prioridade:** UNDEFINED
- **Setores:** GAME_DESIGN
- **Descrição:** Ler `Increment_Ant_Game_Design_Completo_Revisado.docx`, criar os Markdown relativos a cada etapa/assunto do projeto e organizar na pasta correta de documentação.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** —
- **Itens relacionados:** GD-006
- **Referências:** [Docs/README.md](../Docs/README.md)
- **Decisões pendentes:** As decisões de design em aberto foram preservadas nos respectivos documentos; esta solicitação cobre a organização documental.
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado a partir do documento fornecido pelo desenvolvedor.
  - 2026-10-17 — IMPLEMENTED: criados 38 Markdown, separados nas pastas temáticas existentes de Docs, com índice e navegação.
  - 2026-10-17 — VERIFIED: conferência automática contra o DOCX confirmou 38 seções, 507 parágrafos, 28 tabelas, 209 linhas e 604 células em ordem; 156 links locais válidos. Evidência e identificação da fonte em [Docs/README.md](../Docs/README.md).

### REQ-010 — Montar o HUD da partida com os sprites 9-slice

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, ART
- **Descrição:** Definir as bordas 9-slice dos sprites de UI e montar o HUD da partida (referência `05-hud-partida`) por um script de Editor idempotente.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** —
- **Itens relacionados:** PRG-017, ART-004
- **Referências:** [HudGuide.md](../Assets/Project/Scripts/Runtime/UI/HudGuide.md)
- **Decisões pendentes:** O que mostrar nos cartões sem dado no jogo (próxima formiga, ovos/larvas/pupas, taxa de nutrientes); fonte pixel; telas Evolução, Rainha, Ambientes, Avisos e menu de pausa.
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: bordas 9-slice, HUD uGUI e setup idempotente; capturas em Play Mode pelo agente, sem uso por uma pessoa.

### REQ-011 — Painel da rainha para escolher qual formiga criar

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** Painel mínimo da rainha com os três modos (automático, tipo específico, pausado) e os tipos de formiga que o jogo já tem, a partir do design da seção 11.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-010
- **Itens relacionados:** PRG-018, GD-004
- **Referências:** [HudGuide.md](../Assets/Project/Scripts/Runtime/UI/HudGuide.md), [11_Reproducao_e_politica_da_rainha.md](../Docs/01_Design/11_Reproducao_e_politica_da_rainha.md)
- **Decisões pendentes:** Quando entram ovo/larva/pupa, berçário e reserva mínima; metas do automático; os demais tipos de formiga.
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: política e painel; suíte 96/96 e captura em Play Mode pelo agente, sem uso por uma pessoa.

### REQ-012 — Mais tipos de formiga e início com 1 jardineira

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** Adicionar mais tipos de formiga e fazer o jogador começar com 1 jardineira.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-011
- **Itens relacionados:** PRG-019, GD-004
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md), [14](../Docs/01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md), [15](../Docs/04_Data/15_Valores_base_de_cada_formiga.md)
- **Decisões pendentes:** Cuidadora e soldado (dependem de berçário e invasoras); se a cortadeira deve colher como a jardineira; custo por tipo.
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: carregadora, cortadeira e escavadora; início com 1 jardineira; suíte 99/99 e captura em Play Mode pelo agente, sem uso por uma pessoa.

### REQ-013 — Ciclo de criação (ovo, larva, pupa, berçário) seguindo o design

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** Seguir o documento de design no ciclo de criação e resolver as duas interpretações de REQ-012 pelo design (cortadeira colhe; custo estável por tipo).
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-012
- **Itens relacionados:** PRG-020, GD-004
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md), [11](../Docs/01_Design/11_Reproducao_e_politica_da_rainha.md), [12](../Docs/01_Design/12_Ciclo_de_vida_contadores_e_recuperacao.md), [13](../Docs/01_Design/13_Capacidade_superlotacao_e_morte_da_rainha.md), [19](../Docs/01_Design/19_Bercario_habilidades_e_balanceamento.md)
- **Decisões pendentes:** Escala de células por vaga (protótipo x design de 256); envelhecimento, recuperação, superlotação e game over; compras B0 a B3 e pincel pago; reserva inicial de 100 N; cuidadora e soldado.
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: ciclo mínimo, berçário, capacidade e custo estável; suíte 105/105 e Play Mode pelo agente, sem uso por uma pessoa.
  - 2026-10-17 — Corrigido, a pedido do desenvolvedor, o excesso de capacidade no passo do nascimento (adultas + crias = capacidade + 1); teste de regressão, suíte 109/109 e Play Mode (7 adultas + 0 crias com capacidade 7 depois do nascimento).

### REQ-014 — Sprites e desenho da cria (ovo, larva, pupa) no berçário

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART, PROGRAMMING
- **Descrição:** "Precisamos então criar a larva e a pupa no mesmo estilo das formigas, pode fazer 2 sprites de cada para elas serem animadas em idle." Depois, em playtest: "o fato de não estar aparecendo no berçário o ovo, larva e pupa". O ovo ganhou 2 sprites também, por ser a primeira etapa.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-013
- **Itens relacionados:** ART-005, PRG-021, PRG-020
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Desenho da cria"), [Art/Ants/](../Assets/Project/Art/Ants/)
- **Decisões pendentes:** Aprovação visual dos sprites (ovo, larva e pupa em vista lateral; vista de cima não foi feita); o que acontece quando há mais crias que pontos de chão (hoje se sobrepõem).
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: 6 sprites (2 quadros por etapa), `BroodView` e pontos de chão no berçário; suíte 109/109 e Play Mode com captura pelo agente (ovo e larva vistos no chão, pupa só por estado e sprite ativo), sem avaliação visual do desenvolvedor.

### REQ-015 — Operária leva o ovo da rainha ao berçário e cheat F1 de formigas

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "A rainha bota o ovo, a formiga operária pega o ovo e leva para o berçário, em uma célula vazia. Adiciona também um cheat para adicionar 50 formigas operárias e 50 formigas cortadeiras para facilitar os testes para mim. Pode adicionar no atalho F1." O F1 já alternava o HUD de depuração; o agente moveu esse alternador para o F2.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-013, REQ-014
- **Itens relacionados:** PRG-022, PRG-020, PRG-021
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Transporte do ovo", tabela de teclas), [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) (KI-013)
- **Decisões pendentes:** Quem carrega o ovo (hoje operária comum e carregadora; o pedido diz só "operária"); o cheat ignora capacidade adulta e berçário, então a rainha para de pôr ovos até a área escavada acompanhar; o tempo do ovo só corre depois de largado no berçário (escolha do agente).
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: transporte do ovo e cheat F1; suíte 113/113 e Play Mode pelo agente (F1 injetado pelo Input System: +50/+50; ovo `Claimed` → `Carried` → `Placed`, segundo ovo em outro ponto), sem uso por uma pessoa.

### REQ-016 — Construir do lado de fora do formigueiro

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Vamos adicionar a opção de poder construir do lado de fora do formigueiro."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** —
- **Itens relacionados:** PRG-023
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Modo construir")
- **Decisões pendentes:** A obra de fora pode fechar a entrada do formigueiro (nada impede); a terra compactada (inclusive a crosta de chuva forte) não serve mais de material; sem pedido de limite de altura nem de tipo de material da obra.
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: marcas de construção no céu, obra que cresce do chão para cima; suíte 114/114 (teste novo), sem Play Mode nem avaliação visual.

### REQ-017 — Árvore da rainha (classes e melhorias)

- **Data de registro:** 2026-10-17
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Precisamos de montar a árvore de habilidades", começando pela árvore da rainha ("vamos começar com a rainha"), a partir das seções 22 a 28 do design.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-011, REQ-012, REQ-013
- **Itens relacionados:** PRG-024, GD-004
- **Referências:** [QueenTreeGuide.md](../Assets/Project/Scripts/Runtime/Colony/QueenTreeGuide.md), [22](../Docs/01_Design/22_Mapa_da_arvore_da_rainha.md), [28](../Docs/04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md), [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) (KI-014)
- **Decisões pendentes:** O que fazer com os 15 nós sem sistema no jogo (hoje EM BREVE); a colônia começa com 1 jardineira mesmo sem J0 (escolha do agente, para a economia partir); a árvore do formigueiro (cultivo, berçário, depósito, resíduos) ainda não foi pedida; custos são as propostas do design, sem balanceamento; abrir a árvore não pausa o jogo (regra em aberto no design).
- **Histórico:**
  - 2026-10-17 — PLANNED: pedido registrado.
  - 2026-10-17 — IMPLEMENTED: nós, compra, classes liberadas, Q1, Q2, O2, L3, E1, save versão 9 e tela Evolução; suíte 127/127 e Play Mode pelo agente, sem uso por uma pessoa.
  - 2026-10-17 — Ajuste pedido: "Cria a HUD de skillTree". Feito: tela cheia com o kit de arte da árvore (abas Formigueiro, só exibição, e Rainha e classes), nós em camadas, ligações por estado, detalhe e legenda; capturas e compra pelo botão em Play Mode pelo agente.

### REQ-018 — Revisar as funções de cada formiga por papel contra o design

- **Data de registro:** 2026-10-17
- **Estado:** IN_PROGRESS
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** Trabalhar nas funções das formigas, por setor/papel (comum, carregadora, cortadeira, escavadora, jardineira). Primeiro exemplo apontado: a jardineira estava errada (cortava plantas; no design ela cuida do cultivo de fungo).
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-012, REQ-017
- **Itens relacionados:** PRG-025
- **Referências:** [14](../Docs/01_Design/14_Tipos_de_formiga_e_divisao_do_trabalho.md), [15](../Docs/04_Data/15_Valores_base_de_cada_formiga.md), [24](../Docs/01_Design/24_Carregadora_e_cortadeira_habilidades_e_numeros.md), [25](../Docs/01_Design/25_Escavadora_habilidades_e_numeros.md), [26](../Docs/01_Design/26_Jardineira_e_cuidadora_habilidades_e_numeros.md)
- **Decisões pendentes:** Cultivo de fungo (C0, J0 a J2) ainda não existe: a jardineira fica sem tarefa até ser implementado; revisão dos demais papéis (carregadora, cortadeira, escavadora, comum) contra os documentos 14, 15 e 24 a 26.
- **Histórico:**
  - 2026-10-17 — registrado; desenvolvedor escolheu passar o corte à cortadeira (com comum e carregadora cortando quando ociosas) e revisar papel por papel.
  - 2026-10-17 — IN_PROGRESS: corte corrigido (PRG-025); carga por viagem, prioridades, escavadora sem ovo/nutriente, O1/L1/F1/E2 e rocha quebrável (PRG-026, suíte 133/133); faltam o cultivo de fungo e vida/dano/longevidade (sessão separada).
  - 2026-10-17 — cuidado do berçário e cuidadora (PRG-027, suíte 138/138); a região do cultivo de fungo (jardineira) reaproveita o modelo.
  - 2026-10-17 — cuidado da sala de nutrientes pela jardineira e comuns (PRG-028, suíte 143/143, sem Play Mode).
  - 2026-10-17 — cuidado reformulado: bloco em interação recebe o ritmo da especialista, blocos variados e prioridade pelo número de blocos da região (PRG-027/028, suíte 147/147, sem Play Mode).

### REQ-019 — Variantes de cor das formigas em pastas por função

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART
- **Descrição:** Separar as artes de Ants por função, duplicar os quadros Top e Walk e aplicar as cores sugeridas na conversa; integrar as artes no jogo posteriormente. Recoloração por script autorizada pelo desenvolvedor.
- **Critérios de aceitação:** Pastas por função, cópias de Top e Walk com a paleta sugerida e operária mantendo o vermelho.
- **Dependências:** —
- **Itens relacionados:** ART-006
- **Referências:** [Paleta e organização](../Assets/Project/Art/Ants/README.md)
- **Decisões pendentes:** Aprovação visual das cores; integração no jogo em tarefa futura.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: 48 PNGs em seis pastas; comparação de 10.368 pixels e hashes confirmou dimensões, transparência, paleta e preservação dos originais; prévia inspecionada pelo agente. Importação/reprodução na Unity não verificadas.

### REQ-020 — Formigas andam livres do lado de fora (meio da arte do cenário e das obras)

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "As formigas podem andar livremente na área externa, incluindo o meio de props e construções. Antes só podiam andar pelo contorno da arte." Esclarecido pelo desenvolvedor: todo pixel coberto pela arte de cogumelo, graveto, folha seca e pedrinha vira fundo caminhável, como a parede de trás do túnel; construções do lado de fora do formigueiro também ficam completamente caminháveis.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-002, REQ-016
- **Itens relacionados:** PRG-029
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Andar livre do lado de fora")
- **Decisões pendentes:** Grãos (migalhas de menos de 1 célula) ficaram fora do fundo caminhável; confirmar.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: PRG-029; 7 testes novos passando e Play Mode pelo agente (8.266 células do céu cobertas pela arte, todas caminháveis). Sem uso por uma pessoa.

### REQ-021 — Reserva do pixel a coletar: uma formiga por pixel, sem bando indo e voltando

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Comportamento de busca de pixel das formigas leva um grande grupo. O pixel que vai ser coletado precisa de uma flag para informar que ele já vai ser interagido para impedir outras formigas de irem atrás do mesmo, e um grande grupo se mover de uma vez e ficar voltando quando for coletado."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-001, REQ-018
- **Itens relacionados:** PRG-030
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Reserva do pixel marcado")
- **Decisões pendentes:** Nenhuma
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: PRG-030; 2 testes novos passando e Play Mode pelo agente (16 formigas, 136 pixels marcados, nenhuma reserva repetida). Sem uso por uma pessoa.

### REQ-022 — Menu do ESC e menu de configurações do jogo

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Apertar ESC abre o menu. Crie já o menu de configurações do jogo."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-010
- **Itens relacionados:** PRG-031
- **Referências:** [HudGuide.md](../Assets/Project/Scripts/Runtime/UI/HudGuide.md) ("Menu do ESC e configurações"), [SettingsGuide.md](../Assets/Project/Scripts/Runtime/Settings/SettingsGuide.md)
- **Decisões pendentes:** Lista de opções escolhida pelo agente (volumes geral/música/efeitos, efeitos visuais, tela cheia, resolução, VSync, limite de FPS, mostrar FPS e tempo); confirmar se falta alguma (idioma, velocidade da câmera, teclas). Abrir o menu pausa a simulação; confirmar.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: PRG-031; 4 testes novos passando e Play Mode pelo agente (menu, camadas do ESC, aplicação das opções e arquivo gravado). Sem uso por uma pessoa.

### REQ-023 — Alimentar o jogo com SFX e VFX

- **Data de registro:** 2026-10-18
- **Estado:** PLANNED
- **Prioridade:** UNDEFINED
- **Setores:** AUDIO, ART, PROGRAMMING
- **Descrição:** "A nossa próxima tarefa vai ser alimentar com SFX e VFX."
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-022
- **Itens relacionados:** AUD-001, ART-007, PRG-031
- **Referências:** [SettingsGuide.md](../Assets/Project/Scripts/Runtime/Settings/SettingsGuide.md) ("Como ligar SFX, música e VFX")
- **Decisões pendentes:** Quais eventos ganham som e efeito, origem dos arquivos (biblioteca, gerados, encomendados) e se haverá música.
- **Histórico:**
  - 2026-10-18 — PLANNED: anunciado pelo desenvolvedor como a próxima tarefa.

### REQ-024 — Experimento: célula desenhada em 16x16 e mapa gerado dobrado

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART, PROGRAMMING
- **Descrição:** "Vamos fazer uma experimentação de dobrar o elemento artístico. Hoje, cada pixel equivale a 1 pixel. Vamos fazer ser 16x16 cada pixel, e aumentar o geracional de mapa." Esclarecido: "tudo aumenta de tamanho"; a formiga continua pegando uma célula, que agora tem 16 px; mapa dobrado (1920×1040, área inicial 640×400). Feito na branch `experimento/pixel-16x16`.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** —
- **Itens relacionados:** ART-008, PRG-032
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Tiles 16x16"), KI-016 em [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md)
- **Decisões pendentes:** Se o experimento vai para a `main`; redesenhar formigas e cenário a 16 px por célula (hoje 6); texturas de material definitivas e bordas (autotile); se o mapa dobrado compensa a memória a mais (ver Histórico).
- **Histórico:**
  - 2026-10-18 — IMPLEMENTED: shader de tiles, zoom inicial de 16 px por célula e mapa dobrado; Play Mode pelo agente (textura e contorno vistos em captura). Sem uso por uma pessoa.
  - 2026-10-18 — Testes de coleta e física: 64/67, as 3 falhas eram as de antes (KI-015), com os mesmos números.
  - 2026-10-18 — Falhas antigas corrigidas (KI-015 resolvido); suíte EditMode 173/173 na Unity com o mapa dobrado.
  - 2026-10-18 — Pedido complementar: "Faça as formigas carregarem o pixel relativo que foi eliminado, assim como fisicamente, faça o elemento ser referente aquele bloco." A formiga leva o mesmo bloco 16x16 da célula de onde tirou o pixel, do tamanho de uma célula. Visto em Play Mode pelo agente. Custo medido do mapa dobrado (pergunta do desenvolvedor): ver KI-016.

### REQ-025 — Ampliar a resolução da arte das formigas em 6x mantendo o tamanho no jogo

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART, PROGRAMMING
- **Descrição:** Aumentar em 6x a resolução das artes das formigas para permitir melhor detalhamento, mantendo o tamanho delas no jogo, conforme esclarecimento do desenvolvedor.
- **Critérios de aceitação:** Resolução 6x em cada eixo e mesmo tamanho no jogo.
- **Dependências:** —
- **Itens relacionados:** ART-009, PRG-006
- **Referências:** [Arte e validação](../Assets/Project/Art/Ants/README.md); [guia](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md)
- **Decisões pendentes:** —
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado; desenvolvedor esclareceu que o tamanho no jogo deve ser mantido.
  - 2026-10-18 — IMPLEMENTED: 72 PNGs adultos em 108x72, densidade 36 px/unidade; gerador e importador atualizados. Comparados 559.872 pixels RGBA e metadados, preservando GUIDs e pivôs. Confirmação final na Unity pendente (refresh excedeu o tempo de espera; constante nova ainda ausente no tipo carregado); console sem erros na consulta. Sem build.

### REQ-026 — Melhorar formigas adultas e cenário em pixel art sem blur nas bordas

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART
- **Descrição:** Melhorar a qualidade gráfica das formigas adultas e dos elementos de cenário, mantendo o estilo pixel art, pixel perfect e sem blur nas bordas. O desenvolvedor confirmou ambas as categorias.
- **Critérios de aceitação:** Melhorar as duas categorias mantendo pixel art e bordas nítidas, sem blur.
- **Dependências:** ART-009
- **Itens relacionados:** ART-010
- **Referências:** [Formigas](../Assets/Project/Art/Ants/README.md); [cenário](../Assets/Project/Art/Scenery/README.md); [prompts](../Assets/Project/Art/PixelArtGeneration.json)
- **Decisões pendentes:** —
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado após esclarecimento do escopo (formigas adultas e cenário).
  - 2026-10-18 — IMPLEMENTED: refinados 72 quadros adultos e 69 elementos de cenário; alpha binário, paletas limitadas, dimensões e metadados preservados. Validados 1.390.673 pixels; prévias inspecionadas; Unity reconheceu 141 sprites sem falhas nas configurações e reimportou todos de forma síncrona. Aprovação visual e reprodução em Play Mode pendentes. Sem build.

### REQ-027 — Cultivo de nutrientes com terra fofa, planta e água

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN, ART
- **Descrição:** Pedido do desenvolvedor: "além do pedaço da planta, também vai precisar de água e de terra fofa (a terra que tem física). A terra fofa será colocada na sala de nutrição, depois a formiga vai colocar a planta neste mesmo bloco e depois vai colocar o bloco de água. Nisso vai fazer com que os 4 lados gerem um bloco de nutrição já pronto, no mesmo tempo anterior que era feito para converter em nutrição. Pode fazer animação do crescimento." Decisões do desenvolvedor em 2026-10-18: a água vem de qualquer água do mapa; a terra fica (só planta e água se gastam); só lados livres geram nutriente (no chão, 3).
- **Critérios de aceitação:** UNDEFINED
- **Dependências:** REQ-008
- **Itens relacionados:** PRG-033, GD-005
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Canteiros"); KI-017 em [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md)
- **Decisões pendentes:** Arte definitiva do broto e do nutriente brotando.
- **Histórico:**
  - 2026-10-18 — IMPLEMENTED: canteiros (terra fofa → planta → água → brota em 30 s e põe nutriente pronto nos lados livres), animação do crescimento e save versão 12. Suíte EditMode 176/176 na Unity. Sprites vistos em captura de câmera pelo agente; ciclo completo não exercitado por uma pessoa em Play Mode.
  - 2026-10-18 — Pedido complementar: "A terra fofa pode ser coletada da área externa, que já existe" e "ela também pode ser colocada nas paredes da colônia". Formigas ociosas buscam terra fofa na pilha de fora quando não há terra cavada a caminho; o canteiro pode ficar no chão ou na parede e fica preso onde foi posto. Testes de cultivo, nutrientes e cuidado passando no harness .NET (35/35; suíte 133 de 148, as 15 restantes usam código nativo da Unity); a Unity estava desconectada do MCP e não rodou esta versão.
  - 2026-10-18 — Pedido complementar (com imagem da sala marcada no meio): "tem que colocar os blocos de terra fofa nesta região também. No estilo de construir um bloco nas regiões". O canteiro pode ficar em qualquer ponto da sala (chão, parede ou meio), espaçado de 3 células e sem fechar a passagem. Testes de cultivo, nutrientes e cuidado 36/36 no harness .NET (salão 11x8 recebe 6 ou mais canteiros, com algum no meio, sem célula isolada); sem Unity e sem Play Mode.


### REQ-029 — Ataques de cupins, formigas de ataque (soldado) e área de resíduos

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN, ART
- **Descrição:** Pedido do desenvolvedor: depois de a colônia ter no mínimo 15 formigas, a cada 1 a 5 minutos acontece um ataque de cupins (arte já existente), com alerta no centro do topo da tela. Vêm 40% do número de formigas em cupins voando, que pousam em locais aleatórios, largam as asas e vão ao formigueiro destruir todas as crias, depois as fazendas (canteiros) e depois 2 camadas de parede em volta de todos os setores, menos a sala da rainha; feito isso, saem pelas laterais do mapa. As formigas param o trabalho e atacam os cupins. Depois do primeiro ataque, o jogador pode liberar as formigas de ataque. Cupins mortos são desmembrados em "folha" para o cultivo. Libera também a área de resíduos, onde as formigas põem os restos dos cupins e a terra fofa das paredes destruídas.
- **Critérios de aceitação:** UNDEFINED
- **Dependências:** REQ-027 (arte da inimiga alada), PRG-033 (canteiros), PRG-024 (árvore da rainha)
- **Itens relacionados:** PRG-034, GD-007, ART-011
- **Referências:** [RaidGuide.md](../Assets/Project/Scripts/Runtime/Colony/RaidGuide.md); [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md) (KI-018)
- **Decisões pendentes:** balanceamento (vida, dano, intervalo) e o valor de X (crescimento do teto por ataque, hoje 20 pontos); se a rainha pode ser atacada (game over no design); se a área de resíduos passa a ser compra (R0) na árvore do formigueiro.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado. Decisões do desenvolvedor na mesma conversa: parede destruída vira terra fofa solta; a área de resíduos é uma sala pintada (pincel novo); formigas podem morrer em combate mesmo com a morte desligada.
  - 2026-10-18 — IMPLEMENTED: ataques, cupins (voo, pouso, objetivos, saída), combate, corpos em folhas e restos, área de resíduos com decomposição, soldado (S0 a S2) liberado após o primeiro ataque, alerta no HUD, cheat F4 e save versão 13. Testes novos 6/6 no harness .NET (suíte 152 de 186; as 34 restantes dependem de código nativo da Unity ou do módulo JSON e já não rodavam no harness); compilação da Unity sem erros. HUD remontado, arte dos cupins ligada na cena e Play Mode pendentes: a instância Incrementant não estava conectada ao MCP.
  - 2026-10-18 — Pedido complementar: "Apenas formigas guerreiras vão atrás dos cupins. Formigas que não são guerreiras apenas defendem quando o cupim começa a atacar." e "limite crescente até 200% [...] no padrão aleatório. No mínimo 40% do total de formigas e no máximo 200% (começando em 40% e crescendo em X a cada ataque)". Implementado: só o soldado caça; as demais mordem de volta só quando mordidas e voltam ao trabalho; fração sorteada entre 40% e um teto que sobe 20 pontos por ataque (valor de X escolhido pelo agente, ajustável em Raid Settings) até 200%. Suíte EditMode 190/190 na Unity; Play Mode pelo agente: com 4 soldados e 18 outras, nenhuma operária perseguiu cupim, os mordidos se defenderam e mataram os 5 cupins junto aos canteiros sem perder crias; 10 cupins para 22 formigas (41% sorteado com teto de 60%) e teto de 100% depois de 3 ataques.
  - 2026-10-18 — Pedido: pincel "Dedo" no HUD (clique fere o cupim, sem pintar); a rainha ataca os cupins com 3x o dano do soldado; os cupins podem matar a rainha (game over); postura defensiva/ofensiva no menu da rainha. Implementado: 7º pincel Dedo (2 de dano por clique, raio 2); rainha com 60 de vida e 9 por mordida; depois das paredes os cupins vão atrás dela (Hunt Queen) e vida zero = tela GAME OVER (carregar save ou novo mapa); botões Defensiva (só revida) e Ofensiva (sai atrás do invasor), postura no save (versão 14). Vida da rainha (60), raio do dedo e caça à rainha escolhidos pelo agente, ajustáveis em Raid Settings. Suíte EditMode 199/199; Play Mode: HUD sobe e o dedo fere o cupim; tela de game over e botões não vistos por uma pessoa.
  - 2026-10-18 — Novo ajuste: "Diminua a quantidade crescente de cupins" e "Se o cupim entrar no formigueiro, todas as formigas vão atrás dos que entraram." Implementado: teto sobe 5 pontos por ataque até 100% (antes 20 até 200%); cupim vivo abaixo da superfície faz todas as formigas largarem o trabalho e caçá-lo até não restar nenhum (na superfície segue só o soldado). Suíte EditMode 195/195 na Unity; não jogado por uma pessoa.
  - 2026-10-18 — Pedido complementar (com captura): "Na etapa de destruição, vamos fazer destruído todos os blocos dentro da seção, antes de destruir as paredes ao redor." Também relatado: o jogador conseguia tirar a marca do entulho com o pincel. Implementado: etapa nova de setores (todo bloco sólido da área pintada vira entulho) antes das paredes, e o pincel não desmarca entulho. Teste do setor maior que a parte escavada confere a ordem (interior inteiro, depois paredes) e que não sobra terra firme na área pintada.
  - 2026-10-18 — Pedido complementar (com captura): "Vamos fazer com que os cupins destruam o chão até chegarem em seu objetivo, seja onde for este percurso. Mantém a regra dos segundos sem alcançar o destino [...] se o setor for maior que as paredes do local, os cupins vão destruindo tudo até chegar na parede respectiva." Implementado: paredes contadas em volta de toda a área pintada do setor; sem caminho livre, o cupim planeja e cava um túnel até o objetivo (a terra do caminho some, o alvo vira entulho); andar e cavar contam como progresso para o tempo sem progresso. Teste novo com setor pintado maior que a parte escavada (cavam até as paredes de fora e derrubam todas).
  - 2026-10-18 — Correção (segundo relato com capturas: na etapa das paredes derrubaram algumas e passaram a vagar): a parede derrubada vira entulho no lugar e, no chão e nas laterais, fica apoiada, então a segunda camada não encostava em célula livre e saía do campo dos cupins. Agora alcançam a parede através de até 8 células de entulho. Teste novo (sala fechada, com e sem física granular) reproduzia a falha (48 de 92 e depois 3 de 92 sem derrubar) e passa com a correção.
  - 2026-10-18 — Correção (relato do desenvolvedor com captura: cupins foram embora sem derrubar as paredes do setor): o tempo sem progresso que faz o objetivo avançar corria desde o começo do ataque, inclusive no voo e na caminhada até o formigueiro, e só zerava ao destruir algo; cupins que pousavam longe viam os objetivos vencerem no caminho. Agora zera enquanto algum cupim chega, avança, destrói ou luta. Teste novo reproduzia a falha (cupim a ~900 colunas) e passa com a correção.
  - 2026-10-18 — IMPLEMENTED (validação): com o MCP conectado, HUD remontado (6 pincéis, 7 tipos, alerta) e arte dos cupins e do soldado ligada na cena. Suíte EditMode 186/186 na Unity. Play Mode pelo agente em 2026-10-18 (cena AntFarm, HUD remontado e arte ligada): F4 com 20 formigas trouxe 8 cupins voando, alerta com contagem e "CUPINS ATACANDO", pouso e caminhada com a arte nova, todas as formigas lutando; sem alvos, saíram pelas laterais e o fim liberou o pincel Resíduos e o soldado; num segundo ataque dentro do formigueiro, 5 cupins mortos sem baixa, 2 paredes viraram entulho, corpos desmembrados, folhas plantadas nos 3 canteiros e restos empilhados na área de resíduos. O Play Mode achou um bug (o alerta do F4 virava o intervalo do próximo ataque ao chegar a 15 formigas), corrigido com teste novo (RaidTests 7/7). Falta uma pessoa jogar.

### REQ-028 — Formigas não morrem por padrão

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** Pedido do desenvolvedor: "Coloque que, por padrão, as formigas não morrem mais." A morte (velhice e afogamento) começa desligada; continua podendo ser ligada pelo Inspector (**Ant Death**), pelo F3 ou pelo HUD de depuração (F2).
- **Critérios de aceitação:** UNDEFINED
- **Dependências:** —
- **Itens relacionados:** PRG-020
- **Referências:** [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) (tabela de teclas, F3)
- **Decisões pendentes:** —
- **Histórico:**
  - 2026-10-18 — IMPLEMENTED: padrão de `AntFarmGame._antDeath` e valor da cena `AntFarm.unity` passaram a desligado. Sem Play Mode (Unity desconectada do MCP).

### REQ-027 — Arte de formiga inimiga com asas na chegada e sem asas após pousar

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART
- **Descrição:** Criar arte de inimigos no mesmo estilo das formigas: elas chegam com asas, pousam, perdem as asas e passam a andar pelo ambiente.
- **Critérios de aceitação:** Mesmo estilo das formigas existentes; arte com asas para chegada e sem asas para caminhada após pousar.
- **Dependências:** —
- **Itens relacionados:** ART-011
- **Referências:** [Pacote de sprites](../Assets/Project/Art/Ants/InimigaAlada/README.md); [prompt](../Assets/Project/Art/Ants/InimigaAlada/Generation.json)
- **Decisões pendentes:** —
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido de arte registrado.
  - 2026-10-18 — IMPLEMENTED: 16 PNGs de 144x144 (4 voo, 4 pouso/perda de asas, 4 caminhada lateral e 4 superior), com alpha 0/255 e paleta comum de 24 cores. Validados 331.776 pixels e prévias. Importador estendido para Fly/Land a 36 PPU. Compilação/importação na Unity e Play Mode pendentes porque Incrementant não estava conectado. Mecânicas de inimigos não implementadas; nenhuma build gerada.

### REQ-030 — Tutorial da primeira abertura do jogo

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, GAME_DESIGN
- **Descrição:** Criar, com a UI que já existe, o tutorial da primeira vez que o jogador abre o jogo: abertura com a câmera enquadrando a rainha e as operárias; pincel de escavação e primeira câmara; área de nutrientes; berçário e reprodução da rainha; árvore de evolução; inimigos e defesa; despedida. Câmera automática só nas transições, botão "Ver objetivo" se o jogador se afastar.
- **Critérios de aceitação:** Pendente de definição (o roteiro de etapas, textos e botões veio no pedido).
- **Dependências:** REQ-022
- **Itens relacionados:** PRG-035
- **Referências:** [TutorialGuide.md](../Assets/Project/Scripts/Runtime/Tutorial/TutorialGuide.md); [Garantias do tutorial](../Docs/01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md)
- **Decisões pendentes:** não há cinematic no projeto, então a abertura desce da vista mais aberta até o abrigo; pop-ups de "Continuar" extras nas etapas da rainha e dos inimigos; progresso no meio do tutorial não é salvo (recomeça na próxima abertura).
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: etapas, câmera, destaques e pop-ups prontos; planejador coberto por 5 testes EditMode; Play Mode pelo agente até a árvore de evolução. Etapas de inimigos e despedida só revisadas no código.


### REQ-031 — Menu principal com fundo de colônia trabalhando e sem splash da Unity

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, ART
- **Descrição:** Tirar o splash da Unity e criar um menu principal seguindo a imagem de referência (Iniciar nova colônia, Continuar, Configurações, Sobre o jogo, Sair). O fundo simula um gameplay: um formigueiro já pré-produzido com as formigas se movendo e colhendo recursos, sem HUD de gameplay. Depois, pedido o botão Menu principal no menu de pausa.
- **Critérios de aceitação:** Pendente de definição.
- **Dependências:** REQ-022
- **Itens relacionados:** PRG-036
- **Referências:** [MenuGuide.md](../Assets/Project/Scripts/Runtime/Menu/MenuGuide.md); [Menus e fluxo de telas](../Docs/02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md)
- **Decisões pendentes:** splash da Fábula; confirmação ao iniciar nova colônia havendo save; textos e créditos do Sobre o jogo.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: cena MainMenu, vitrine, botões e menu de pausa com Menu principal; Play Mode pelo agente (fundo, Configurações, Continuar com save, Menu principal pela pausa).

### REQ-032 — Sala Gotas de Orvalho

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING
- **Descrição:** "Crie a sala Gotas de orvalhos, que as formigas operárias e jardineiras podem trabalhar. A lógica delas é as formigas colocarem terra fofa em 30% da parte de cima da sala, depois vão colocar folha. Após isso, a cada X segundos vai cair uma gota de água dela. Se a gota cair dentro da sala, ela não é absorvida pelas paredes."
- **Critérios de aceitação:** Pendente de definição.
- **Dependências:** —
- **Itens relacionados:** PRG-037
- **Referências:** [DewRoomGuide.md](../Assets/Project/Scripts/Runtime/Colony/DewRoomGuide.md)
- **Decisões pendentes:** valores assumidos pelo agente (X = 8 s, gota de 1 célula, folha em colunas pares, altura mínima de coluna 4, teto de 24 células de água na sala); onde a gota nasce; se os cupins devem destruir a sala; ícone do botão.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: sala, pincel, etapas, gota e água não absorvida; suíte EditMode 217/217 na Unity (8 testes novos).

### REQ-033 — Arte do pulgão menor que a formiga

- **Data de registro:** 2026-10-18
- **Estado:** IMPLEMENTED
- **Prioridade:** UNDEFINED
- **Setores:** ART
- **Descrição:** "Assim como para as formigas, crie para pulgão. A arte deve ser menor que a da formiga."
- **Critérios de aceitação:** Arte no padrão das formigas e menor que a delas.
- **Dependências:** —
- **Itens relacionados:** ART-012
- **Referências:** [Sprites, escala e prompt](../Assets/Project/Art/Aphids/README.md)
- **Decisões pendentes:** aprovação visual pelo desenvolvedor.
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado.
  - 2026-10-18 — IMPLEMENTED: oito PNGs (4 laterais, 4 superiores), 72x48 a 36 PPU. Validador na Unity conferiu 27.648 pixels, dimensões, pivôs, alpha binário, paleta e importação. Arte pronta para integração; novo importador aguarda recarga após Play Mode.

### REQ-034 — Efeitos de dopamina (juice): botões animados, partículas e animações de feedback

- **Data de registro:** 2026-10-18
- **Estado:** PLANNED
- **Prioridade:** UNDEFINED
- **Setores:** PROGRAMMING, ART
- **Descrição:** "Precisamos agora de adicionar efeitos de dopamina no projeto." Botões animados, com feedback visual de clique e hover; efeitos no jogo com partículas e animações como fade, squash e pulo. Análise e skill primeiro; implementação depois, em fases.
- **Critérios de aceitação:** Pendente de definição
- **Dependências:** REQ-022, REQ-023
- **Itens relacionados:** PRG-038, PRG-039, PRG-040, PRG-041, PRG-042, PRG-043, PRG-044, PRG-045, ART-007
- **Referências:** [Skill juice-dopamina](../.claude/skills/juice-dopamina/SKILL.md); [estado atual e lacunas](../.claude/skills/juice-dopamina/references/estado-atual.md); [arquitetura e ordem de execução](../.claude/skills/juice-dopamina/references/arquitetura.md); [35 — Áudio atmosfera e juice](../Docs/02_World_Art_UI/35_Audio_atmosfera_e_juice.md)
- **Decisões pendentes:** Paleta de feedback e aprovação visual; se haverá opção separada para tremor de câmera; quais marcos ganham momento próprio; relação com REQ-023 (a parte de VFX dele é entregue por este pedido).
- **Histórico:**
  - 2026-10-18 — PLANNED: pedido registrado; análise do projeto e skill `juice-dopamina` criadas (sem código de jogo); ordem de execução em PRG-038 a PRG-045.
  - 2026-10-18 — IN_PROGRESS: fases 0 e 1 implementadas (`Vfx`, `Easing`, `ButtonJuice` no HUD, menu e compra negada); testes EditMode passam; falta conferir em Play Mode.
  - 2026-10-18 — IN_PROGRESS: fase 2 implementada (`PanelAnimator` nos painéis do HUD e menu do ESC, `Pulse` em Nutrientes e Formigas); compila, falta Play Mode.
  - 2026-10-18 — IN_PROGRESS: fases 1 e 2 confirmadas em Play Mode pelo desenvolvedor (PRG-039, PRG-040 VERIFIED); fase 3 implementada (`AntColony.Fx`, `WorldFx`: escavar, despejar, obra, nascimento, cupim); compila, falta Play Mode.
  - 2026-10-18 — IN_PROGRESS: fase 3 confirmada em Play Mode pelo desenvolvedor (PRG-041 VERIFIED, partículas ampliadas a pedido); fase 4 implementada (`FloatingText`: +N de nutrientes, -N de gasto, -1 de formiga ferida); compila, falta Play Mode.
