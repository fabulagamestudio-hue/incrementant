"""Peças do kit do graveto a 8 texels por célula (piloto de 2026-10-23, branch experimento/arte-8x8).

O graveto não é um PNG único: o TwigKitBuilder monta cada um por código a partir destas peças, no tamanho do mundo do
graveto antigo (200 a 360 células de comprimento). Todas as cores são da paleta mestra; o contorno preto é posto pelo
montador no graveto inteiro, então as peças não têm contorno.

  bark.png         128 x 64, repete nos dois sentidos: casca com fibras ao longo do graveto e fendas escuras. O montador
                   sombreia em degraus da própria rampa (as cores da casca ordenadas por brilho), como um cilindro.
  cut_end.png      48 x 112: face do corte com anéis, na ponta grossa (112 texels = 14 células, a espessura da base).
  broken_end.png   48 x 80: lascas da ponta partida, na ponta fina (80 texels = 10 células).
  branch_tip.png   40 x 32: lascas da ponta do galho, apontando para cima (40 texels = 5 células).
  knot_0..2.png    nós da casca, carimbados sobre o tronco.

Uso: python -I twig_kit.py <MasterPalette.hex> <pasta de saída>
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def load_palette(path):
    cols = [line.strip() for line in Path(path).read_text().split() if line.strip()]
    return np.array([[int(c[i:i + 2], 16) for i in (0, 2, 4)] for c in cols], np.uint8)


def value_noise(w, h, px, py, seed):
    """Ruído de valor periódico em w x h, com px x py nós (divisores de w e h), em [0, 1]."""
    rng = np.random.default_rng(seed)
    grid = rng.random((py, px))
    y = np.arange(h)[:, None] * py / h
    x = np.arange(w)[None, :] * px / w
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    tx, ty = x - x0, y - y0
    tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    a = grid[y0 % py, x0 % px]
    b = grid[y0 % py, (x0 + 1) % px]
    c = grid[(y0 + 1) % py, x0 % px]
    d = grid[(y0 + 1) % py, (x0 + 1) % px]
    return (a * (1 - tx) + b * tx) * (1 - ty) + (c * (1 - tx) + d * tx) * ty


def to_rgba(index, pal):
    """Mapa de índices da paleta (-1 = transparente) para RGBA."""
    out = np.zeros(index.shape + (4,), np.uint8)
    mask = index >= 0
    out[mask, :3] = pal[index[mask]]
    out[mask, 3] = 255
    return out


BARK = [1, 2, 3, 9, 10, 11]          # rampa da casca, do escuro ao claro
WOOD = [5, 6, 7, 15]                  # madeira por dentro, do escuro ao claro


def bark(pal):
    w, h = 128, 64
    # Fibras: variação forte entre linhas (através do graveto) e lenta ao longo dele.
    streak = 0.6 * value_noise(w, h, 4, 16, 11) + 0.25 * value_noise(w, h, 16, 32, 12) + 0.15 * value_noise(w, h, 32, 64, 14)
    level = np.clip((streak * 4.2).astype(int), 0, 3) + 1          # índices 1..4 da rampa; o claro (5) fica para realces
    idx = np.array(BARK)[np.clip(level, 0, len(BARK) - 1)]
    # Fendas: traços escuros ao longo do graveto, de 6 a 28 texels, 1 texel de altura, às vezes mudando de linha.
    rng = np.random.default_rng(13)
    for _ in range(46):
        x, y, length = rng.integers(0, w), rng.integers(0, h), rng.integers(6, 29)
        for i in range(length):
            idx[y % h, (x + i) % w] = BARK[0] if i % 7 else BARK[1]
            if rng.random() < 0.12:
                y += rng.choice([-1, 1])
    # Pontinhos claros (casca levantada).
    for _ in range(20):
        x, y = rng.integers(0, w), rng.integers(0, h)
        idx[y, x] = BARK[5]
    return to_rgba(idx, pal)


def cut_end(pal):
    w, h = 48, 112
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy, rx, ry = w / 2 - 0.5, h / 2 - 0.5, w / 2, h / 2
    d = np.sqrt(((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2)
    idx = np.full((h, w), -1)
    inside = d <= 1.0
    # Anéis: distância deformada por ruído, alternando dois tons da madeira; casca na borda.
    wobble = 0.05 * (value_noise(w, h, 4, 8, 21) - 0.5)
    rings = ((d + wobble) * 7).astype(int)
    idx[inside] = np.where(rings[inside] % 2 == 0, WOOD[2], WOOD[1])
    idx[inside & (d < 0.12)] = WOOD[0]
    # Rachadura do centro para a borda.
    crack = inside & (np.abs((yy - cy) * 0.18 - (xx - cx) * 0.9) < 0.6) & (xx > cx)
    idx[crack] = BARK[2]
    # Luz de cima à esquerda na face.
    light = inside & (d > 0.25) & (d < 0.8) & ((xx - cx) / rx + (yy - cy) / ry < -0.7)
    idx[light & (idx == WOOD[2])] = WOOD[3]
    rim = inside & (d > 0.86)
    idx[rim] = BARK[3]
    idx[inside & (d > 0.94)] = BARK[1]
    return to_rgba(idx, pal)


def splinters(pal, w, h, seed, pointing_up=False):
    """Lascas: a base (lado do tronco) cheia, pontas irregulares saindo; casca por fora e madeira clara exposta."""
    rng = np.random.default_rng(seed)
    idx = np.full((h, w), -1)
    teeth = []
    pos = 0
    while pos < h:
        size = int(rng.integers(5, 12))
        teeth.append((pos, min(h, pos + size), int(rng.integers(w // 3, w))))
        pos += size
    for start, end, reach in teeth:
        mid = (start + end - 1) / 2
        for y in range(start, end):
            # Ponta afinando: as linhas do meio do dente vão mais longe.
            frac = 1 - abs(y - mid) / max(1.0, (end - start) / 2)
            length = int(round(reach * (0.35 + 0.65 * frac)))
            for x in range(length):
                inner = 0.2 < y / h < 0.8
                # Fibra da madeira ao longo da lasca: cada linha com o seu tom, trocando em trechos.
                grain = WOOD[1] if (y * 7 + x // 9) % 6 == 0 else (WOOD[3] if (y * 5 + x // 11) % 9 == 0 else WOOD[2])
                idx[y, x] = grain if inner else BARK[3]
            if length > 0:
                idx[y, length - 1] = 12 if rng.random() < 0.5 else 13   # ponta clara e amarelada, como no graveto antigo
    # Sombra na base das lascas de baixo.
    idx[int(h * 0.8):, :4][idx[int(h * 0.8):, :4] >= 0] = BARK[1]
    if pointing_up:
        idx = np.rot90(idx, 1)   # +x (ponta) vira para cima; a linha 0 do PNG é o topo
    return to_rgba(idx, pal)


def knot(pal, w, h, seed):
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = w / 2 - 0.5 + rng.uniform(-1, 1), h / 2 - 0.5
    d = np.sqrt(((xx - cx) / (w / 2)) ** 2 + ((yy - cy) / (h / 2)) ** 2)
    idx = np.full((h, w), -1)
    idx[d <= 1.0] = BARK[2]
    idx[d <= 0.82] = BARK[4]
    idx[d <= 0.62] = BARK[1]
    idx[d <= 0.42] = BARK[3]
    idx[d <= 0.2] = BARK[0]
    # Luz em cima à esquerda do calo.
    hi = (d > 0.62) & (d <= 0.82) & ((xx - cx) + (yy - cy) < -w * 0.15)
    idx[hi] = BARK[5]
    return to_rgba(idx, pal)


def main():
    pal = load_palette(sys.argv[1])
    out = Path(sys.argv[2])
    out.mkdir(parents=True, exist_ok=True)
    pieces = {
        "bark.png": bark(pal),
        "cut_end.png": cut_end(pal),
        "broken_end.png": splinters(pal, 48, 80, 31),
        "branch_tip.png": splinters(pal, 32, 40, 37, pointing_up=True),
        "knot_0.png": knot(pal, 32, 28, 41),
        "knot_1.png": knot(pal, 24, 22, 42),
        "knot_2.png": knot(pal, 40, 30, 43),
    }
    for name, rgba in pieces.items():
        Image.fromarray(rgba).save(out / name)
        print(name, rgba.shape[1], "x", rgba.shape[0])


if __name__ == "__main__":
    main()
