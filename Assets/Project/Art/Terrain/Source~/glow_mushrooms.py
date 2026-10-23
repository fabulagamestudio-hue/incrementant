"""Desenha os dois cogumelos luminosos no TerrainAtlas.png (2026-10-23), 8x8 texels por célula, só cores da paleta mestra.

- linha 17 (GlowMushroom): o cogumelo dos pulgões, permanente (chapéu dourado com pontos claros, caule creme);
- última linha (CellTileArt.SoilGlowRow): o cogumelo de terra, passageiro (chapéu em tons de terra, brilho fraco, torrões
  na base). Se o PNG ainda tem as 59 linhas antigas, ela é acrescentada em cima (a linha 0 fica embaixo); rodar de novo só redesenha.
Cada linha repete o mesmo desenho a cada célula (como as ferramentas e armas), então o tile de 64 x 64 é 8 x 8 cópias.
Uso: python -I glow_mushrooms.py <MasterPalette.hex> <TerrainAtlas.png> [prévia.png]
"""
import sys

import numpy as np
from PIL import Image

TILE = 64
CELL = 8
GLOW_ROW = 17
OLD_ROWS = 59  # linhas do atlas antes da linha do cogumelo de terra
BG = 1

# Legenda do desenho: índices da paleta mestra.
APHID = dict(m=13, l=16, d=12, e=11, s=15, t=14)
SOIL = dict(c=6, l=7, d=5, e=4, s=14, t=5, x=3, y=9)
APHID_ART = ["........",
             "..mmmm..",
             ".mlmmmd.",
             "mmmmlmmd",
             "edddddde",
             "...st...",
             "...st...",
             "..sttt.."]
SOIL_ART = ["........",
            "..cccc..",
            ".cldccd.",
            "ccccldcd",
            "edddddde",
            "...st...",
            "..xsty..",
            ".xyxtyx."]


def load_palette(path):
    return np.array([[int(l.strip()[i:i + 2], 16) for i in (0, 2, 4)] for l in open(path) if l.strip()], np.uint8)


def tile(pal, art, colors):
    cell = np.array([[pal[BG] if ch == '.' else pal[colors[ch]] for ch in row] for row in art], np.uint8)
    return np.tile(cell, (TILE // CELL, TILE // CELL, 1))


def row_slice(row, rows):
    top = TILE * (rows - 1 - row)  # linha 0 embaixo no PNG
    return slice(top, top + TILE)


if __name__ == '__main__':
    pal = load_palette(sys.argv[1])
    atlas = np.array(Image.open(sys.argv[2]).convert('RGB'))
    rows = atlas.shape[0] // TILE
    if rows * TILE != atlas.shape[0] or atlas.shape[1] != TILE:
        raise SystemExit(f'tamanho inesperado: {atlas.shape}')
    if rows == OLD_ROWS:
        atlas = np.concatenate([np.zeros((TILE, TILE, 3), np.uint8), atlas], axis=0)
        rows += 1
    elif rows != OLD_ROWS + 1:
        raise SystemExit(f'esperava {OLD_ROWS} ou {OLD_ROWS + 1} linhas, achei {rows}')
    soil_row = rows - 1
    atlas[row_slice(GLOW_ROW, rows)] = tile(pal, APHID_ART, APHID)
    atlas[row_slice(soil_row, rows)] = tile(pal, SOIL_ART, SOIL)
    palset = {tuple(int(v) for v in c) for c in pal}
    assert all(tuple(int(v) for v in c) in palset for c in np.unique(atlas.reshape(-1, 3), axis=0)), 'cor fora da paleta'
    Image.fromarray(atlas).save(sys.argv[2])
    if len(sys.argv) > 3:
        both = np.concatenate([atlas[row_slice(GLOW_ROW, rows)], atlas[row_slice(soil_row, rows)]], axis=1)
        Image.fromarray(both).resize((both.shape[1] * 4, both.shape[0] * 4), Image.NEAREST).save(sys.argv[3])
    print('ok', atlas.shape, 'linhas', rows)
