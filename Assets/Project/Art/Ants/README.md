# Arte e variantes de cor das formigas

> **Experimento 8x8 (branch `experimento/arte-8x8`, 2026-10-22):** os PNGs desta pasta (e de `Aphids`) estão com metade do tamanho descrito abaixo (adultos 24×16, rainha 48×32, cupins 32×32, itens 8×8), reduzidos com `../Source~/halve_pixel_art.py`, e o PPU do `AntSpriteImporter` caiu junto (8; rainha 16). Depois receberam o contorno preto de 1 texel por dentro (`../Source~/outline_pixel_art.py`). Os exportadores ainda geram a 16 px por célula. Ver `AntFarmGuide.md`, "Arte 8x8".

## Equipamento das formigas — 2026-10-23 (REQ-071)

Camadas 24×16 (mesmo canvas, PPU e pivô do papel) para desenhar **por cima** do sprite da formiga: `Equipamento/<Ferro|Cobre|Ouro>/<Ferramenta|Arma|Armadura>/Ant_Walk_0.png` (lateral) e `Ant_Top_0.png` (superior). Uma camada por vista, porque o corpo não muda entre os quatro quadros (só as patas, que ficam por baixo). Ferramenta é a ponteira nas mandíbulas (escavadora e comum), arma é a lâmina nas costas e armadura é capacete e placa no tórax (soldado e cuspideira). Metal por troca de rampa (ferro em aço cinza-azulado para não sumir no corpo vermelho). Gerar de novo: `python Source~/equip_layers.py [--sheet prancha.png]`. [Prancha](../../../../Docs/02_World_Art_UI/Formigas_equipadas_2026-10-23.png). Ligadas no `AntSwarmView` (**Equip Walk** e **Equip Top**, 9 sprites cada) por **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins**.

**Camadas próprias por papel (2026-10-23):** o soldado, de cabeção, tem a armadura em `Equipamento/<Metal>/Armadura/Soldado/` (`Ant_Walk_0`, `Ant_Top_0`): placa no tórax e capacete que cobre só o alto da cabeça, no lugar da placa de rosto geral, que atravessava o cabeção. Gerada por `soldier_armor()` em `Source~/equip_layers.py`. Ligadas em **Role Equip** do `AntSwarmView` (posição null = camada geral). Cuspideira, escavadora e operária continuam com as gerais.

### Itens de equipamento e barras (8 × 8)

`Item_<Copper|Iron|Gold><Tool|Weapon|Armor|Bar>.png` (12 PNGs; a barra é um lingote visto de frente, desde 2026-10-23, e vai no fim da lista, 8×8, RGBA32 sem compressão, centrados): o mesmo desenho das camadas vestidas, como objeto (picareta, espada, capacete), com o contorno de 1 texel. Aparecem na boca da formiga a caminho do armazém e guardados na grade, no lugar da arte provisória do atlas. Gerar de novo: `python Source~/equip_items.py [--sheet prancha.png]`. [Prancha](../../../../Docs/02_World_Art_UI/Itens_equipamento_2026-10-23.png). Ligados em **Equip Item Sprites** do `AntSwarmView` e do `AntFarmView`.

## Refinamento de aparência — 2026-10-21 (REQ-049)

A amostra refinada de `Ant_Top_0.png` foi aprovada pelo desenvolvedor, que pediu aplicar a aparência às formigas e à Rainha. A entrega usa 16 masters (Top/Walk 0–3 para operária e Rainha) em `Source~/Refined/`, com carapaça em volumes e segmentos mais legíveis, luz superior esquerda e a rampa vermelha existente. As funções derivam dos mesmos masters por troca exata da rampa da tabela abaixo: 64 quadros em 48×32 e 8 da Rainha em 96×64, nos mesmos caminhos e com os mesmos GUIDs, PPU e pivôs.

Os masters foram editados com o ImageGen integrado; os prompts ficam em `Source~/refinement-prompts.json`. `normalize-refined.ps1` reduz cada saída à grade nativa, às cinco cores permitidas e a alpha 0/255. `stabilize-refined.ps1` mantém a carapaça e as antenas do quadro zero entre as poses de patas. Os PNGs finais de `Refined/` são a fonte de reexportação, sem depender do serviço de imagem. Para reexportar os 72 sprites:

