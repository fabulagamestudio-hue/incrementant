# Pebbles — pedras de cenário refinadas

As 14 variantes foram redesenhadas com o ImageGen integrado em 2026-10-21 (REQ-053, ART-018), seguindo a direção de arte das formigas: paleta mestra, planos de luz no alto à esquerda, facetas, lascas e fissuras legíveis, transparência binária e contorno escuro na silhueta.

Cada PNG tem 16 vezes a largura e a altura anteriores e é importado a **16 PPU**. O tamanho físico continua de **8×5 a 76×62 células**, conforme a variante; a operária mede 3×2 células. As pedras continuam maiores que as formigas. Nomes, GUIDs e pivôs normalizados são preservados; o PPU é alterado intencionalmente de 6 para 16.

`SceneryStamp.FromSprite` usa o desenho completo para renderizar e uma máscara de 6 px/célula para apoio, espaçamento e navegação. `SurfaceSceneryView` desenha as pedras como sprites separados atrás do terreno, sem o shader que subdivide as células do cenário legado. A textura de toda a superfície não é ampliada. As demais famílias de cenário permanecem como estão.

## Rollback

[`snapshot-pebbles.ps1`](../Source~/Pebbles/snapshot-pebbles.ps1) guarda cópias das 14 pedras em `Source~/Pebbles/Snapshots/<nome>`. A restauração copia o snapshot para `Refined/` e para os PNGs do jogo, sem mexer nos `.meta`. Depois, reimporte na Unity.

A política de execução do Windows bloqueia o script se ele for chamado direto. Use `-ExecutionPolicy Bypass`:

```powershell
powershell -ExecutionPolicy Bypass -File .\snapshot-pebbles.ps1 -Save -Name <nome>
powershell -ExecutionPolicy Bypass -File .\snapshot-pebbles.ps1 -Restore -Name <nome>
powershell -ExecutionPolicy Bypass -File .\snapshot-pebbles.ps1 -List
```

Um snapshot com nome já usado é recusado, para não sobrescrever um ponto de restauração.

| Snapshot | Estado |
|---|---|
| `2026-10-22_antes-textura` | Pebble_05_2x do Qwen, sem textura, e as outras 13 a 16 px por célula |
| `2026-10-22_textura-v1` | Igual ao anterior, mas com a Pebble_05_2x texturizada por script (descartada) |
| `2026-10-22_qwen-textura-s23` | Pebble_05_2x com o Qwen convertido em pontilhado de 1 px (descartada: vira chiado no jogo) |
| `2026-10-22_qwen-guiada-2px` | Só a Pebble_05_2x com marcas de 2 px guiadas pelo Qwen |
| `2026-10-22_qwen-guiada-2px-todas` | As 14 pedras com marcas de 2 px guiadas pelo Qwen (versão em uso) |

## Textura (2026-10-22)

**Todas as 14 pedras** usam marcas de 2 px guiadas pelo Qwen (seed 23, mesmo prompt), com os parâmetros aprovados na Pebble_05_2x. O desenvolvedor aprovou a Pebble_05_2x no jogo e pediu para estender a técnica às outras. Para o Qwen ver os pixels, cada pedra lisa foi ampliada por um fator inteiro até perto de 1024 px de lado (×1 a ×8). As saídas brutas estão em `../Source~/Pebbles/Generated/Pebble_NN_qwen_tex_s23.png`.

As 13 pedras originais já foram desenhadas com 16 px por célula e têm detalhe fino, então sobram poucas manchas lisas com 6 px de folga. Quantidade de marcas:
- Pebble_01: 617
- Pebble_05: 1.281
- Pebble_06: 838
- Pebble_07: 1.250
- Pebble_02, 03, 08 e 11: de 11 a 29
- Pebble_00, 00_2x, 04, 09 e 10: de 0 a 4, praticamente iguais

Uma folga de 2 px rendia mais marcas, mas manchava as bordas das faixas de cor, como na Pebble_07, e foi descartada na comparação na escala do jogo.

```bash
python -I qwen-guided-texture.py Snapshots/2026-10-22_antes-textura/Pebble_NN.png Generated/Pebble_NN_qwen_tex_s23.png Refined/Pebble_NN.png --pixel 2 --dark 0.08 --light 0.05
```

### Pebble_05_2x

**Versão em uso: marcas de 2 px guiadas pelo Qwen (seed 23)**, escolhida pelo desenvolvedor numa comparação na escala do jogo (≈12,8 px de tela por célula, filtro Point). Parte da pedra lisa do snapshot `antes-textura`, que fornece forma, cores e contorno. Da saída do Qwen, [`qwen-guided-texture.py`](../Source~/Pebbles/qwen-guided-texture.py) usa só o granulado: o brilho de cada ponto menos a média da vizinhança. Os 8% de blocos de 2 px com maior desvio para baixo recebem o tom vizinho mais escuro da mesma rampa, e os 5% com maior desvio para cima recebem o mais claro. As marcas ficam só no miolo das manchas, com 6 px de folga até a borda. Sem essa folga, as marcas desenhavam um contorno escuro em volta das manchas. Silhueta, contorno, rachaduras e paleta ficam iguais aos da pedra lisa: são 13.564 marcas.

