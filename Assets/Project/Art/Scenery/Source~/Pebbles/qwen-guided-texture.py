"""Textura de pedra guiada pela saída do Qwen, em marcas do tamanho certo para o jogo.

A pedra lisa dá forma, cores e contorno; a imagem do Qwen só diz ONDE a pedra é mais clara
ou mais escura que a vizinhança (o granulado dela). Cada marca de --pixel px vira o tom
vizinho da mesma rampa da paleta (mais claro ou mais escuro), só no miolo das manchas:
bordas, contorno e silhueta não mudam e nenhuma cor sai da paleta.

Uso:
  python -I qwen-guided-texture.py lisa.png qwen.png saida.png --pixel 2 --dark 0.07 --light 0.04
  --dark / --light: fração das marcas candidatas que escurecem / clareiam (as de maior desvio).
"""
import argparse

import numpy as np
from PIL import Image, ImageFilter

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
    ap.add_argument("smooth")
    ap.add_argument("guide")
    ap.add_argument("dst")
    ap.add_argument("--pixel", type=int, default=2)
    ap.add_argument("--dark", type=float, default=0.07)
    ap.add_argument("--light", type=float, default=0.04)
    ap.add_argument("--blur", type=float, default=12, help="raio da vizinhança do guia, em px do PNG")
    ap.add_argument("--margin", type=int, default=6, help="folga até a borda da mancha, em px do PNG")
    a = ap.parse_args()

    img = np.array(Image.open(a.smooth).convert("RGBA"))
    H, W = img.shape[:2]
    g = Image.open(a.guide).convert("L").resize((W, H), Image.LANCZOS)
    detail = np.asarray(g, float) - np.asarray(g.filter(ImageFilter.GaussianBlur(a.blur)), float)

    k = a.pixel
    h, w = H // k, W // k
    dev = detail[:h * k, :w * k].reshape(h, k, w, k).mean(axis=(1, 3))
    nb = neighbors_map()

    # Candidatas: blocos no miolo de uma mancha, com --margin px de folga até a borda dela.
    # Sem folga, o guia (que muda bruscamente nas bordas) desenha um contorno em volta das manchas.
    m = a.margin
    cand = np.zeros((h, w), bool)
    for y in range(h):
        for x in range(w):
            y0, x0 = y * k - m, x * k - m
            if y0 < 0 or x0 < 0 or y0 + k + 2 * m > H or x0 + k + 2 * m > W:
                continue
            blk = img[y0:y0 + k + 2 * m, x0:x0 + k + 2 * m]
            c = img[y * k, x * k, :3]
            if (blk[..., 3] > 0).all() and (blk[..., :3] == c).all() and tuple(c.tolist()) in nb:
                cand[y, x] = True
    vals = dev[cand]
    if vals.size == 0:
        # pedra sem mancha lisa grande o bastante: sai igual à entrada
        Image.fromarray(img).save(a.dst)
        print(f"0 marcas de {k} px (nenhuma mancha com {m} px de folga)")
        return
    lo =np.quantile(vals, a.dark) if a.dark > 0 else -np.inf
    hi = np.quantile(vals, 1 - a.light) if a.light > 0 else np.inf

    out = img.copy()
    n = 0
    for y, x in zip(*np.where(cand)):
        d = dev[y, x]
        if lo < d < hi:
            continue
        darker, lighter = nb[tuple(img[y * k, x * k, :3].tolist())]
        t = darker if d <= lo else lighter
        if t is None:
            continue
        out[y * k:(y + 1) * k, x * k:(x + 1) * k, :3] = t
        n += 1
    Image.fromarray(out).save(a.dst)
    print(f"{n} marcas de {k} px")


if __name__ == "__main__":
    main()
