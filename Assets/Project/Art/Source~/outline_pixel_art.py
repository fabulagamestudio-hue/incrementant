"""Contorno preto de 1 texel por dentro do sprite (experimento 8x8 de 2026-10-22, branch experimento/arte-8x8).

Pedido do desenvolvedor: "linha preta ao redor dos elementos", com o preto da paleta mestra (índice 0, #1b1815).
O texel opaco que encosta, nas 4 direções, num transparente ou na borda da imagem vira o preto; o tamanho do sprite não
muda e rodar de novo não altera nada (o mesmo teste do contorno da terra no shader TerrainTiles).

Uso: python -I outline_pixel_art.py entrada.png [saida.png]   (sem saída, sobrescreve a entrada)
"""
import sys

import numpy as np
from PIL import Image

OUTLINE = (0x1B, 0x18, 0x15)


def outline(rgba):
    opaque = rgba[:, :, 3] >= 128
    padded = np.pad(opaque, 1, constant_values=False)
    inner = padded[:-2, 1:-1] & padded[2:, 1:-1] & padded[1:-1, :-2] & padded[1:-1, 2:]
    edge = opaque & ~inner
    out = rgba.copy()
    out[edge, :3] = OUTLINE
    out[edge, 3] = 255
    return out, int(edge.sum())


def main():
    src = sys.argv[1]
    dst = sys.argv[2] if len(sys.argv) > 2 else src
    image = np.array(Image.open(src).convert("RGBA"))
    result, count = outline(image)
    Image.fromarray(result).save(dst)
    print(f"{src}: {count} texels de contorno")


if __name__ == "__main__":
    main()
