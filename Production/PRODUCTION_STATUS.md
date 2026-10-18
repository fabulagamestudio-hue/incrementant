# Incrementant — Estado da produção

## Identificação

- **Nome:** Incrementant
- **Descrição:** Formigueiro visto de lado com terreno em pixels; formigas cavam e levam a terra para fora (em fase de protótipo de viabilidade).
- **Engine:** Unity 6000.3.11f1, URP 17.3.0
- **Plataformas-alvo:** Pendente de definição
- **Marco atual:** Pendente de definição
- **Última avaliação:** 2026-10-18

## Resumo executivo

Ataques de cupins implementados (PRG-034, REQ-029): a partir de 15 formigas, a cada 1 a 5 minutos um alerta no topo anuncia o ataque; cupins (sorteados entre 40% das formigas e um teto que sobe a cada ataque até 200%) pousam pelo mapa, destroem crias, canteiros e paredes dos setores (menos a sala da rainha) e saem pelas laterais; só os soldados vão caçá-los, as demais formigas se defendem quando mordidas. Corpos viram folhas para os canteiros e restos para a nova área de resíduos; o primeiro ataque libera esse pincel e o soldado. Suíte EditMode 190/190 na Unity e ataques exercitados em Play Mode pelo agente (HUD e arte já na cena); falta uma pessoa jogar e balancear.

Apertar ESC abre o menu do jogo (continuar, configurações, salvar, carregar, sair), que pausa a simulação; a tela de configurações tem volumes, efeitos visuais, tela cheia, resolução, VSync, limite de FPS e o contador do canto, gravados num arquivo próprio (PRG-031, REQ-022). Testes novos e Play Mode pelo agente; falta uso por uma pessoa. Os volumes de música e efeitos e o nível de efeitos visuais esperam os sistemas de SFX e VFX, próxima tarefa anunciada (REQ-023).

As formigas passaram a reservar o pixel marcado ao sair para buscá-lo: cada pixel tem no máximo uma formiga indo atrás dele, e as demais vão a outro pixel ou a outro trabalho, em vez de um bando ir e voltar (PRG-030, REQ-021). Testes novos e Play Mode pelo agente; falta ver o movimento em jogo.

As formigas passaram a andar livres do lado de fora: pelo meio da arte do cenário de perto (cogumelos, gravetos, folhas secas, pedrinhas) e por dentro das obras de fora, não só pelo contorno (PRG-029, REQ-020). Testes novos e Play Mode pelo agente; quatro testes antigos já falhavam no working tree e foram registrados em KI-015.

Variantes de cor das formigas preparadas em seis pastas (48 PNGs Top/Walk, ART-006, REQ-019), com dimensões, transparência e troca de paleta conferidas por comparação de pixels. A integração no jogo fica para depois, conforme pedido do desenvolvedor; importação na Unity e aprovação visual pendentes.

A referência de design revisada de 2026-10-17 foi organizada em 38 Markdown em [Docs/](../Docs/README.md), preservando 28 tabelas e as propostas e decisões em aberto (GD-006, REQ-009). A transcrição foi conferida automaticamente; a implementação dos sistemas não foi reavaliada nessa entrega.

Protótipo de viabilidade do formigário (REQ-001) implementado na cena `AntFarm`: terreno em pixels com física granular, formigas que cavam pixels marcados pelo jogador e os despejam na superfície, e compra de formigas com a terra entregue. O mapa é procedural por padrão (novo a cada Play) e pode ser fixado na cena como PNG editável, com ninho e despejo posicionáveis. No modo procedural, o mapa cresce para os lados e para baixo conforme a câmera se move, até um limite de mundo (960x520), com paralaxe no céu e na grama distante (REQ-003). A velocidade 8x roda com FPS próximo do 1x graças à simulação por blocos (REQ-004). Há dois tipos de chuva, trocados no HUD ou com C: a leve dissolve a terra despejada aos poucos e a forte compacta areia e terra solta, que param de cair (REQ-005). A chuva também cria água, que escorre, forma poças e laguinhos na superfície, amolece a terra dentro do formigueiro e afoga formigas (REQ-006). Há uma formiga rainha (o dobro do tamanho) que só anda na sala pintada pelo jogador (a maior, se houver várias), e as formigas sem trabalho vagam por todo o formigueiro ligado a ela (REQ-007). Grama e mudas crescem na superfície; o jogador as marca com o pincel Colher, as formigas colhem da ponta para a base e guardam cada pixel na sala de nutrientes pintada, e outras levam os nutrientes à rainha, que os consome: nutrientes passaram a ser a moeda das formigas novas (REQ-008). As formigas são sprites animados com pixels menores que os do terreno, e a superfície tem um cenário (grama, flores, trevos, cogumelos, pedrinhas) na escala delas, montado a partir de PNGs isolados e editáveis de cada elemento (arte provisória, REQ-002). Núcleo testado e exercitado em Play Mode pelo agente; falta playtest do desenvolvedor para avaliar o loop.

