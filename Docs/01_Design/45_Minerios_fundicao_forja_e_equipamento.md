# 45 — Minérios, fundição, forja e equipamento

Documento do projeto, 2026-10-23 (REQ-071). Não faz parte do documento revisado de 2026-10-17. Mundo horizontal: [documento 44](../02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md).

[Índice da documentação](../README.md)

> **Estado das definições.** Decidido pelo desenvolvedor em 2026-10-23:
> - o mundo pequeno segue o Terraria, mas sem oceano, masmorra e bioma do mal;
> - os biomas laterais são o canteiro de flores, a serapilheira, o areal (deserto) e a fruteira (selva);
> - o corte vertical acrescenta minérios em veios, a fundição, a forja e dois armazéns: ferramentas e armas, roupas e armaduras;
> - os minérios são três: cobre, ferro e ouro;
> - a formiga equipa o item do papel dela;
> - a fundição queima serapilheira.
>
> Os números e as regras marcadas como **suposição** são do agente e ainda precisam ser aprovados.

## Mundo pequeno, na horizontal

O Terraria pequeno tem 4200 colunas, e o nosso mundo tem 1920. As larguras abaixo foram reduzidas na mesma proporção, aproximadamente. A ordem das faixas, a partir da entrada, é:

| Faixa | Bioma | Papel no Terraria | Largura (células) | Estado |
|---|---|---|---|---|
| Ponta esquerda | Areal | Deserto | até a borda (~200) | Novo |
| Esquerda | Canteiro de flores | — | 360 | Existe |
| Centro | Gramado | Floresta do spawn | ~480 em volta da entrada | Existe |
| Direita | Serapilheira | — | 360 | Existe |
| Ponta direita | Fruteira | Selva | até a borda (~200) | Novo |

- **Areal (suposição):** o chão é de areia, e embaixo dele há mais areia, até ~40 células de profundidade, como o deserto subterrâneo. O insumo é a areia, que já vai ao depósito de areia. Não há plantas.
- **Fruteira (suposição):** o chão é de terra adubada, com plantas folhosas mais densas. O insumo será o **fruto caído** da seção 31, que rende 2× no cultivo. Ele fica para uma etapa seguinte; nesta, a fruteira é só o mundo.
- Ponta **sorteada** pela seed, como o Terraria faz com a selva e a neve: o areal e a fruteira podem trocar de lado, e o canteiro e a serapilheira trocam junto.

## Minérios em veios, na vertical

| Minério | Faixa principal (células abaixo do solo) | Bolsões por 1000 colunas | Raio do bolsão | Passos | Resto do mapa (bolsões por 1000 colunas, abaixo da faixa) | Tempo para cavar |
|---|---|---|---|---|---|---|
| Cobre | 12 a 140 | 40 | 3 a 5,5 | 6 a 14 | 8 | 0,5 s |
| Ferro | 60 a 300 | 30 | 3 a 5,5 | 6 a 14 | 8 | 0,8 s |
| Ouro | 180 até o fundo, mais denso perto do fundo | 40 | 2,5 a 4,5 | 5 a 12 | — | 1,1 s |

(Tudo na tabela é **suposição**.)

- **Veio:** um bolsão irregular, como os do Terraria (pedido do desenvolvedor em 2026-10-23: "Deixe os veios mais grossos, em bolsões como no Terraria, e ponha mais ouro no fundo. Não exclua completamente os outros veios no resto do mapa"). Cada bolsão é uma caminhada curta que carimba discos de borda roída, afinando até a metade no fim; sai com uns 10 a 15 células de largura. A trilha fina de 2 células da primeira versão saiu.
- **Faixas:** cada metal se concentra na faixa principal. O cobre e o ferro também aparecem, em bolsões menores (3/4 do raio), do fim da faixa até o fundo do mundo. O ouro fica mais denso quanto mais fundo.
- O veio substitui terra e areia, mas não substitui pedra, água nem o ninho inicial.
- Os minérios são escaváveis com o pincel Cavar (não exigem E2) e são fixos (não caem).
- **Minério cavado:** vai ao **depósito de minério**, se houver; senão, à fundição. Sem nenhum dos dois, fica no chão como item solto, como o cogumelo. Nunca vai ao despejo.
- **Minério solto cai** (pedido do desenvolvedor em 2026-10-23: "adicionar física quando não estiverem no depósito ou em uso"): fora do depósito de minério e da fundição, desce até apoiar e escorrega de cima de outro item, fazendo monte. No depósito e na fundição fica onde foi posto.

## Cadeia: minério → barra → equipamento

### Fundição (sala pintada)

