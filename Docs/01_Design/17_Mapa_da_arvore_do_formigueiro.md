# 17 — Mapa da árvore do formigueiro

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 17. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

A árvore do formigueiro reúne pincéis de ambientes e melhorias desses espaços. As setas textuais indicam pré-requisitos. Cada compra é única nesta proposta e os ramos seguintes detalham os comportamentos, custos e mudanças observáveis.

| Ramo | Sequência definida |
| --- | --- |
| Base | P0 Postura e T0 túnel comum disponíveis no início |
| Cultivo | C0 inicial → C1 canteiros → C2 preparo ou C3 rendimento → C4 integrado após ambos |
| Berçário | B0 inicial → B1 leitos ou B2 maturação → B3 organizado após ambos |
| Depósito | D0 inicial → D1 pilhas organizadas → D2 cargas grandes |
| Resíduos | R0 área de resíduos → R1 decomposição acelerada |

Depois do cultivo, o berçário abre o crescimento da população. Depósito e resíduos são ramos de organização independentes: o jogador pode escolher quando investir em cada um, sem serem condição para produzir nutrientes básicos. A ordem pedagógica sugere berçário antes de apresentar toda a árvore.

O jogador pode ampliar qualquer ambiente desbloqueado pintando mais área, pagando sua tarifa e aguardando escavação. Isso não é uma nova compra de desbloqueio. Mais área funcional aumenta a capacidade do ambiente conforme a tabela, sem exigir que a sala tenha forma redonda.

Os pontos de retirada múltiplos são básicos e não aparecem como compra. Resíduos têm somente dois níveis. Pontes e travessias sobre água foram removidas. Não acrescentar retenção de umidade ou estabilidade abstrata como ramos confirmados; foram discutidas e deixadas de lado em favor de efeitos concretos.

## O que não é uma compra desta árvore

Ampliar uma área usa o pincel e sua tarifa, sem repetir o desbloqueio. Várias formigas retirarem alimento simultaneamente é funcionamento básico do depósito. Passagens genéricas são liberadas desde o início. Não existe habilidade de ponte ou travessia sobre água.

A árvore atual não inclui um terceiro nível de resíduos, pátio de compostagem, retenção de umidade ou estabilidade abstrata do terreno. Os ramos visíveis nas tabelas são a referência de conteúdo, sem preencher espaços vazios com habilidades presumidas.

---

[← 16 — Fluxos de tarefas e emergências](../01_Design/16_Fluxos_de_tarefas_e_emergencias.md) · [18 — Cultivo habilidades e balanceamento →](../01_Design/18_Cultivo_habilidades_e_balanceamento.md)
