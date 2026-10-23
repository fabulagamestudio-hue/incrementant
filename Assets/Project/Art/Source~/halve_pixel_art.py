"""Reduz pixel art pela metade (experimento 8x8 de 2026-10-22, branch experimento/arte-8x8).

Cada bloco de 2x2 texels vira um texel, sem criar cor nova (a paleta mestra continua valendo):
- fica opaco se pelo menos 2 dos 4 texels forem opacos (alpha >= 128), para pernas e frestas de 1 texel não sumirem;
- a cor é a mais frequente entre os opacos; no empate, a mais escura (contornos, juntas e frestas sobrevivem).
A mesma regra está em C# em PixelArtFilters.Halve (sprites gerados em código).

Uso: python -I halve_pixel_art.py entrada.png [saida.png]   (sem saída, sobrescreve a entrada)
"""
import sys

import numpy as np
from PIL import Image


def halve(rgba):
    h, w = rgba.shape[:2]
    if h % 2 or w % 2:
        raise SystemExit(f"tamanho ímpar: {w}x{h}")
    # (h/2, w/2, 4 texels do bloco, RGBA)
    blocks = rgba.reshape(h // 2, 2, w // 2, 2, 4).transpose(0, 2, 1, 3, 4).reshape(h // 2, w // 2, 4, 4)
    rgb = blocks[..., :3].astype(np.int32)
    opaque = blocks[..., 3] >= 128
    key = (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2]
    # Quantos texels opacos do bloco têm a mesma cor de cada texel (0 nos transparentes).
    same = (key[..., :, None] == key[..., None, :]) & opaque[..., None, :]
    count = np.where(opaque, same.sum(-1), 0)
    luma = rgb @ np.array([299, 587, 114])
    # Mais frequente primeiro; no empate, o mais escuro (luma até 255000, então cabe abaixo de 1 unidade de contagem).
    score = count * 1_000_000 - luma
    score = np.where(opaque, score, -1)
    pick = score.argmax(-1)
    chosen = np.take_along_axis(blocks, pick[..., None, None], axis=2)[..., 0, :]
    out = np.zeros((h // 2, w // 2, 4), np.uint8)
    keep = opaque.sum(-1) >= 2
    out[keep, :3] = chosen[keep, :3]
    out[keep, 3] = 255
    return out


def main():
    src = sys.argv[1]
    dst = sys.argv[2] if len(sys.argv) > 2 else src
    image = Image.open(src).convert("RGBA")
    Image.fromarray(halve(np.array(image))).save(dst)
    print(f"{src}: {image.size[0]}x{image.size[1]} -> {image.size[0] // 2}x{image.size[1] // 2}")


if __name__ == "__main__":
    main()
