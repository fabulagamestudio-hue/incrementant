# Incrementant — Estado da produção

## Identificação

- **Nome:** Incrementant
- **Descrição:** Formigueiro visto de lado com terreno em pixels; formigas cavam e levam a terra para fora (em fase de protótipo de viabilidade).
- **Engine:** Unity 6000.3.11f1, URP 17.3.0
- **Plataformas-alvo:** Pendente de definição
- **Marco atual:** Pendente de definição
- **Última avaliação:** 2026-10-23

## Resumo executivo

Papel da área externa decidido em 2026-10-23 (REQ-070; GD-012, ART-026): o interior produz volume, com teto, e cada bioma da superfície, em faixas para os lados do formigueiro, fornece um insumo que só existe nele; entradas secundárias (postos avançados) encurtam a viagem. Biomas, insumos e lista de arte são proposta do agente no [documento 44](../Docs/02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md), a aprovar; nada implementado. No mesmo dia foram implementados os dois primeiros biomas (PRG-074): a Serapilheira à direita (chão de serapilheira como insumo, folhas que voltam) e o Canteiro de flores à esquerda (terra adubada, flores que soltam gotas de néctar), cada um com depósito próprio, cenário só no bioma e gramado limpo; suíte EditMode 457/457, falta o playtest do desenvolvedor. O uso dos insumos foi adiado pelo desenvolvedor.

Mundo pequeno no molde do Terraria e corte vertical implementados em 2026-10-23 (REQ-071; GD-013, PRG-075, ART-027): areal e fruteira nas pontas, veios de cobre, ferro e ouro (em bolsões, com mais ouro no fundo), fundição que queima serapilheira, forja, armazéns e equipamento das formigas por papel. Números e a transformação automática são suposições do agente; suíte EditMode verde, sem Play Mode nem avaliação do desenvolvedor.

Salas construídas com visual próprio implementadas em 2026-10-22 (REQ-058; ART-023, PRG-071, GD-010): as formigas rebocam com terra todas as paredes em contato com o vão de cada sala pintada (piso, teto, lados e a terra de dentro da área), que passam a mostrar o bloco da família, e a sala pronta troca o fundo do vão; só visual, sem tirar células vazias da sala, com a construção antes das automatizações (decisão do desenvolvedor depois de jogar). Suíte EditMode 447/447 e quicksave do desenvolvedor rebocado em Play Mode pelo agente; falta a aprovação final.

Fazenda de pulgões implementada em 2026-10-22 (REQ-061; PRG-066 a PRG-070, GD-011, ART-022): sala pintada (câmara A0) com baias de cocho de areia e de água, pulgões criados sozinhos até o limite (A+1), água sem fazendeira e, com a fazendeira (nó Z0) ordenhando de cochos cheios, areia, folhas, água e às vezes um cogumelo luminoso, que passou a ser o insumo do pincel Cogumelo luminoso; depósitos de areia e de cogumelos (separados do de construção, decisão do desenvolvedor); cupins vão atrás dos pulgões e o mordido vira o pulgão preto, que ataca as formigas até morrer. Suíte EditMode 438/438 e Play Mode conferido pelo agente; valores assumidos e falta uma pessoa jogar (KI-058).

Carregadora reorganizada em 2026-10-21 (PRG-065, REQ-059): leva o dobro de terra (6), até 4 águas por viagem (do mapa ao depósito e do depósito aos canteiros), recolhe vários itens iguais do depósito numa viagem e trabalha nesta ordem: encher os depósitos, entregar o que está neles e, sem isso, o trabalho da comum. Nutriente, ovo e partes de cupim seguem um por viagem (KI-057). Suíte EditMode 427/427 na Unity; falta jogar.

Cuspideira e lixeira implementadas em 2026-10-21 (PRG-063, PRG-064, REQ-056, REQ-057): a cuspideira (nó K0, ramo do soldado) cospe nos cupins a até 8 células com a linha livre; a lixeira (nó W0) tira corpos, leva itens soltos e abastece a área de resíduos antes de tudo. Valores assumidos (GD-009); HUD e localização remontados, suíte EditMode 423/423 e Play Mode conferido pelo agente. Quadros próprios por troca de rampa (cuspideira verde-limão, lixeira cinza; ART-020) e cuspideira que não cola no cupim com a linha fechada, ambos de 2026-10-21; suíte EditMode 424/424. Falta uma pessoa jogar e aprovar cores e valores. Registrada como tarefa futura a sala construída com visual próprio (REQ-058, GD-010).

Sala de nutrientes abastecida pelos depósitos em 2026-10-21 (PRG-047, REQ-055): com depósito de alimentos ou de água, jardineira e comum que trabalham na sala buscam neles a planta e a água dos canteiros; sem depósito, o fluxo de entrega segue igual. Teste EditMode novo e StorageTests 16/16 na Unity; conferência em jogo pendente.

Pebbles refinados em 2026-10-21 (ART-018, PRG-061, REQ-053): 14 pedras a 16 px/célula, com tamanho físico preservado, maiores que as operárias. Sprites detalhados separados da máscara de navegação e da camada grosseira do cenário. Prancha, 14 importações, 3.158.528 pixels e metadados conferidos na Unity; compilação sem erros e 2 testes EditMode aprovados. Em 2026-10-22 as 14 pedras ganharam textura leve guiada pelo Qwen, em marcas de 2 px, com snapshots para rollback. A Pebble_05_2x foi aprovada no jogo pelo desenvolvedor; as outras 13 aguardam aprovação. Demais props continuam aguardando refinamento. [Arte e fontes](../Assets/Project/Art/Scenery/Pebbles/README.md).

Pulgões refinados em 2026-10-21 (ART-017, REQ-052): oito quadros laterais/superiores em 40×24, paleta e metadados preservados. Prancha e importação na Unity conferidas; aprovação visual e reprodução das animações pendentes. O pacote ainda aguarda integração no gameplay, conforme ART-012.

Ovos, larvas e pupas refinados em 2026-10-21 (ART-016, REQ-051): seis quadros de repouso em 48×32, com paletas, escala, apoio no chão e metadados preservados. Prancha antes/depois e importação na Unity conferidas; reprodução em Play Mode e aprovação visual do conjunto pendentes.

