# 37 — Continuidade pausa e game over

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 37. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## O que continuar uma partida preserva

Continuar preserva a colônia como o jogador a deixou: nutrientes e estoques, idade dos alimentos, fungo e resíduos, terreno e marcações, progresso das obras, melhorias compradas, formigas vivas, crias e fases, política da rainha, idade e risco de água das formigas, capacidade, eventos e posição da rainha. Preferências de áudio e tela também permanecem disponíveis.

Proposta de continuidade: oferecer um ponto de retorno a cada 30 s de jogo e após marcos relevantes, mantendo três momentos anteriores recuperáveis. Voltar ao menu ou sair preserva a partida. Após game over, ainda deve existir uma opção de voltar a um momento em que a rainha estava viva. Esses intervalos e quantidades são propostas de design.

## Fluxo de derrota

1. Detectar vida da rainha em zero por ataque, água ou outra causa definida.

2. Interromper a simulação e mostrar causa da morte no game over.

3. Retomar ponto anterior apresenta estados recuperáveis com tempo de jogo e informação útil.

4. Retomar devolve compras, nascimentos, materiais e obras ao momento escolhido, sem cobrar novamente ou repetir uma recompensa.

5. Reiniciar formigueiro volta ao estado de nova partida e à cinemática ou ao início equivalente.

A ausência de produtivas dispara recuperação; não é game over. Um deadlock físico sem solução pode levar o jogador a usar retorno ou reinício voluntário. Disponibilizar essas ações em algum lugar acessível da pausa é necessário, mas sua posição exata ainda precisa ser fechada.

Pausa congela movimento, processos, deterioração, decomposição, idade, reprodução, exposição à água, combate e eventos. Sons de interface e ajustes de áudio podem continuar. A partida não avança com base no tempo de relógio durante a pausa ou enquanto o jogo está fechado. Progresso offline não foi definido para esta versão.

## Retorno coerente ao mundo

Retomar uma partida devolve as situações ao ponto escolhido: uma pupa continua perto de nascer, um ambiente soterrado continua com sua marcação e uma melhoria paga continua comprada. Retorno anterior após derrota volta também as consequências posteriores àquele momento, sem manter ganhos do futuro.

Pausar não consome a vida útil do alimento, o tempo de uma cria ou a tolerância de uma formiga na água. O próximo nascimento e o evento em andamento permanecem na mesma posição temporal. Configurações de interface e áudio podem responder enquanto a colônia fica parada.

Em um deadlock com a rainha ainda viva, não surge um game over obrigatório apenas por falta de progresso. O jogador pode procurar saída, retomar um momento anterior ou reiniciar. A localização desses comandos na pausa e a seleção de momentos anteriores precisam ser finalizadas, preservando as opções aprovadas.

---

[← 36 — Escopo completo e demonstração da jam](../03_Production/36_Escopo_completo_e_demonstracao_da_jam.md) · [38 — Referências da ambientação →](../05_Narrative/38_Referencias_da_ambientacao.md)
