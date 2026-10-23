#!/usr/bin/env python
"""
Variantes de silhueta das formigas por papel (experimento de design, 2026-10-23).

Parte da operária (e da rainha) e acrescenta um traço próprio de cada papel: cabeção e mandíbulas do soldado, bunda de
ácido da cuspideira, folhas da jardineira, chapéu de palha da fazendeira, coroa e gáster enorme da rainha etc.
O corpo (cabeça, tórax, gáster) é igual nos quatro quadros de caminhada, só as patas mudam; por isso cada alteração é
desenhada uma vez por vista e aplicada nos quatro quadros.

É idempotente: lê sempre de Source~/Variants/Base (cópia dos PNGs originais da Operaria e da Rainha, criada na primeira
execução) e grava em Operaria/ (intacta), Soldado/, Coletora/ ... e Rainha/. NÃO use depois de bake-refined.ps1 sem rodar
este script de novo: o bake regrava as formigas sem as variantes.

  python -I Source~/ant_variants.py                 # grava os PNGs
  python -I Source~/ant_variants.py --sheet p.png   # grava e salva uma prancha de conferência
  python -I Source~/ant_variants.py --dry --sheet p.png   # só a prancha
"""
import argparse
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ANTS = HERE.parent
BASE = HERE / "Variants" / "Base"

OUT = (0x1B, 0x18, 0x15, 255)


def rgb(h):
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# (sombra, base, luz) de cada pasta, as mesmas do README.
RAMPS = {
    "Operaria": ("5C1408", "9E311E", "D3592A"),
    "Soldado": ("3A1A5C", "6F339E", "A06BD0"),
    "Coletora": ("32550E", "437122", "91B13B"),
    "Escavadora": ("823A10", "D6872F", "F4B815"),
    "Cuidadora": ("1F3E70", "346EB4", "6EAADC"),
    "Jardineira": ("6B1F45", "C2477F", "E889B4"),
    "Cortadeira": ("0F4F47", "1F9E8E", "6FD6C4"),
    "Cuspideira": ("638929", "B3CF51", "E2EA7A"),
    "Lixeira": ("9A9AA8", "E8E8EE", "FFFFFF"),
    "Fazendeira": ("0A2A9E", "0144F7", "5C8CFF"),
}
RAMPS = {k: tuple(rgb(c) for c in v) for k, v in RAMPS.items()}

GOLD = (rgb("8A5A10"), rgb("D9A21B"), rgb("FBE27A"))
STRAW = (rgb("9A7B3A"), rgb("D9B85C"), rgb("F2DC93"))
LEAF = (rgb("32550E"), rgb("437122"), rgb("91B13B"))
SACK = (rgb("4A3A2A"), rgb("7A6448"), rgb("A08B64"))
CREAM = (rgb("B8AC96"), rgb("EDE3CF"), rgb("FFFFFF"))
RED = rgb("D3322A")
GEM = rgb("D3322A")
GEM_LIGHT = rgb("F58A7A")
BRASS = rgb("C99A2E")
HAT_BAND = rgb("7B4F2E")  # fita do chapéu da fazendeira: marrom, independente da cor do corpo