```powershell
& "Assets/Project/Art/Ants/Source~/bake-refined.ps1"
```

`-Only` limita a exportação a algumas pastas (ex.: `-Only @('Cuspideira','Lixeira')`). A fazendeira (pedido de 2026-10-22, fazenda de pulgões) foi exportada assim no mesmo formato, com a rampa marrom 9-10-11 da tabela da etapa 8, e ligada por **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins**; aprovação visual pendente. A cuspideira e a lixeira (REQ-056, REQ-057) foram exportadas assim em 2026-10-21, a partir dos mesmos masters da operária, com as rampas da tabela da etapa 8; as outras pastas não foram regravadas. Importadas a 16 PPU com o mesmo pivô da operária e ligadas no `AntSwarmView` por **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins**.

O exportador confere dimensões, cores e alpha antes de salvar. Não altera `.meta`. Os geradores procedurais antigos descritos no histórico abaixo **sobrescrevem este refinamento**; não os use para restaurar a aparência atual.

Validação em 2026-10-21: 72 PNGs (147.456 pixels), dimensões, cores e alpha binário conferidos pelo exportador; quatro quadros distintos por vista e personagem; prancha completa inspecionada. Importação síncrona dos 72 sprites na Unity 6000.3.11f1 sem divergências de dimensões, PPU, pivôs, Point, mipmaps, compressão ou modo Single; console sem erros. Os 72 `.meta` permaneceram idênticos aos da sessão antes da troca. [Prancha do conjunto](../../../../Docs/02_World_Art_UI/Formigas_refinadas_2026-10-21.png).

A aprovação da amostra não equivale à aprovação visual dos demais quadros. Reprodução em Play Mode e aprovação visual do conjunto pelo desenvolvedor ainda pendentes (ART-014). Nenhuma build gerada.

## Etapa 9 — crias e itens em 16 px (2026-10-21, REQ-047)

### Refinamento das crias — 2026-10-21 (REQ-051)

Os seis quadros `Egg_Idle_0–1`, `Larva_Idle_0–1` e `Pupa_Idle_0–1` receberam o tratamento visual das formigas aprovadas: ovo oval com volume em três tons, larva em gomos com cabeça castanha e pupa com dobras do casulo e as duas faixas existentes. Mantidos **48×32**, **16 PPU**, pivô (24,8), o envelope de tamanho de cada cria e o apoio no chão. Paletas: ovo 0/14/15/16; larva 0/6/8/14/15/16; pupa 0/6/7/8/15. Contorno de um pixel em `1B1815`, luz superior esquerda e alpha 0/255.

Masters em `Source~/BroodRefined/`, editados com ImageGen integrado; prompts em `Source~/brood-refinement-prompts.json`. `normalize-brood-refined.ps1` amostra a fonte diretamente no tamanho final, aplica a paleta do estágio e o contorno, sem reduzir e reampliar uma miniatura. Para reexportar os seis PNGs, preservando os metadados:

```powershell
& "Assets/Project/Art/Ants/Source~/bake-brood-refined.ps1"
```

Validação em 2026-10-21: seis PNGs e 9.216 pixels conferidos (dimensões, paletas e alpha), dois quadros distintos por estágio, [prancha antes/depois](../../../../Docs/02_World_Art_UI/Crias_refinadas_2026-10-21.png) inspecionada. Importação síncrona na Unity 6000.3.11f1 sem divergências de dimensões, PPU, pivô, Point, mipmaps, compressão ou modo Single. Os seis `.meta` permaneceram idênticos após importação. Reprodução em Play Mode e aprovação visual do conjunto pendentes (ART-016). O console continha mensagens de PlayerLoop e atlas de uma execução de testes, fora do escopo desta alteração de sprites. Nenhuma build gerada.

### Base procedural anterior e itens carregados

O grupo `brood` do gerador procedural descrito abaixo sobrescreve este refinamento das crias. Reaplique `bake-brood-refined.ps1` para restaurar os seis quadros; o exportador não altera os itens carregados.

`Egg_Idle_0–1`, `Larva_Idle_0–1` e `Pupa_Idle_0–1` foram redesenhados em **48 × 32** (as mesmas 3 × 2 células do 18 × 12 a 6 px), apoiados na linha de baixo, na paleta mestra com contorno 0: ovo nos cremes (14, 15, 16), larva em gomos curvados com a cabeça em 6 e a boca escura, pupa em casulo (6, 7, 15) com as duas faixas escuras (8). Mesmos caminhos e GUIDs; `AntSpriteImporter` importa a 16 PPU (os de 18 × 12 continuariam a 6).

