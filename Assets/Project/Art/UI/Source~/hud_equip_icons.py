"""Ícones da HUD da forja e dos armazéns (REQ-071), 40x40, no estilo dos de Sprites/icons: contorno marrom-escuro de 2 px,
luz no canto superior esquerdo, alpha 0/255.

Cada forma é um polígono em coordenadas de 40x40; o brilho e a sombra saem do deslocamento da própria máscara (bisel).
Uso: python hud_equip_icons.py [--sheet caminho.png]   (grava Art/UI/Sprites/icons/<nome>.png)
"""
import math, os, sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.dirname(HERE) + "/Sprites/icons/"
N, SS = 40, 8
OUTLINE = (0x1f, 0x08, 0x01)
def rgb(h): return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
STEEL = (rgb("5a5d68"), rgb("8c8e96"), rgb("c4c6cc"))
WOOD = (rgb("7a4018"), rgb("b2682e"), rgb("dc9650"))
STONE = (rgb("4a4850"), rgb("77747c"), rgb("a5a2a8"))
COPPER = (rgb("8a4a22"), rgb("c8743a"), rgb("f0b27a"))
GOLD = (rgb("b98310"), rgb("f4b815"), rgb("fff08a"))
IRON = (rgb("7a2410"), rgb("b8402a"), rgb("e07050"))
FIRE = (rgb("c8400c"), rgb("f08a1c"), rgb("ffe060"))
DARK = (rgb("1f0801"), rgb("2a0d03"), rgb("3a1a08"))

def mask(poly):
    big = Image.new("L", (N * SS, N * SS), 0)
    ImageDraw.Draw(big).polygon([(x * SS, y * SS) for x, y in poly], fill=255)
    small = big.resize((N, N), Image.BOX)
    return small.point(lambda v: 255 if v >= 128 else 0)

def line(p0, p1, w):
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    d = math.hypot(dx, dy)
    nx, ny = -dy / d * w / 2, dx / d * w / 2
    return [(p0[0] + nx, p0[1] + ny), (p1[0] + nx, p1[1] + ny), (p1[0] - nx, p1[1] - ny), (p0[0] - nx, p0[1] - ny)]

def blob(cx, cy, r, sides=7, turn=0.0):
    return [(cx + r * math.cos(turn + i * 2 * math.pi / sides), cy + r * math.sin(turn + i * 2 * math.pi / sides)) for i in range(sides)]

class Icon:
    def __init__(self):
        self.color = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        self.fill = Image.new("L", (N, N), 0)

    def shape(self, poly, ramp, bevel=1):
        m = mask(poly)
        dark, mid, light = ramp
        shade = ImageChops.subtract(m, ImageChops.offset(m, -bevel, -bevel))   # borda de baixo e da direita
        glow = ImageChops.subtract(m, ImageChops.offset(m, bevel, bevel))     # borda de cima e da esquerda
        for layer, col in ((m, mid), (shade, dark), (glow, light)):
            self.color.paste(Image.new("RGBA", (N, N), col + (255,)), (0, 0), layer)
        self.fill = ImageChops.lighter(self.fill, m)
        return self

    def done(self):
        # contorno de 2 px, cantos arredondados: dilatação circular da máscara
        grown = self.fill.filter(ImageFilter.MaxFilter(5)).point(lambda v: 255 if v else 0)
        k = Image.new("L", (N, N), 0)
        for y in range(N):
            for x in range(N):
                if grown.getpixel((x, y)) and not self.fill.getpixel((x, y)):
                    near = any(self.fill.getpixel((x + dx, y + dy)) for dx in range(-2, 3) for dy in range(-2, 3)
                               if 0 <= x + dx < N and 0 <= y + dy < N and dx * dx + dy * dy <= 5)
                    if near:
                        k.putpixel((x, y), 255)
        out = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        out.paste(Image.new("RGBA", (N, N), OUTLINE + (255,)), (0, 0), k)
        out.alpha_composite(self.color)
        return out

def ore():
    i = Icon()
    i.shape([(7, 31), (6, 21), (11, 13), (20, 10), (29, 13), (34, 21), (33, 31), (27, 35), (12, 35)], STONE)
    i.shape(blob(14, 21, 4.2, 6, 0.3), COPPER)
    i.shape(blob(24, 17, 4.4, 6, 0.6), GOLD)
    i.shape(blob(28, 28, 3.6, 6, 0.1), IRON)
    i.shape(blob(15, 29, 3, 5, 0.4), COPPER)
    return i

def smelter():
    i = Icon()
    i.shape([(24, 15), (25, 6), (32, 6), (32, 15)], STONE)
    i.shape([(7, 35), (9, 15), (31, 15), (33, 35)], STONE)
    i.shape([(15, 35), (15, 26), (20, 21), (25, 26), (25, 35)], DARK, 0)
    i.shape([(17, 35), (18, 29), (20, 25), (22, 29), (23, 35)], FIRE)
    return i

def forge():
    i = Icon()
    i.shape([(12, 32), (31, 32), (31, 37), (12, 37)], WOOD)
    i.shape([(4, 13), (34, 13), (34, 18), (27, 19), (25, 25), (30, 27), (30, 32), (13, 32), (13, 27), (17, 25), (15, 19), (11, 18)], STEEL)
    i.shape(blob(30, 7, 3, 4, 0.78), GOLD, 0)
    return i

def armory():
    i = Icon()
    i.shape(line((8, 8), (31, 31), 3.6), WOOD)                       # cabo da picareta
    i.shape(line((4, 16), (16, 4), 4.2), STEEL)                      # cabeça da picareta
    i.shape(line((33, 6), (15, 24), 5), STEEL)                       # lâmina
    i.shape(line((11, 19), (20, 28), 3), COPPER)                     # guarda
    i.shape(line((14, 25), (7, 32), 3.2), WOOD)                      # empunhadura
    return i

def wardrobe():
    i = Icon()
    i.shape([(17, 10), (20, 3), (23, 10)], IRON)
    i.shape([(7, 31), (7, 20), (10, 13), (16, 9), (24, 9), (30, 13), (33, 20), (33, 31), (27, 31), (27, 25), (13, 25), (13, 31)], STEEL)
    i.shape([(13, 19), (27, 19), (27, 23), (13, 23)], DARK, 0)
    return i

ICONS = {"ore": ore, "smelter": smelter, "forge": forge, "armory": armory, "wardrobe": wardrobe}

def main():
    made = {}
    for name, f in ICONS.items():
        im = f().done()
        made[name] = im
        im.save(OUT + "equip_" + name + ".png")
    if "--sheet" in sys.argv:
        names = ["shovel", "crate", "mushroom"]
        S = 6
        sheet = Image.new("RGBA", ((len(made) + len(names)) * (N * S + 10) + 10, N * S + 20), (70, 58, 44, 255))
        x = 10
        for n in names:
            im = Image.open(OUT + n + ".png").convert("RGBA")
            sheet.alpha_composite(im.resize((im.width * S, im.height * S), Image.NEAREST), (x, 10))
            x += N * S + 10
        for n, im in made.items():
            sheet.alpha_composite(im.resize((N * S, N * S), Image.NEAREST), (x, 10))
            x += N * S + 10
        sheet.save(sys.argv[sys.argv.index("--sheet") + 1])

main()
