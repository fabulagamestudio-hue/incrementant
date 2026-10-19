# Catálogo — interface, compra e progressão

Valores são pontos de partida a afinar jogando. Toda animação de UI usa tempo não escalado, respeita as opções (coluna **Reduzido / Off**) e termina em estado exato (escala 1, alfa 1). Exemplo completo com números de um jogo real: [exemplos/incrementant/catalogo-ui.md](exemplos/incrementant/catalogo-ui.md).

## 1. Botões e controles

Anexe o comportamento a **todos** os botões por uma chamada única em runtime (sobrevive a UI gerada por script). Anime o `RectTransform` do botão ou de um filho visual; **não troque sprite nem modo de transição**, para não brigar com o realce que o projeto já tem.

| Estado | Efeito | Parâmetros iniciais | Reduzido / Off |
|---|---|---|---|
| Hover entra | escala sobe e leve brilho de cor | 1,0→1,06 em 0,10 s ease-out | só cor / nada |
| Hover sai | volta | 0,12 s ease-out | idem |
| Pressionar | afunda (squash) | escala ~0,94 em 0,06 s | 0,97 / nada |
| Soltar (clique válido) | retorno elástico, anel opcional no ponto do clique | →1 em 0,22 s com overshoot ~1,1 | só retorno / nada |
| Clique negado | tremidinha horizontal + cor de erro curta, com o som de recusa | ±3 px, 3 ciclos em 0,18 s | só cor / nada |
| Desabilitado | sem hover nem pressão | — | — |
| Foco por teclado/controle | mesmo realce do hover | — | — |
| Alternar (toggle) | "pop" do marcador ao ligar | 1→1,25→1 em 0,15 s | nada |
| Idle | balanço lento em repouso e brilho que varre, **um botão por vez**, sorteado | respiração de 1,5–2,5 s | nada |

Regras:
- Se o hover está em andamento e vem um clique, parta do valor atual, não do repouso.
- Ao desativar o objeto no meio, volte ao repouso em `OnDisable`.
- Som de hover e de clique em todo botão, com tipos separados.
- Controles contínuos (slider) têm interação própria: tique de som, sem escalar.

## 2. Painéis e telas

Painéis que aparecem e somem por ativação seca ganham transição por grupo de opacidade:

| Elemento | Entrada | Saída |
|---|---|---|
| Menu/opções | fundo escurece em 0,15 s; painel sobe ~12 px e escala 0,96→1 com overshoot, 0,2 s | inverso em 0,12 s |
| Tela cheia | fade 0,18 s + itens em **cascata** (atraso 12–50 ms por item, teto 0,3 s) | fade 0,12 s |
| Fim de partida | fundo escurece devagar (0,8 s), título desce com leve quique, botões surgem em sequência | — |
| Submenus | deslizar ~8 px + fade 0,12 s | 0,08 s |
| Alerta | desliza do topo 0,2 s; pulso 2× enquanto a contagem é iminente | sobe e some 0,2 s |
| Dica | fade 0,08 s | imediato |

Regras: interatividade só liga ao fim da entrada, **sem bloquear o botão de voltar/ESC**. Cascata nova no mesmo contêiner devolve as linhas ao normal antes de recomeçar; ao fechar a tela, restaure (senão o grupo fica em alfa 0). Com efeitos desligados, estado final imediato.

## 3. Contadores e saldos

| Evento | Efeito |
|---|---|
| Saldo sobe | número pulsa 1→1,12→1 (0,15 s) + cor de ganho 0,3 s; ícone dá um "tic" |
| Saldo desce (compra) | número em cor de custo 0,2 s e "afunda" 2 px; custo sai flutuando |
| Contagem animada | o valor mostrado interpola até o real em 0,2–0,6 s, com tique, soco e clarão (quente ao gastar, dourado ao ganhar). Nunca mostre mais que o real |
| Capacidade cheia | cor de aviso suave, sem pulso repetido |

