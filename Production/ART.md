# Arte — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** ART
**Última avaliação:** 2026-10-17

## Resumo

Só há arte provisória gerada por código para o protótipo: as formigas (pixel art 18x12, 4 quadros de caminhada de lado e 4 vista de cima), o cenário da superfície na escala delas e as cores do terreno. A direção de arte não foi definida; o desenvolvedor aprovou que a formiga tenha pixels menores que os do terreno (2026-10-17).

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| ART-001 | Sprites provisórios das formigas (caminhada, 4 quadros) | IMPLEMENTED | UNDEFINED | [Ant_Walk_0.png](../Assets/Project/Art/Ants/Ant_Walk_0.png) | PRG-006 | 2026-10-17 |
| ART-002 | Sprites provisórios da formiga vista de cima (4 quadros) | IMPLEMENTED | UNDEFINED | [Ant_Top_0.png](../Assets/Project/Art/Ants/Ant_Top_0.png) | PRG-008 | 2026-10-17 |
| ART-003 | Cenário provisório da superfície na escala das formigas | IMPLEMENTED | UNDEFINED | [Art/Scenery/](../Assets/Project/Art/Scenery/) (67 PNGs isolados), [SurfaceSceneryPainter.cs](../Assets/Project/Scripts/Runtime/AntFarm/SurfaceSceneryPainter.cs) | REQ-002, PRG-009 | 2026-10-17 |

## Detalhes

### ART-001 — Sprites provisórios das formigas (caminhada, 4 quadros)

Gerados por [AntSpriteGenerator.cs](../Assets/Project/Scripts/Editor/AntFarm/AntSpriteGenerator.cs) como ponto de partida; podem ser repintados à mão mantendo 18x12 px, virados para a direita e com patas na linha de baixo. Formato e importação no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md). Exibidos em jogo (PRG-006); a aprovação visual cabe ao desenvolvedor.

### ART-002 — Sprites provisórios da formiga vista de cima (4 quadros)

Usados quando a formiga anda no fundo do formigueiro (PRG-008). Mesmo gerador e mesmas regras de ART-001 (18x12, virada para a direita, simétrica em relação à linha central). Aprovação visual cabe ao desenvolvedor.

### ART-003 — Cenário provisório da superfície na escala das formigas

Pedido em REQ-002. Pixel art gerada por código na densidade das formigas (6 px por célula): céu, grama distante, grama alta, margaridas, trevos, cogumelos, gravetos, folhas secas, pedrinhas e grãos, com tamanhos derivados da formiga (3 células ≈ 5 mm). Medidas e camadas no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md). Desde 2026-10-17, a pedido do desenvolvedor, cada elemento (grama, tufo, margarida, trevo, cogumelo, graveto, folha seca, pedrinha, grão) é um PNG isolado e editável em `Assets/Project/Art/Scenery/<Elemento>/` (67 variantes exportadas da arte procedural), e a geração do cenário só posiciona esses PNGs; adicionar ou apagar variantes muda o que é sorteado. Céu e faixas de grama distante continuam pintados por código. Como editar está no guia ("Elementos isolados"). Aprovação visual cabe ao desenvolvedor.

## Riscos e bloqueios

- Nenhum.

## Não verificado

- Aprovação visual da arte provisória pelo desenvolvedor.
