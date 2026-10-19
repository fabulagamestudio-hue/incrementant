# Catálogo — mundo, combate e câmera

Cada linha liga um **fato** a um efeito. Intensidade pela magnitude; canal pela frequência (frequente = leve; raro = forte). "Teto" é o máximo simultâneo; em velocidade alta reduza cadência, não quantidade por rajada. Reduzido = metade das partículas, sem tremor/flash; Off = nada além da informação. Exemplos com números de dois jogos: [exemplos/spacefall.md](exemplos/spacefall.md) e [exemplos/incrementant/catalogo-mundo.md](exemplos/incrementant/catalogo-mundo.md).

## 1. Ação do jogador

| Fato | Efeito |
|---|---|
| Disparo/golpe/habilidade | clarão na boca/ponto de origem na direção da ação, tingido com a cor da arma; miolo curto (~0,13 s), raios rápidos e leque de faíscas; uma vez por rajada, não por projétil; recuo do corpo |
| Uso de habilidade especial | efeito próprio por habilidade, com carga e liberação distintas (descargas convergindo, jato ao liberar) |

## 2. Acerto e dano causado

| Fato | Efeito |
|---|---|
| Acerto | marcador de traços que abrem do ponto do impacto (nascem finos e engrossam, ease-out), toque seco; número de dano (ver catálogo de UI) |
| Dano num inimigo comum | clarão branco no alvo (valor acima de 1 para estourar) e volta à cor; não em chefes com brilho próprio |
| Camada de proteção quebra | estilhaço por camada, maior na última |
| Proteção do alvo zerada | pausa de impacto curta |

## 3. Morte e abate

| Fato | Efeito |
|---|---|
| Morte de inimigo comum | marcador de kill maior (mais traços), toque agudo (máx. 1 a cada ~60 ms), explosão do jogo, tremor de câmera que **cresce com a robustez** do alvo (camadas ou vida) |
| Morte robusta (2+ camadas) | pausa de impacto curta |
| Multikill (N kills quase simultâneas) | `+N` no centro das mortes e sino; agrupe por intervalo curto (~0,15 s), assim explosões em área contam sem saber a origem |

## 4. Encadeamento (combo)

- **Sequência:** cada kill reinicia a janela (ex.: 4 s); o contador só aparece a partir de um mínimo (ex.: 3); a barra mostra a janela; cada kill dá um tranco de mola no número; a cor esquenta com o tamanho.
- **Marcos do combo** (ex.: 10, 25, 50, 100): número salta, bordas da tela brilham, sino sobe de tom a cada marco; o contador passa a ser desenhado maior por faixa.
- **Janela esvaziando:** abaixo de ~30% o número treme e a barra pisca em vermelho cada vez mais depressa; cruzar 30/20/10% toca um tique grave; uma kill que enche a barra quando estava abaixo de ~40% "salva" o combo (pisca branco, número pula).
- **Recompensa no fim:** bônus **separado do loot**, pago de uma vez como clímax. Fórmula pura e testada, crescente mais que linear. Durante o combo a placa mostra o bônus futuro. No fim, os recursos **saem do número do combo** e caem como itens coletáveis (com arco e giro), divididos em 2–8 pedaços escalonados; ao pousar, faíscas, tranco leve e toque agudo. Festa: varredura de luz, bordas brilhando, tamanho do combo subindo, soco de FOV, arpejo e sino de tom crescente.
- Combo perdido se a sessão termina antes de pagar: decisão de design, registre.

## 5. Marcos da sessão

Total acumulado (ex.: 50, 100, 250, 500, 1000 e a cada 500): número sobe da nave/personagem, bordas brilham, soco de FOV crescente (de ~2° a ~3,5°), pausa curta, tremor e arpejo mais agudo a cada marco.

## 6. Ritmo da partida

- **Chegada de onda:** som grave, bordas âmbar e tranco de câmera, escalados pela ameaça normalizada. Sem texto (ondas vêm toda hora). A primeira onda não avisa.
- **Onda limpa:** arpejo + um brilho que desce a tela **acendendo só os elementos da HUD** (troca momentânea de material aditivo por sprite).
- **Aviso de chefe:** sirene, tremor, tela escurece, faixa com título e nome, bordas piscando três vezes (~2,8 s).

## 7. Clímax: morte de chefe

Pausa de impacto (~0,12 s) → **câmera lenta** → soco de FOV (~−4°, volta em ~1,3 s) → bordas douradas → fanfarra (arpejo + sino grave). Não acrescente chuva de loot sem decisão de balanceamento.

## 8. Dano recebido e risco