Cupins refinados em 2026-10-21 (ART-015, REQ-050), no tratamento visual aprovado para as formigas: 16 quadros de voo, pouso e caminhada em 64×64, com a paleta e os metadados preservados. Prancha, paleta, alpha e importação dos 16 sprites conferidos; reprodução em Play Mode e aprovação visual do conjunto pendentes.

Refinamento de aparência das formigas e da Rainha implementado em 2026-10-21 (ART-014, REQ-049), seguindo a amostra aprovada: 64 quadros em 48×32 e 8 da Rainha em 96×64, cores por função e metadados preservados. Paleta, alpha, prancha e importação dos 72 sprites conferidos; reprodução em Play Mode e aprovação visual do conjunto pendentes.

Barra inferior e opção Mostrar ambientes (PRG-060, REQ-048): pausa, painel da colônia, sino e, desde 2026-10-22, o botão Mostrar ambientes dividem um cartão só no canto inferior esquerdo. O botão esconde a pintura das salas e depósitos (ela volta com um pincel de ambiente na mão) e saiu da tela de configurações a pedido do desenvolvedor; a escolha continua gravada nas configurações. Conferido pelo agente em Play Mode; falta o desenvolvedor ver.

Limite de pintura por ambiente (PRG-058, REQ-046): cada sala e cada depósito tem um limite de células dividido entre no máximo 2 salas não conectadas; pintar sem área ou abrir uma 3ª sala é recusado com o aviso da rainha sem recurso. Seis galerias novas na tela Evolução (Q+4, C+4, B+3, D+3, R+1, G+3) aumentam a área, a T+3 permite +1 sala por nível (até 5), e os botões dos ambientes mostram uma barra de área livre. Testes EditMode e conferência no Editor pelo agente; valores sem balanceamento e saves antigos podem já estar acima do limite.

Área de resíduos com modos de conversão (PRG-055, REQ-045): com o pincel Resíduos, cada área tem o próprio modo, escolhido no submenu RESÍDUOS antes de pintar: restos de cupim em folhas, folhas em água ou terra em areia e água, sem misturar conteúdo entre áreas (1 terra, 1 areia e 1 água); a água vai ao depósito de água ou aos canteiros e a areia ao despejo. O nó R2 da tela Evolução deixa a conversão da terra 2× mais rápida. Testes EditMode novos e submenu conferido em Play Mode pelo agente; falta uma pessoa jogar o ciclo.

Ataques de cupins implementados (PRG-034, REQ-029): a partir de 15 formigas, a cada 1 a 5 minutos um alerta no topo anuncia o ataque; cupins (sorteados entre 40% das formigas e um teto que sobe a cada ataque até 100%) pousam pelo mapa, destroem crias, canteiros e paredes dos setores (menos a sala da rainha) e saem pelas laterais; na superfície só os soldados vão caçá-los (as demais se defendem quando mordidas), mas cupim dentro do formigueiro leva todas as formigas atrás dele. Corpos viram folhas para os canteiros e restos para a nova área de resíduos; o primeiro ataque libera esse pincel e o soldado. Suíte EditMode 190/190 na Unity e ataques exercitados em Play Mode pelo agente (HUD e arte já na cena); falta uma pessoa jogar e balancear.

Apertar ESC abre o menu do jogo (continuar, configurações, salvar, carregar, sair), que pausa a simulação; a tela de configurações tem volumes, efeitos visuais, tela cheia, resolução, VSync, limite de FPS e o contador do canto, gravados num arquivo próprio (PRG-031, REQ-022). Testes novos e Play Mode pelo agente; falta uso por uma pessoa. Os volumes de música e efeitos e o nível de efeitos visuais esperam os sistemas de SFX e VFX, próxima tarefa anunciada (REQ-023).

As formigas passaram a reservar o pixel marcado ao sair para buscá-lo: cada pixel tem no máximo uma formiga indo atrás dele, e as demais vão a outro pixel ou a outro trabalho, em vez de um bando ir e voltar (PRG-030, REQ-021). Em 2026-10-19, o corte da planta e a busca de terra para obra passaram a marcar o pedido como atendido por quem está indo, para não chamar todas as ociosas de uma vez, e quem busca água reserva o bloco de água e o canteiro que vai regar. Testes novos (e Play Mode pelo agente só na primeira parte); falta ver o movimento em jogo.

As formigas passaram a andar livres do lado de fora: pelo meio da arte do cenário de perto (cogumelos, gravetos, folhas secas, pedrinhas) e por dentro das obras de fora, não só pelo contorno (PRG-029, REQ-020). Testes novos e Play Mode pelo agente; quatro testes antigos já falhavam no working tree e foram registrados em KI-015.

Variantes de cor das formigas preparadas em seis pastas (48 PNGs Top/Walk, ART-006, REQ-019), com dimensões, transparência e troca de paleta conferidas por comparação de pixels. A integração no jogo fica para depois, conforme pedido do desenvolvedor; importação na Unity e aprovação visual pendentes.

A referência de design revisada de 2026-10-17 foi organizada em 38 Markdown em [Docs/](../Docs/README.md), preservando 28 tabelas e as propostas e decisões em aberto (GD-006, REQ-009). A transcrição foi conferida automaticamente; a implementação dos sistemas não foi reavaliada nessa entrega.