Itens novos que a formiga leva na boca, 16 × 16 (1 célula), pivô no centro: `Item_Nutrient.png` (grão amarelo: 12, 13, 28) e `Item_Water.png` (gota: 29, 30, 31). Ligados em `AntSwarmView` → **Nutrient Sprite** / **Water Sprite**; vazios, volta o quadrado na cor do Inspector. O ovo na boca já usa a arte da cria.

Minério solto (REQ-071, 2026-10-23): `Item_CopperOre.png`, `Item_IronOre.png` e `Item_GoldOre.png`, 8 × 8 desenhados direto nessa escala (a redução de um 16 × 16 apagava as pepitas): torrão de pedra (17, 18, 19) com pepitas do metal (cobre 8, 11, 12; ferro 44, 45, 46 (aço claro, como no atlas; era 32, 33, 34 em 2026-10-23); ouro 7, 13, 16). Gerador: `ore_item` em `Source~/ant_sprites.py` (grupo `brood`). O mesmo sprite vai na boca (`AntSwarmView` → **Ore Sprites**) e no chão ou no depósito (`AntFarmView` → **Ore Item Sprites**).

Gerados por `Source~/ant_sprites.py` com o grupo `brood`:

```bash
python -I "Assets/Project/Art/Ants/Source~/ant_sprites.py" Assets/Project/Art/Palette/MasterPalette.hex Assets/Project/Art/Ants brood
```

O gerador agora grava só os grupos pedidos (`workers`, `queen`, `termite`, `aphids`, `brood`). **Não use `workers` nem `queen`**: as operárias, as funções e a rainha foram refinadas depois (seção acima) e seriam sobrescritas. Em 2026-10-21 uma rodada do gerador (grupo inteiro, às 17:59) chegou a regravar esses quadros; o refinamento foi reexportado logo depois (18:00) e é o que está no projeto.

## Inimigos

- [InimigaAlada/](InimigaAlada/README.md): cupins refinados em 2026-10-21 (REQ-050), 16 sprites em 64×64 a 16 PPU — voo, pouso com perda de asas, caminhada lateral e vista superior. Fontes e exportador em `Source~/TermiteRefined/` e `Source~/bake-termite-refined.ps1`; mesmos caminhos, GUIDs e pivôs. Usados pelos cupins dos ataques desde 2026-10-18 (REQ-029).

## Etapa 8 — formigas em 16 px (2026-10-21, REQ-047)

Os quadros adultos da raiz e das pastas `Operaria/`, `Soldado/`, `Coletora/`, `Escavadora/`, `Cuidadora/`, `Jardineira/` e `Cortadeira/` (`Ant_Walk_0–3` e `Ant_Top_0–3`) foram redesenhados em **48 × 32** (3 × 2 células a 16 px por célula), na [paleta mestra](../Palette/README.md), com contorno de 1 px na cor 0 (`1B1815`), luz de cima à esquerda e brilho no olho (cor 16). Mesmos caminhos e GUIDs. Importados a 16 PPU pelo `AntSpriteImporter`, com os pivôs normalizados de antes.

| Pasta | Rampa (sombra, base, luz) | Índices |
|---|---|---|
| raiz e `Operaria/` | `5C1408` `9E311E` `D3592A` | 32–34 |
| `Soldado/` | `3A1A5C` `6F339E` `A06BD0` | 35–37 |
| `Coletora/` | `32550E` `437122` `91B13B` | 23, 24, 26 |
| `Escavadora/` | `823A10` `D6872F` `F4B815` | 8, 12, 13 |
| `Cuidadora/` | `1F3E70` `346EB4` `6EAADC` | 29–31 |
| `Jardineira/` | `6B1F45` `C2477F` `E889B4` | 38–40 |
| `Cortadeira/` | `0F4F47` `1F9E8E` `6FD6C4` | 41–43 |
| `Cuspideira/` | `638929` `B3CF51` `E2EA7A` | 25, 27, 28 |
| `Lixeira/` | `9A9AA8` `E8E8EE` `FFFFFF` (branca, 2026-10-23; antes `504D55` `7D7676` `BBB29F`) | 44–46 |
| `Fazendeira/` | `0A2A9E` `0144F7` `5C8CFF` (azul vivo, 2026-10-23; antes `7B4F2E` `9E6534` `C5833E`) | 47–49 |

