# 08 — Fluxo contínuo da colônia

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 08. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

1. Plantas oferecem folhas e acontecimentos podem disponibilizar frutos ou folha caída.

2. Comuns, cortadeiras e carregadoras realizam corte e transporte conforme capacidades.

3. Cargas vão ao depósito quando disponível ou ao abastecimento do cultivo. O depósito permite reserva para interrupções da coleta.

4. Folhas e frutos alimentam o fungo; o preparo opcional tritura material e forma sua própria reserva.

5. O cultivo gera fungo processável, colheita e nutrientes. Fruto rende duas vezes a folha para a mesma unidade de material processado.

6. Material perecível se deteriora e gera resíduo, além dos restos normais do processamento. Comuns retiram cargas e corpos para a área de resíduos.

7. A reserva de nutrientes financia pintura, desbloqueios, melhorias e alimento reservado de novas crias.

8. A rainha aplica a política escolhida, confere berçário, capacidade e reserva mínima, e inicia novos ovos ou lotes.

9. Crias seguem até adultas, assumem tarefas e envelhecem. Perdas por idade, combate e água diminuem a população.

10. A expansão aumenta área útil e funções. Desmoronamentos e água podem retirar capacidade e exigir recuperação física.

11. Eventos alteram a rotina. Soldados enfrentam invasoras; operárias e cuidadoras aprimoradas podem resgatar adultas e crias.

12. O jogador observa gargalos e investe na próxima transformação. Sem população produtiva, aplica-se a recuperação de cinco operárias; morte da rainha abre game over.

## Princípio de ritmo

A próxima conquista deve estar visível antes de a atual terminar. O desejo vem de algo concreto a acompanhar: carga maior, fase de cria organizada, fungo se espalhando, rocha rachando. Custos crescentes sem transformação não bastam.

Testar quando há escolha relevante e quando há apenas espera. Acompanhar uma obra pode sustentar um intervalo maior que olhar o saldo subir. A experiência usa efeitos de juice para reforçar resultados, mantendo produção automática e escolhas periódicas.

## Escolhas recorrentes

Mais coleta só aumenta a produção final se o cultivo consegue receber e processar o material. Mais cultivo só ajuda se há abastecimento e atendimento. Mais berçário só ajuda se a rainha pode iniciar crias e se há capacidade adulta futura. A interface deve tornar essas relações compreensíveis.

A pessoa alterna observar e decidir: economizar para um novo papel, ampliar o que já funciona, manter alimento para reprodução ou resolver perda de espaço. A rotina não pede cliques contínuos para cada entrega. Eventos criam variação sem substituir a progressão econômica.

---

[← 07 — Garantias do tutorial e primeiros objetivos](../01_Design/07_Garantias_do_tutorial_e_primeiros_objetivos.md) · [09 — Recursos fungo e perecibilidade →](../01_Design/09_Recursos_fungo_e_perecibilidade.md)
