# 01 — Apresentação e uso do documento

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 01. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

Increment Ant

Game design completo e primeira proposta de balanceamento

Fábula Game Studio • Game jam de três dias • Consolidação de 17 de outubro de 2026

Increment Ant é um jogo incremental de gestão de uma colônia de saúvas, em 2D e pixel art. O jogador pinta túneis e ambientes, acompanha formigas escavando e transportando terra, orienta o nascimento de especialistas e investe nutrientes no desenvolvimento do formigueiro. O trabalho acontece automaticamente e as escolhas mudam aquilo que se vê na colônia.

A experiência procura despertar a vontade de acompanhar a próxima conquista: mais uma formiga nascendo, mais um cultivo ocupado, uma rocha sendo atravessada ou uma nova sala entrando em funcionamento. O tom é aconchegante, com música tranquila, sons de atividade e respostas visuais abundantes.

Este documento descreve o jogo completo, seus comportamentos, escolhas, consequências e exceções. A equipe fará um recorte para a jam, sem confundir a demonstração com o limite do projeto. Cada assunto reúne suas regras atuais, suas limitações e as decisões de design ainda necessárias, para permitir leitura autossuficiente por pessoas ou por uma IA de desenvolvimento.

## Resultado esperado para a equipe

Os fluxos descrevem a sequência das ações e seus resultados. As tabelas distinguem valores base e adicionais. Os códigos das habilidades servem como referência de design entre a árvore, a descrição da compra e seus efeitos. O documento não escolhe linguagem, motor, arquitetura de software ou forma de programar o jogo.

## Natureza das definições

Uma regra definida descreve a experiência aprovada. Um valor proposto de balanceamento é uma primeira hipótese de custo, duração, capacidade ou intensidade. Uma proposta de comportamento detalha uma consequência ainda não fechada na conversa. Uma decisão em aberto permanece próxima da mecânica, sem virar automaticamente uma funcionalidade obrigatória.

Quando uma condição impede uma ação, a descrição deve permitir compreender o motivo: saldo, falta de espaço, falta de acesso ou classe indisponível. A IA que usar este documento recebe um contrato de experiência e de relações entre sistemas, sem uma solução de programação pré-escolhida.

Os números de todas as tabelas continuam como primeira proposta. Mudanças de balanceamento podem alterar esses números sem mudar o significado da habilidade. Mudar quem executa uma tarefa, tornar gratuita uma pintura ou acrescentar uma moeda diferente já altera a regra de design e exige uma decisão explícita.

## Guia de leitura

Os números abaixo identificam seções, não páginas. Os títulos do painel de navegação do Word permitem consultar cada assunto. Regras e situações especiais aparecem junto da mecânica a que pertencem. As tabelas de cada ramo apresentam custos, pré-requisitos, base, resultado após a compra e efeito visível.

Visão e regras de pintura  —  Seções 02 a 05

Passo a passo inicial e loop contínuo  —  Seções 06 a 08

Recursos fungo perecibilidade e resíduos  —  Seções 09 e 10

Reprodução vida capacidade e recuperação  —  Seções 11 a 13

Classes valores base e tarefas  —  Seções 14 a 16

Árvore do formigueiro e todos os seus níveis  —  Seções 17 a 21

Árvore da rainha e todas as especializações  —  Seções 22 a 27

Custos totais e verificação do balanceamento  —  Seções 28 e 29

Chuva jardim frutos e invasoras  —  Seções 30 a 32

Menus interface áudio e juice  —  Seções 33 a 35

Visão completa e recorte sugerido da jam  —  Seções 36

Continuidade pausa e derrota  —  Seção 37

Referências da ambientação  —  Seção 38

### Como interpretar a primeira proposta numérica

Todos os tempos usam segundos e todas as compras usam nutrientes. Uma habilidade com custo zero indica estado inicial ou desbloqueio gratuito, sem tornar gratuita a pintura da área. As quantidades de alimento, fungo e resíduo são estoques internos, não moedas adicionais.

A tabela da habilidade descreve sua mudança de base para resultado. O texto explica a função, a representação percebida pelo jogador e a relação com outros sistemas. Números propostos são um ponto inicial para testar ritmo, dificuldade e prazer de acompanhar a colônia.

### Vocabulário de design

Ambiente ou câmara: área com função determinada pelo pincel usado, qualquer que seja seu formato. Túnel: área pintada com o pincel genérico. Acesso: caminho físico que permite a uma formiga alcançar a frente de escavação, um item ou um destino.

Área marcada: espaço pago e com função permanente, que pode ainda conter solo. Área escavada: espaço onde material foi retirado. Área funcional: porção escavada e alcançável que consegue exercer sua função. Área útil populacional: espaço interno disponível que contribui para o limite de adultas. Essas quatro situações não são equivalentes.

Operária produtiva: comum ou especialista de trabalho capaz de participar da manutenção econômica. Soldado é defensora, não produtiva para a regra de recuperação. Cria: ovo, larva ou pupa antes da adulta. Vaga reservada: lugar futuro comprometido quando a rainha inicia uma cria.

Nutrientes ou N: única moeda. UM: quantidade de folha ou fruto. UF: fungo processável que dá origem aos nutrientes. UR: quantidade de resíduo que ocupa espaço até o descarte. B: unidade contábil de área equivalente a 256 células de terreno, usada apenas para comparar preços e capacidades.

Reserva de matéria prima: folhas e frutos guardados. Reserva preparada: alimento já triturado. Reserva mínima de nutrientes: saldo que a reprodução normal não pode consumir. A alimentação de uma cria já iniciada está garantida, independentemente da reserva mínima futura.

---

[02 — Identidade mundo e papel do jogador →](../01_Design/02_Identidade_mundo_e_papel_do_jogador.md)
