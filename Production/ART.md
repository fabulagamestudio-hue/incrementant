# Arte — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** ART
**Última avaliação:** 2026-10-23

## Resumo

Arte do equipamento das formigas aprovada em 2026-10-23 (ART-028, REQ-071): camadas vestidas, nove itens 8 × 8 e cinco ícones de HUD em cobre, ferro e ouro, vistos em Play Mode; minérios e barras do atlas foram redesenhados em 2026-10-23, aguardando aprovação; ferramentas, armas e armaduras do atlas continuam provisórias (ART-027).

Tiles de sala construída no atlas do terreno em 2026-10-22 (ART-023, REQ-058): bloco e parede de fundo para sete famílias (os cinco depósitos dividem uma), na paleta mestra, aprovados pelo desenvolvedor na maquete. Desde o mesmo dia aparecem no jogo quando as formigas rebocam a sala (PRG-071); berçário e depósito conferidos em Play Mode pelo agente.

Refação do cenário no padrão de pixel art, com limpeza pelo Qwen, registrada para outro momento (ART-021, REQ-060). O piloto foi a Pebble_05_2x, com master trocado e importação conferida; ela ainda precisa ser refeita nativa a 16 px por célula.

Pedras de cenário refinadas em 14 variantes a 16 px/célula, preservando o tamanho físico maior que as operárias (ART-018, REQ-053). Desde 2026-10-22 todas têm textura leve guiada pelo Qwen, em marcas de 2 px. A Pebble_05_2x foi aprovada pelo desenvolvedor no jogo; as outras 13 têm importação conferida e aguardam aprovação.

Pulgões refinados em oito quadros de 40×24, com prancha e importação conferidas (ART-017); integrados no jogo pela fazenda de pulgões em 2026-10-22 (REQ-061), aguardando aprovação visual do conjunto. Fazendeira por troca de rampa (marrom) em 2026-10-22 (ART-022).

Ovos, larvas e pupas refinados em seis quadros, com paletas e importação conferidas (ART-016); reprodução em jogo e aprovação visual do conjunto pendentes.

Cupins refinados em 16 quadros de 64×64, com prancha e importação conferidas (ART-015); reprodução em jogo e aprovação visual do conjunto pendentes.

A direção de arte unificada foi aprovada em 2026-10-21 (ART-013): 16 px por célula, paleta mestra e filtro Point. O refinamento das formigas e da Rainha (ART-014) está implementado em 72 quadros, com cores por função preservadas e importação conferida na Unity; reprodução em jogo e aprovação visual do conjunto pendentes. Cenário, crias, UI e demais etapas continuam com o estado registrado em ART-013.

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
| ART-013 | Direção de arte unificada: paleta mestra, atlas RGB do terreno e do fundo, formigas em 48×32, crias, cenário e UI a 16 px por célula | IN_PROGRESS | UNDEFINED | [Plano em etapas](../Docs/02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md) | REQ-047, PRG-059 | — |

| ART-014 | Refinamento da aparência das formigas e da Rainha a partir da amostra aprovada | IMPLEMENTED | UNDEFINED | [Sprites, prancha e validação dos 72 PNGs na Unity](../Assets/Project/Art/Ants/README.md) | REQ-049, ART-013 | 2026-10-21 |

| ART-015 | Refinamento dos cupins: voo, pouso, caminhada e vista superior | IMPLEMENTED | UNDEFINED | [16 sprites, prancha e validação de importação](../Assets/Project/Art/Ants/InimigaAlada/README.md) | REQ-050, ART-014 | 2026-10-21 |

| ART-016 | Refinamento das crias: ovo, larva e pupa | IMPLEMENTED | UNDEFINED | [Seis sprites, prancha e validação na Unity](../Assets/Project/Art/Ants/README.md) | REQ-051, ART-013 | 2026-10-21 |