Agregação: no máximo um pulso por contador a cada ~0,25 s. Se o ganho vem de uma coleta visível, **adie o pulso para a chegada** da luz ao contador.

## 4. Números flutuantes

Pool de rótulos numa camada própria acima do mundo, com contorno na cor do fundo para ler sobre qualquer cenário.

| Uso | Cor | Movimento |
|---|---|---|
| Dano causado | branco frio no casco, ciano no escudo (ou paleta do jogo) | nasce pequeno, estoura além do tamanho, assenta como mola e sobe |
| Produção/ganho | dourado, agregado por origem em janela de 0,4–0,6 s | sobe ~24 px em 0,8 s, fade nos últimos 30% |
| Compra | cor de custo, saindo do contador | cai ~16 px |
| Recusa | cinza-avermelhado, curto | tremidinha 0,4 s |

Tamanho cresce com a magnitude (logaritmo entre ~0,85× e ~1,7× se não houver crítico). O primeiro acerto num alvo mostra **na hora**; os que chegam em ~0,14 s somam e saem juntos. Teto de simultâneos (~12 a 40) com reciclagem do mais antigo; nunca sobre o cursor.

## 5. Compra, upgrade e árvore de progresso

| Momento | Efeito |
|---|---|
| Hover em nó | anel pulsa; ligações acendem |
| Compra bem-sucedida | som de dois sinos subindo (mais agudo no marco); **faíscas do saldo até o item**; item estoura 1→1,3→1; ligações acendem em onda; saldo conta ao novo valor |
| Item que passa a ser comprável | brilho respirando lento até ser visto |
| Compra recusada | item treme + som de recusa |
| Segmento de nível que enche | estoura ao preencher |
| Cascata ao liberar nós | itens filhos aparecem em sequência |

## 6. Revelação por raridade

Quando o jogo oferece cartas/itens com raridade, a revelação diferencia o raro:
- Entrada em sequência, no compasso de um estalo cujo tom sobe a cada carta.
- Anel que enche do nível atual ao próximo, ícone que estoura, fundo que pisca na cor da raridade (as mais raras com cor própria).
- Som: estalo + sino (novo), sino agudo (reforço), arpejo + sino alto (especial). O mais raro chega ~0,15 s depois, para pesar.
- Ao confirmar: clarão, arpejo, faíscas do saldo que pagou até a carta, e a carta **fica na tela** enquanto as outras saem.

## 7. Marcos e celebrações raras

Curtos (≤ 2 s), dispensáveis, sem pausar o jogo, só em eventos raros: nova habilidade/classe liberada, ameaça repelida, marcos de população ou pontuação, primeiro de cada tipo. Combine faixa/pulso na UI, partículas discretas e um som de conquista. O tamanho da celebração cresce com o marco.

## 8. Progresso fora do combate

- **Tutorial/objetivos:** tique por quarto da barra, comemoração ao cumprir cada passo (faíscas radiais do elemento + clarão que assenta na cor final) e uma maior no fim.
- **Descoberta de conteúdo opcional:** pulso de sonar quando entra na vista, reação ao primeiro contato, sino e faixa de luz quando a recompensa chega ao jogador, contador no diálogo.

## 9. Fim de sessão

Relatório com sequência de etapas: contagens em ease-out com **tique de tom subindo** a cada passo (mín. ~50 ms entre tiques), sino por etapa em escada de tons, "soco" nos valores finais. Em derrota, tiques mais graves e sinos mudos. Pular a apresentação não toca nada.

## 10. Menu principal e ambiente vivo

Título com flutuação lenta, fundo com deriva (estrelas, poeira, folhas), cantos e logo animados, contagem dos slots de save, popups com entrada. O mundo do menu pode ligar o serviço de partículas de propósito, respeitando as opções. Splash só precisa obedecer às opções quando existirem antes do primeiro jogo.
