"""Remapeia os PNGs de Art/Scenery para a paleta mestra (etapa 10 do documento 40).

Cada pixel opaco vai para a cor da paleta mais próxima em OKLab; o alfa, o tamanho e o arquivo .meta (pivô) não mudam.
As 29 cores da paleta que vieram do cenário saem iguais; as demais caem no tom vizinho da mesma rampa.

Uso: python -I palette_remap.py <MasterPalette.hex> <pasta Art/Scenery> [--check]
Com --check só conta quantos pixels mudariam, sem gravar.
"""
import glob
import os
import sys

import numpy as np
from PIL import Image


def oklab(rgb):
    c = np.asarray(rgb, float) / 255.0
    lin = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929],
                   [0.2119034982, 0.6806995451, 0.1073969566],
                   [0.0883024619, 0.2817188376, 0.6299787005]])
    m2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468],
                   [1.9779984951, -2.4285922050, 0.4505937099],
                   [0.0259040371, 0.7827717662, -0.8086757660]])
    return np.cbrt(lin @ m1.T) @ m2.T


def main():
    hexes = [l.strip() for l in open(sys.argv[1]) if l.strip()]
    pal = np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in hexes], float)
    pal_lab = oklab(pal)
    check = '--check' in sys.argv
    total = changed = 0
    for path in sorted(glob.glob(os.path.join(sys.argv[2], '*', '*.png'))):
        img = np.array(Image.open(path).convert('RGBA'))
        flat = img.reshape(-1, 4)
        opaque = flat[:, 3] > 0
        rgb = flat[opaque, :3].astype(float)
        nearest = ((oklab(rgb)[:, None, :] - pal_lab[None]) ** 2).sum(-1).argmin(1)
        new = pal[nearest].astype(np.uint8)
        diff = (new != flat[opaque, :3]).any(1)
        total += int(opaque.sum())
        changed += int(diff.sum())
        if not check and diff.any():
            flat[opaque, :3] = new
            Image.fromarray(flat.reshape(img.shape)).save(path)
    print('pixels opacos', total, 'mudaram', changed, '(só conferência)' if check else '')


if __name__ == '__main__':
    main()
