# Solicitações do desenvolvedor

**Projeto:** Incrementant
**Última atualização:** 2026-10-17

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