Gerador: `Source~/ant_sprites.py` (pasta ignorada pela Unity). Desenha por formas (elipses do gáster, pecíolo, tórax e cabeça; linhas das patas, antenas e mandíbulas), com ciclo de tripé em 4 quadros. Para refazer:

```bash
python -I "Assets/Project/Art/Ants/Source~/ant_sprites.py" Assets/Project/Art/Palette/MasterPalette.hex Assets/Project/Art/Ants
```

**Rainha (`Rainha/`, 2026-10-21):** quadros próprios em **96 × 64**, na rampa vermelha, com gáster maior e listrado, tórax maior, patas de 2 px e as marcas das asas. Em jogo ela é desenhada com **Queen Scale** 2 (6 × 4 células) e o importador a lê a 32 px por unidade (`AntSpriteImporter.QueenPixelsPerUnit`), então o pixel dela é o mesmo das operárias (1/16 de célula). Substituíram os quadros dourados de 108 × 72, que não eram usados em jogo (mesmos caminhos e GUIDs); ligados em `AntSwarmView` → **Queen Walk Frames** / **Queen Top Frames**. Saem do mesmo gerador (`side`/`top` com escala 2); as operárias continuam idênticas.

**Cupim alado (`InimigaAlada/`, 2026-10-21):** os 16 quadros (Fly, Land, Walk e Top 0–3) em **64 × 64** (4 × 4 células, as mesmas de antes) a 16 px por célula, com o corpo da operária no lugar em que estava no canvas antigo (pés meia célula abaixo do pivô de lado; centro de cima), carapaça carvão-violeta (cores 0, 35, 17), patas vinho (38), olho âmbar (12) e asas claras (16 com borda e nervura 20). Voo com patas recolhidas e asas batendo em 4 ângulos; pouso com asas abertas, dobradas, soltando-se e já sem elas. Mesmos caminhos e GUIDs (as cenas não mudaram). Saem do mesmo gerador (`termite_frames`).

**Pulgões (`Art/Aphids`, 2026-10-21):** oito quadros em 40×24 a 16 PPU, refinados com ImageGen em REQ-052; fontes e exportador em `Art/Aphids/Source~/` (ver o [README dos pulgões](../Aphids/README.md)). O grupo procedural `aphids` sobrescreve o refinamento. Adultos e crias estão a 16 px por célula; o menu antigo `AntSpriteGenerator` gera arte em 108×72 e não restaura a aparência refinada.

## Melhoria gráfica — 2026-10-18 (REQ-026)

Os 72 quadros adultos Top/Walk foram redesenhados com corpo segmentado, cintura, antenas, mandíbulas e luz na carapaça, mantendo 108x72 px, os quatro quadros por vista, caminhos, GUIDs e pivôs. A base vermelha e as oito pastas usam a mesma animação; as funções mantêm suas referências de cor abaixo. Paleta de até 16 cores por quadro, alpha exclusivamente 0/255 e amostragem sem suavização: nenhum halo semitransparente nas bordas. A operária é idêntica à base. Ovo, larva e pupa não foram alterados.

As pranchas-base foram criadas pelo `image_gen.imagegen` integrado, usando os quadros anteriores como referência; recorte, redução à resolução final e aplicação das paletas preservam pixels inteiros. [Prompts e processamento](../PixelArtGeneration.json). A densidade de 36 pixels por unidade mantém o tamanho de 3x2 unidades. O menu de regeneração continua sendo o gerador procedural provisório e sobrescreve esta arte se acionado; o setup preserva arquivos existentes.

Validação desta entrega: 141 PNGs (72 adultos e 69 elementos de cenário), 1.390.673 pixels conferidos, paletas de até 24 cores, alpha binário, dimensões e metadados preservados. Prévias ampliadas inspecionadas. Importação na Unity e comportamento em Play Mode são verificações separadas; veja o registro ART-010 em `Production/ART.md`.

## Histórico e referências de cor

