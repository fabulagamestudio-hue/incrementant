# 24 — Carregadora e cortadeira habilidades e números

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 24. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| L0  Carregadora<br>Exige Árvore da rainha | 140 | Comum: carga 2 e velocidade 24<br>Após compra: Carga 4 UM; velocidade 28 células/s |
| L1  Cargas maiores<br>Exige L0 | 160 | 4 UM<br>Após compra: 6 UM (+50%) |
| L2  Transporte cooperativo<br>Exige L1 | 300 | Transporte individual<br>Após compra: 2 carregadoras levam peça de até 8 UM a 20 células/s |
| L3  Velocidade das carregadoras<br>Exige L0 | 200 | 28 células/s<br>Após compra: 35 células/s (+25%); dupla: 25 células/s |
| F0  Cortadeira<br>Exige Árvore da rainha | 160 | Corte comum: 2 s por 2 UM<br>Após compra: Corte: 1 s por 2 UM |
| F1  Cortes maiores<br>Exige F0 | 220 | 2 UM em 1 s<br>Após compra: 4 UM em 1,5 s |

L0 Carregadora. Nova classe nas trilhas. Consegue cortar e levar folhas mesmo sem cortadeira. A especialização prioriza carga e deslocamento.

L1 Cargas maiores. Folha ou fruto maior. Aumenta visivelmente o volume levado e reduz o número de viagens por mesma quantidade.

L2 Transporte cooperativo. Transporte em dupla. As duas carregadoras reservam a mesma carga cooperativa. A peça só se move quando ambas chegam. Se uma morrer ou perder acesso, a tarefa pode largar a carga e liberar reservas.

L3 Velocidade das carregadoras. Fluxo mais rápido das cargas. Mantém a cooperação existente e melhora deslocamento individual e da dupla. A posição desta compra como ramo paralelo é uma proposta de requisito para simplificar escolhas.

F0 Cortadeira. Corte dedicado na superfície. Diferencia corte dedicado. Mais de uma formiga no mesmo recurso funciona desde o básico.

F1 Cortes maiores. Pedaços maiores preparados. Prepara uma peça maior em uma ação, com animação compatível. Não obriga cooperação para toda folha: peças podem ser repartidas por coleta comum.

O transporte cooperativo é melhoria ativa do projeto. Cortar a mesma folha em grupo já funciona desde o início; carregar uma peça em dupla exige L2. São comportamentos diferentes, e somente o segundo ocupa uma compra da árvore.

Custo de alimentação por cria: carregadora 24 N e cortadeira 24 N. Cargas grandes de até 8 UM podem ser divididas no destino antes de D2; o depósito aprimorado permite manter a peça inteira. A relação entre corte F1, carga L1 e cooperação L2 precisa de teste visual e de caminhos.

## Ação cooperativa e corte paralelo

No transporte cooperativo, a peça grande depende de duas carregadoras para iniciar movimento. Quando uma delas morre, fica sem acesso ou precisa abandonar a tarefa, a carga pode cair e aguardar nova dupla, sem multiplicar o material. O estado visível da peça acompanha o transporte de verdade.

A cortadeira prepara alimento para transporte individual ou cooperativo. Cortes maiores não fazem toda coleta exigir carregadoras em dupla. Várias formigas na mesma folha é um comportamento inicial, mesmo antes da cortadeira, desde que haja espaço físico para trabalhar.

L3 aparece como ramo paralelo após L0 na proposta de pré-requisitos. A equipe pode testar outra posição sem apagar sua função. Não retirar L2 para acomodar L3: as duas habilidades existem no conjunto final.

---

[← 23 — Rainha e operária habilidades e números](../01_Design/23_Rainha_e_operaria_habilidades_e_numeros.md) · [25 — Escavadora habilidades e números →](../01_Design/25_Escavadora_habilidades_e_numeros.md)