- Cada célula da sala guarda um item, como num depósito.
- As formigas trazem minério e serapilheira para ela.
- **1 minério + 1 serapilheira viram 1 barra** do mesmo metal, em 20 s de jogo (**suposição**).
- **Mudança do agente na implementação (a aprovar):** a transformação corre sozinha, com o tempo, assim que a entrada está na sala. As formigas fazem só a logística. Isso evita um papel novo de ferreira e segue o funcionamento das salas que já existem.
- A barra fica na célula até ser levada à forja.

### Forja (sala pintada)

- A forja faz **1 equipamento com 2 barras**, em 30 s (**suposição**).
- Ela escolhe o que fazer pela falta nos armazéns. Repõe primeiro o tipo com menos estoque em relação às formigas que o usariam, e usa o melhor metal disponível.

### Armazéns (depósitos)

- **Ferramentas e armas:** guarda ferramentas e armas.
- **Roupas e armaduras:** guarda as armaduras.

### Quem equipa (suposição)

| Item | Quem usa | Efeito (cobre / ferro / ouro) | Durabilidade |
|---|---|---|---|
| Ferramenta | Escavadora e operária comum | Tempo de cavar ×0,8 / ×0,65 / ×0,5 | 200 / 400 / 800 células cavadas |
| Arma | Soldado e cuspideira | Dano ×1,3 / ×1,6 / ×2,0 | 40 / 80 / 160 golpes |
| Armadura | Soldado e cuspideira | Dano recebido ×0,8 / ×0,65 / ×0,5 | 30 / 60 / 120 golpes recebidos |

- **Equipar:** a formiga sem o item do papel dela, e ociosa, busca no armazém o melhor que houver e o leva consigo. Cada formiga carrega no máximo um item de cada tipo.
- **Desgaste:** quando o item gasta, ele **volta à forja** como sucata. A formiga solta 1 barra do metal no chão, e essa barra é levada à forja.
- **Formiga que morre:** deixa o equipamento no chão, como item solto.

### Fundição × serapilheira

A fundição é o primeiro uso da serapilheira. A decisão de 2026-10-23 adiava o uso do insumo, e esta cadeia o resolve em parte. Nas melhorias, a serapilheira continua sem uso.

## Implementação

Em 2026-10-23 foram implementadas as três etapas, com os números deste documento e uma mudança: a transformação é automática. Como funciona: [WorkshopGuide.md](../../Assets/Project/Scripts/Runtime/Colony/WorkshopGuide.md) e [BiomesGuide.md](../../Assets/Project/Scripts/Runtime/World/BiomesGuide.md). Arte provisória.

## Etapas de implementação

1. Disposição horizontal (areal e fruteira) e minérios em veios, com o depósito de minério.
2. Fundição: serapilheira como combustível, barras.
3. Forja, os dois armazéns e o equipamento das formigas.

## Lista de arte

- **Atlas:** uma linha para cada minério na terra (pedra com pintas do metal). Linhas provisórias para a barra e para os três equipamentos de cada metal, que aparecem no bloco carregado e no item guardado.
- **Fundição, forja e armazéns de equipamento:** bloco e fundo de sala construída, feitos em 2026-10-23 (famílias Smelter, Forge, Armory e Wardrobe no atlas; maquete em [43_Salas_construidas.png](../02_World_Art_UI/43_Salas_construidas.png)). Fundição: tijolo refratário e brasa nas frestas. Forja: pedra escura com rebites e o brilho do fogo. Armas: tábuas com cabideiro. Armaduras: painéis de madeira clara e tecido azul.
- **Item guardado:** desde 2026-10-23, desenhado com a arte da linha do atlas como um objeto de 6 × 6 texels com contorno, sobre a parede de fundo (antes, a célula ficava tingida com a cor chapada).
- **Minério solto** (pedido do desenvolvedor em 2026-10-23: "a arte dos minérios soltos, igual fizemos com a água"): sprite 8 × 8 próprio por metal, torrão de pedra com pepitas (`Art/Ants/Item_CopperOre`, `Item_IronOre`, `Item_GoldOre`), o mesmo na boca da formiga e na grade, como a gota de água.
- **Ícones da HUD:** ferramenta, arma, armadura e bigorna.
- **Formiga equipada:** sobreposição no sprite (capacete, arma), numa etapa posterior.

## Em aberto

- Os números marcados como suposição.
- O fruto da fruteira.
- Aprovar os custos e efeitos dos nós de minérios e forja da árvore (documento 46; os pincéis viraram câmaras pagas em 2026-10-23).
- O que acontece com o excesso de barras.
- Se as roupas, diferentes da armadura, terão uso próprio para as operárias (proteção contra a chuva, por exemplo).

---

[← 44 — Biomas da superfície e postos avançados](../02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md)