## Estado por setor

| Setor | Situação | Documento |
|---|---|---|
| Arte | Só arte provisória (formigas e cenário da superfície) | [ART.md](ART.md) |
| Programação | Protótipo do formigário implementado; núcleo verificado | [PROGRAMMING.md](PROGRAMMING.md) |
| Game design | Referência revisada organizada em 38 Markdown (GD-006); loop do protótipo a validar | [GAME_DESIGN.md](GAME_DESIGN.md) |
| Áudio | Sem conteúdo; SFX planejado (AUD-001, REQ-023) | [AUDIO.md](AUDIO.md) |
| Release | Sem conteúdo | [RELEASE.md](RELEASE.md) |

### Arte

- **Inimiga alada:** ART-011 implementado (REQ-027): 16 sprites para voo, pouso/perda das asas e caminhada sem asas (lateral e superior), preparados a 36 PPU com alpha binário. Em uso como cupim (REQ-029), ligada na cena e vista em Play Mode pelo agente; aprovação visual pendente.

- **Refinamento gráfico:** ART-010 implementado (REQ-026): 72 quadros adultos e 69 elementos de cenário com novo desenho, paleta limitada e alpha 0/255. Dimensões e metadados preservados, 1.390.673 pixels conferidos e 141 PNGs reimportados na Unity; aprovação visual e Play Mode pendentes.

- **Resolução dos adultos:** ART-009 implementado (REQ-025): 72 quadros ampliados 6x para 108x72, mantendo o tamanho no jogo por densidade de 36 px/unidade. Pixels RGBA, GUIDs e pivôs conferidos; confirmação final na Unity pendente.

- **Entregas verificadas:** Nenhuma
- **Variantes por função:** ART-006 implementado; 48 PNGs conferidos, aguardando aprovação visual e futura integração na Unity (REQ-019).
- **Pendências:** ART-008 Texturas 16x16 do terreno (provisórias, experimento REQ-024); ART-007 VFX do jogo (planejado, REQ-023); ART-001 e ART-002 Sprites provisórios das formigas; ART-005 Sprites da cria (ovo, larva, pupa; aprovação visual); ART-003 Cenário da superfície (aprovação visual); ART-004 Sprites de UI 9-slice (bordas e fonte a aprovar)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Programação

- **Entregas verificadas:** PRG-001 Grade e física granular; PRG-002 Formigas; PRG-004 Cena por setup; PRG-005 Modo do mapa; PRG-006 Formigas em sprite; PRG-007 Agarre em paredes; PRG-008 Andar no fundo do formigueiro; PRG-009 Cenário da superfície em camadas; PRG-010 Mundo expansível; PRG-012 Simulação por blocos (8x no mesmo FPS); PRG-013 Chuva leve e forte; PRG-014 Água; PRG-015 Rainha e sala da rainha; PRG-016 Plantas, sala de nutrientes e nutrientes
- **Pendências:** PRG-003 Render, input e HUD; PRG-017 HUD uGUI e PRG-018 Política da rainha e PRG-019 Tipos de formiga e PRG-025 Corte de plantas corrigido e PRG-026 Carga, rocha e prioridades e PRG-027 Cuidado do berçário e PRG-028 Cuidado da sala de nutrientes e PRG-020 Ciclo de criação, PRG-021 Cria desenhada no berçário e PRG-022 Transporte do ovo e cheat F1 e PRG-023 Construção de fora (sem Play Mode) e PRG-024 Árvore da rainha (tela Evolução) e PRG-029 Andar livre do lado de fora e PRG-030 Reserva do pixel marcado e PRG-031 Menu do ESC e configurações (faltam uso com mouse; só Pintar e Rainha funcionam; cria vista só em captura do agente); PRG-011 Câmera, limitador e paralaxe (falta uso real por mouse/teclado); PRG-034 Ataques de cupins e área de resíduos (Play Mode só pelo agente)
- **Bloqueios:** Nenhum
- **Riscos:** Render passou a ser o maior custo por frame (KI-001); soluço único ao montar o cache de navegação de áreas grandes (KI-004)

