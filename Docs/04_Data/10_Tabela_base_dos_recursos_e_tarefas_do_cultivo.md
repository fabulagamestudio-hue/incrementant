# 10 — Tabela base dos recursos e tarefas do cultivo

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 10. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Grandeza | Valor proposto | Aplicação |
| --- | --- | --- |
| Rendimento da folha | 3 N por UM processada | Após conversão em fungo e colheita |
| Rendimento do fruto | 6 N por UM processada | Dobro definido; bônus de C3 mantém a relação |
| Cultivo base | 0,25 UM/s por B funcional | 4 B processam no máximo 1 UM/s |
| Colheita comum | 2 UF por visita de 1 s | Pode atender 4 B com deslocamento local |
| Colheita jardineira | 3 UF por visita de 1 s | Atende 6 B; J1 altera crescimento, não rendimento |
| Estoque bruto no cultivo | 6 UM por B | Depósito acrescenta sua própria capacidade |
| Fungo processável | 4 UF por B | No limite, crescimento adicional aguarda colheita |
| Folha crua | Deteriora em 600 s | 1 UM perdida gera 1 UR |
| Fruto cru | Deteriora em 360 s | 1 UM perdida gera 1 UR |
| Material preparado | Deteriora em 240 s | Preserva idade proporcional ao transferir e preparar |
| Resíduo normal do cultivo | 0,15 UR por UM consumida | Surge independentemente da deterioração |
| Corpo de adulta | 1 UR equivalente para ocupação | Visual de corpo preservado durante transporte |
| Capacidade do descarte | 12 UR por B | Cada UR se decompõe em 120 s; R1 reduz para 60 s |

Deteriorar em determinado tempo significa perder massa gradualmente ao longo desse intervalo, conforme a idade do lote. Proposta de cálculo: cada lote retém a fração máxima entre zero e 1 − idade/vida útil. A quantidade restante conserva sua idade e origem quando uma parte é consumida. A perda deve ocorrer uma única vez ao longo do envelhecimento, em vez de reaparecer como outra penalidade a cada transporte.

O efeito exato da decomposição visual usa partes menores sumindo ao longo do tempo. Todas as taxas são propostas iniciais. Não atribuir temperatura, inverno ou ciclo de dia e noite a essa primeira fórmula, pois essas mecânicas não foram aprovadas para a versão atual.

## Idade do material e interrupção de produção

Mover uma carga, guardar no depósito ou triturar uma folha não faz o alimento voltar a ser novo. A proposta de retirada prioriza o material mais antigo disponível para reduzir desperdício. O estoque pode ter partes novas e antigas ao mesmo tempo.

Sem matéria prima utilizável, o cultivo para de gerar novo fungo. Com fungo nutritivo acumulado, a colheita ainda pode gerar N até essa reserva acabar. Sem atendimento ou sem acesso, a atividade aguarda. Retomar a operação exige voltar a ter a condição faltante; não exige recomprar o cultivo.

Os tempos de deterioração e a taxa de resíduos são propostas numéricas. O fato definido é que folhas e frutos são perecíveis e suas sobras ocupam espaço, produzindo uma tarefa de retirada. A moeda já gerada não apodrece.

---

[← 09 — Recursos fungo e perecibilidade](../01_Design/09_Recursos_fungo_e_perecibilidade.md) · [11 — Reprodução e política da rainha →](../01_Design/11_Reproducao_e_politica_da_rainha.md)
