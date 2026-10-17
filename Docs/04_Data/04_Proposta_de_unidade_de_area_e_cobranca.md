# 04 — Proposta de unidade de área e cobrança

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 04. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

Para balancear, propõe-se B como bloco contábil de 256 células da simulação, equivalente a uma área de 16 por 16 células. B não é uma sala de tamanho fixo nem exige pintura retangular. O custo e a capacidade usam a quantidade real de área marcada, medida em células de terreno. A escala da arte não muda o preço de uma mesma área de jogo.

UM significa unidade de matéria prima, UF significa unidade de fungo processável e UR significa unidade de resíduo. Essas unidades internas não são moedas. A interface principal pode mostrar somente nutrientes, com estoques e fluxos nos painéis das salas.

| Pincel | Custo proposto por B | Observação |
| --- | --- | --- |
| Túnel comum | 0.25 | 2 células de largura mínima caminhável; recomendação 4 |
| Cultivo | 16 | Mínimo funcional sugerido 1 B; tutorial recomenda 4 B |
| Berçário | 20 | Mínimo funcional sugerido 1 B; tutorial recomenda 2 B |
| Depósito | 12 | Mínimo funcional sugerido 1 B |
| Área de resíduos | 10 | Mínimo funcional sugerido 1 B |
| Área de preparo | 14 | Exige C2; extensão funcional do cultivo, sem nova classe de formiga |

Preço de pintura = área nova em células ÷ 256 × custo do pincel. O preço precisa preservar a proporção da área, inclusive frações de nutrientes, e aparecer com até duas casas decimais. Um traço fino não custa um nutriente inteiro por célula. Um túnel de 10 B custa 2,5 N, enquanto um cultivo de 10 B custa 160 N.

Na interação proposta, o jogador arrasta uma prévia e vê o custo. A confirmação debita e fixa a marcação. Antes de confirmar, a prévia pode ser ajustada; depois, não existe apagar ou reembolso por troca de ideia. É uma proposta de interface para tornar a regra de pintura permanente compreensível.

Se faltar saldo, a confirmação não ocorre. Partes já confirmadas permanecem. Limitar o trecho da prévia ao orçamento ou exigir confirmação integral são alternativas de interação em aberto. A regra essencial é não permitir débito negativo nem cobrar duas vezes a mesma célula.

## Primeira pintura e reserva inicial

Proposta de mapa inicial: postura já escavada de 5 B e acesso inicial de 1 B, fornecidos pela cinemática. Essa área inicial comporta o grupo de três ou cinco adultas. Toda pintura adicional pelo jogador continua sendo paga.

Proposta: começar com 100 N, cultivo C0 desbloqueado gratuitamente e três operárias. Pintar 4 B de cultivo custa 64 N e um acesso de 2 B custa 0,5 N. Sobram 35,5 N. Toda pintura custa nutrientes: o desbloqueio inicial é gratuito, e a reserva proposta financia a primeira obra. Testar também início com cinco operárias sem mudar o restante da regra.

## Separação entre desbloqueio e área comprada

Comprar o pincel de berçário permite escolher sua função. Pintar o berçário paga pelo espaço. Escavar transforma esse espaço em lugar utilizável. Essas etapas têm consequências diferentes e devem aparecer como diferentes momentos da experiência.

Toda expansão desenhada pelo jogador custa nutrientes, inclusive túnel. Uma melhoria de densidade, como Pilhas organizadas, torna a área existente mais capaz, sem apagar sua forma. Um desbloqueio novo não concede espaço escavado pronto.

O custo de uma área soterrada já foi pago. Reabrir o mesmo espaço exige trabalho e transporte de terra, sem comprar de novo sua função. A reserva inicial de 100 N é proposta para permitir esse modelo pago no tutorial; não altera a regra geral de pintura.

---

[← 03 — Pintura de ambientes e custo de expansão](../01_Design/03_Pintura_de_ambientes_e_custo_de_expansao.md) · [05 — Terreno escavação e navegação →](../01_Design/05_Terreno_escavacao_e_navegacao.md)