### Game design

- **Entregas verificadas:** GD-006 Documento revisado convertido e conferido contra o DOCX (38 seções e 28 tabelas); [índice](../Docs/README.md)
- **Pendências:** GD-001 Conceito (alinhamento do protótipo à referência revisada e validação do loop); GD-002 Regras da chuva (interpretações a confirmar); GD-003 Regras da água (valores de X/Y e interpretações a confirmar); GD-004 Regras da rainha (interpretações a confirmar); GD-005 Regras das plantas e nutrientes (interpretações a confirmar)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Áudio

- **Entregas verificadas:** Nenhuma
- **Pendências:** AUD-001 Efeitos sonoros (planejado, REQ-023)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Release

- **Entregas verificadas:** Nenhuma
- **Pendências:** Nenhuma registrada
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

## Funcionalidades principais

| Sistema | Estado | Item |
|---|---|---|
| Terreno em pixels e física granular | VERIFIED | PRG-001 |
| Formigas: cavar e despejar | VERIFIED | PRG-002 |
| Render, input e HUD | IMPLEMENTED | PRG-003 |
| Modo do mapa: procedural (padrão) ou PNG fixo em cena | VERIFIED | PRG-005 |
| Formigas em sprite (rotação, animação, grão na boca) | VERIFIED | PRG-006 |
| Formigas se agarram em paredes/teto sem piso | VERIFIED | PRG-007 |
| Formigas andam no fundo do formigueiro (vista de cima) | VERIFIED | PRG-008 |
| Cenário da superfície na escala das formigas | VERIFIED | PRG-009 |
| Mundo expansível com geração por blocos e limite | VERIFIED | PRG-010 |
| Câmera móvel, limitador e paralaxe | IMPLEMENTED | PRG-011 |
| Simulação por blocos e navegação com cache (8x) | VERIFIED | PRG-012 |
| Chuva leve (erosão) e chuva forte (compactação) | VERIFIED | PRG-013 |
| Água: laguinhos, escoamento, terra mole e afogamento | VERIFIED | PRG-014 |
| Rainha e sala da rainha pintável | VERIFIED | PRG-015 |
| Plantas, colheita, sala de nutrientes e nutrientes como moeda | VERIFIED | PRG-016 |
| Canteiros da sala de nutrientes (terra fofa, planta, água) e animação | IMPLEMENTED | PRG-033 |
| Ciclo de criação: berçário, ovo/larva/pupa, capacidade | IMPLEMENTED | PRG-020 |
| Cria desenhada no chão do berçário | IMPLEMENTED | PRG-021 |
| Operária leva o ovo ao berçário; cheat F1 (+50 operárias, +50 cortadeiras) | IMPLEMENTED | PRG-022 |
| Construção do lado de fora do formigueiro | IMPLEMENTED | PRG-023 |
| Árvore da rainha: classes por compra, Q1, Q2, O2, L3, E1 e tela Evolução | IMPLEMENTED | PRG-024 |
| Formigas andam pelo meio da arte do cenário e das obras de fora | IMPLEMENTED | PRG-029 |
| Menu do ESC e tela de configurações | IMPLEMENTED | PRG-031 |
| Ataques de cupins, combate, área de resíduos e soldado | IMPLEMENTED | PRG-034 |
| Experimento: terreno em tiles 16x16 e mapa dobrado (branch `experimento/pixel-16x16`) | IMPLEMENTED | PRG-032, ART-008 |