Protótipo de viabilidade do formigário (REQ-001) implementado na cena `AntFarm`: terreno em pixels com física granular, formigas que cavam pixels marcados pelo jogador e os despejam na superfície, e compra de formigas com a terra entregue. O mapa é procedural por padrão (novo a cada Play) e pode ser fixado na cena como PNG editável, com ninho e despejo posicionáveis. No modo procedural, o mapa cresce para os lados e para baixo conforme a câmera se move, até um limite de mundo (960x520), com paralaxe no céu e na grama distante (REQ-003). A velocidade 8x roda com FPS próximo do 1x graças à simulação por blocos (REQ-004). Há dois tipos de chuva, trocados no HUD ou com C: a leve dissolve a terra despejada aos poucos e a forte compacta areia e terra solta, que param de cair (REQ-005). A chuva também cria água, que escorre, forma poças e laguinhos na superfície, amolece a terra dentro do formigueiro e afoga formigas (REQ-006). Há uma formiga rainha (o dobro do tamanho) que só anda na sala pintada pelo jogador (a maior, se houver várias), e as formigas sem trabalho vagam por todo o formigueiro ligado a ela (REQ-007). Grama e mudas crescem na superfície; o jogador as marca com o pincel Colher, as formigas colhem da ponta para a base e guardam cada pixel na sala de nutrientes pintada, e outras levam os nutrientes à rainha, que os consome: nutrientes passaram a ser a moeda das formigas novas (REQ-008). As formigas são sprites animados com pixels menores que os do terreno, e a superfície tem um cenário (grama, flores, trevos, cogumelos, pedrinhas) na escala delas, montado a partir de PNGs isolados e editáveis de cada elemento (arte provisória, REQ-002). Núcleo testado e exercitado em Play Mode pelo agente; falta playtest do desenvolvedor para avaliar o loop.

Tutorial refeito como tour da interface (REQ-065, PRG-035; o roteiro guiado de REQ-030 foi cancelado): em todo jogo novo, 14 paradas explicam os cartões, os menus, a barra inferior, os cupins e as dicas, com Próximo/Voltar/Pular, jogo pausado e o jogador livre para clicar. Suíte EditMode e Play Mode pelo agente; falta uma pessoa jogar o tour. O glossário pedido junto (REQ-066, PRG-072) está implementado: botão `?` na barra inferior e entrada no menu do ESC, 49 entradas em 9 categorias, 30 delas com imagem (fotos de salas montadas num mapa limpo e sprites); Play Mode pelo agente, falta revisar textos e imagens. O menu Colônia agora fica recuado até o cursor chegar perto e aberto nas paradas do tour sobre ele (REQ-067, PRG-073; Play Mode pelo agente).

Menu principal implementado (REQ-031, PRG-036): cena `MainMenu` com a colônia trabalhando ao fundo, Continuar/Iniciar/Configurações/Sobre/Sair, splash da Unity desligado e Menu principal no menu de pausa; verificado só em Play Mode pelo agente, falta uma pessoa usar. Logo da Fábula aplicado; splash da Fábula pendente.

Painel da colônia implementado (PRG-046, REQ-035): botão à esquerda do sino abre a janela com Estatísticas (formigas por seção, tipo e lugar; produção por segundo) e Configurações (ligar, parar e limitar cada uma das 8 seções de trabalho; comportamento da rainha: botar ovos ou trabalhar como uma das classes liberadas, seletor de 2026-10-21). Testes EditMode e Play Mode pelo agente; falta uma pessoa usar.

Formigas sem trabalho do papel fazem o da operária comum (PRG-048, REQ-037): soldado fora do combate, cortadeira sem corte, cuidadora sem cria etc. cavam, carregam e cuidam como a comum e voltam ao papel quando ele tem trabalho. Testes EditMode novos; sem Play Mode.

Depósitos implementados (PRG-047, REQ-036): Ambientes → DEPÓSITO abre os pincéis dos depósitos de alimentos, construção e água. Planta cortada, terra cavada e água do mapa vão primeiro para eles, guardadas sem bloquear a passagem, e canteiros, sala de orvalho e obra consomem só de lá (sem o depósito, como antes); a área de resíduos também deixou de obstruir o caminho. Testes EditMode novos e Play Mode pelo agente; a pilha de fora, inclusive a despejada antes do depósito, enche o depósito de construção sempre que ele tem vaga (decidido em 2026-10-19); falta uma pessoa jogar.

Sala Gotas de Orvalho implementada (PRG-037, REQ-032): pincel novo; operárias e jardineiras cobrem de terra fofa os 30% de cima da sala, depois penduram folhas, e a sala pronta solta uma gota a cada 8 s que as paredes não absorvem. VERIFIED em 2026-10-20: o desenvolvedor construiu a sala numa partida e a marcou como pronta; o teto de água agora conta a largura (KI-049) e o transbordo para os túneis foi resolvido (KI-050, dado como resolvido pelo desenvolvedor em 2026-10-20).

Busca de água por vizinhança (PRG-049, REQ-038): o bloco de água reservado libera os vizinhos ainda obstruídos para outras formigas; quem chega antes pega o bloco do meio e passa o seu à outra, e a formiga que acha a água escorrida pega um bloco a um passo em vez de parar. Testes EditMode novos passando fora da Unity; falta a Unity e Play Mode.

Água no mapa (PRG-053, REQ-042): laguinhos de profundidade 8 a 16 e bolsões de água lacrados, forrados de pedra, no subterrâneo; a água só sai quando a colônia cava até ela. Testes novos passando na Unity e aprovado pelo desenvolvedor em 2026-10-20.

Melhorias incrementais documentadas (GD-008, REQ-039): o [documento 39](../Docs/01_Design/39_Melhorias_incrementais_e_formigueiro_de_dependencias.md) e a [planilha de valores](../Docs/04_Data/39_Melhorias_incrementais_valores.xlsx) propõem câmaras de compra única, galerias com nível (teto nos atributos físicos, infinitas na economia) e marcos, em anéis de profundidade com dependências cruzadas entre as abas Formigueiro e Rainha. Níveis repetíveis e valores aprovados pelo desenvolvedor em 2026-10-19. A tela Evolução já segue o padrão formigueiro (PRG-050): arrastar e zoom, anéis fechados escuros, portais entre abas e galerias com selo de nível; só os nós da rainha que já existiam têm compra, o resto aparece como EM BREVE (KI-024). Testes EditMode novos e Play Mode pelo agente; falta uma pessoa usar.

## Estado por setor

| Setor | Situação | Documento |
|---|---|---|
| Arte | Direção unificada aprovada; refinamento das formigas e Rainha implementado e importado, aguardando conferência em jogo | [ART.md](ART.md) |
| Programação | Protótipo do formigário implementado; núcleo verificado | [PROGRAMMING.md](PROGRAMMING.md) |
| Game design | Referência revisada organizada em 38 Markdown (GD-006); melhorias incrementais documentadas (GD-008); loop do protótipo a validar | [GAME_DESIGN.md](GAME_DESIGN.md) |
| Áudio | SFX sintetizados em código (AUD-001) e som espacial da colônia (AUD-002, REQ-041, DONE); SFX IMPLEMENTED | [AUDIO.md](AUDIO.md) |
| Release | Build Assistant instalado (REL-001), versão na tela (REL-002) e histórico de versões (REL-003); nenhuma build gerada | [RELEASE.md](RELEASE.md) |

