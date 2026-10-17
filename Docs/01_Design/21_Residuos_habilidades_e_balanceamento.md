# 21 — Resíduos habilidades e balanceamento

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 21. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| R0  Área de resíduos<br>Exige C0 concluído | 60 | Resíduos ocupam origem<br>Após compra: 12 UR/B; decomposição de 120 s por UR |
| R1  Decomposição acelerada<br>Exige R0 | 140 | 120 s por UR<br>Após compra: 60 s por UR (−50%) |

R0 Área de resíduos. Transporte e pilhas diminuindo. A área recebe restos de folhas, frutos e partes velhas do fungo, além de corpos removidos por operárias. O transporte é uma atividade visível.

R1 Decomposição acelerada. Fragmentação e desaparecimento mais rápidos. Acelera o desaparecimento de cada carga depois da chegada, sem gerar uma moeda nova ou adubo obrigatório.

## Fluxo e espaço ocupado

1. Processamento e deterioração deixam resíduos no local de origem.

2. Uma comum ou trabalhadora adequada reserva e recolhe a carga.

3. A carga percorre um caminho alcançável até uma área de resíduos com espaço.

4. Ao ser depositada, inicia decomposição própria e perde volume em etapas.

5. A área libera espaço para novos descartes conforme o material desaparece.

Resíduos em cultivo, depósito ou preparo ocupam capacidade útil local. Proposta: acima de 25% da capacidade de itens de uma área, eficiência local cai para 75%; acima de 50%, cai para 50%. A limpeza reverte a penalidade. Não deteriorar o próprio saldo monetário de nutrientes.

Antes do desbloqueio de R0, os restos se acumulam lentamente na origem. O valor proposto de 0,15 UR por UM deve deixar tempo para descobrir o ramo, evitando uma parede de progresso logo após o primeiro cultivo. Considerar aviso quando a capacidade local chegar a 20% de ocupação por lixo.

Cada corpo equivale inicialmente a 1 UR de ocupação, mas conserva aparência de formiga durante transporte e desaparecimento. Terra escavada não é levada ao descarte interno: forma pilhas externas e interage com chuva. Pátio de compostagem e um terceiro nível foram removidos.

## Descarte e consequências da falta de espaço

Quando o descarte está cheio ou sem acesso, resíduos ficam na origem ou aguardam transporte. O jogador vê a sujeira ocupar o espaço e pode ampliar a área paga ou reabrir o caminho. A decomposição que libera capacidade começa ao chegar à área de resíduos.

R0 já faz as pilhas desaparecerem gradualmente. R1 apenas acelera esse processo. Não há moeda de lixo, produção de adubo ou compra de compostagem presumida. A terra de escavação continua tendo destino externo.

A redução de produção por lixo é uma proposta de balanceamento para dar consequência à limpeza. O ponto central definido é retirar sobras do cultivo para liberar espaço. Outros tipos de restos podem ser adicionados no futuro, mas a versão atual contempla sobras de folhas e frutos, partes velhas do fungo e corpos.

---

[← 20 — Depósito habilidades e balanceamento](../01_Design/20_Deposito_habilidades_e_balanceamento.md) · [22 — Mapa da árvore da rainha →](../01_Design/22_Mapa_da_arvore_da_rainha.md)
