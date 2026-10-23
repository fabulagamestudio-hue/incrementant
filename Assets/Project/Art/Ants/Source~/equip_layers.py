"""Camadas de equipamento das formigas (REQ-071): ferramenta, arma e armadura em ferro, cobre e ouro.

Cada camada tem o canvas do sprite adulto (24x16) e é desenhada por cima do papel. O corpo não muda entre os quatro
quadros de caminhada (só as patas), então uma camada por vista serve para todos os quadros.
Uso: python equip_layers.py [--sheet caminho.png]   (grava em Art/Ants/Equipamento/<Metal>/<Tipo>/)
"""
import os, sys
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ANTS = os.path.dirname(HERE) + "/"
OUT = ANTS + "Equipamento/"
OUTLINE = (0x1b, 0x18, 0x15)
METALS = {  # escuro, médio, claro
    "Ferro": ((0x3d, 0x4a, 0x5c), (0x7f, 0x93, 0xa8), (0xc5, 0xd3, 0xe0)),
    "Cobre": ((0x7a, 0x34, 0x12), (0xc8, 0x74, 0x3a), (0xf0, 0xb2, 0x7a)),
    "Ouro": ((0x8a, 0x64, 0x10), (0xe8, 0xb8, 0x20), (0xff, 0xf0, 0x8a)),
}
E = "." * 24

def rows(d):
    """d: {y: texto} -> 16 linhas de 24 colunas."""
    out = []
    for y in range(16):
        r = d.get(y, "")
        assert len(r) <= 24, (y, r)
        out.append(r.ljust(24, "."))
    return out

SIDE = {
    "Ferramenta": rows({7: "......................oo", 8: "....................olmo", 9: "...................olmmo",
                        10: "....................ooo.", 11: "....................olmo", 12: ".....................oo."}),
    "Arma": rows({2: "..............oo", 3: ".............olmo", 4: "............olmo", 5: "...........olmo", 6: "..........olmo",
                  7: ".........oomo", 8: "........odmo", 9: "........oodo"}),
    "Armadura": rows({7: "..................ooo", 8: "..........ooo....ollmo", 9: ".........ollmo...odmmmo",
                      10: ".........odddo", 11: "..........ooo"}),
}
TOP = {
    "Ferramenta": rows({6: "......................oo", 7: "....................olmo", 8: "...................olmmo",
                        9: "....................olmo", 10: "......................oo"}),
    "Arma": rows({1: "..............oo", 2: ".............olmo", 3: "............olmo", 4: "...........olmo", 5: "..........olmo",
                  6: ".........oomo", 7: "........odmo", 8: "........oodo"}),
    "Armadura": rows({6: "...........ooooooooooo", 7: "...........olllolllmo", 8: "...........ollmollmmo",
                      9: "...........oddmoddmmo", 10: "...........ooooo.oooo"}),
}

def put(d, y, x, text):
    """Escreve text na linha y a partir da coluna x (para montar camadas por papel)."""
    r = d.get(y, "." * 24).ljust(24, ".")
    d[y] = r[:x] + text + r[x + len(text):]

def soldier_armor():
    """Armadura do soldado (cabeção): placa no tórax como a padrão, mais um capacete que cobre só o alto da cabeça
    em vez da placa de rosto, que atravessava o cabeção. Camadas em Equipamento/<Metal>/Armadura/Soldado/."""
    side = {}
    put(side, 8, 10, "ooo"); put(side, 9, 9, "ollmo"); put(side, 10, 9, "odddo"); put(side, 11, 10, "ooo")
    put(side, 4, 16, "ooooo"); put(side, 5, 15, "ollllmo"); put(side, 6, 15, "oddddmo"); put(side, 7, 15, "od")
    top = {}
    put(top, 6, 11, "ooooo"); put(top, 7, 11, "olllo"); put(top, 8, 11, "ollmo"); put(top, 9, 11, "oddmo"); put(top, 10, 11, "ooooo")
    for y, t in ((4, ".ooo"), (5, "ollmo"), (6, "ollmo"), (7, "olmmo"), (8, "olmmo"), (9, "odmmo"), (10, "oddmo"), (11, ".ooo")):
        put(top, y, 15, t)
    return rows(side), rows(top)

ROLE_ARMOR = {"Soldado": soldier_armor}

def layer(grid, ramp):
    pal = {"o": OUTLINE, "d": ramp[0], "m": ramp[1], "l": ramp[2]}
    im = Image.new("RGBA", (24, 16), (0, 0, 0, 0))
    for y, r in enumerate(grid):
        for x, c in enumerate(r):
            if c in pal:
                im.putpixel((x, y), pal[c] + (255,))
    return im

def main():
    layers = {}
    for metal, ramp in METALS.items():
        for kind in SIDE:
            for view, grid in (("Walk", SIDE[kind]), ("Top", TOP[kind])):
                im = layer(grid, ramp)
                layers[(metal, kind, view)] = im
                d = OUT + metal + "/" + kind + "/"
                os.makedirs(d, exist_ok=True)
                im.save(d + "Ant_%s_0.png" % view)
    for metal, ramp in METALS.items():
        for role, build in ROLE_ARMOR.items():
            side, top = build()
            d = OUT + metal + "/Armadura/" + role + "/"
            os.makedirs(d, exist_ok=True)
            layer(side, ramp).save(d + "Ant_Walk_0.png")
            layer(top, ramp).save(d + "Ant_Top_0.png")
    if "--sheet" in sys.argv:
        sheet(layers, sys.argv[sys.argv.index("--sheet") + 1])

def comp(base, *ovs):
    b = Image.open(ANTS + base).convert("RGBA")
    for o in ovs:
        b.alpha_composite(o)
    return b

def sheet(layers, path):
    S = 8
    W, H = 24 * S + 10, 16 * S + 8
    cols = [("ferramenta", "Escavadora", ["Ferramenta"]), ("arma", "Soldado", ["Arma"]),
            ("armadura", "Soldado", ["Armadura"]), ("arma+armadura", "Soldado", ["Arma", "Armadura"])]
    rows_ = [(m, v) for m in METALS for v in ("Walk", "Top")]
    img = Image.new("RGBA", (W * len(cols), H * len(rows_)), (70, 58, 44, 255))
    d = ImageDraw.Draw(img)
    for r, (m, v) in enumerate(rows_):
        for c, (name, papel, kinds) in enumerate(cols):
            base = comp("%s/Ant_%s_0.png" % (papel, v), *[layers[(m, k, v)] for k in kinds])
            img.alpha_composite(base.resize((24 * S, 16 * S), Image.NEAREST), (c * W + 5, r * H + 4))
            if r == 0:
                d.text((c * W + 5, 2), name, fill=(255, 255, 255, 255))
        d.text((6, r * H + H - 12), "%s, %s" % (m, "lateral" if v == "Walk" else "superior"), fill=(255, 230, 160, 255))
    img.save(path)

main()