### Arte

- **Silhuetas das formigas por papel:** ART-029 verificado: cada papel e a rainha têm traço próprio (coroa, cabeção, chapéu etc.), lixeira branca e fazendeira azul, aprovados pelo desenvolvedor em 2026-10-23.
- **Rachadura dos blocos:** ART-030 implementado (REQ-072): máscaras provisórias do agente, vistas em Play Mode; falta a avaliação do desenvolvedor.
- **Equipamento das formigas:** ART-028 verificado (REQ-071): camadas vestidas, nove itens 8 × 8 e cinco ícones de HUD em cobre, ferro e ouro, aprovados pelo desenvolvedor em 2026-10-23 e vistos em Play Mode; barras e minérios do atlas seguem provisórios (ART-027).
- **Biomas da superfície:** ART-026 em andamento (tiles provisórios de serapilheira, terra adubada e néctar no atlas em 2026-10-23); (REQ-070): lista de arte por bioma (topo, camada rasa, coletáveis, silhueta, fundo, item), argila no subsolo e entrada secundária, no [documento 44](../Docs/02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md); depende da aprovação dos biomas (GD-012).
- **Kit do graveto:** ART-025 implementado na branch `experimento/arte-8x8` (REQ-069): os gravetos do cenário são montados por peças de 8 px, no tamanho do mundo dos antigos, que saíram; falta a avaliação do desenvolvedor.
- **Experimento 8x8:** ART-024 implementado na branch `experimento/arte-8x8` (REQ-068): terreno, formigas, crias, cupins, pulgões, cenário (refinado, arte de células e faixas distantes) e cultivo com 8 texels por célula, mostrados em 2x, e contorno preto de 1 texel em tudo no primeiro plano; falta a avaliação do desenvolvedor.
- **Salas construídas:** ART-023 implementado (REQ-058): bloco e fundo de sete famílias no atlas do terreno, aprovados na maquete; em jogo, berçário e depósito vistos pelo agente.

- **Pulgão:** em jogo desde 2026-10-22 pela fazenda (REQ-061, ART-017); fazendeira com arte por troca de rampa (ART-022), aguardando aprovação. ART-012 implementado (REQ-033): oito sprites transparentes (lateral e superior), 72x48 a 36 PPU, dois terços do canvas das formigas. Dimensões, paleta, alpha e importação validados na Unity em 2026-10-18. Aprovação visual e integração em gameplay pendentes; novo importador aguarda compilação ao encerrar Play Mode.

- **Inimiga alada:** ART-011 implementado (REQ-027): 16 sprites para voo, pouso/perda das asas e caminhada sem asas (lateral e superior), preparados a 36 PPU com alpha binário. Em uso como cupim (REQ-029), ligada na cena e vista em Play Mode pelo agente; aprovação visual pendente.

- **Refinamento gráfico:** ART-010 implementado (REQ-026): 72 quadros adultos e 69 elementos de cenário com novo desenho, paleta limitada e alpha 0/255. Dimensões e metadados preservados, 1.390.673 pixels conferidos e 141 PNGs reimportados na Unity; aprovação visual e Play Mode pendentes.

- **Resolução dos adultos:** ART-009 implementado (REQ-025): 72 quadros ampliados 6x para 108x72, mantendo o tamanho no jogo por densidade de 36 px/unidade. Pixels RGBA, GUIDs e pivôs conferidos; confirmação final na Unity pendente.

- **Cenário em pixel art limpo:** ART-021 planejado (REQ-060), para outro momento: refazer os assets de `Art/Scenery` com redesenho no Qwen e limpeza de grade, paleta e contorno. O piloto foi a Pebble_05_2x, com master trocado e importação conferida; ela ainda precisa ser refeita nativa a 16 px por célula.

- **Entregas verificadas:** Nenhuma
- **Variantes por função:** ART-006 implementado; 48 PNGs conferidos, aguardando aprovação visual e futura integração na Unity (REQ-019).
- **Pendências:** ART-008 Texturas 16x16 do terreno (provisórias, experimento REQ-024); ART-007 VFX do jogo (planejado, REQ-023); ART-001 e ART-002 Sprites provisórios das formigas; ART-005 Sprites da cria (ovo, larva, pupa; aprovação visual); ART-003 Cenário da superfície (aprovação visual); ART-004 Sprites de UI 9-slice (bordas e fonte a aprovar)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Programação

- **Rachadura dos blocos:** PRG-076 verificado (REQ-072, etapa 1): golpes, dano na célula e desenho no shader; suíte EditMode 474/475 (falha da máscara corrigida, `BlockCrackTests` 5/5 depois) e Play Mode pelo agente em 2026-10-23. Etapa 2 (preenchimento vertical dos depósitos) em PRG-077, verificado: pilha de itens iguais por célula com capacidade por depósito, save versão 27, suíte EditMode 482/482 e Play Mode; sem o nó da melhoria nas árvores, a capacidade segue 1 no jogo.
- **Árvore de evolução em níveis:** PRG-078 verificado (REQ-073, etapa 1): domínio e nível de cada nó no catálogo e 7 testes; suíte EditMode 489/489 em 2026-10-23. PRG-079 verificado (etapa 2): tela única em colunas e níveis; suíte EditMode 490/490 e Play Mode em 2026-10-23; os nós ainda não somem de longe.
- **Minérios e forja:** PRG-075 implementado (REQ-071): veios, fundição, forja, armazéns e equipamento; suíte EditMode verde. PRG-081 implementado: os pincéis dessas salas viram câmaras pagas na árvore (V0, V1, Y0, Y1, Y2) e há galerias de tempo, durabilidade e reforço do equipamento.
- **Biomas da superfície:** PRG-074 implementado (REQ-070): Serapilheira à direita e Canteiro de flores à esquerda, com insumos (serapilheira e néctar) e depósitos próprios, folhas que voltam, gotas de néctar, cenário e plantas por bioma; EditMode 457/457 e Play Mode visto pelo agente em 2026-10-23.
- **Salas construídas:** PRG-071 implementado (REQ-058): slots de reboco, obra das formigas, sala pronta e save versão 25; testado em EditMode e em Play Mode pelo agente, limites em KI-059.

