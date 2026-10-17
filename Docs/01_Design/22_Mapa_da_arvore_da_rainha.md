# 22 — Mapa da árvore da rainha

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 22. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Ramo | Sequência ou opções |
| --- | --- |
| Rainha | Q0 postura → Q1 acelerada → Q2 lotes |
| Operária comum | O0 inicial → O1 carga, O2 velocidade e O3 resgate em ramos independentes |
| Carregadora | L0 classe → L1 carga → L2 cooperação; L3 velocidade após L0 como requisito proposto |
| Cortadeira | F0 classe → F1 cortes maiores; várias na mesma folha é funcionamento básico |
| Escavadora | E0 classe → E1 remoção ampliada → E2 quebra de rocha |
| Jardineira | J0 classe → J1 produção de fungo → J2 transplante |
| Cuidadora | N0 classe → N1 alimentação em lote → N2 resgate |
| Soldado | S0 classe → S1 mandíbulas ou S2 carapaça → S3 contenção após ambos |

Tipos de formiga nunca são liberados por depósito, cultivo ou outro ambiente. O desbloqueio da classe permite à rainha produzi-la em ciclos futuros. Não cria adultas gratuitas e não transforma uma comum instantaneamente numa especialista. Pré-requisitos internos seguem o ramo; custos em N são propostas.

Postura em lotes permite mais de um ovo por ciclo. Trabalho em grupo na mesma folha é uma capacidade básica. Transporte cooperativo e velocidade são melhorias distintas das carregadoras, ambas presentes na árvore.

As árvores são separadas, mas os efeitos se encontram na simulação: é possível desbloquear uma classe antes de construir uma área útil para ela. A interface informa que ela está sem tarefa pertinente, em vez de impor um vínculo de desbloqueio que a equipe rejeitou.

## Critério de desbloqueio das classes

A árvore da rainha permite escolher as classes independentemente dos ambientes. O sistema de reprodução precisa de berçário para iniciar ciclos normais, mas isso não transforma o berçário em compra que libera cuidadoras. Função do ambiente e acesso à classe permanecem decisões diferentes.

A cooperação de transporte das carregadoras continua presente. A atividade básica de várias formigas cortarem a mesma folha permanece desde o início. O aumento de velocidade das carregadoras é outro efeito, aplicado ao deslocamento. Essa distinção vale na árvore e na descrição das ações.

---

[← 21 — Resíduos habilidades e balanceamento](../01_Design/21_Residuos_habilidades_e_balanceamento.md) · [23 — Rainha e operária habilidades e números →](../01_Design/23_Rainha_e_operaria_habilidades_e_numeros.md)