## Problemas e riscos

- As quatro falhas antigas de teste foram corrigidas em 2026-10-18 (KI-015, resolvido): eram bugs latentes da ordem da obra e da mudança de cria, e a planta larga parava pela morte por velhice. Suíte EditMode 173/173.
- O render da textura do terreno (~4,3 ms por frame no editor) é agora o maior custo; a simulação no 8x caiu para ~2 ms com o mundo inteiro (KI-002 resolvido). Montar o cache de navegação de uma área grande de uma vez custa ~14 ms (KI-004). Ver [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md).

## Solicitações em aberto

| ID | Título | Estado | Prioridade |
|---|---|---|---|
| REQ-001 | Protótipo de viabilidade do formigário em pixels | IMPLEMENTED | UNDEFINED |
| REQ-002 | Cenário na superfície na escala das formigas | IMPLEMENTED | UNDEFINED |
| REQ-003 | Mapa procedural que cresce com a câmera, paralaxe e limite de câmera | IMPLEMENTED | UNDEFINED |
| REQ-004 | Velocidade 8x com o mesmo FPS e simulação constante | IMPLEMENTED | UNDEFINED |
| REQ-005 | Chuva leve (dissolve a terra despejada) e chuva forte (compacta o granular) | IMPLEMENTED | UNDEFINED |
| REQ-006 | Água: laguinhos, chuva que cria água, erosão da terra e afogamento | IMPLEMENTED | UNDEFINED |
| REQ-007 | Formiga rainha e pintura da sala da rainha | IMPLEMENTED | UNDEFINED |
| REQ-008 | Sala de nutrientes, colheita de plantas da superfície e nutrientes como moeda | IMPLEMENTED | UNDEFINED |
| REQ-014 | Sprites e desenho da cria (ovo, larva, pupa) no berçário | IMPLEMENTED | UNDEFINED |
| REQ-015 | Operária leva o ovo da rainha ao berçário e cheat F1 de formigas | IMPLEMENTED | UNDEFINED |
| REQ-016 | Construir do lado de fora do formigueiro | IMPLEMENTED | UNDEFINED |
| REQ-017 | Árvore da rainha (classes e melhorias) | IMPLEMENTED | UNDEFINED |
| REQ-018 | Revisar as funções de cada formiga por papel contra o design | IN_PROGRESS | UNDEFINED |
| REQ-020 | Formigas andam livres do lado de fora (meio da arte do cenário e das obras) | IMPLEMENTED | UNDEFINED |
| REQ-021 | Reserva do pixel a coletar: uma formiga por pixel, sem bando indo e voltando | IMPLEMENTED | UNDEFINED |
| REQ-022 | Menu do ESC e menu de configurações do jogo | IMPLEMENTED | UNDEFINED |
| REQ-023 | Alimentar o jogo com SFX e VFX | PLANNED | UNDEFINED |
| REQ-024 | Experimento: célula 16x16 e mapa dobrado (branch `experimento/pixel-16x16`) | IMPLEMENTED | UNDEFINED |
| REQ-025 | Resolução 6x da arte das formigas, mantendo tamanho no jogo | IMPLEMENTED | UNDEFINED |
| REQ-026 | Melhorar formigas adultas e cenário em pixel art sem blur nas bordas | IMPLEMENTED | UNDEFINED |
| REQ-027 | Arte de formiga inimiga com asas na chegada e sem asas após pousar | IMPLEMENTED | UNDEFINED |
| REQ-027 | Cultivo de nutrientes com terra fofa, planta e água | IMPLEMENTED | UNDEFINED |
| REQ-028 | Formigas não morrem por padrão | IMPLEMENTED | UNDEFINED |
| REQ-029 | Ataques de cupins, formigas de ataque (soldado) e área de resíduos | IMPLEMENTED | UNDEFINED |

Registro completo: [REQUESTS.md](REQUESTS.md)

## Próximas prioridades

### Definidas pelo desenvolvedor

- SFX e VFX são a próxima tarefa (REQ-023, informado em 2026-10-18).

### Sugeridas pelo agente