- **Entregas verificadas:** PRG-001 Grade e física granular; PRG-002 Formigas; PRG-004 Cena por setup; PRG-005 Modo do mapa; PRG-006 Formigas em sprite; PRG-007 Agarre em paredes; PRG-008 Andar no fundo do formigueiro; PRG-009 Cenário da superfície em camadas; PRG-010 Mundo expansível; PRG-012 Simulação por blocos (8x no mesmo FPS); PRG-013 Chuva leve e forte; PRG-014 Água; PRG-015 Rainha e sala da rainha; PRG-016 Plantas, sala de nutrientes e nutrientes
- **Pendências:** PRG-003 Render, input e HUD; PRG-017 HUD uGUI e PRG-018 Política da rainha e PRG-019 Tipos de formiga e PRG-025 Corte de plantas corrigido e PRG-026 Carga, rocha e prioridades e PRG-027 Cuidado do berçário e PRG-028 Cuidado da sala de nutrientes e PRG-020 Ciclo de criação, PRG-021 Cria desenhada no berçário e PRG-022 Transporte do ovo e cheat F1 e PRG-023 Construção de fora (sem Play Mode) e PRG-024 Árvore da rainha (tela Evolução) e PRG-029 Andar livre do lado de fora e PRG-030 Reserva do pixel marcado e PRG-031 Menu do ESC e configurações (faltam uso com mouse; só Pintar e Rainha funcionam; cria vista só em captura do agente); PRG-011 Câmera, limitador e paralaxe (falta uso real por mouse/teclado); PRG-034 Ataques de cupins e área de resíduos (Play Mode só pelo agente); PRG-038 a PRG-045 Efeitos de dopamina em oito fases (planejado, REQ-034); PRG-046 Painel da colônia (falta uso por uma pessoa); PRG-047 Depósitos e resíduos sem obstruir (falta uso por uma pessoa)
- **Bloqueios:** Nenhum
- **Riscos:** Render passou a ser o maior custo por frame (KI-001)

### Game design

- **Árvore de evolução:** GD-014 em andamento (REQ-073): [documento 46](../Docs/01_Design/46_Arvore_de_evolucao_em_niveis_e_tuneis.md) aprovado em 2026-10-23 (formigueiro descendo da rainha, domínios em colunas, níveis por progresso, túneis por valor do upgrade); etapa 1 (dados) verificada em PRG-078; etapa 2 (tela em colunas e níveis) verificada em PRG-079; etapa 3 (compra unificada) verificada em PRG-080 (suíte 493/493); etapa 4 (nós de Minérios e Forja e galerias de nível 5) implementada em PRG-081, com custos e efeitos assumidos pelo agente.
- **Exterior:** GD-012 em andamento (dois biomas implementados, PRG-074, com valores assumidos); (REQ-070): direção A decidida pelo desenvolvedor em 2026-10-23 (biomas laterais com insumos únicos e postos avançados) e regra do cenário (elementos grandes só no bioma deles, gramado central limpo, decoração não imita insumo); biomas, insumos, números e biomas da demo a aprovar.
- **Entregas verificadas:** GD-006 Documento revisado convertido e conferido contra o DOCX (38 seções e 28 tabelas); [índice](../Docs/README.md)
- **Pendências:** GD-001 Conceito (alinhamento do protótipo à referência revisada e validação do loop); GD-002 Regras da chuva (interpretações a confirmar); GD-003 Regras da água (valores de X/Y e interpretações a confirmar); GD-004 Regras da rainha (interpretações a confirmar); GD-005 Regras das plantas e nutrientes (interpretações a confirmar); GD-008 Melhorias incrementais (proposta sem balanceamento, construção não pedida)
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Áudio

- **Entregas verificadas:** AUD-002 Som espacial da colônia (DONE, aprovado pelo desenvolvedor em 2026-10-20)
- **Pendências:** AUD-001 sem aprovação registrada (IMPLEMENTED); REQ-023 ainda PLANNED no registro embora os SFX existam
- **Bloqueios:** Nenhum
- **Riscos:** Nenhum

### Release

- **Entregas verificadas:** REL-001 Build Assistant no projeto (VERIFIED, REQ-062); REL-002 versão da build na tela (VERIFIED no editor, REQ-063); REL-003 histórico de versões (VERIFIED no editor, REQ-064)
- **Pendências:** Primeira build pelo Build Assistant; identidade, pasta de saída e versionamento a definir
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
| Limite de pintura por ambiente e galerias de área | IMPLEMENTED | PRG-058 |
| Formigas andam pelo meio da arte do cenário, das obras e dos blocos de terra de fora | IMPLEMENTED | PRG-029 |
| Menu do ESC e tela de configurações | IMPLEMENTED | PRG-031 |
| Ataques de cupins, combate, área de resíduos e soldado | IMPLEMENTED | PRG-034 |
| Postos de guarda, distribuição das soldadas e velocidade por situação | IMPLEMENTED | PRG-052 |
| Sala Gotas de Orvalho (terra fofa, folha, gota que as paredes não absorvem) | VERIFIED | PRG-037 |
| Painel da colônia: estatísticas, seções de trabalho e comportamento da rainha | IMPLEMENTED | PRG-046 |
| Depósitos de alimentos, construção e água; resíduos sem obstruir | IMPLEMENTED | PRG-047 |
| Formiga sem trabalho do papel faz o trabalho da operária comum | IMPLEMENTED | PRG-048 |
| Liberação de blocos de água por vizinhança e troca de alvo | IMPLEMENTED | PRG-049 |
| Água no mapa: laguinhos fundos e bolsões de água subterrâneos | VERIFIED | PRG-053 |
| Desempenho das formigas ociosas e medição no F11 | VERIFIED | PRG-056, PRG-057 |
| Área de resíduos: modos de conversão (folhas em água, terra em areia e água) e upgrade R2 | IMPLEMENTED | PRG-055 |
| Experimento: terreno em tiles 16x16 e mapa dobrado (branch `experimento/pixel-16x16`) | IMPLEMENTED | PRG-032, ART-008 |
| Fazenda de pulgões, fazendeira, depósitos de areia e de cogumelos e pulgão preto | IMPLEMENTED | PRG-066, PRG-067, PRG-068, PRG-069, PRG-070 |
| Salas construídas com visual próprio (reboco em volta da sala e fundo da sala pronta) | IMPLEMENTED | PRG-071, ART-023 |

