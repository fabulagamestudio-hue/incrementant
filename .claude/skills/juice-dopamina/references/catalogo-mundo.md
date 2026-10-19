# Catálogo — mundo (partículas e animação)

Cada linha liga um **fato da simulação** a um efeito. "Fonte" é onde o fato já nasce hoje (`Emit(SfxId.X)` ou estado lido pela view). "Teto" é o máximo simultâneo; a 8x, reduza cadência, não quantidade por rajada. `Reduzido` = metade das partículas, sem tremor/flash; `Off` = nada.

## Partículas (`WorldFx`, `ParticleSystem` com pool por tipo)

Partícula padrão: quadrado de 1 célula (ou 0,5 célula) em `Sprites/Default`, filtro Point, sem rotação, simulação em espaço de mundo, gravidade leve, vida 0,25–0,6 s, cor do material afetado. Um sistema pré-aquecido por `FxId`; dispara com `Emit(count)` na posição da célula (sem `Instantiate`).

| Fato (fonte) | Efeito | Qtd / vida | Teto |
|---|---|---|---|
| Escavar (`Dig`) | poeira: 3–5 grãos na cor do material, saltam para cima/lado e caem | 4 / 0,4 s | 24 vivos, 1 emissão por célula a cada 0,15 s |
| Despejar / depositar (`Drop`) | grãos caindo da mandíbula, "tum" de poeira ao pousar | 3 / 0,35 s | 16 |
| Obra concluída (`Build`) | contorno da área pisca claro 0,3 s + anel de poeira dourada nos cantos; marca de obra some em fade | 8 / 0,5 s | 1 por sala por vez |
| Plantar / broto (`Plant`) | 2–3 pixels verdes sobem e o broto faz "pop" (0→1,2→1 em 0,25 s) | 3 / 0,5 s | 12 |
| Nutriente entregue à rainha | faíscas douradas do ponto de entrega + "+N" agregado (ver UI) | 3–6 / 0,6 s | 20 |
| Ovo posto (`EggLaid`) | ovo "salta" ao surgir (0→1,15→1), 2 pontinhos claros | 2 / 0,4 s | 8 |
| Nascimento (`AntBorn`) | casca se abre: 5 pixels de casca, formiga emerge com squash vertical (0,6→1,1→1, 0,3 s), halo claro curto | 5 / 0,5 s | 8 |
| Formiga morre (`AntDie`, velhice) | desmaio suave: fade+afunda 4 px, 2 pontos esbranquiçados sobem (sem sangue) | 2 / 0,6 s | 6 |
| Afogamento (`Drowned`) | bolhas azuis sobem, formiga balança | 4 / 0,8 s | 6 |
| Água/chuva | respingos na superfície, ondinhas nas poças; só na vista, pela câmera | 1–2 / 0,25 s por gota visível | 40 (proporcional à intensidade) |
| Aviso de ataque (`RaidWarning`) | poeira cai do teto, vibração de 1 px da borda superior do formigueiro (só Completo) | 6 / 0,8 s | 12 |
| Cupim pousa (`RaidStart`) | nuvem de poeira e folhinhas no ponto de pouso; asas soltas caem (já há quadros de "pouso") | 8 / 0,6 s | 16 |
| Cupim atingido (`TermiteHit`) | pisca (já existe `_termiteHitColor`) + 3 faíscas + recuo de 1 célula em 0,1 s | 3 / 0,25 s | 12 |
| Cupim morto (`TermiteKilled`) | estouro de pixels marrons, "plop" de escala; corpo cai e escurece (já há cor de cadáver) | 8 / 0,5 s | 16 |
| Formiga ferida (`AntHurt`) | pisca branco-avermelhado 0,08 s + 2 pixels; encolhe (squash) 1→0,85→1 | 2 / 0,3 s | 12 |
| Desabamento (`Crumble`) | rajada de grãos conforme as células caem + tremor leve da câmera (só Completo) | 12 / 0,7 s | 48, 1 por 0,3 s |
| Decomposição de resíduos | fragmentos pequenos desaparecendo, pilha encolhe | 2 / 0,5 s | 10 |
| Gotas de orvalho (sala) | gotinha brilha e cai no ritmo da produção | 1 / 0,6 s | 6 |
| Compra de evolução (`Buy`) | ver catálogo de UI (a faísca nasce na tela, não no mundo) | — | — |

## Animação corporal (views, sem partículas)

Aplique no filho/visual, nunca na rotação que `AntSwarmView` calcula.

| Situação | Animação | Duração |
|---|---|---|
| Entregar carga | "sacudidela" de alívio: squash 1→0,9 (y) →1,05→1 | 0,2 s |
| Pegar carga | leve inclinação para baixo e retorno | 0,15 s |
| Cavar | já tem quadros (`_digFramesPerSecond`); adicionar micro vibração de 0,5 px a cada golpe | contínuo |
| Nascer | squash vertical (ver acima) | 0,3 s |
| Ferida | recuo + pisca; fica 0,3 s mais lenta visualmente (não na simulação) | 0,3 s |
| Rainha põe ovo | barra de postura já existe; adicionar "contração" 1→1,08→1 e pulso ao pôr | 0,25 s |
| Cupim em voo/pouso | oscilação vertical senoidal no voo; "queda" com stretch ao pousar | 0,3 s |
| Cultivo (fungo/broto) | `CultivationView` já recebe `time`; adicionar balanço de brisa ±1 px e "pop" ao amadurecer | contínuo |
| Cria (BroodView) | ovo/larva balançam lentamente, sincronizados por fase aleatória fixa | contínuo |

Regra de custo: a animação é função do **tempo e de um timestamp por entidade** (padrão `LastHitTime`), calculada na view ao desenhar — nada de corrotina por formiga.

## Câmera (`CameraKick`)

| Gatilho | Efeito | Só em |
|---|---|---|
| Desabamento grande, pouso do cupim, game over | deslocamento aditivo decaindo 0,15–0,3 s, amplitude ≤ 0,3 célula | Completo |
| Marco (nova classe) | leve "respiro": zoom +2% e volta em 0,5 s, **só** se o jogador não estiver arrastando a câmera | Completo |
| Clique no mapa com Dedo | micro empurrão de 1 px na direção do impacto (opcional) | Completo |

Aplicar após o `Clamp`/`StepTransition` de `MapCamera`, remover antes do próximo quadro. Nunca em Reduzido ou Desligado.

## Feedback do pincel (cavar, construir)

| Momento | Efeito |
|---|---|
| Passar o pincel | prévia colorida com custo acompanhando (design 35) |
| Início do traço (`Mark`) | pulso curto no ponto inicial |
| Preenchimento confirmado | área marcada "enche" com animação (varredura de 0,2 s) em vez de aparecer de uma vez |
| Pincel inválido/negado | borda pisca vermelho-terra + `UiDeny` |

## Atmosfera (vida de fundo, barata)

Poeira flutuando na luz da superfície, folhas ocasionais, vagalumes à noite se houver ciclo dia/noite. Máx. ~30 partículas, só visíveis, parados na pausa. `Off` desliga; `Reduzido` corta pela metade.

## Orçamento global

- Soma de partículas vivas ≤ ~300 em Completo, ~150 em Reduzido (medir no F11 antes de aumentar).
- Emissões por segundo limitadas por `FxId` (tabela acima) e agregadas por célula/sala.
- Culling por câmera: não emita fora da vista (use a janela visível que `AntFarmView` já calcula).
- A 8x, o fato continua chegando em ritmo de simulação; mantenha o teto por tempo real, não por passo.
