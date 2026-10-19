# Arte — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** ART
**Última avaliação:** 2026-10-18

## Resumo

Só há arte provisória gerada por código para o protótipo: as formigas (pixel art 18x12, 4 quadros de caminhada de lado e 4 vista de cima), o cenário da superfície na escala delas e as cores do terreno. A direção de arte não foi definida; o desenvolvedor aprovou que a formiga tenha pixels menores que os do terreno (2026-10-17).

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| ART-001 | Sprites provisórios das formigas (caminhada, 4 quadros) | IMPLEMENTED | UNDEFINED | [Ant_Walk_0.png](../Assets/Project/Art/Ants/Ant_Walk_0.png) | PRG-006 | 2026-10-17 |
| ART-002 | Sprites provisórios da formiga vista de cima (4 quadros) | IMPLEMENTED | UNDEFINED | [Ant_Top_0.png](../Assets/Project/Art/Ants/Ant_Top_0.png) | PRG-008 | 2026-10-17 |
| ART-003 | Cenário provisório da superfície na escala das formigas | IMPLEMENTED | UNDEFINED | [Art/Scenery/](../Assets/Project/Art/Scenery/) (67 PNGs isolados), [SurfaceSceneryPainter.cs](../Assets/Project/Scripts/Runtime/AntFarm/SurfaceSceneryPainter.cs) | REQ-002, PRG-009 | 2026-10-17 |
| ART-004 | Sprites de UI 9-slice (painéis, botões, controles, ícones) | IMPLEMENTED | UNDEFINED | [Art/UI/Sprites/](../Assets/Project/Art/UI/Sprites/), [UiSpriteImporter.cs](../Assets/Project/Scripts/Editor/UI/UiSpriteImporter.cs) | REQ-010 | 2026-10-17 |
| ART-005 | Sprites provisórios da cria: ovo, larva e pupa (2 quadros de idle cada) | IMPLEMENTED | UNDEFINED | [Larva_Idle_0.png](../Assets/Project/Art/Ants/Larva_Idle_0.png), [Pupa_Idle_0.png](../Assets/Project/Art/Ants/Pupa_Idle_0.png), [Egg_Idle_0.png](../Assets/Project/Art/Ants/Egg_Idle_0.png) | REQ-014, PRG-021 | 2026-10-17 |
| ART-006 | Variantes de cor por função (Top e Walk) | IMPLEMENTED | UNDEFINED | [Paleta e validação dos 48 PNGs](../Assets/Project/Art/Ants/README.md) | REQ-019 | — |
| ART-007 | Efeitos visuais (VFX) do jogo | PLANNED | UNDEFINED | — | REQ-023, PRG-031 | — |
| ART-008 | Texturas 16x16 por material do terreno e do bloco que a formiga carrega (provisórias, geradas por código; branch `experimento/pixel-16x16`) | IMPLEMENTED | UNDEFINED | [CellTileArt.cs](../Assets/Project/Scripts/Runtime/AntFarm/CellTileArt.cs), [AntSwarmView.cs](../Assets/Project/Scripts/Runtime/AntFarm/AntSwarmView.cs) `BlockSprite`; captura em Play Mode pelo agente | REQ-024, PRG-032 | 2026-10-18 |
| ART-009 | Resolução 6x dos sprites adultos, mantendo tamanho no jogo | IMPLEMENTED | UNDEFINED | [Dimensões e validação](../Assets/Project/Art/Ants/README.md) | REQ-025, PRG-006 | — |
| ART-010 | Refinamento pixel art das formigas adultas e do cenário | IMPLEMENTED | UNDEFINED | [Formigas](../Assets/Project/Art/Ants/README.md); [cenário](../Assets/Project/Art/Scenery/README.md); 141 PNGs reimportados e configurações conferidas na Unity em 2026-10-18 | REQ-026, ART-009 | 2026-10-18 |
| ART-011 | Formiga inimiga alada: voo, perda das asas e caminhada (usada como cupim) | IMPLEMENTED | UNDEFINED | [16 sprites e validação](../Assets/Project/Art/Ants/InimigaAlada/README.md); ligada aos cupins na cena e vista em Play Mode pelo agente em 2026-10-18 (PRG-034) | REQ-027, REQ-029 | 2026-10-18 |