## Problemas e riscos

- O ataque só termina quando o pulgão preto morre; preso num bolsão inalcançável, ele segura o ataque e o relógio do próximo (PRG-070, KI-058).
- Engasgos de 100 a 500 ms por frame com ~850 formigas ociosas (relato de 2026-10-21) corrigidos e confirmados pelo desenvolvedor (PRG-056, KI-053, KI-054); o painel F11 passou a mostrar o tempo por estado e os passos lentos (PRG-057). Falta um teste EditMode que fixe a correção.
- Escavação só na fronteira da marca: num save de 2026-10-21, 4012 células marcadas rendiam 13 pontos de escavação e 193 de 261 escavadoras ficavam ociosas (KI-055, PRG-002).
- Seis testes EditMode já falham no código commitado (5 de água e 1 da planta-alvo; KI-022), sem investigação.
- As quatro falhas antigas de teste foram corrigidas em 2026-10-18 (KI-015, resolvido): eram bugs latentes da ordem da obra e da mudança de cria, e a planta larga parava pela morte por velhice. Suíte EditMode 173/173.
- O render da textura do terreno (~4,3 ms por frame no editor) é agora o maior custo; a simulação no 8x caiu para ~2 ms com o mundo inteiro (KI-002 resolvido). Montar o cache de navegação do mundo inteiro de uma vez caiu para ~3,5 ms (KI-004 resolvido em 2026-10-20). Ver [KnownIssues.md](../Assets/Project/Scripts/KnownIssues.md).

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
| REQ-030 | Tutorial da primeira abertura do jogo | CANCELLED | UNDEFINED |
| REQ-031 | Menu principal com fundo de colônia trabalhando e sem splash da Unity | IMPLEMENTED | UNDEFINED |
| REQ-032 | Sala Gotas de Orvalho | VERIFIED | UNDEFINED |
| REQ-034 | Efeitos de dopamina (juice): botões animados, partículas e animações de feedback | IN_PROGRESS | UNDEFINED |
| REQ-035 | Janela de estatísticas e configurações da colônia | IMPLEMENTED | UNDEFINED |
| REQ-036 | Depósitos de alimentos, construção e água; resíduos sem obstruir | IMPLEMENTED | UNDEFINED |
| REQ-037 | Formigas sem trabalho se comportam como operárias | IMPLEMENTED | UNDEFINED |
| REQ-038 | Liberação de blocos de água por vizinhança e troca de alvo entre formigas | IMPLEMENTED | UNDEFINED |
| REQ-039 | Melhorias incrementais com nutrientes e formigueiro de dependências | IMPLEMENTED | UNDEFINED |
| REQ-040 | Combate e cupins: postos de guarda, distribuição das soldadas e velocidade por situação | IMPLEMENTED | UNDEFINED |
| REQ-041 | Áudio espacial: SFX pela posição da câmera e pelo zoom | DONE | UNDEFINED |
| REQ-042 | Água no mapa: lagos mais fundos e bolsões de água no subterrâneo | DONE | UNDEFINED |
| REQ-043 | Ordens de trabalho completas com travas e duas funções por formiga | IN_PROGRESS | UNDEFINED |
| REQ-045 | Modos de conversão da área de resíduos: folhas em água e terra em areia e água | IMPLEMENTED | UNDEFINED |
| REQ-046 | Limite de pintura por ambiente, upgrades de área e barra de área livre | IMPLEMENTED | UNDEFINED |
| REQ-048 | Barra inferior num cartão só e opção Mostrar ambientes | IMPLEMENTED | UNDEFINED |
| REQ-049 | Aplicar aparência aprovada às formigas e à Rainha | IMPLEMENTED | UNDEFINED |
| REQ-050 | Refinar a aparência dos cupins no estilo das formigas aprovadas | IMPLEMENTED | UNDEFINED |
| REQ-051 | Refinar ovos, larvas e pupas no estilo aprovado | IMPLEMENTED | UNDEFINED |
| REQ-052 | Refinar a aparência do pulgão no estilo aprovado | IMPLEMENTED | UNDEFINED |
| REQ-047 | Direção de arte unificada (16 px por célula) e terreno iluminado, em etapas (etapas 2, 3 e 6 feitas e aprovadas; paleta mestra de 32 cores aprovada; atlas RGB (etapa 4) implementado; atlas desenhado (etapa 5) aprovado; formigas em 16 px (etapa 8) concluída e aprovada (operárias, rainha, cupim alado e pulgões); crias e itens (etapa 9) concluída e aprovada em jogo; cenário (etapa 10) com os elementos adiados à espera de nova estratégia; zoom em passos inteiros até 32 (etapa 1) aprovado; etapa 7 sem diferença visível para o desenvolvedor; penumbra e cogumelo luminoso implementados; borda do terreno recusada) | IN_PROGRESS | UNDEFINED |
| REQ-053 | Refinar as pedras de cenário no estilo aprovado, maiores que as formigas | IMPLEMENTED | UNDEFINED |
| REQ-054 | Refinar os demais assets de Scenery no estilo aprovado | IN_PROGRESS | UNDEFINED |
| REQ-055 | Sala de nutrientes busca folhas e água nos depósitos | IMPLEMENTED | UNDEFINED |
| REQ-056 | Formiga guerreira de ataque a distância (cuspideira) | IMPLEMENTED | UNDEFINED |
| REQ-057 | Formiga que cuida dos resíduos (lixeira) | IMPLEMENTED | UNDEFINED |
| REQ-058 | Sala construída muda de visual: formigas põem terra nos slots do ambiente | IMPLEMENTED | UNDEFINED |
| REQ-059 | Carregadora leva mais de todos os itens e prioriza o estoque | IMPLEMENTED | UNDEFINED |
| REQ-060 | Refazer os assets de cenário no padrão de pixel art (limpeza com Qwen) | PLANNED | UNDEFINED |
| REQ-061 | Fazenda de pulgões, fazendeira, depósitos de areia e de cogumelos e pulgão preto | IMPLEMENTED | UNDEFINED |
| REQ-062 | Trazer o Build Assistant do SpaceFall | VERIFIED | UNDEFINED |
| REQ-063 | Versão da build no canto inferior da tela, em todas as cenas | VERIFIED | UNDEFINED |
| REQ-064 | Histórico de versões ao clicar na versão, sem clique no mundo sob ela | VERIFIED | UNDEFINED |
| REQ-065 | Tutorial como tour da interface, com o jogador livre para clicar | IMPLEMENTED | UNDEFINED |
| REQ-066 | Glossário com a explicação de tudo | IMPLEMENTED | UNDEFINED |
| REQ-067 | Menu Colônia recuado até o cursor chegar perto, visível no tutorial | IMPLEMENTED | UNDEFINED |
| REQ-070 | Razão para trabalhar na área externa: biomas laterais com insumos únicos e postos avançados | IN_PROGRESS | UNDEFINED |
| REQ-071 | Mundo pequeno no molde do Terraria, minérios em veios, fundição, forja e armazéns de equipamento | IN_PROGRESS | UNDEFINED |
| REQ-072 | Blocos em porcentagem: rachadura na coleta e preenchimento vertical nos depósitos | IMPLEMENTED | UNDEFINED |
| REQ-073 | Árvore de evolução como formigueiro descendo da rainha: domínios, níveis e túneis | IN_PROGRESS | UNDEFINED |

