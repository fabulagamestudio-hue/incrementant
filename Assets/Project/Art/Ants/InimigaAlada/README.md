# Formiga inimiga alada

Arte de 2026-10-18 (REQ-027). Mesma linguagem de pixel art das formigas da colônia, com carapaça carvão/índigo, detalhes violáceos, olhos âmbar e asas claras. A mesma formiga chega voando, pousa, perde as asas e continua andando.

| Arquivos | Quadros | Uso |
|---|---|---|
| `Ant_Fly_0.png` a `Ant_Fly_3.png` | 4 | Chegada em voo; fases do bater das asas |
| `Ant_Land_0.png` a `Ant_Land_3.png` | 4 | Patas tocam o chão, asas se recolhem, se desprendem e desaparecem |
| `Ant_Walk_0.png` a `Ant_Walk_3.png` | 4 | Caminhada lateral após perder as asas |
| `Ant_Top_0.png` a `Ant_Top_3.png` | 4 | Caminhada sem asas vista de cima, para o fundo do ambiente |

Os 16 PNGs têm canvas comum de 144x144 px, 36 pixels por unidade, com espaço para as asas e o corpo na escala aproximada das demais formigas. Âncora lateral: pivô normalizado (0,5; 0,25); vista superior: (0,5; 0,5). Não reenquadrar cada quadro separadamente: o canvas comum preserva a posição do corpo entre os estados. A arte não precisa crescer para caberem as asas.

`AntSpriteImporter` reconhece Walk, Top, Fly e Land. Metadados preparados com filtro Point, sem mipmaps nem compressão, e GUIDs/spriteIDs novos. Paleta compartilhada de 24 cores; alpha exclusivamente 0/255, sem franjas semitransparentes. As asas são representadas por cores claras, sem alpha parcial.

Criado com `image_gen.imagegen` integrado a partir de uma prancha das formigas existentes como referência de estilo e alinhamento. Os recortes foram amostrados sem suavização e preparados na resolução final; [prompt e preparação](Generation.json).

Validação: 16 arquivos e 331.776 pixels conferidos (144x144, conteúdo não vazio, até 24 cores e alpha binário); prévias dos quatro estados inspecionadas. Importação/compilação no Editor de Incrementant e reprodução em Play Mode pendentes: apenas outra instância de projeto estava conectada ao MCP. Nenhuma build gerada.

Em uso desde 2026-10-18 como os **cupins** dos ataques (REQ-029): Fly na chegada, Land tocado uma vez no pouso, depois Walk/Top sem asas; o corpo morto usa Top virado. Ligação na cena e comportamento: [RaidGuide](../../../Scripts/Runtime/Colony/RaidGuide.md).
