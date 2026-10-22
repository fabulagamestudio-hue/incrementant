"""Textura leve nas manchas chapadas de uma pedra, no estilo dos cogumelos.

Espalha pontos e riscos curtos de 1 pixel de arte com o tom vizinho da mesma rampa da
paleta (mais escuro ou mais claro), só no miolo das manchas: a borda das manchas, o
contorno e a silhueta ficam intactos. Determinístico pela seed; não sai da paleta.

Uso:
  python -I texture-pebble.py entrada.png saida.png --pixel 4 --density 0.025 --streak 0.5 --seed 1
  --pixel: lado da marca em px do PNG. Com 1 px de arte a textura vira granulado no jogo;
  na Pebble_05_2x (16 px por célula) o aprovado para teste foi 4 px, ¼ de célula.
"""
import argparse

import numpy as np
from PIL import Image

# Rampas da paleta das pedras, do escuro ao claro. 1B1815 (contorno) fica de fora.
RAMPS = [
    ["382414", "463123", "785A40", "9A7F5E", "B9986D", "CFB691", "E4D2AE"],
    ["504D55", "7D7676", "A59685", "BBB29F"],
]


def hexrgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def neighbors_map():
    m = {}
    for ramp in RAMPS:
        cols = [hexrgb(h) for h in ramp]
        for i, c in enumerate(cols):
            m[c] = (cols[i - 1] if i > 0 else None, cols[i + 1] if i + 1 < len(cols) else None)
    return m


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--pixel", type=int, default=1)
    ap.add_argument("--density", type=float, default=0.05)
    ap.add_argument("--dark", type=float, default=0.6, help="fração dos pontos que escurecem")
    ap.add_argument("--streak", type=float, default=0.35, help="chance de o ponto virar risco de 2 px")
    ap.add_argument("--seed", type=int, default=1)
    a = ap.parse_args()

    img = np.array(Image.open(a.src).convert("RGBA"))
    k = a.pixel
    h, w = img.shape[0] // k, img.shape[1] // k
    nb = neighbors_map()
    rng = np.random.default_rng(a.seed)

    def interior(y, x, c):
        # só no miolo: a marca e uma marca de folga em volta têm a mesma cor e são opacas
        if y < 1 or x < 1 or y >= h - 1 or x >= w - 1:
            return False
        blk = img[(y - 1) * k:(y + 2) * k, (x - 1) * k:(x + 2) * k]
        return bool((blk[..., 3] > 0).all() and (blk[..., :3] == c).all())

    # As marcas são pintadas sobre o original, que pode ter pixels de arte menores que --pixel.
    art = img[::k, ::k]
    out = img.copy()
    used = np.zeros((h, w), bool)
    for y in range(h):
        for x in range(w):
            if art[y, x, 3] == 0 or rng.random() >= a.density:
                continue
            c = tuple(art[y, x, :3].tolist())
            if c not in nb or not interior(y, x, art[y, x, :3]):
                continue
            darker, lighter = nb[c]
            t = darker if rng.random() < a.dark else lighter
            t = t or darker or lighter
            cells = [(y, x)]
            if rng.random() < a.streak:
                # riscos curtos, como as fibras do cogumelo; na diagonal da luz (alto à esquerda)
                dy, dx = [(0, 1), (1, 0), (1, 1)][rng.integers(3)]
                if interior(y + dy, x + dx, art[y, x, :3]):
                    cells.append((y + dy, x + dx))
            if any(used[p] for p in cells):
                continue
            for p in cells:
                out[p[0] * k:(p[0] + 1) * k, p[1] * k:(p[1] + 1) * k, :3] = t
                used[p] = True

    Image.fromarray(out).save(a.dst)
    print(f"{used.sum()} marcas de {k} px")


if __name__ == "__main__":
    main()