class Cv:
    """Tela RGBA (y para baixo) com as ferramentas de desenho de pixel art."""

    def __init__(self, image):
        self.w, self.h = image.size
        self.px = [[image.getpixel((x, y)) for x in range(self.w)] for y in range(self.h)]

    def get(self, x, y):
        return self.px[y][x] if 0 <= x < self.w and 0 <= y < self.h else (0, 0, 0, 0)

    def put(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y][x] = c

    def clear(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, (0, 0, 0, 0))

    def line(self, x0, y0, x1, y1, c):
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.put(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def pat(self, x, y, rows, key):
        """Pinta o desenho em ASCII com o canto superior esquerdo em (x, y); '.' deixa como está, '_' apaga."""
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch == ".":
                    continue
                self.put(x + i, y + j, (0, 0, 0, 0) if ch == "_" else key[ch])

    def blob(self, cx, cy, rx, ry, ramp, ring=True, ring_overwrite=True, shade=True):
        """Elipse sombreada (luz em cima à esquerda) com contorno de 1 px na cor da paleta."""
        dark, base, light = ramp
        mask = set()
        for y in range(self.h):
            for x in range(self.w):
                u, v = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                if u * u + v * v <= 1.0:
                    mask.add((x, y))
        for x, y in mask:
            u, v = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
            c = base
            if shade:
                if u + v < -0.8:
                    c = light
                elif v > 0.4 or u + v > 0.9:
                    c = dark
            self.put(x, y, c)
        if ring:
            for x, y in list(mask):
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if (nx, ny) in mask:
                        continue
                    if ring_overwrite or self.get(nx, ny)[3] == 0:
                        self.put(nx, ny, OUT)
        return mask

    def image(self):
        im = Image.new("RGBA", (self.w, self.h))
        im.putdata([self.px[y][x] for y in range(self.h) for x in range(self.w)])
        return im


def swap_ramp(image, ramp):
    """Troca a rampa vermelha da operária pela do papel (sombra, base, luz)."""
    src = {rgb("5C1408"): ramp[0], rgb("9E311E"): ramp[1], rgb("D3592A"): ramp[2]}
    out = image.copy()
    for y in range(out.height):
        for x in range(out.width):
            c = out.getpixel((x, y))
            if c in src:
                out.putpixel((x, y), src[c])
    return out


# ---------------------------------------------------------------------------------------------------------------------
# Vista de lado (24x16, virada para a direita, y para baixo; a cabeça fica em x 17-21, o gáster em x 1-9).
# ---------------------------------------------------------------------------------------------------------------------

def antenna_side(cv, x, y):
    """Antena curta saindo da testa (x, y) para cima e para a frente."""
    cv.line(x, y, x + 1, y - 1, OUT)
    cv.line(x + 1, y - 1, x + 3, y - 1, OUT)


def side_soldier(cv, ramp):
    cv.clear(17, 2, 23, 11)
    cv.blob(18.5, 8.0, 3.5, 3.5, ramp)                      # cabeção
    cv.pat(22, 7, ["o.", "lo", "lo", "o.", ], {"o": OUT, "l": ramp[2]})  # mandíbula em pinça
    cv.put(19, 7, OUT)
    cv.put(20, 7, OUT)
    cv.put(19, 6, ramp[0])
    cv.put(20, 6, ramp[0])
    cv.line(19, 4, 20, 2, OUT)
    cv.line(20, 2, 22, 2, OUT)


def side_spitter(cv, ramp):
    cv.blob(5.5, 8.5, 4.5, 4.0, ramp)                       # gáster inchado de ácido
    cv.put(3, 6, (255, 255, 255, 255))
    cv.put(4, 6, ramp[2])
    cv.put(3, 7, ramp[2])
    cv.put(23, 10, ramp[2])                                 # gota na boca
    cv.put(23, 11, ramp[1])


def side_carrier(cv, ramp):
    cv.blob(13.5, 6.5, 3.0, 2.6, ramp, ring_overwrite=False)  # ombros de carregadora


def side_cutter(cv, ramp):
    cv.pat(19, 8, [
        "..ooo",
        ".ollo",
        "ollbo",
        ".olbo",
        "..olo",
        "...oo",
    ], {"o": OUT, "l": ramp[2], "b": ramp[1]})              # mandíbula-lâmina
    cv.put(23, 11, (0, 0, 0, 0))                            # dentes da serra
    cv.put(23, 9, (0, 0, 0, 0))


def side_digger(cv, ramp):
    cv.clear(17, 2, 23, 11)
    cv.blob(18.5, 8.0, 2.6, 2.8, ramp)
    cv.put(19, 7, OUT)
    cv.line(19, 4, 21, 2, OUT)
    cv.pat(21, 5, [
        "oo.",
        "loo",
        "lbo",
        "lbo",
        "lbo",
        "lbo",
        "loo",
        "oo.",
    ], {"o": OUT, "l": ramp[2], "b": ramp[1]})              # pá na frente da cabeça


def side_nurse(cv, ramp):
    cv.clear(18, 2, 23, 6)
    cv.pat(17, 4, [
        ".oooo.",
        "owwrwo",
        "owwwwo",
    ], {"o": OUT, "w": CREAM[1], "r": RED})                 # touca com cruz
    cv.pat(5, 8, ["ww", "ww", "ww"], {"w": CREAM[1]})       # avental no gáster
    cv.pat(4, 8, ["o"], {"o": OUT})
    cv.put(7, 8, OUT)


def side_gardener(cv, ramp):
    cv.line(14, 8, 14, 5, LEAF[0])
    cv.blob(12.0, 4.5, 2.2, 1.4, LEAF, ring_overwrite=False)
    cv.blob(16.5, 3.5, 2.2, 1.4, LEAF, ring_overwrite=False)


def side_midden(cv, ramp):
    cv.blob(7.0, 5.5, 2.8, 2.3, SACK, ring_overwrite=False)  # saco de lixo nas costas
    cv.pat(5, 1, ["o.o", "ooo"], {"o": OUT})
    cv.put(6, 2, SACK[1])
    cv.put(7, 6, rgb("6E8B3A"))


def side_farmer(cv, ramp):
    cv.clear(17, 2, 23, 6)
    cv.pat(15, 3, [
        "..oooo..",
        ".osssso.",
        ".obbbbo.",
        "ossssSso",
        "oooooooo",
    ], {"o": OUT, "s": STRAW[1], "S": STRAW[2], "b": HAT_BAND})  # chapéu de palha
    cv.line(22, 8, 23, 7, OUT)


# ---------------------------------------------------------------------------------------------------------------------
# Vista de cima (24x16, simétrica em relação a y = 7.5; a cabeça fica em x 15-19, o gáster em x 1-8).
# ---------------------------------------------------------------------------------------------------------------------

def top_soldier(cv, ramp):
    cv.blob(18.5, 7.5, 3.5, 4.0, ramp)
    cv.put(20, 5, OUT)
    cv.put(20, 10, OUT)
    cv.pat(22, 4, ["o.", "lo"], {"o": OUT, "l": ramp[2]})
    cv.pat(22, 10, ["lo", "o."], {"o": OUT, "l": ramp[2]})


def top_spitter(cv, ramp):
    cv.blob(4.5, 7.5, 4.5, 4.0, ramp)
    cv.put(2, 5, (255, 255, 255, 255))
    cv.put(3, 5, ramp[2])
    cv.put(2, 6, ramp[2])


def top_carrier(cv, ramp):
    cv.blob(12.3, 7.5, 2.7, 2.6, ramp)                      # mochila nas costas, com alça
    cv.line(12, 5, 12, 10, ramp[0])
    cv.put(11, 6, ramp[2])


def top_cutter(cv, ramp):
    cv.pat(20, 2, ["..oo", ".oll", "olbo", "olo.", "oo.."], {"o": OUT, "l": ramp[2], "b": ramp[1]})
    cv.pat(20, 9, ["oo..", "olo.", "olbo", ".oll", "..oo"], {"o": OUT, "l": ramp[2], "b": ramp[1]})


def top_digger(cv, ramp):
    cv.blob(17.5, 7.5, 3.0, 3.6, ramp)
    cv.pat(20, 2, [
        "oo",
        "lo",
        "lo",
        "lo",
        "lo",
        "lo",
        "lo",
        "lo",
        "lo",
        "lo",
        "lo",
        "oo",
    ], {"o": OUT, "l": ramp[2]})


def top_nurse(cv, ramp):
    cv.blob(16.5, 7.5, 2.0, 3.0, CREAM)
    cv.put(16, 7, RED)
    cv.put(16, 8, RED)
    cv.put(17, 7, RED)
    cv.put(17, 8, RED)
    cv.pat(3, 5, ["ww", "ww", "ww", "ww", "ww", "ww"], {"w": CREAM[1]})


def top_gardener(cv, ramp):
    cv.line(12, 7, 12, 8, LEAF[0])
    cv.blob(11.5, 4.0, 2.6, 1.8, LEAF, ring_overwrite=False)
    cv.blob(11.5, 11.0, 2.6, 1.8, LEAF, ring_overwrite=False)


def top_midden(cv, ramp):
    cv.blob(6.0, 7.5, 2.7, 2.6, SACK)
    cv.pat(5, 7, ["oo", "oo"], {"o": OUT})
    cv.put(7, 6, rgb("6E8B3A"))


def top_farmer(cv, ramp):
    cv.blob(16.3, 7.5, 3.6, 4.4, STRAW)                       # aba, deixando o rosto aparecer na frente
    cv.blob(16.3, 7.5, 2.3, 2.8, (HAT_BAND, HAT_BAND, HAT_BAND), ring=False)  # fita
    cv.blob(16.3, 7.5, 1.6, 2.0, STRAW, ring=False)           # copa
    cv.put(19, 5, OUT)
    cv.put(19, 10, OUT)


SIDE = {
    "Soldado": side_soldier, "Cuspideira": side_spitter, "Coletora": side_carrier, "Cortadeira": side_cutter,
    "Escavadora": side_digger, "Cuidadora": side_nurse, "Jardineira": side_gardener, "Lixeira": side_midden,
    "Fazendeira": side_farmer,
}
TOP = {
    "Soldado": top_soldier, "Cuspideira": top_spitter, "Coletora": top_carrier, "Cortadeira": top_cutter,
    "Escavadora": top_digger, "Cuidadora": top_nurse, "Jardineira": top_gardener, "Lixeira": top_midden,
    "Fazendeira": top_farmer,
}


# ---------------------------------------------------------------------------------------------------------------------
# Rainha (48x32, vista de lado e de cima): gáster enorme e coroa de ouro.
# ---------------------------------------------------------------------------------------------------------------------

QUEEN = RAMPS["Operaria"]


def gaster_bands(cv, cx, cy, rx, ry, mask, xs, dark):
    """Anéis curvos escuros que separam os segmentos do gáster."""
    for k in xs:
        for y in range(cy - ry + 2, cy + ry - 1):
            off = round(2.2 * ((y - cy) / ry) ** 2)
            x = k + off
            if (x, y) in mask and (x - 1, y) in mask and (x + 1, y) in mask:
                cv.put(x, y, dark)


def queen_side(cv):
    cv.clear(0, 4, 24, 30)                                    # gáster antigo
    cv.blob(24.0, 16.5, 3.0, 1.8, QUEEN)                      # pecíolo
    mask = cv.blob(11.5, 17.5, 11.5, 8.5, QUEEN)
    gaster_bands(cv, 11, 17, 11, 8, mask, (7, 13, 18), QUEEN[0])
    cv.pat(5, 12, ["lll", "ll."], {"l": QUEEN[2]})
    cv.clear(36, 3, 47, 11)                                   # antenas antigas
    cv.pat(33, 4, [
        "..o.....o.....o...",
        ".ogo...ogo...ogo..",
        ".ogyo.ogyyo.oygo..",
        ".oggyoggyyoyyggo..",
        ".oggyyggyyyyyggo..",
        ".oyRyyyyyyyyRyyo..",
        ".oyyyyyyGyyyyyyo..",
        ".oyyyyyyyyyyyyyo..",
        ".odddddddddddddo..",
        "..ooooooooooooo...",
    ], {"o": OUT, "g": GOLD[2], "y": GOLD[1], "d": GOLD[0], "R": GEM, "G": GEM_LIGHT})
    cv.line(44, 12, 46, 9, OUT)
    cv.line(46, 9, 47, 7, OUT)


def queen_top(cv):
    cv.clear(0, 3, 24, 28)
    mask = cv.blob(11.5, 15.5, 11.0, 11.0, QUEEN)
    gaster_bands(cv, 11, 15, 11, 11, mask, (7, 13, 18), QUEEN[0])
    cv.pat(5, 8, ["lll", "ll."], {"l": QUEEN[2]})
    # Coroa vista de cima: anel de ouro em volta da cabeça, com pontas e joias.
    cx, cy, rx, ry = 37.5, 15.5, 4.6, 6.6
    ring = set()
    for y in range(cv.h):
        for x in range(cv.w):
            u = ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2
            if 0.55 <= u <= 1.0 and x <= 40:
                ring.add((x, y))
    for x, y in ring:
        cv.put(x, y, GOLD[2] if y < 14 else GOLD[1] if y < 18 else GOLD[0])
    for x, y in ring:
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if (nx, ny) not in ring and nx <= 40 and cv.get(nx, ny) not in (GOLD[0], GOLD[1], GOLD[2]):
                if not (nx > 36 and 12 <= ny <= 19):
                    cv.put(nx, ny, OUT)
    for x, y in ((33, 14), (33, 17), (35, 9), (35, 22), (38, 8), (38, 23)):   # pontas
        cv.put(x, y, GOLD[2])
        cv.put(x - 1 if x < 37 else x, y if x < 37 else y - 1 if y < 16 else y + 1, GOLD[1])
    for x, y in ((32, 15), (32, 16), (35, 10), (35, 21)):                       # joias
        cv.put(x, y, GEM)


# ---------------------------------------------------------------------------------------------------------------------

def ensure_base():
    """Copia os PNGs originais da Operaria e da Rainha na primeira execução, para o script poder rodar de novo."""
    for folder in ("Operaria", "Rainha"):
        target = BASE / folder
        if target.exists():
            continue
        target.mkdir(parents=True)
        for v in ("Walk", "Top"):
            for i in range(4):
                shutil.copy2(ANTS / folder / f"Ant_{v}_{i}.png", target / f"Ant_{v}_{i}.png")


def build():
    """Devolve {(pasta, vista, quadro): imagem} com todas as variantes."""
    result = {}
    for v, table in (("Walk", SIDE), ("Top", TOP)):
        for i in range(4):
            base = Image.open(BASE / "Operaria" / f"Ant_{v}_{i}.png").convert("RGBA")
            for folder, ramp in RAMPS.items():
                cv = Cv(swap_ramp(base, ramp) if folder != "Operaria" else base)
                if folder in table:
                    table[folder](cv, ramp)
                result[(folder, v, i)] = cv.image()
            queen = Cv(Image.open(BASE / "Rainha" / f"Ant_{v}_{i}.png").convert("RGBA"))
            (queen_side if v == "Walk" else queen_top)(queen)
            result[("Rainha", v, i)] = queen.image()
    return result


def sheet(images, path, scale=10):
    folders = list(RAMPS) + ["Rainha"]
    cell_w, cell_h = 24 * scale + 10, 16 * scale + 10
    rows = len(folders)
    out = Image.new("RGBA", (cell_w * 4 + 100, cell_h * rows), (58, 68, 58, 255))
    d = ImageDraw.Draw(out)
    for r, folder in enumerate(folders):
        d.text((4, r * cell_h + 4), folder[:9], fill=(255, 255, 255, 255))
        for c, (v, i) in enumerate((("Walk", 0), ("Walk", 2), ("Top", 0), ("Top", 2))):
            im = images[(folder, v, i)]
            if folder == "Rainha":
                im = im.resize((24, 16), Image.NEAREST)
            im = im.resize((24 * scale, 16 * scale), Image.NEAREST)
            out.alpha_composite(im, (100 + c * cell_w, r * cell_h))
    out.save(path)


def big_sheet(images, path, scale=8):
    out = Image.new("RGBA", (48 * scale * 2 + 30, 32 * scale + 10), (58, 68, 58, 255))
    for c, v in enumerate(("Walk", "Top")):
        im = images[("Rainha", v, 0)].resize((48 * scale, 32 * scale), Image.NEAREST)
        out.alpha_composite(im, (10 + c * (48 * scale + 10), 5))
    out.save(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sheet", help="salva a prancha de conferência neste caminho")
    parser.add_argument("--dry", action="store_true", help="não grava os PNGs do projeto")
    args = parser.parse_args()

    ensure_base()
    images = build()
    if not args.dry:
        for (folder, v, i), im in images.items():
            if folder == "Operaria":
                continue
            im.save(ANTS / folder / f"Ant_{v}_{i}.png")
        print(f"{len(images) - 8} PNGs gravados")
    if args.sheet:
        sheet(images, args.sheet)
        big_sheet(images, str(Path(args.sheet).with_name(Path(args.sheet).stem + "_rainha.png")))
        print("prancha:", args.sheet)


if __name__ == "__main__":
    sys.exit(main())
