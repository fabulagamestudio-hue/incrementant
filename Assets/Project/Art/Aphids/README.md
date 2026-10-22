# Arte do pulgão

## Em jogo — 2026-10-22

Os oito quadros passaram a ser usados pela fazenda de pulgões: `AntSwarmView` → **Aphid Walk** / **Aphid Top**, ligados por **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins** (`AntSwarmView.RenderAphids`; o pulgão preto, que o cupim mordeu, usa os mesmos quadros tingidos com **Black Aphid Color**). Sistema: [FarmGuide](../../Scripts/Runtime/Colony/FarmGuide.md). As notas abaixo que dizem "sem uso no jogo" valem para antes desta data.

## Refinamento — 2026-10-21 (REQ-052)

Os oito quadros Walk/Top 0–3 receberam o tratamento visual das formigas aprovadas, mantendo a identidade do pulgão: corpo verde em pera, cabeça pequena, seis patas, antenas longas, cornículos e ausência de asas. Volume em cores planas com luz superior esquerda; somente `1B1815`, `638929`, `91B13B`, `B3CF51`, `437122`, `32550E` e `F5EDD9`, da paleta mestra. Alpha exclusivamente 0/255. Canvas **40×24**, **16 PPU**; pivôs laterais (20,6) e superiores (20,12). Mesmos caminhos, GUIDs e spriteIDs.

Masters ImageGen em `Source~/Refined/`; prompts em `Source~/refinement-prompts.json`. `normalize-refined.ps1` amostra a fonte diretamente na grade final, aplica a paleta e o contorno e ajusta os apêndices que sairiam do canvas, preservando o corpo. `stabilize-refined.ps1` mantém corpo, cabeça, antenas e cornículos estáveis entre as poses de patas. Reexportação dos oito masters, sem depender do serviço de imagem:

```powershell
& "Assets/Project/Art/Aphids/Source~/bake-refined.ps1"
```

Validação em 2026-10-21: oito PNGs e 7.680 pixels conferidos (dimensões, paleta e alpha), quatro quadros distintos por vista e [prancha antes/depois](../../../../Docs/02_World_Art_UI/Pulgoes_refinados_2026-10-21.png) inspecionada. Importação síncrona na Unity 6000.3.11f1 sem divergências de dimensões, PPU, pivôs, Point, mipmaps, compressão ou modo Single; oito `.meta` idênticos após importação. A consulta ao console ficou indisponível durante recarga do Editor. Aprovação visual do conjunto e reprodução das animações pendentes (ART-017). O pacote continua aguardando integração no gameplay, conforme a documentação existente; esta tarefa altera a arte. Nenhuma build gerada.

## Histórico

O grupo `aphids` do gerador procedural citado abaixo sobrescreve este refinamento. Para restaurar a aparência atual, use o exportador dos masters acima.

**Etapa 8 (2026-10-21):** os oito quadros foram redesenhados em **40 × 24** (2,5 × 1,5 células) a **16 px por célula**, na paleta mestra (corpo 25, 26, 27; patas e antenas 24 e 23; contorno 0), pelo gerador `Art/Ants/Source~/ant_sprites.py` (`aphid_side`/`aphid_top`): corpo em pera, cabeça separada com olho escuro, seis patas em tripé, antenas varridas para trás e cornículos. Mesmos caminhos e GUIDs; `AphidSpriteImporter` importa a 16 PPU com os pivôs normalizados de antes. Continuam sem uso no jogo (nenhum código nem cena os referencia). O texto abaixo descreve a arte anterior (72 × 48).

Oito sprites de pixel art verde, com transparência: `Aphid_Walk_0.png` a `Aphid_Walk_3.png` (lateral) e `Aphid_Top_0.png` a `Aphid_Top_3.png` (superior). Todos virados para a direita, em ordem de caminhada. Corpo em forma de pera, seis patas, antenas e cornículos traseiros.

Cada quadro tem **72x48 px**, importado a **36 pixels por unidade**: canvas de **2 × 1,33 unidades**, dois terços do canvas das formigas adultas (108x72 a 36 PPU, 3 × 2 unidades). Filtro Point, sem mipmaps nem compressão, malha FullRect e alpha binário. Pivô lateral (0,5; 0,25); superior (0,5; 0,5). Alinhamento comum dentro de cada vista, sem recorte individual que faça o corpo saltar na animação.

Importação automática por `Scripts/Editor/AntFarm/AphidSpriteImporter.cs`. Os PNGs estão preparados como assets; ainda não há comportamento ou renderizador de pulgões no jogo. A velocidade da futura animação fica a cargo do consumidor.

Gerados em 2026-10-18 pelo `image_gen.imagegen` integrado, usando `Art/Ants/Ant_Walk_0.png` como referência de estilo. A prancha 4x2 foi separada, reduzida sem interpolação e limitada a 16 cores opacas, com transparência 0/255. Prompt em [Generation.json](Generation.json). Aprovação visual pelo desenvolvedor pendente.

Validação em 2026-10-18 pela API do Editor Unity: oito sprites carregáveis, 27.648 pixels, dimensões, PPU, pivôs, paleta, alpha e configurações de importação conferidos. Prévia lateral inspecionada. Configuração gravada nos assets durante Play Mode; a compilação do novo importador ainda depende da recarga de scripts no Editor. Nenhuma build gerada.
