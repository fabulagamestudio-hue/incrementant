"""Sprites 8x8 dos itens de metal (REQ-071): barra, ferramenta, arma e armadura em cobre, ferro e ouro.

O mesmo desenho das camadas vestidas (equip_layers.py), como objeto solto: o item na boca da formiga, guardado na grade e
nos armazéns. Cada forma é desenhada em 6x6 e o contorno de 1 texel (1B1815) é gerado em volta, como nos Item_*Ore.
Uso: python equip_items.py [--sheet caminho.png]   (grava Art/Ants/Item_<Metal><Tipo>.png)
"""
import os, sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ANTS = os.path.dirname(HERE) + "/"
OUTLINE = (0x1b, 0x18, 0x15)
WOOD = ((0x6b, 0x4a, 0x2b), (0x9a, 0x6b, 0x3c))
METALS = {  # nome do arquivo, escuro, médio, claro (as mesmas rampas de equip_layers.py)
    "Copper": ((0x7a, 0x34, 0x12), (0xc8, 0x74, 0x3a), (0xf0, 0xb2, 0x7a)),
    "Iron": ((0x3d, 0x4a, 0x5c), (0x7f, 0x93, 0xa8), (0xc5, 0xd3, 0xe0)),
    "Gold": ((0x8a, 0x64, 0x10), (0xe8, 0xb8, 0x20), (0xff, 0xf0, 0x8a)),
}
# l claro, m médio, d escuro, w madeira escura, h madeira clara, k vão escuro (viseira). 6x6, dentro do 8x8.
SHAPES = {
    "Tool": [
        ".lmmd.",
        "l.mwdd",
        "..hwd.",
        "..hw..",
        ".hw...",
        "hw....",
    ],
    "Weapon": [
        "....ll",
        "...lmd",
        "..lmd.",
        "wlmd..",
        ".wd...",
        "w.w...",
    ],
    "Armor": [
        ".lllm.",
        "llmmmd",
        "lmmmdd",
        "lmkkmd",
        "lm..md",
        ".d..d.",
    ],
    "Bar": [  # lingote visto de frente: topo claro, corpo, base na sombra
        "......",
        ".llll.",
        "lmmmmd",
        "mmmmdd",
        ".dddd.",
        "......",
    ],
}

def build(shape, ramp):
    pal = {"d": ramp[0], "m": ramp[1], "l": ramp[2], "w": WOOD[0], "h": WOOD[1], "k": OUTLINE}
    im = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    fill = set()
    for y, row in enumerate(shape):
        for x, c in enumerate(row):
            if c in pal:
                im.putpixel((x + 1, y + 1), pal[c] + (255,))
                fill.add((x + 1, y + 1))
    for (x, y) in list(fill):
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                p = (x + dx, y + dy)
                if p not in fill and 0 <= p[0] < 8 and 0 <= p[1] < 8:
                    im.putpixel(p, OUTLINE + (255,))
    return im

def main():
    items = {}
    for metal, ramp in METALS.items():
        for kind, shape in SHAPES.items():
            im = build(shape, ramp)
            items[(metal, kind)] = im
            im.save(ANTS + "Item_%s%s.png" % (metal, kind))
    if "--sheet" in sys.argv:
        S = 12
        sheet = Image.new("RGBA", (len(SHAPES) * 8 * S + 20 * len(SHAPES), len(METALS) * 8 * S + 10 * len(METALS)), (70, 58, 44, 255))
        for r, metal in enumerate(METALS):
            for c, kind in enumerate(SHAPES):
                sheet.alpha_composite(items[(metal, kind)].resize((8 * S, 8 * S), Image.NEAREST), (10 + c * (8 * S + 20), 5 + r * (8 * S + 10)))
        sheet.save(sys.argv[sys.argv.index("--sheet") + 1])

main()
