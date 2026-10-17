# 20 — Depósito habilidades e balanceamento

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 20. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| D0  Depósito inicial<br>Exige C0 concluído | 80 | 6 UM/B no cultivo<br>Após compra: 20 UM/B no depósito |
| D1  Pilhas organizadas<br>Exige D0 | 150 | 20 UM/B<br>Após compra: 30 UM/B (+50%) |
| D2  Espaço para cargas grandes<br>Exige D1 | 260 | Cargas soltas de até 4 UM<br>Após compra: Guarda peças inteiras de até 8 UM |

D0 Depósito inicial. Reserva de folhas e frutos. Formigas guardam folhas e frutos crus. Comuns e especialistas podem fazer retiradas simultâneas desde o início.

D1 Pilhas organizadas. Montes compactos. Transforma o estoque espalhado em montes reconhecíveis e aumenta a densidade sem aumentar a área pintada.

D2 Espaço para cargas grandes. Grandes peças consumidas em porções. Peças maiores ficam inteiras no chão e operárias retiram porções. A estrutura autoriza armazenar cargas, não concede transporte cooperativo às classes.

## Funcionamento com e sem cargas grandes

Antes de D2, uma peça grande que chega pode ser dividida em porções pequenas no destino, desde que haja espaço. Depois de D2, o depósito armazena a peça inteira até 8 UM. O custo de quebra e movimentação dessa divisão deve ser medido sem criar dependência circular com a carregadora.

Folhas e frutos mantém sua idade dentro do depósito. O armazenamento não oferece conservação infinita. O jogo deve retirar material mais antigo primeiro, mostrar o estoque diminuindo e transportar lotes deteriorados ao descarte. Isso permite reservas úteis sem produzir montes eternos de folhas.

O depósito aumenta a autonomia do cultivo durante pausas de coleta e eventos. Inverno foi citado como analogia e possibilidade futura, mas não é um sistema desta versão. A circulação simultânea das operárias e os pontos de retirada não são uma melhoria comprável.

## Depósito e preparo têm propósitos diferentes

O depósito guarda matéria prima crua para quando a coleta diminui. O preparo do cultivo transforma material em porções para a etapa seguinte. O depósito não processa automaticamente folhas em nutrientes, e não impede a deterioração por apenas existir.

Pilhas organizadas aumenta densidade; espaço para cargas grandes muda a forma de armazenar uma peça inteira. Uma carregadora pode conseguir mover uma peça antes de o depósito conseguir guardá-la inteira: nesse caso, a peça pode ser dividida no destino conforme a proposta descrita.

A interação de dividir uma peça em porções precisa ser legível, para D2 ser reconhecido como organização e autonomia de estoque. Inverno permanece fora do escopo atual; a reserva serve aos períodos de interrupção que já existem no jogo.

---

[← 19 — Berçário habilidades e balanceamento](../01_Design/19_Bercario_habilidades_e_balanceamento.md) · [21 — Resíduos habilidades e balanceamento →](../01_Design/21_Residuos_habilidades_e_balanceamento.md)