| ART-017 | Refinamento do pulgão: caminhada lateral e superior | IMPLEMENTED | UNDEFINED | [Oito sprites, prancha e validação na Unity](../Assets/Project/Art/Aphids/README.md); em jogo desde 2026-10-22 (pulgões da fazenda e pulgão preto tingido), vistos pelo agente na Game view | REQ-052, ART-013, REQ-061 | 2026-10-22 |

| ART-018 | Pedras de cenário refinadas a 16 px/célula, com textura leve guiada pelo Qwen | IMPLEMENTED | UNDEFINED | [14 sprites, fontes, textura e rollback](../Assets/Project/Art/Scenery/Pebbles/README.md); [prancha](../Docs/02_World_Art_UI/Pedras_refinadas_2026-10-21.png); Pebble_05_2x aprovada no jogo pelo desenvolvedor em 2026-10-22 | REQ-053, ART-013 | 2026-10-22 |

| ART-019 | Refinamento das oito famílias restantes de cenário | IN_PROGRESS | UNDEFINED | [Scenery](../Assets/Project/Art/Scenery/README.md) | REQ-054 | — |
| ART-020 | Arte da cuspideira e da lixeira (pastas em Art/Ants) | IMPLEMENTED | UNDEFINED | 16 quadros por troca de rampa dos masters da operária ([README](../Assets/Project/Art/Ants/README.md)): cuspideira verde-limão (25, 27, 28), lixeira cinza (17, 18, 20); exportador conferiu dimensões, alpha e paleta; importados a 16 PPU, ligados na cena e vistos pelo agente em Play Mode ao lado das demais funções em 2026-10-21; falta a aprovação do desenvolvedor | REQ-056, REQ-057, ART-013 | 2026-10-21 |
| ART-021 | Refazer o cenário no padrão de pixel art com limpeza pelo Qwen (piloto: Pebble_05_2x) | PLANNED | UNDEFINED | Piloto: [Pebbles/README.md](../Assets/Project/Art/Scenery/Pebbles/README.md) (Pebble_05_2x redesenhada, master trocado, bake e importação conferidos) | REQ-060, ART-018, ART-019 | 2026-10-21 |
| ART-022 | Arte da fazendeira (pasta Art/Ants/Fazendeira) | IMPLEMENTED | UNDEFINED | 8 quadros por troca de rampa dos masters da operária, rampa marrom 9-10-11 ([README](../Assets/Project/Art/Ants/README.md)); exportador conferiu dimensões, alpha e paleta; importados e ligados na cena; vistos pelo agente na Game view em 2026-10-22; falta a aprovação do desenvolvedor | REQ-061, ART-020 | 2026-10-22 |
| ART-023 | Tiles de sala construída no atlas do terreno: bloco e fundo de sete famílias | IMPLEMENTED | UNDEFINED | [Terrain/README.md](../Assets/Project/Art/Terrain/README.md) ("Salas construídas"); [maquete](../Docs/02_World_Art_UI/43_Salas_construidas.png); atlas de 128 × 4224 aceito pela Unity; tiles aprovados pelo desenvolvedor na maquete; berçário e depósito vistos em Play Mode pelo agente com a luz do jogo em 2026-10-22 | REQ-058, ART-013, PRG-071, GD-010 | 2026-10-22 |
| ART-024 | Experimento 8x8: terreno, formigas, crias, cupins, pulgões, cenário refinado e cultivo com 8 texels por célula, mostrados em 2x, com contorno preto de 1 texel (branch `experimento/arte-8x8`) | IMPLEMENTED | UNDEFINED | [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Arte 8x8"); suíte EditMode 445/445; quicksave visto em Play Mode pelo agente | REQ-068, ART-013, ART-023 | 2026-10-22 |
| ART-025 | Kit do graveto: gravetos montados por peças de 8 px no tamanho do mundo do antigo, no lugar dele (branch `experimento/arte-8x8`) | IMPLEMENTED | UNDEFINED | [SceneryKits/README.md](../Assets/Project/Art/SceneryKits/README.md); `TwigKitBuilderTests`; visto em Play Mode pelo agente no quicksave | REQ-069, ART-024 | 2026-10-23 |
| ART-026 | Arte dos biomas da superfície: topo, camada rasa, coletáveis, silhueta, fundo, item, argila e entrada secundária | IN_PROGRESS | UNDEFINED | Tile da serapilheira na linha 18 do [atlas](../Assets/Project/Art/Terrain/README.md), visto em Play Mode pelo agente em 2026-10-23; lista em [Documento 44](../Docs/02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md) | REQ-070, GD-012, REQ-068 | — |
| ART-027 | Minérios, barras e equipamentos no atlas; fundição e forja | IN_PROGRESS | UNDEFINED | Linhas 21 a 35 do [atlas](../Assets/Project/Art/Terrain/README.md), provisórias, geradas por código em 2026-10-23; sprites 8 × 8 do minério solto ([Art/Ants](../Assets/Project/Art/Ants/README.md)), 2026-10-23; a arte do equipamento ficou em ART-028 | REQ-071 | — |
| ART-028 | Arte do equipamento das formigas: camadas vestidas, 9 itens 8 × 8 e 5 ícones de HUD (cobre, ferro e ouro) | VERIFIED | UNDEFINED | [Art/Ants/README.md](../Assets/Project/Art/Ants/README.md) ("Equipamento das formigas" e "Itens de equipamento"); pranchas [vestidas](../Docs/02_World_Art_UI/Formigas_equipadas_2026-10-23.png), [itens](../Docs/02_World_Art_UI/Itens_equipamento_2026-10-23.png) e [ícones](../Docs/02_World_Art_UI/Icones_HUD_forja_armazens_2026-10-23.png); importação conferida na Unity e vista em Play Mode pelo agente; aprovada pelo desenvolvedor em 2026-10-23 | REQ-071, ART-027, ART-024 | 2026-10-23 |
| ART-029 | Silhuetas por papel das formigas: rainha com coroa e gáster enorme, soldado com cabeção, cuspideira, coletora, cortadeira, escavadora, cuidadora, jardineira, lixeira (branca) e fazendeira (azul, chapéu de palha) | VERIFIED | UNDEFINED | [Art/Ants/README.md](../Assets/Project/Art/Ants/README.md) ("Variantes de silhueta por papel"); gerador [ant_variants.py](../Assets/Project/Art/Ants/Source~/ant_variants.py); 80 PNGs importados sem erros na Unity, vistos em Play Mode pelo agente (baixa ampliação); aprovado pelo desenvolvedor em 2026-10-23 | ART-014, ART-022 | 2026-10-23 |
| ART-030 | Rachadura dos blocos golpeados: máscaras 8 × 8 de 4 estágios, na cor do contorno (provisória do agente) | IMPLEMENTED | UNDEFINED | [CrackArt.cs](../Assets/Project/Scripts/Runtime/AntFarm/CrackArt.cs); vista em Play Mode pelo agente em 2026-10-23; sem avaliação do desenvolvedor | REQ-072, PRG-076 | 2026-10-23 |

## Detalhes

### ART-021 — Cenário no padrão de pixel art (limpeza com Qwen)

Pedido para outro momento (REQ-060): passar todos os assets de `Art/Scenery` pelo fluxo usado na Pebble_05_2x, porque a normalização por amostragem direta deixou blocos de tamanhos misturados, granulado, filetes de 1 px e contorno grosso. O fluxo roda no estúdio externo `H:\QWEN` (ComfyUI + Qwen Image 2.1):
1. Redesenho com edição do Qwen (`tools/qwen_run.py`, 25 steps, 2 seeds fixos), com o asset sobre fundo branco.
2. Ajuste à grade e à paleta da família (`tools/pixel_snap_palette.py`, opções `--thin` e `--edge-dark`), com contorno de 1 px em `1B1815` e alpha 0/255.
3. Zoom 8× antes × depois e aprovação do desenvolvedor.
4. Backup e troca do master em `Source~/<Família>/Refined`; bake e reimportação.

Os critérios de aceitação estão em REQ-060.

Piloto em 2026-10-21: a Pebble_05_2x foi trocada; o bake validou os 14 masters; a importação foi conferida pelo MCP (16 PPU, Point, alpha 0/255, GUID, pivô e caixa da silhueta iguais); console sem erros. Play Mode não foi feito.

Cuidados para a rodada completa:
- **Variantes `_2x`:** uma `_2x` pode ser outro desenho. A Pebble_05 e a Pebble_05_2x são pedras diferentes; compare sempre com o master da mesma variante.
- **Densidade das `_2x`:** devem ser geradas nativas, com 16 px por célula. A Pebble_05_2x atual é a ampliação 2× de 608×496 (8 px por célula) e precisa ser refeita. Na tentativa nativa a borda ficou esfiapada; ajuste `--smooth` e `--edge-band`.
- **Metadados:** os PNGs atuais das outras 12 pedras têm metadados; limpe-os no mesmo passo.
- **Exportador procedural:** ele sobrescreve os desenhos; não use para restaurar.

### ART-018 — Pebbles no estilo das formigas

Quatorze pedras redesenhadas com ImageGen integrado, na paleta mestra, com facetas, lascas e fissuras. PNGs com 16 vezes a largura/altura anteriores, a 16 PPU; dimensões físicas preservadas de 8×5 a 76×62 células, contra 3×2 da operária. Sprites detalhados separados da máscara de navegação e da textura grosseira da superfície (PRG-061). Fontes, prompts e reexportação em [Scenery/Source~/Pebbles](../Assets/Project/Art/Scenery/Source~/Pebbles/); [guia do pacote](../Assets/Project/Art/Scenery/Pebbles/README.md).

Validação em 2026-10-21: prancha antes/depois com operária em escala inspecionada; 14 sprites importados na Unity 6000.3.11f1, com 3.158.528 pixels conferidos, paleta, alpha, dimensões, PPU, filtro, leitura e máscaras sem divergências. Quatorze GUIDs e pivôs normalizados preservados. Compilação final sem erros e 2 testes EditMode direcionados aprovados. Aprovação visual do desenvolvedor e conferência em Play Mode pendentes. Nenhuma build gerada.


### ART-017 — Refinamento da aparência do pulgão

Implementado em 2026-10-21 (REQ-052): oito quadros Walk/Top em 40×24 refinados com ImageGen, mantendo paleta verde, corpo em pera, cornículos, seis patas e antenas. Masters, prompts e reexportação em [Art/Aphids](../Assets/Project/Art/Aphids/README.md). Validação: 7.680 pixels com paleta e alpha binário conferidos, quatro quadros distintos por vista, prancha antes/depois inspecionada e importação dos oito sprites na Unity 6000.3.11f1 sem divergências de configuração; oito metadados preservados. Consulta ao console indisponível durante recarga do Editor. Aprovação visual e reprodução das animações pendentes; o pacote aguarda integração no gameplay, conforme ART-012. Nenhuma build gerada.

### ART-016 — Refinamento de ovos, larvas e pupas

Implementado em 2026-10-21 (REQ-051), após o refinamento das formigas e dos cupins: seis quadros de repouso em 48×32, com paletas claras existentes, volume em degraus, gomos da larva e dobras do casulo, preservando escala, pivôs, PPU e GUIDs. Fontes ImageGen e reexportação em [Art/Ants](../Assets/Project/Art/Ants/README.md). Validação: 9.216 pixels com paletas e alpha binário conferidos, dois quadros distintos por estágio, prancha antes/depois inspecionada e importação dos seis sprites na Unity 6000.3.11f1 sem divergências de configuração; seis metadados idênticos após importação. Reprodução em Play Mode e aprovação visual do conjunto pendentes. Console com mensagens de PlayerLoop e atlas de uma execução de testes, fora do escopo da arte. Nenhuma build gerada.

### ART-015 — Refinamento da aparência dos cupins

Implementado em 2026-10-21 após aprovação do refinamento das formigas e da Rainha (REQ-050). Os 16 quadros de 64×64 receberam carapaça segmentada e luz superior esquerda, com carvão/violeta, patas vinho, olhos âmbar e asas claras da paleta mestra. Preservados voo, pouso com perda das asas, caminhada lateral e superior; Land_3 termina na pose Walk_0. Fontes ImageGen, prompts e exportador em [Cupins](../Assets/Project/Art/Ants/InimigaAlada/README.md). Validação: 65.536 pixels com paleta e alpha binário conferidos, quatro quadros distintos por estado, prancha inspecionada e 16 sprites reimportados sincronamente na Unity 6000.3.11f1 sem divergências de dimensões, PPU, pivôs ou configurações; os 16 `.meta` permaneceram idênticos. Reprodução em Play Mode e aprovação visual do conjunto pendentes. O console continha mensagens de RenderTexture e atlas de uma execução de testes, fora do escopo desta alteração de arte. Nenhuma build gerada.

### ART-014 — Refinamento da aparência das formigas e da Rainha

Solicitado em 2026-10-21 após aprovação da amostra refinada de `Ant_Top_0.png`. Implementados 64 quadros das operárias e funções em 48×32 e 8 da Rainha em 96×64, com carapaça mais segmentada e sombreada, preservando as rampas de cor, caminhos e metadados. Fontes ImageGen, prompts e reexportação determinística em [Art/Ants](../Assets/Project/Art/Ants/README.md). Validação de 2026-10-21: 147.456 pixels com cores permitidas e alpha binário, quatro quadros distintos por vista/personagem, prancha completa inspecionada, 72 `.meta` idênticos aos anteriores e 72 sprites reimportados sincronamente na Unity 6000.3.11f1 sem divergências de dimensões, PPU, pivôs, Point, mipmaps ou compressão; console sem erros. Reprodução das animações em Play Mode e aprovação visual do conjunto pelo desenvolvedor pendentes. Nenhuma build gerada.

### ART-013 — Direção de arte unificada

Planejado em 2026-10-21 (REQ-047). O diagnóstico achou densidades de 1, 4, 6, 16 e 36 px de arte por célula, contornos e paletas diferentes por família. A proposta fixa 16 px por célula e uma paleta mestra; as etapas de arte (0, 5, 8, 9, 10, 11 e 12) estão no [documento 40](../Docs/02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md). Regras e decisões aprovadas pelo desenvolvedor em 2026-10-21, substituindo a decisão de 2026-10-17 de a formiga ter pixels menores que os do terreno: câmera continua em 16 px por célula, sem pixels grossos (D1); formigas mantêm os quadros de lado e de cima (D2); a luz substitui a névoa (D3); o cenário também vai para 16 px por célula, sem exceção de fundo (D4); a paleta mestra sai do cenário e vale para tudo (D5). Etapa 0 feita em 2026-10-21: o desenvolvedor escolheu 32 cores, e a paleta está em [Art/Palette](../Assets/Project/Art/Palette/README.md) (`MasterPalette.png` 32×1, importada na Unity com Point e sem compressão, conferida pixel a pixel, e `MasterPalette.hex`), com prancha em [41_Paleta_mestra.png](../Docs/02_World_Art_UI/41_Paleta_mestra.png). O cenário remapeado para ela ficou quase igual ao original (prévia do agente). Aprovada pelo desenvolvedor em 2026-10-21 ("A paleta de 32 cores está aprovada"), o que libera as etapas de arte (5, 8 a 12). Etapa 5 feita em 2026-10-21: atlas do terreno desenhado na paleta em [Art/Terrain](../Assets/Project/Art/Terrain/README.md) (`TerrainAtlas.png` e o gerador `Source~/terrain_atlas.py`), atribuído nas cenas AntFarm e MainMenu, conferido pelo agente em Play Mode e aprovado pelo desenvolvedor em 2026-10-21. Etapa 8 começada em 2026-10-21 pelas operárias e variantes de função: 64 quadros em 48 × 32 na paleta (ampliada com 12 cores das formigas por decisão do desenvolvedor, D6) em [Art/Ants](../Assets/Project/Art/Ants/README.md), importados a 16 PPU e conferidos pelo agente em Play Mode; aprovados pelo desenvolvedor em 2026-10-21. Rainha com quadros próprios em 96 × 64 (`Rainha/`, mesmo pixel das operárias) ligada no jogo e conferida pelo agente em Play Mode e aprovada pelo desenvolvedor em 2026-10-21. Cupim alado em 64 × 64 a 16 px por célula (`InimigaAlada/`) conferido pelo agente em Play Mode num ataque forçado e aprovado pelo desenvolvedor em 2026-10-21. Pulgões em 40 × 24 a 16 px por célula (`Art/Aphids`) conferidos na importação e em prévia (ainda sem uso no jogo) e aprovados pelo desenvolvedor em 2026-10-21. Etapa 8 concluída. Etapa 9 feita em 2026-10-21: ovo, larva e pupa em 48 × 32 a 16 px por célula e sprites do nutriente e da água carregados (`Art/Ants`), importados, ligados, conferidos em jogo e aprovados pelo desenvolvedor em 2026-10-21. Etapa 9 concluída.

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

### ART-023 — Tiles de sala construída no atlas do terreno

Feito em 2026-10-22 a pedido do desenvolvedor (REQ-058: "começar com a criação dos tiles, depois a lógica"). Decisões dele no mesmo dia: um visual por família (Rainha, Cultivo, Berçário, Resíduos, Orvalho, Fazenda e Depósito, este para os cinco depósitos) e, para cada uma, o bloco que as formigas montam nos slots e a parede de fundo da sala. São 14 linhas novas no fim do atlas, desenhadas pelo gerador na paleta mestra; o que cada uma desenha está no [README do terreno](../Assets/Project/Art/Terrain/README.md). Os blocos usam unidades alinhadas à grade das células, para o construído se distinguir da terra natural. O desenvolvedor aprovou os tiles na maquete ("os tiles estão bons", 2026-10-22) e a lógica entrou no mesmo dia (PRG-071). Falta ver em jogo as outras cinco famílias (só berçário e depósito foram vistos) e a aprovação delas em jogo.

### ART-024 — Experimento 8x8

Pedido em 2026-10-22 (REQ-068). Toda a arte do mundo que estava a 16 px por célula foi reduzida pela metade por um script que não cria cor nova (cor mais frequente de cada 2x2, a mais escura no empate), e o PPU dos sprites caiu junto, então nada muda de tamanho no jogo; só o pixel fica com o dobro do tamanho. A redução engrossa contornos e juntas: pedra, tijolo do depósito e lajota da rainha ficaram mais escuros, e as pernas das formigas viraram traço escuro. Arte definitiva nessa escala pediria redesenho. Na mesma data o cenário refinado (48 PNGs) e os sprites do cultivo também passaram a 8 px por célula, e tudo no primeiro plano ganhou contorno preto de 1 texel no preto da paleta (pedido: "linha preta ao redor dos elementos"): terra e folhas no shader, sprites no PNG, bloco carregado, cultivo e camadas Near/Front do cenário em código. A arte de células (Mushrooms, Tufts, Twigs) já tinha contorno pelo shader `SceneryCells` e passou a 8 texels por célula; as faixas de grama distante também passaram a 8 px por célula e ficam sem linha, como no Terraria. Contra o túnel escuro, o contorno das formigas quase some.

### ART-025 — Kit do graveto

Piloto de 2026-10-23 (REQ-069): em vez de redesenhar à mão gravetos de 200 a 360 células, um kit de peças a 8 texels por célula (casca repetível de 128 × 64, face do corte, lascas da ponta e do galho, três nós), montado por código com sombreado de cilindro na rampa da própria casca e contorno preto. No jogo, os gravetos do kit substituem os antigos (cinco variantes, posicionados e caminháveis como as pedras refinadas). Comparado ao antigo, sai mais escuro e com fibra fina; o galho tem junta simples, sem calo. A ponta do galho segue a direção dele desde o relato do desenvolvedor no mesmo dia.

### ART-026 — Arte dos biomas da superfície

Lista de arte por bioma, por camada e para os postos avançados, com ordem sugerida de produção, no [Documento 44](../Docs/02_World_Art_UI/44_Biomas_da_superficie_e_postos_avancados.md) (REQ-070). Segue a paleta mestra e a densidade que for adotada (16 px aprovada; 8 px em experimento, REQ-068); elementos grandes montados por peças, como o kit do graveto (ART-025). Depende da aprovação dos biomas em GD-012; a sugestão do agente para a demo é o gramado mais flores à esquerda e serapilheira à direita. Regra decidida em 2026-10-23: elementos grandes só no bioma deles, gramado central limpo e decoração que não imita insumo; com isso, os gigantes refeitos por peças (ART-025) se limitam aos que têm bioma. Em 2026-10-23, tile provisório da serapilheira (cobertura do chão da Serapilheira) gerado por `litter()` em `terrain_atlas.py`, reduzido a 8 px e inserido no atlas; a repetição a cada 8 células é visível. No mesmo dia, linhas 19 (terra adubada) e 20 (néctar) pelo mesmo caminho. Faltam camadas rasas, fundos, transbordos, silhueta do canteiro, item carregado da serapilheira e a aprovação do desenvolvedor.

### ART-027 — Minérios, barras e equipamentos

Linhas 21 a 23 (minérios: a pedra com pintas grandes do metal) e 24 a 35 (barras, ferramentas, armas e armaduras de cada metal, padrões provisórios só para o bloco carregado e o item guardado), geradas por `terrain_atlas.py`, reduzidas a 8 px e inseridas no atlas. Em 2026-10-23, depois do teste de jogo, pedido do desenvolvedor: famílias de sala construída próprias para a fundição, a forja e os dois armazéns de equipamento (linhas 51 a 58 do atlas) e os itens guardados desenhados com a arte do atlas, como objetos sobre a parede de fundo. A arte do equipamento das formigas (camadas vestidas, itens e ícones de HUD) passou para ART-028. Em 2026-10-23 os minérios (linhas 21 a 23) e as barras (24 a 26) foram redesenhados: pepitas com contorno escuro e lingotes empilhados, gerados a 16 texels, reduzidos e inseridos só nessas linhas; importação conferida na Unity e os seis materiais vistos em Play Mode pelo agente (amostras postas em área iluminada); falta a aprovação do desenvolvedor. O ferro foi clareado (pepitas de aço claro) por falta de contraste com a pedra; a versão clara só foi vista na prévia e importada na Unity, não em Play Mode. No mesmo dia as ferramentas, armas e armaduras (linhas 27 a 35) viraram objetos 8 × 8 por célula (picareta, espada, escudo), vistos em Play Mode pelo agente na mesma amostra; falta a aprovação do desenvolvedor, e o bloco carregado e o item guardado reais não foram vistos. Lista no [Documento 45](../Docs/01_Design/45_Minerios_fundicao_forja_e_equipamento.md).

### ART-028 — Arte do equipamento das formigas

Pedido de REQ-071 ("a arte das formigas que utilizam as ferramentas e as roupas/armaduras"). Três entregas, todas aprovadas pelo desenvolvedor em 2026-10-23, em etapas: a prova em ferro (lateral) e depois o conjunto, os itens e os ícones.
- **Camadas vestidas:** 18 PNGs 24 × 16 em `Art/Ants/Equipamento/<Metal>/<Tipo>/` (`Ant_Walk_0` e `Ant_Top_0`; uma camada por vista porque o corpo não muda entre os quatro quadros). Ligadas ao `AntSwarmView` (**Equip Walk** e **Equip Top**).
- **Itens:** 9 sprites 8 × 8 (`Item_<Metal><Tipo>`) na grade e na boca da formiga (**Equip Item Sprites** no `AntFarmView` e no `AntSwarmView`).
- **Ícones de HUD:** 5 PNGs 40 × 40 (`Art/UI/Sprites/icons/equip_*`) nos botões do depósito de minério, da fundição, da forja e dos dois armazéns.

Validação: importação conferida na Unity, console sem erros e Play Mode pelo agente (camadas lateral e superior, itens na grade e na boca, ícones da fundição, da forja e do depósito de minério; os dois armazéns ficaram abaixo da janela de teste). Suíte EditMode não rodada. O equipamento vindo da forja não foi exercitado com este visual. Guias: [Art/Ants](../Assets/Project/Art/Ants/README.md), [AntFarmGuide](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md), [WorkshopGuide](../Assets/Project/Scripts/Runtime/Colony/WorkshopGuide.md).

### ART-030 — Rachadura dos blocos golpeados

Arte provisória feita pelo agente em 2026-10-23 (REQ-072): quatro máscaras 8 × 8 desenhadas em texto em `CrackArt.cs`, cada uma contendo a anterior, pintadas pelo shader com o preto do contorno e espelhadas por célula. Falta a avaliação do desenvolvedor (forma, cor, se cada material deve ter a sua). Como trocar: [AntFarmGuide.md](../Assets/Project/Scripts/Runtime/AntFarm/AntFarmGuide.md) ("Rachadura dos blocos").

## Riscos e bloqueios


- Nenhum.

## Não verificado

- Kit do graveto (ART-025): desenvolvedor ainda não avaliou a versão que substitui os antigos; formigas andando sobre o graveto não foram observadas.
- Experimento 8x8 (ART-024): desenvolvedor ainda não avaliou a escala nem o contorno; custo do contorno no shader do terreno não medido.
- Tiles de sala construída (ART-023): rainha, cultivo, resíduos, orvalho e fazenda vistos em Play Mode pelo agente em 2026-10-23, em salas de teste montadas por código com a luz de fora (cada família desenha o bloco e o fundo próprios, conforme a maquete); falta a aprovação do desenvolvedor em jogo e vê-los numa sala feita pelas formigas.
- Sprites 8 × 8 das barras (ART-027, 2026-10-23): três lingotes (`Item_<Metal>Bar`) ligados na lista de equipamento do `AntFarmView` e do `AntSwarmView`; vistos em Play Mode na grade de uma fundição de teste; a suíte EditMode não foi rodada. Falta a aprovação do desenvolvedor. O `Item_IronOre` foi recolorido para aço claro (44, 45, 46) e acompanha as pepitas do atlas; as três barras foram vistas na boca da formiga em Play Mode.
- Pebble_05_2x limpa (ART-021): não vista em Play Mode; a densidade fica em 8 px por célula até ser refeita nativa.
- Aprovação visual e importação/reprodução na Unity das variantes por função (ART-006); integração no jogo adiada a pedido do desenvolvedor.

- Aprovação visual da arte provisória pelo desenvolvedor, inclusive os sprites da cria (ART-005).
- Aprovação visual das bordas 9-slice e da fonte do HUD (fonte padrão do Unity, sem fonte pixel) pelo desenvolvedor (ART-004).
