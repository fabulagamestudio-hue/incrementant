# 44 — Biomas da superfície e postos avançados

Documento do projeto, 2026-10-23 (REQ-070). Não faz parte do documento revisado de 2026-10-17.

[Índice da documentação](../README.md)

> **Estado das definições.** A direção foi decidida pelo desenvolvedor em 2026-10-23: "Fica a opção A (biomas laterais com insumos únicos e postos avançados)". A lista de biomas, os insumos, os usos e a lista de arte são propostas do agente feitas na mesma conversa. Eles servem de ponto de partida e ainda precisam ser aprovados item a item. Nada aqui está implementado.

## Implementação

- **2026-10-23 — primeiro bioma:** a Serapilheira, à direita do formigueiro, no escopo "mundo + coletável" (escolhas do desenvolvedor). Faixa do bioma na geração, chão coberto de serapilheira (material novo, insumo único), depósito de serapilheira, folhas que voltam a cobrir o chão, cenário só com gravetos e folhas secas nela, plantas colhíveis só no gramado, e pedras, cogumelos e grãos fora da geração. Arte do chão provisória. Como funciona: [BiomesGuide.md](../../Assets/Project/Scripts/Runtime/World/BiomesGuide.md). Ainda não: uso do insumo, camada rasa de húmus, fundo e transbordo próprios, entrada secundária.
- **2026-10-23 — segundo bioma:** o Canteiro de flores, à esquerda do formigueiro, no mesmo escopo (escolhas do desenvolvedor: néctar em gotas que caem; flores só no canteiro). Chão de terra adubada, flores (tipo de planta próprio, faixas mais estreitas que as do gramado), gotas de néctar ao pé das flores inteiras e depósito de néctar. O gramado ficou só com grama e trevos. Silhueta do canteiro (vaso ou borda) e fundo próprio ainda não.

## Problema

O jogador já consegue montar salas internas para todos os recursos: canteiro, fazenda de pulgões, depósitos e a água da chuva. Com isso, a superfície só importa no começo da partida. Faltava uma razão permanente para trabalhar do lado de fora, e essa definição é necessária para saber que arte produzir.

## Decisão

**O interior produz volume; o exterior fornece o que não se fabrica.**

- **Interior:** a economia de base continua fechada e funcionando, mas com um teto. As salas rendem quantidade.
- **Exterior:** cada bioma tem um insumo que só existe nele. Esse insumo libera um ramo de melhorias, um tipo de sala ou um multiplicador. É o insumo que muda o nível da colônia, não o volume.
- **Distância:** quanto mais longe do formigueiro, melhor o insumo e mais longa a viagem.
- **Postos avançados:** para encurtar a viagem, o jogador cava por baixo da terra até perto do bioma e abre uma **entrada secundária**. A colônia passa a crescer para os lados, em direção aos recursos, e não só para baixo.
- **Eventos da seção 31** (folha caída, fruto, visitantes, chuva) acontecem dentro dos biomas e não substituem esta estrutura.
- **Território (cupins vindos de ninhos visíveis):** fica para depois, como o bioma da borda.

## Como o mundo se monta (modelo de Terraria)

O mundo tem duas leituras.

### Horizontal: biomas em faixas

O formigueiro inicial fica no centro, no gramado. Os biomas se sucedem para a esquerda e para a direita, cada um ocupando uma faixa de colunas. Entre dois biomas há uma faixa de transição, onde o bloco de topo e as plantas se misturam. Cada bioma muda quatro coisas:

1. o bloco do topo;
2. as plantas e os coletáveis;
3. o fundo (parallax);
4. a camada logo abaixo do chão, nas primeiras células de profundidade.

### Vertical: camadas iguais em todo o mapa

| Camada | Conteúdo |
|---|---|
| Superfície | topo do bioma, plantas, raízes rasas |
| Solo | terra (com a variação rasa do bioma logo abaixo do topo) |
| Subsolo | argila |
| Fundo | pedra |
| Base | lençol d'água e rocha-mãe |

As profundidades de cada camada ainda não foram definidas. A geração atual do terreno (terra, areia, pedra, rocha-mãe) é o ponto de partida.

## Biomas propostos

Proposta do agente, a aprovar. A distância é contada a partir do formigueiro inicial.

| Bioma | Distância | Insumo único | Uso proposto |
|---|---|---|---|
| Gramado | junto do formigueiro | grama e folha (já existentes) | economia de base, a mesma de hoje |
| Canteiro de flores | perto | néctar | rainha e formigas especiais |
| Fruteira / horta | média | fruto caído | rende 2× no cultivo (regra da seção 31) |
| Serapilheira / tronco caído | média | graveto e folha seca | estrutura: salas maiores, pontes, entrada secundária |
| Margem úmida / poça | longe | argila e água limpa | reboco forte, que resiste à chuva e aos cupins |
| Borda: ninho de cupins | extremo | origem dos ataques | combate (fica para depois) |

## Cenário na geração do mapa

Decidido pelo desenvolvedor em 2026-10-23: "elementos grandes pertencem a um bioma, gramado central limpo, decoração não imita insumo".