Cada pasta contém `Ant_Top_0.png` a `Ant_Top_3.png` e `Ant_Walk_0.png` a `Ant_Walk_3.png`, com 108x72 pixels e transparência. Em 2026-10-18, os 72 quadros adultos (raiz e oito pastas) foram ampliados exatamente 6x em cada eixo a partir dos originais 18x12, sem suavização nem alteração de cores. Há 36x mais pixels disponíveis para retoques; a ampliação por si só não acrescenta detalhes.

| Pasta | Cor | Referência da paleta |
|---|---|---|
| `Operaria/` | Vermelho original | `#9E311E` |
| `Soldado/` | Roxo escuro | `#6F339E` |
| `Coletora/` | Verde | `#318B39` |
| `Escavadora/` | Laranja | `#D36318` |
| `Cuidadora/` | Azul-claro | `#419EC7` |
| `Jardineira/` | Rosa | `#C2477F` |
| `Cortadeira/` | Verde-azulado | `#1F9E8E` |
| `Rainha/` | Dourado até 2026-10-21; hoje a rampa vermelha, ver "Etapa 8" | `#CA9623` |

As variantes aplicam a paleta proposta na conversa de 2026-10-18; os nomes são categorias de arte e não adicionam classes ao jogo. A rainha mantém o desenho e o tamanho dos quadros originais: somente a cor muda.

Os caminhos dos PNGs da raiz e seus GUIDs foram preservados. `Operaria/` contém cópias idênticas desses quadros. Ovo, larva e pupa permanecem na raiz, sem alteração de resolução ou densidade.

As variantes já estão ligadas ao jogo (ver `AntFarmGuide.md`, "Formigas em sprite"). O importador `AntSpriteImporter` cobre as subpastas e configura filtro Point e 36 pixels por unidade para `Ant_Top_*` e `Ant_Walk_*`: 108/36 por 72/36 mantém o tamanho de 3x2 unidades no jogo. Os pivôs normalizados continuam Top (0,5; 0,5) e Walk (0,5; 0,25). Cenário e cria continuam usando a densidade base de 6 pixels por unidade. O gerador também exporta os adultos em 108x72.

Validação da ampliação em 2026-10-18: 72 PNGs, 559.872 pixels RGBA comparados com os originais da sessão; cada bloco 6x6 corresponde exatamente ao pixel original. Metadados comparados: somente `spritePixelsToUnits` passou de 6 para 36, preservando GUIDs e pivôs. Confirmação final de compilação/importação na Unity pendente: refresh excedeu o tempo de espera; console sem erros no momento da consulta. Nenhuma build gerada.

Validação em 2026-10-18: 48 PNGs e 10.368 pixels comparados com os originais, conferindo dimensões, alpha e substituição exata da paleta; hashes dos originais e das cópias vermelhas conferidos. Prévia ampliada inspecionada. Importação e reprodução na Unity ainda não verificadas.

Em 2026-10-18 foram criadas `Jardineira/` e `Cortadeira/` por troca de paleta a partir de `Operaria/` (mesmos 4 tons: patas escuras, patas do outro lado, corpo e brilho; olho preservado). Carregadora usa `Coletora/`.

## Variantes de silhueta por papel — 2026-10-23 (experimento)

Cada papel ganhou um traço próprio sobre a base da operária: soldado com cabeção e pinça, cuspideira com gáster inchado de ácido, coletora com ombros, cortadeira com mandíbula-lâmina, escavadora com pá, cuidadora com touca e avental, jardineira com brotos, lixeira com saco de lixo, fazendeira com chapéu de palha; a rainha tem gáster enorme e coroa (diadema na vista de cima). Operária intacta. Gerar de novo: `python -I Source~/ant_variants.py [--sheet prancha.png] [--dry]` (lê de `Source~/Variants/Base`, cópia dos originais). **O `bake-refined.ps1` regrava as formigas sem as variantes: rode este script depois dele.** Mesmos caminhos, GUIDs e pivôs. As camadas de equipamento (ferro) foram sobrepostas às variantes e não pedem realinhamento: a ponteira cai na boca, a lâmina nas costas e a placa no tórax, só ficam por cima do cabeção do soldado e da pá da escavadora. Conferido em Play Mode (rainha, operárias e 6 papéis trocados em tempo de execução, em baixa ampliação); aprovação visual pendente.