| ART-012 | Pulgão: 4 quadros laterais e 4 superiores, menor que a formiga | IMPLEMENTED | UNDEFINED | [Pacote e validação na Unity](../Assets/Project/Art/Aphids/README.md); ver detalhes ART-012 | REQ-033 | 2026-10-18 |

## Detalhes

### ART-012 — Arte do pulgão menor que a formiga

Pacote em [Art/Aphids](../Assets/Project/Art/Aphids/README.md), solicitado em REQ-033: quatro quadros laterais e quatro superiores, gerados com `image_gen.imagegen` usando a formiga atual como referência. Em 2026-10-18, validador executado na Unity confirmou 8 sprites, 27.648 pixels, 72x48, 36 PPU, pivôs, Point, sem compressão/mipmaps, até 16 cores e alpha binário. Prévia do quadro lateral inspecionada. Canvas com dois terços do tamanho das formigas. Importação gravada diretamente nos assets durante Play Mode; compilação do novo importador aguarda recarga do Editor. Aprovação visual e integração em gameplay pendentes.

### ART-001 — Sprites provisórios das formigas (caminhada, 4 quadros)

Gerados por [AntSpriteGenerator.cs](../Assets/Project/Scripts/Editor/AntFarm/AntSpriteGenerator.cs) como ponto de partida; podem ser repintados à mão mantendo 108x72 px (ampliação 6x em REQ-025), virados para a direita e com patas na linha de baixo. Formato e importação no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md). Exibidos em jogo (PRG-006); a aprovação visual cabe ao desenvolvedor.

### ART-002 — Sprites provisórios da formiga vista de cima (4 quadros)

Usados quando a formiga anda no fundo do formigueiro (PRG-008). Mesmo gerador e mesmas regras de ART-001 (108x72, virada para a direita, simétrica em relação à linha central). Aprovação visual cabe ao desenvolvedor.

### ART-003 — Cenário provisório da superfície na escala das formigas

Pedido em REQ-002. Pixel art gerada por código na densidade das formigas (6 px por célula): céu, grama distante, grama alta, margaridas, trevos, cogumelos, gravetos, folhas secas, pedrinhas e grãos, com tamanhos derivados da formiga (3 células ≈ 5 mm). Medidas e camadas no [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md). Desde 2026-10-17, a pedido do desenvolvedor, cada elemento (grama, tufo, margarida, trevo, cogumelo, graveto, folha seca, pedrinha, grão) é um PNG isolado e editável em `Assets/Project/Art/Scenery/<Elemento>/` (67 variantes exportadas da arte procedural), e a geração do cenário só posiciona esses PNGs; adicionar ou apagar variantes muda o que é sorteado. Céu e faixas de grama distante continuam pintados por código. Como editar está no guia ("Elementos isolados"). Aprovação visual cabe ao desenvolvedor.

### ART-004 — Sprites de UI 9-slice

58 sprites de UI (painéis, botões, abas, controles, barras, ícones) importados como sprite Point, com bordas 9-slice definidas pelo agente em [UiSpriteImporter.cs](../Assets/Project/Scripts/Editor/UI/UiSpriteImporter.cs); as bordas são estimadas a olho e podem precisar de ajuste. Usados no HUD da partida (PRG-017); os demais ainda sem tela. O conjunto `FormigueiroSkillTree` não foi tocado. Funcionamento em [HudGuide.md](../Assets/Project/Scripts/Runtime/UI/HudGuide.md).

### ART-005 — Sprites provisórios da cria (ovo, larva, pupa)

Pedido em REQ-014. Pixel art 18x12 em vista lateral, mesmo importador e densidade das formigas (6 px por célula, pivô a meia célula do chão), cores claras (creme para ovo e larva, bege para a pupa) com o marrom escuro das formigas nos detalhes. Dois quadros de idle por etapa: ovo balança, larva respira (1 px), pupa sobe 1 px. Desenhados por script pontual do agente (não há gerador no repositório); os PNGs são a fonte e podem ser repintados à mão mantendo o tamanho. Só vista lateral: a vista de cima dos túneis de fundo não tem versão. Aprovação visual cabe ao desenvolvedor.

