# 05 — Terreno escavação e navegação

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 05. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

1. Uma célula recebe uma marcação de obra paga e permanente.

2. O sistema localiza frentes alcançáveis por alguma trabalhadora capaz da tarefa.

3. Uma operária comum ou escavadora remove material e recebe uma carga de terra.

4. Ao atingir a carga, ela percorre uma rota até um ponto de descarte na superfície.

5. O material descartado forma pilhas e passa a participar da física e da chuva.

6. A trabalhadora retorna. A obra avança até que as células marcadas estejam livres e o espaço funcional.

## Materiais

| Material | Comportamento definido | Proposta inicial |
| --- | --- | --- |
| Terra firme | Mantém aberturas e permite escavação normal | Comum remove 16 células/s; escavadora 32 células/s |
| Areia | Escorre e desmorona quando perde sustentação | Taxa de remoção igual à terra; queda gera trabalho adicional |
| Pedra | Bloqueia as classes comuns; exige melhoria de escavadora | Escavadora com E2 remove 4 células/s |
| Crosta de pilha | Forma rígida nas bordas após chuva forte | Escavável como terra firme; comportamento fino ainda a validar |
| Água | Forma poças e ameaça formigas por exposição prolongada | Morte ao atingir 8 s de submersão acumulada sem recuperação |

As formigas respeitam a geometria existente, sem atravessar terra, pedra ou ocupações sólidas. Quando uma passagem muda, recalculam caminho ou aguardam. Nenhuma habilidade faz a pedra sumir globalmente: o trabalho aparece em rachaduras e fragmentos carregados para fora.

Reescavar uma área soterrada remove material de uma marcação existente e não cobra outra pintura. A areia pode aumentar o trabalho de uma obra sem alterar seu orçamento já pago. Travessias ou pontes sobre água foram removidas. A proteção contra poças depende de planejamento, resgate e rotas possíveis.

## O que o jogador acompanha numa obra

Uma marca de obra mostra intenção. A frente ativa mostra onde alguma formiga consegue trabalhar agora. Cargas que saem e pilhas que crescem demonstram a transformação real do espaço. Um desenho concluído visualmente não deve continuar sendo uma sala vazia sem função por uma condição escondida.

Terra firme, areia e rocha mantêm suas diferenças durante a obra. Areia que cai em uma região paga continua sendo areia a remover. Se cair numa passagem antiga, a passagem continua identificada e pode voltar a funcionar depois da limpeza. O trabalho não recebe uma segunda cobrança de pintura.

A navegação é consequência do terreno: uma formiga sem caminho espera ou busca outro trajeto possível. O jogador pode reconhecer o bloqueio e corrigi-lo. A velocidade de escavação mede retirada de material, enquanto velocidade de deslocamento mede viagem; uma não substitui a outra.

---

[← 04 — Proposta de unidade de área e cobrança](../04_Data/04_Proposta_de_unidade_de_area_e_cobranca.md) · [06 — Fluxo completo do início do jogo →](../01_Design/06_Fluxo_completo_do_inicio_do_jogo.md)