Registro completo: [REQUESTS.md](REQUESTS.md)

## Próximas prioridades

- Plantar cogumelos luminosos (pincel Cogumelo luminoso, submenu FERRAMENTA) e conferir a penumbra do interior; aprovar ou pedir ajuste do alcance e da força (REQ-047, PRG-059).
- Pós-processamento (etapa 7): o desenvolvedor não viu diferença; decidir se reforça bloom/vinheta/cor ou descarta (REQ-047, PRG-059).
### Definidas pelo desenvolvedor

- SFX e VFX são a próxima tarefa (REQ-023, informado em 2026-10-18).
- Efeitos de dopamina (juice) pedidos em 2026-10-18 (REQ-034): análise e skill `juice-dopamina` feitas, fases 0 a 2 (PRG-038 a PRG-040: `Vfx`, `Easing`, botões animados, painéis e pulso dos contadores) verificadas em Play Mode pelo desenvolvedor; fase 3 (PRG-041, partículas de escavar, despejar, obra, nascimento e cupim) verificada em Play Mode pelo desenvolvedor; fase 4 (PRG-042, números flutuantes de nutriente ganho/gasto e formiga ferida) implementada, sem Play Mode; revisão pela skill em 2026-10-19 corrigiu negado, gasto como fato e escala dos números, e o idle ficou com todos os botões dessincronizados e mais rápidos a pedido do desenvolvedor, o que devolve PRG-039 a IMPLEMENTED até nova conferência em Play Mode; fases 5 a 7 pendentes.

### Sugeridas pelo agente

- Jogar a fazenda de pulgões (comprar A0 e Z0, pintar a fazenda e os depósitos de areia e de cogumelos) e um ataque com pulgões; aprovar ou ajustar os valores assumidos e a arte da fazendeira (REQ-061, GD-011, ART-022).
- Jogar um ataque (F4) com cuspideiras e lixeiras e aprovar ou ajustar os valores assumidos (REQ-056, REQ-057, GD-009).
- Pintar os ambientes até o limite, comprar uma galeria de área e conferir barra e aviso; ajustar os limites de base (REQ-046, PRG-058).
- Jogar os modos da área de resíduos (submenu RESÍDUOS) e ajustar os 60 s de conversão e o custo do R2 (REQ-045, PRG-055).
- Jogar um ataque (F4 força um) com berçário, canteiros e área de resíduos, e ajustar vida, dano, 40% e intervalo (REQ-029, PRG-034).
- Playtest do protótipo pelo desenvolvedor (cena `AntFarm`), para validar o loop e o input (REQ-001, PRG-003).
- Aprovação visual do cenário da superfície (REQ-002, ART-003).
- Testar a câmera (WASD/botão do meio, Q/E ou Ctrl+scroll) e o crescimento do mapa (REQ-003, PRG-011).
- Jogar no 8x com o mapa grande e confirmar o FPS (REQ-004, PRG-012).
- Jogar com as duas chuvas e ajustar ritmo de erosão e densidade das gotas (REQ-005, PRG-013).
- Jogar com chuva forte até a água entrar no ninho e ajustar X (erosão), Y (afogamento) e a quantidade de água (REQ-006, PRG-014).
- Pintar uma sala da rainha (tecla B até "Sala da rainha"), cavá-la e conferir a rainha e as ociosas espalhadas (REQ-007, PRG-015).
- Pintar uma sala de nutrientes (pincel "Sala de nutrientes"), ver as jardineiras colherem sozinhas (maior planta crescida, de cima para baixo) e conferir o ciclo até a rainha e a compra de formigas com nutrientes (REQ-008, PRG-016).
- Ver as formigas atravessando um cogumelo, uma obra e um monte de terra (solto ou compactado pela chuva) de fora em jogo (REQ-020, PRG-029; ampliado em 2026-10-19).
- Abrir o menu com ESC, mexer nas configurações e conferir que elas voltam ao reiniciar o Play (REQ-022, PRG-031).
- Definir o que o protótipo precisa provar para ser considerado viável (critérios de REQ-001).

## Decisões necessárias

