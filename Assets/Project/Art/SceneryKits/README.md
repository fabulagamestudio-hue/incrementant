# Kits de cenário

Peças a 8 texels por célula para montar por código os elementos de cenário maiores que a tela, em vez de desenhar cada um como um PNG inteiro (2026-10-23, branch `experimento/arte-8x8`). Hoje só o graveto, que substitui os PNGs de `../Scenery/Twigs`. Como o kit é montado e posto no jogo: `Scripts/Runtime/AntFarm/AntFarmGuide.md`, "Kit do graveto".

Importação (`SceneryKitImporter`, tudo nesta pasta): textura comum, Point, sem compressão nem mipmap, Read/Write e Repeat. As peças não têm contorno: o montador põe o contorno preto no elemento inteiro.

| Pasta / arquivo | Conteúdo |
|---|---|
| `Twig/TwigKit.asset` | Kit do graveto (`TwigKit`): referências das peças e faixas das medidas. Criado e ligado na cena ativa por **Incrementant/Setup/Criar kit do graveto** (ligado nas cenas AntFarm e MainMenu) |
| `Twig/bark.png` | 128 × 64, casca repetível: fibras na horizontal (ao longo do graveto) e fendas. O sombreado do cilindro usa as cores dela ordenadas por brilho |
| `Twig/cut_end.png` | 48 × 112, face do corte com anéis: a altura é a espessura da ponta grossa (14 células) |
| `Twig/broken_end.png` | 48 × 80, lascas da ponta fina, apontando para a direita: a altura é a espessura ali (10 células) |
| `Twig/branch_tip.png` | 40 × 32, lascas da ponta do galho, apontando para cima: a largura é a espessura do galho (5 células). O montador a lê girada, na direção do galho |
| `Twig/knot_0..2.png` | Nós da casca, carimbados sobre o tronco |
| `Twig/Source~/twig_kit.py` | Gerador das peças na paleta mestra (pintar à mão também vale, mantendo tamanhos e paleta) |

## Como refazer

```bash
python -I "Assets/Project/Art/SceneryKits/Twig/Source~/twig_kit.py" Assets/Project/Art/Palette/MasterPalette.hex Assets/Project/Art/SceneryKits/Twig
```

Precisa de Python com `numpy` e `Pillow`. Trocar a altura de `cut_end.png` ou `broken_end.png` muda a espessura do tronco; a largura de `branch_tip.png`, a do galho.
