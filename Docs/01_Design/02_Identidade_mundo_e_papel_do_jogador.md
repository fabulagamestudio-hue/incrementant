# 02 — Identidade mundo e papel do jogador

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 02. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Papel do jogador

O jogador desenha a expansão com pincéis, desbloqueia funções, melhora capacidades e orienta a reprodução. Ele não precisa mover cada formiga, entregar folhas manualmente ou clicar repetidamente para produzir. A participação se concentra no planejamento e no investimento, com acompanhamento visual do resultado.

O cenário é um corte lateral: a superfície tem jardim, gramíneas, plantas baixas, flores, frutos e visitantes; abaixo dela está o solo escavável. A composição inicial pode dedicar aproximadamente metade da vista à superfície e metade ao subsolo. Árvores grandes ficam fora da direção visual inicial.

## Base da experiência

Formigas funcionais que escavam pixel a pixel e transportam material para fora do formigueiro.

Pincel para marcar regiões de escavação e possibilidade de criar novas formigas no protótipo.

Terra firme, areia que escorre ao perder apoio e pedra que bloqueia a escavação inicial.

Desmoronamentos, acúmulo de terra na superfície e jardim acima do formigueiro.

Chuva fraca que reduz pilhas e chuva forte que também forma crosta rígida nas bordas.

## Organização do gameplay

Câmaras também usam pincéis por função. Postura e grupo inicial vêm da cinemática; cultivo e berçário estruturam a entrada no jogo. Recursos passam pelo cultivo antes de virar nutrientes. A rainha reproduz automaticamente após o berçário. Árvores de formigueiro e rainha têm responsabilidades separadas.

A experiência combina autonomia das formigas com escolhas periódicas do jogador. O jardim e o subsolo devem permanecer legíveis como partes do mesmo mundo. Uma expansão altera o espaço, acrescenta trabalho e abre possibilidades de produção e população.

## Referência natural e adaptação

Saúva é o nome popular do gênero Atta. A principal referência de pesquisa é Atta sexdens. Tanajura ou içá identifica a fêmea reprodutiva das saúvas; não é o nome de toda rainha de qualquer espécie. A fundadora já chega fecundada. Operárias não se transformam em rainha. Soldados são operárias especializadas; as classes do jogo representam tarefas e capacidades, não espécies distintas.

## Promessa da experiência

O interesse vem de acompanhar transformações compreensíveis. Uma compra deve revelar uma atividade, tornar uma carga diferente, mudar a organização do fungo ou abrir uma região antes inacessível. A progressão combina escolhas de eficiência com acontecimentos que alteram a rotina.

A natureza inspira a colônia, mas não define automaticamente todo comportamento. A separação de postura, berçário e cultivo é uma decisão de gestão. As especialistas são papéis lúdicos da mesma colônia. Água é perigo físico; o projeto atual não acrescenta uma necessidade separada de beber água ou um recurso de hidratação.

---

[← 01 — Apresentação e uso do documento](../01_Design/01_Apresentacao_e_uso_do_documento.md) · [03 — Pintura de ambientes e custo de expansão →](../01_Design/03_Pintura_de_ambientes_e_custo_de_expansao.md)