### ART-006 — Variantes de cor por função

Pedido em REQ-019: seis pastas com 8 quadros cada (4 Top e 4 Walk): operária vermelha, soldado roxo escuro, coletora verde, escavadora laranja, cuidadora azul-claro e rainha dourada. Recoloração por script autorizada pelo desenvolvedor em 2026-10-18. Originais da raiz e seus GUIDs preservados; as cópias da operária são idênticas. Comparação de 10.368 pixels confirmou dimensões, alpha e paleta; prévia ampliada inspecionada pelo agente. Aguardam aprovação visual e futura integração/importação no jogo; não foram adicionadas classes ou alteradas cenas. [Organização e paleta](../Assets/Project/Art/Ants/README.md).

### ART-009 — Resolução 6x dos sprites adultos

Pedido em REQ-025, implementado em 2026-10-18: 72 PNGs adultos Top/Walk ampliados de 18x12 para 108x72, sem suavização. Importador e gerador ajustados para 36 pixels por unidade, mantendo 3x2 unidades, pivôs e GUIDs; cria e cenário preservados. Comparação com backup da sessão validou exatamente 559.872 pixels RGBA e a alteração exclusiva da densidade nos metadados. Não acrescenta detalhe artístico automático. Compilação/importação final e reprodução na Unity pendentes: refresh excedeu 60 s; tipo compilado ainda sem a constante nova; console sem erros na consulta. Nenhuma build gerada. [Detalhes](../Assets/Project/Art/Ants/README.md).

### ART-010 — Refinamento pixel art das formigas adultas e do cenário

Em 2026-10-18, a pedido do desenvolvedor, foram refinados 72 quadros adultos (base e oito variantes de cor, quatro quadros Top e quatro Walk) e 69 elementos das nove famílias de cenário. Pranchas geradas com `image_gen.imagegen` integrado a partir das artes anteriores e preparadas em resolução nativa, paleta limitada e alpha binário; prompts em [PixelArtGeneration.json](../Assets/Project/Art/PixelArtGeneration.json). Dimensões, caminhos, GUIDs, pivôs e densidades preservados. Comparação automatizada conferiu 1.390.673 pixels (alpha 0/255 e até 24 cores), dimensões e metadados. Prévias ampliadas das formigas e das nove famílias inspecionadas. Unity 6000.3.11f1: 72 adultos e 69 elementos reconhecidos sem falhas de configuração (Point, sem mipmaps/compressão; PPU 36/6, adultos 3x2 unidades; cenário legível); reimportação síncrona dos 141 PNGs concluída. Sem build. Aprovação visual pelo desenvolvedor e reprodução das animações/plantas em Play Mode pendentes; a máscara das plantas e do fundo caminhável acompanha a arte nova.

### ART-011 — Formiga inimiga alada

Arte de 2026-10-18, conforme REQ-027: 16 PNGs de 144x144, quatro por estado (Fly, Land com desprendimento das asas, Walk lateral e Top sem asas), no estilo da colônia e com paleta própria carvão/violácea. Alpha binário, até 24 cores, densidade 36 PPU e pivôs preparados. Conferidos 331.776 pixels; prévias inspecionadas. O importador passou a reconhecer Fly/Land na densidade adulta. Compilação/importação na Unity e reprodução em Play Mode pendentes: a instância Incrementant não estava conectada. Em 2026-10-18 passou a ser a arte dos cupins (REQ-029, PRG-034): Fly na chegada, Land uma vez no pouso, Walk/Top no chão e Top virado no corpo; ligada no `AntSwarmView` da cena em 2026-10-18 e vista em Play Mode pelo agente (voo, pouso e caminhada). [Arquivos, preparação e limites](../Assets/Project/Art/Ants/InimigaAlada/README.md).

## Riscos e bloqueios


- Nenhum.

## Não verificado

- Aprovação visual e importação/reprodução na Unity das variantes por função (ART-006); integração no jogo adiada a pedido do desenvolvedor.

- Aprovação visual da arte provisória pelo desenvolvedor, inclusive os sprites da cria (ART-005).
- Aprovação visual das bordas 9-slice e da fonte do HUD (fonte padrão do Unity, sem fonte pixel) pelo desenvolvedor (ART-004).
