# 28 — Custos totais e regras de aplicação de melhorias

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 28. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Ramo | Custo total proposto em N | Inclui |
| --- | --- | --- |
| Cultivo | 870 | Desbloqueios C0 a C4; não inclui pintura |
| Berçário | 530 | B0 a B3 |
| Depósito | 490 | D0 a D2 |
| Resíduos | 200 | R0 e R1 |
| Rainha | 600 | Q0 a Q2 |
| Comum | 440 | O1, O2 e O3 independentes |
| Carregadora | 800 | Classe e todas as três capacidades |
| Cortadeira | 380 | Classe e cortes maiores |
| Escavadora | 780 | Classe, remoção e rocha |
| Jardineira | 720 | Classe, produção e transplante |
| Cuidadora | 760 | Classe, lote e resgate |
| Soldado | 1160 | Classe, mandíbulas, carapaça e contenção |

Custos são pagos uma vez por habilidade, separados de custo por pintura e custo alimentar por cria. Uma compra afeta o conjunto correspondente de indivíduos existentes e futuros. Ela não reaparece mais cara para cada nova formiga. A configuração inicial usa uma compra por nó, sem níveis infinitos já definidos.

Pré-requisitos com “e” exigem ambos. Ramos paralelos podem ser comprados em qualquer ordem depois da base. A árvore mostra custo, efeito, base e valor após a compra, requisitos e mudança visual esperada. Se o saldo não cobre, não debitar nem aplicar parcialmente.

Modificadores de fontes diferentes multiplicam apenas quando atuam na mesma taxa de forma declarada. Melhorias que trocam o valor base, como velocidade da classe, aplicam a substituição uma vez. Ao continuar uma partida, as compras já realizadas permanecem adquiridas, sem nova cobrança.

## Compra e continuidade de efeitos

Uma compra concluída desconta o valor e passa a valer uma vez. Clicar de novo num nó adquirido não cobra de novo. Sem saldo ou pré-requisito, a compra não acontece e o motivo aparece. Uma classe comprada abre a produção futura, enquanto uma capacidade comprada melhora suas adultas atuais e futuras.

Pintura, desbloqueio e alimento são custos distintos. Ao continuar uma partida, melhorias e obras já pagas permanecem. Nenhum retorno ao menu exige recomprar sala, pagar outra vez uma cria ou repetir a recompensa de uma colheita.

---

[← 27 — Soldado habilidades e números](../01_Design/27_Soldado_habilidades_e_numeros.md) · [29 — Consistência econômica e cenários de teste →](../04_Data/29_Consistencia_economica_e_cenarios_de_teste.md)