- Árvore de evolução em níveis (REQ-073, GD-014): limite do nível 5, ordem das colunas, aprovação dos custos e efeitos dos nós de Minérios e Forja ([documento 46](../Docs/01_Design/46_Arvore_de_evolucao_em_niveis_e_tuneis.md)).
- Blocos em porcentagem (REQ-072): lugar da melhoria de tamanho nas árvores de melhoria (árvore incompleta, a trabalhar no futuro pelo desenvolvedor) e quais depósitos a ganham; avaliação da arte da rachadura (ART-030).
- Minérios e forja (REQ-071, GD-013): aprovar os números do documento 45 e a transformação automática da fundição e da forja; fruto da fruteira; uso das roupas; custos dos nós da árvore (PRG-081).
- Uso da serapilheira e do néctar (REQ-070): adiado pelo desenvolvedor em 2026-10-23; hoje os insumos só se acumulam.
- Exterior (REQ-070, GD-012): aprovar os biomas, insumos e usos do documento 44; escolher os biomas da demo e o lado de cada um; definir a forma da entrada secundária e a recompensa do ninho de cupins.
- Gigantes de cenário (REQ-069): aprovar o kit do graveto e decidir se cogumelos, tufos, folhas secas, grama, trevos, flores e pedras grandes seguem o mesmo caminho.
- Escala da arte (REQ-068): adotar ou não os 8 texels por célula e o contorno preto do experimento (ART-024).
- Fazenda de pulgões (REQ-061, GD-011): valores e custos assumidos.
- Carregadora (REQ-059, KI-057): se deve levar vários nutrientes e crias por viagem, quantas águas por viagem (hoje 4, como a planta) e se encher o depósito de água deve mesmo vir antes de entregar aos canteiros.
- Cuspideira e lixeira (GD-009): custos de K0 (260) e W0 (160), alcance, dano e ritmo do cuspe, vida e custo de cria, se ganham galerias próprias e se a lixeira acelera a área de resíduos.
- Sala construída (REQ-058): se a sala pronta deve dar algum bônus no futuro (hoje só visual).
- Refação do cenário em pixel art limpo (REQ-060, ART-021): quando executar, em que ordem as famílias, se o refinamento de REQ-054 já passa por este fluxo e se formigas, crias, pulgão e cupim entram no escopo.
- Direção de arte (REQ-047): regras e D1 a D5 decididas em 2026-10-21 ([documento 40](../Docs/02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md)); paleta mestra de 32 cores aprovada (etapa 0, ART-013); zoom até 32 px por célula e UI com escala própria decididos em 2026-10-21 (D7, etapa 11 descartada); falta a nova estratégia dos elementos de cenário (etapa 10).
- Limite de pintura: valores de base por ambiente, máximo de 2 salas e custo/teto da T+3 e o que fazer com saves já acima do limite (hoje ficam como estão e bloqueiam o pincel) (REQ-046, KI-043).
- Escavação de blocos grandes marcados: manter só a fronteira como frente de trabalho ou deixar as escavadoras abrirem mais frentes (KI-055, PRG-002).
- Área de resíduos: tempos de conversão, custo/anel do R2, se a terra deve ir à área antes do depósito de construção (hoje só a que iria ao despejo) e configuração por região (KI-037) (REQ-045, KI-042, KI-045).
- Árvore da rainha: o que fazer com os 15 nós sem sistema no jogo (EM BREVE) e se a árvore do formigueiro entra no protótipo; a jardineira (J0) não tem tarefa até existir o cultivo de fungo (REQ-018, KI-014)
- Tamanho do mundo (960x520) e zoom máximo aberto preso ao enquadramento inicial estão bons? (REQ-003)
- Regras da chuva: quando chove (manual × ciclo automático), compactação permanente ou temporária, se a chuva leve afeta areia despejada e o compactado (GD-002)
- Regras da água: valores de X e Y, se as formigas devem evitar a água (GD-003); conferir se a superfície inunda com chuva forte agora que a água só some no formigueiro (KI-009)
- Regras da rainha: o que ela faz sem sala, se formigas novas nascem nela, arte própria e se pintar a sala deveria já marcar para cavar (GD-004)
- Regras das plantas e nutrientes: valor de X (60 s por passo de crescimento), forma das plantas vinda dos sprites (touceira de ~300 a 420 pedaços), reinício da planta só quando não sobra nada para colher, densidade, jardineira escolhendo a planta sozinha e crescimento mínimo para colher, preço das formigas em nutrientes (GD-005)
- O loop do protótipo (marcar pixels para cavar; jardineiras colhem plantas sozinhas; nutrientes compram formigas) é o desejado? (GD-001)
- Se o experimento 16x16 e o mapa dobrado vão para a `main` e se formigas e cenário serão redesenhados a 16 px por célula (REQ-024). O mapa dobrado custa ~415 MB a mais e ~1,6x no tempo da colônia com 200 formigas cavando (KI-016); a arte 16x16 não pesa na simulação.
- Opções da tela de configurações e pausa ao abrir o menu (REQ-022); eventos, origem dos arquivos e música para SFX e VFX (REQ-023).
- Depósitos: areia e rocha ficam fora do depósito de construção? (REQ-036, KI-021)
- Formiga emprestada ao trabalho da comum mantém velocidade, carga, vida e ritmos do próprio papel e continua contada no papel dela no painel; confirmar (REQ-037).
- Juice (REQ-034): paleta de feedback e aprovação visual, opção separada para tremor de câmera, quais marcos ganham momento próprio e como REQ-023 e REQ-034 se dividem (a parte de VFX de REQ-023 é coberta por REQ-034).
- Ataques de cupins: balanceamento, crescimento do teto de cupins por ataque (hoje +5 pontos, até 100%), se a rainha pode ser atacada e se a área de resíduos vira compra (R0) (REQ-029, GD-007, KI-018).
- Melhorias incrementais (GD-008, REQ-039): níveis repetíveis e valores aprovados em 2026-10-19; falta decidir se a profundidade dá bônus direto, os requisitos cruzados novos em nós que já existem e como a tela Evolução comporta ~100 nós (arrastar/zoom).
- Descrição, plataformas-alvo e marco atual do jogo.
- Substituir a cena de template `Assets/Scenes/SampleScene.unity` e mover `Assets/Settings` para `Assets/Project`? (não feito: quebraria referências; fica como tarefa separada).

## Limitações da avaliação

- Input real e HUD não foram usados por uma pessoa; desempenho medido só no editor.