| Fato | Efeito |
|---|---|
| Golpe no casco | tremor `base + k × fração do dano`, brilho vermelho nas bordas (força e duração pela fração), pausa curta em golpe grande (≥ ~15%) |
| Golpe na proteção | tremor leve, brilho ciano discreto |
| Vida baixa (< ~30%) | bordas **pulsam** em vermelho, batimento cardíaco que acelera até ~5%; apaga ao curar/morrer |
| Reparo grande (≥ ~5% mais de uma vez) | brilho verde nas bordas e sino; a regeneração lenta **não** conta |
| Trava/proteção que anula dano | placas que fecham sobre o ponto atingido, raio na emenda, faíscas para o lado do golpe, som seco; sem bolha nem disco |
| Marca/debuff em inimigo | descargas curtas no casco, ligação por raio entre origem e alvo |

Só no modo solo se for efeito de tela cheia.

## 9. Movimento

| Fato | Efeito |
|---|---|
| Arrancada/dash | som de rajada (mais agudo no dash, máx. 1 a cada ~0,3 s), soco de FOV **para fora** só no dash, leque de faíscas, rastros |
| Enquanto dura | FOV abre aos poucos (a velocidade sobe devagar e desce mais rápido), rastros laterais com largura e duração pela mistura, **linhas de velocidade** na velocidade real na tela (≥ 4–7 px, paralaxe 0,55–1,25) |
| Esgotar o recurso | tom que cai com um sopro, borda pisca laranja |

Corrida contínua não leva soco (parece impacto); só abre o FOV.

## 10. Recompensa e coleta

- **Brilho no loot:** raios finos em cruz no plano do jogo (sem disco, coluna nem nada subindo no espaço), na cor do recurso, girando devagar e cintilando; cresce com a quantidade; **grande** o bastante para sobreviver à pixelização.
- **Atração:** dentro do raio, começa lenta (~0,7×) e acelera em ease-in até ~3× em ~0,45 s; o brilho cresce e deixa rastro.
- **Coleta:** luz em cometa (cabeça + fita com cauda) do ponto da coleta até o ícone do contador, com arco para um lado sorteado; ao chegar, o contador pulsa e o ícone dá um tranco.
- **Cadeia:** o tom sobe numa escala pentatônica (0, 2, 4, 7, 9, 12 semitons) e fecha na oitava; a HUD mostra `+total xN` com soco por coleta; a sexta seguida pisca as bordas em dourado.
- Fora do alcance de voo (muitos em voo, ponto atrás da câmera, jogo parado, sem alvo) o contador pulsa na hora.

## 11. Animação corporal e reação de entidades

Função do tempo e de um instante guardado por entidade, calculada ao desenhar. Aplique no filho/visual, nunca na rotação que o movimento calcula.

| Situação | Animação | Duração |
|---|---|---|
| Entregar/pegar carga | squash de alívio (1→0,9→1,05→1) / leve inclinação | 0,15–0,2 s |
| Nascer | squash vertical 0,6→1,1→1, casca/halo | 0,3 s |
| Ferida | recuo + pisca + squash 1→0,85→1 | 0,3 s |
| Morte natural | fade, afunda, pontos claros sobem (sem sangue em jogo cozy) | 0,6 s |
| Ciclo contínuo (cria, planta) | balanço lento com fase aleatória fixa | contínuo |

## 12. Câmera

| Gatilho | Efeito |
|---|---|
| Desabamento, pouso, game over, explosão grande | deslocamento aditivo decaindo 0,15–0,3 s, amplitude proporcional |
| Marco | respiro de zoom (+2%) que volta em 0,5 s, **só** se o jogador não estiver arrastando a câmera |
| Impacto forte | soco de FOV de −2° a −4° em tempo real |

Aplicar depois da suavização e do limitador, remover antes do quadro seguinte, nunca em Reduzido/Off. Atenuar com a distância ao ponto de interesse (cheio até X, some em Y).

## 13. Partículas e atmosfera

Partícula padrão: forma simples no estilo do jogo (quadrado de 1 pixel em pixel art; faísca em 3D), espaço de mundo, vida 0,25–0,6 s, cor do material afetado. Um sistema pré-aquecido por tipo, disparado com `Emit(n)` na posição; sem instanciar. Atmosfera de fundo barata (poeira, folhas, vagalumes), ≤ ~30 partículas, só visíveis, paradas na pausa.

Orçamento global: soma viva ≲ 300 em Completo e ≲ 150 em Reduzido (meça antes de aumentar); emissões por segundo limitadas por tipo; sem emitir fora da vista.