1. **Todo elemento grande pertence a um bioma** e só aparece nele. Ele é a silhueta que identifica o bioma. Distribuição proposta pelo agente, a aprovar: pedras gigantes na margem úmida (ou num areal, se entrar), gravetos e folhas secas na serapilheira, cogumelos na margem úmida, flores grandes no canteiro de flores.
2. **O gramado central fica limpo:** grama, trevo e no máximo uma silhueta. É onde o jogo começa, e as trilhas iniciais não devem esbarrar em obstáculos.
3. **A decoração não imita um insumo.** Um elemento que se parece com o recurso de um bioma vira coletável nesse bioma ou sai do cenário.

Consequências, a confirmar: elementos sem bioma (hoje, por exemplo, os grãos) saem da geração; os gigantes refeitos por peças (REQ-069) se limitam aos que têm bioma, começando pelos da demo. Como o cenário espalhado também escondia a repetição da superfície, o gramado precisa de variação no próprio topo (altura da grama, trevos, pequenas ondulações).

## Em aberto

- Os números: rendimento, reserva e recuperação de cada fonte, distâncias em células e custo da entrada secundária.
- Quais biomas ficam de cada lado e quais entram na demo. A sugestão do agente é o gramado mais um bioma de cada lado: flores à esquerda e serapilheira à direita.
- O que o ninho de cupins rende. O design revisado diz que bichos mortos e cogumelos não alimentam o cultivo ([31](31_Jardim_frutas_folhas_e_visitantes.md)), então a recompensa precisa ser outra coisa.
- **Uso da serapilheira (adiado pelo desenvolvedor em 2026-10-23, "vamos deixar registrado para uma decisão futura"):** hoje ela só se acumula no depósito. Proposta do agente, não aprovada: material de estrutura (salas maiores, pontes, entrada secundária), pago nas melhorias.
- Onde os novos insumos entram nas árvores de melhoria ([39](../01_Design/39_Melhorias_incrementais_e_formigueiro_de_dependencias.md)).
- Se a entrada secundária é uma sala pintada, uma construção nova ou só uma segunda boca cavada até a superfície.
- Quais elementos do cenário atual saem da geração e para que bioma vai cada um dos que ficam.
- Se a grama atual, que hoje cobre toda a superfície, fica restrita ao gramado central.

## Lista de arte

Segue a direção de arte em vigor ([40](40_Direcao_de_arte_unificada_e_terreno_iluminado.md)): paleta mestra, contorno preto e a densidade de texels por célula que for adotada (16 px aprovado; 8 px em experimento, REQ-068). Elementos grandes são montados por peças, como o kit do graveto (REQ-069), e não desenhados como uma imagem só.

### Por bioma

Cada bioma precisa de:

| Peça | Conteúdo | Observação |
|---|---|---|
| Bloco de topo | tile do material da superfície e transbordo do topo, como a grama | uma linha nova no atlas do terreno |
| Transição | variação do topo para o bioma vizinho | pode vir de máscara entre os dois topos |
| Camada rasa | tile do material logo abaixo do topo | uma linha no atlas; pode repetir o material de outro bioma |
| Coletáveis | 2 a 3 plantas ou fontes, com a máscara de crescimento e colheita | o insumo único precisa ser reconhecível de longe |
| Silhueta grande | 1 elemento montado por peças (tronco, pedra, vaso, cerca) | marca o bioma na tela e serve de ponto de referência |
| Fundo | 1 camada de parallax própria, nas faixas média e distante | reaproveita a estrutura das faixas atuais |
| Item carregado | o insumo na mandíbula da formiga e no depósito | no tamanho do bloco carregado |

### Por bioma proposto

| Bioma | Topo | Camada rasa | Coletáveis | Silhueta | Item |
|---|---|---|---|---|---|
| Gramado | grama (existente) | terra (existente) | grama, trevo, flor (existentes) | graveto (kit existente) | folha (existente) |
| Canteiro de flores | terra adubada | terra escura | 2 a 3 flores com néctar | vaso ou borda de canteiro | gota de néctar |
| Fruteira / horta | terra com palha | terra | fruto caído, folha larga | pé de planta ou estaca | pedaço de fruto |
| Serapilheira / tronco | folhas secas | húmus | graveto pequeno, folha seca | tronco caído | graveto, folha seca |
| Margem úmida / poça | lama | argila | musgo, junco | pedra molhada | bolinha de argila, gota d'água limpa |
| Ninho de cupins | terra batida | terra de cupinzeiro | — | cupinzeiro | — |

### Por camada (todo o mapa)

| Peça | Situação |
|---|---|
| Terra, areia, pedra, rocha-mãe | existentes no atlas |
| Argila (subsolo) | nova linha no atlas |
| Lençol d'água | depende das regras de água ([30](30_Chuva_agua_e_perigos_ambientais.md)); arte nova se for um material próprio |
| Raízes rasas | elemento de cenário dentro da terra, opcional |

### Postos avançados

| Peça | Conteúdo |
|---|---|
| Entrada secundária | boca na superfície, com o mesmo tratamento da entrada principal |
| Marcador no mapa | ícone para a entrada e para o bioma na interface |

### Ordem sugerida de produção

1. Gramado ajustado à transição (já existe quase todo).
2. Os dois biomas da demo: topo, camada rasa, coletáveis, item e silhueta.
3. Entrada secundária.
4. Fundos dos dois biomas.
5. Argila no subsolo.
6. Demais biomas.

---

[← 40 — Direção de arte unificada e terreno iluminado](40_Direcao_de_arte_unificada_e_terreno_iluminado.md)