```bash
python -I qwen-guided-texture.py Snapshots/2026-10-22_antes-textura/Pebble_05_2x.png Generated/Pebble_05_2x_qwen_tex_s23.png Refined/Pebble_05_2x.png --pixel 2 --dark 0.08 --light 0.05
```

Previews reduzidos enganam. A conversão anterior em pontilhado de 1 px parecia boa reduzida, mas virava chiado na escala real. Avalie sempre em 100% na escala do jogo.

**Geração no Qwen.** A entrada foi a pedra sem textura (snapshot `antes-textura`), reduzida a 608×496 sobre fundo branco. O Qwen Image 2.1 rodou em modo de edição, com `--steps 25 --resolution 1024`, pelo `H:\QWEN\tools\qwen_run.py`. Prompt:

> Edit <image1>: keep the exact same pixel art rock, with the exact same silhouette, outline, size, proportions, color layout, light planes, cracks, chips and embedded pebbles, all in the same positions. Only add a subtle, sparse surface texture inside the flat color areas, like weathered stone: tiny scattered specks and short fine streaks, each one just one tone lighter or darker than the area it sits on. Keep it light and even, so the stone no longer looks perfectly smooth but the shapes stay clean and readable. Same limited earthy palette, crisp pixel art on a single uniform pixel grid, hard edges, no gradients, no anti-aliasing, no blur, no noise or dithering. Plain white background.

A saída bruta, sem metadados, está em `../Source~/Pebbles/Generated/Pebble_05_2x_qwen_tex_s23.png`. O granulado do Qwen é mais sutil que a distância entre duas cores da paleta: o `pixel_snap_palette.py` o apagava, mesmo com `--min-cluster 1`. A seed 11 ficou fiel demais à pedra lisa: a textura sumia.

**Tentativas descartadas:**
- [`qwen-dither-snap.py`](../Source~/Pebbles/qwen-dither-snap.py) passa a saída do Qwen para a paleta com difusão de erro, em pontos de 1 px (snapshot `qwen-textura-s23`).
- [`texture-pebble.py`](../Source~/Pebbles/texture-pebble.py) espalha marcas aleatórias de 4 px, sem guia (snapshot `textura-v1`). As outras 13 pedras ainda não têm textura.

## Fontes e reexportação

- [Prompts e referências](../Source~/Pebbles/refinement-prompts.json), gerados com o ImageGen integrado.
- `../Source~/Pebbles/Generated/`: saídas originais da geração.
- `../Source~/Pebbles/Layouts/`: PNGs anteriores, para enquadramento e escala.
- `../Source~/Pebbles/Refined/`: masters finais na paleta.
- [Normalização](../Source~/Pebbles/normalize-refined.ps1): amostragem direta para a resolução final, paleta e alpha 0/255.
- [Reexportação](../Source~/Pebbles/bake-refined.ps1): valida os 14 masters antes de copiar os PNGs; não altera `.meta`.

**Pebble_05_2x (2026-10-21):** o master tinha artefatos da amostragem direta (blocos de tamanhos misturados, granulado, contorno grosso). Foi redesenhado com o Qwen Image 2.1 (seed 11) e limpo em `H:\QWEN` (`tools/pixel_snap_palette.py`): mesma silhueta e paleta, manchas chapadas, contorno de 1 px em `1B1815` no lado iluminado. Pendência: o master é a ampliação 2× de uma versão de 608×496, então tem 8 px de arte por célula em vez de 16. O master anterior está em `H:\QWEN\assets\incrementant\pebbles\backup_refined_2026-10-21`. A `Pebble_05` (608×496) é outro desenho e não foi alterada.

Execute `bake-refined.ps1` para restaurar esta aparência. O exportador procedural de cenário sobrescreve o desenho e não deve ser usado para restaurar estas pedras. A pasta `Source~` é ignorada pela Unity.

## Validação

Compilação no Editor sem erros e dois testes EditMode direcionados aprovados em 2026-10-21: tamanho físico/pivô e preservação do fundo caminhável sem pintar uma cópia grosseira sob o sprite. Os 14 sprites foram importados e conferidos na Unity: 3.158.528 pixels, paleta de 12 cores da paleta mestra, alpha binário, dimensões, Point, ausência de mipmaps/compressão, leitura, 16 PPU, pivôs e máscaras no tamanho físico esperado. Os 14 GUIDs e pivôs normalizados foram comparados com a cópia anterior; prancha antes/depois inspecionada. Console sem erros após a compilação final. Aprovação visual do desenvolvedor e conferência em Play Mode pendentes. Nenhuma build gerada.

[Prancha com comparação e escala da operária](../../../../../Docs/02_World_Art_UI/Pedras_refinadas_2026-10-21.png).