- Jogar um ataque (F4 força um) com berçário, canteiros e área de resíduos, e ajustar vida, dano, 40% e intervalo (REQ-029, PRG-034).
- Playtest do protótipo pelo desenvolvedor (cena `AntFarm`), para validar o loop e o input (REQ-001, PRG-003).
- Aprovação visual do cenário da superfície (REQ-002, ART-003).
- Testar a câmera (WASD/botão do meio, Q/E ou Ctrl+scroll) e o crescimento do mapa (REQ-003, PRG-011).
- Jogar no 8x com o mapa grande e confirmar o FPS (REQ-004, PRG-012).
- Jogar com as duas chuvas e ajustar ritmo de erosão e densidade das gotas (REQ-005, PRG-013).
- Jogar com chuva forte até a água entrar no ninho e ajustar X (erosão), Y (afogamento) e a quantidade de água (REQ-006, PRG-014).
- Pintar uma sala da rainha (tecla B até "Sala da rainha"), cavá-la e conferir a rainha e as ociosas espalhadas (REQ-007, PRG-015).
- Pintar uma sala de nutrientes (pincel "Sala de nutrientes"), ver as jardineiras colherem sozinhas (maior planta crescida, de cima para baixo) e conferir o ciclo até a rainha e a compra de formigas com nutrientes (REQ-008, PRG-016).
- Ver as formigas atravessando um cogumelo e uma obra de fora em jogo (REQ-020, PRG-029).
- Abrir o menu com ESC, mexer nas configurações e conferir que elas voltam ao reiniciar o Play (REQ-022, PRG-031).
- Definir o que o protótipo precisa provar para ser considerado viável (critérios de REQ-001).

## Decisões necessárias

- Árvore da rainha: o que fazer com os 15 nós sem sistema no jogo (EM BREVE) e se a árvore do formigueiro entra no protótipo; a jardineira (J0) não tem tarefa até existir o cultivo de fungo (REQ-018, KI-014)
- Tamanho do mundo (960x520) e zoom máximo aberto preso ao enquadramento inicial estão bons? (REQ-003)
- Regras da chuva: quando chove (manual × ciclo automático), compactação permanente ou temporária, se a chuva leve afeta areia despejada e o compactado (GD-002)
- Regras da água: valores de X e Y, se as formigas devem evitar a água (GD-003); conferir se a superfície inunda com chuva forte agora que a água só some no formigueiro (KI-009)
- Regras da rainha: o que ela faz sem sala, se formigas novas nascem nela, arte própria e se pintar a sala deveria já marcar para cavar (GD-004)
- Regras das plantas e nutrientes: valor de X (60 s por passo de crescimento), forma das plantas vinda dos sprites (touceira de ~300 a 420 pedaços), reinício da planta só quando não sobra nada para colher, densidade, jardineira escolhendo a planta sozinha e crescimento mínimo para colher, preço das formigas em nutrientes (GD-005)
- O loop do protótipo (marcar pixels para cavar; jardineiras colhem plantas sozinhas; nutrientes compram formigas) é o desejado? (GD-001)
- Se o experimento 16x16 e o mapa dobrado vão para a `main` e se formigas e cenário serão redesenhados a 16 px por célula (REQ-024). O mapa dobrado custa ~415 MB a mais e ~1,6x no tempo da colônia com 200 formigas cavando (KI-016); a arte 16x16 não pesa na simulação.
- Opções da tela de configurações e pausa ao abrir o menu (REQ-022); eventos, origem dos arquivos e música para SFX e VFX (REQ-023).
- Ataques de cupins: balanceamento, crescimento do teto de cupins por ataque (hoje +20 pontos), se a rainha pode ser atacada e se a área de resíduos vira compra (R0) (REQ-029, GD-007, KI-018).
- Descrição, plataformas-alvo e marco atual do jogo.
- Substituir a cena de template `Assets/Scenes/SampleScene.unity` e mover `Assets/Settings` para `Assets/Project`? (não feito: quebraria referências; fica como tarefa separada).

## Limitações da avaliação

- Input real e HUD não foram usados por uma pessoa; desempenho medido só no editor.
